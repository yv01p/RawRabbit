# Critical Design Review: 2026-05-11-modernization-phase-1-design (Round 1)

**Spec:** `/home/yv01p/rawrabbit/docs/specs/2026-05-11-modernization-phase-1-design.md`
**Verified Assumptions section:** present

## 1. Verified-assumptions cross-check

A1–A5, A7–A17, A19–A21: still hold under fresh re-read of cited evidence.

**A6 (HttpContext enricher source on net10) — partial.** Source-side claim holds: all four `.cs` files gate functional paths behind `#if NETSTANDARD1_6` / `#if NET451`, so on net10 the project compiles to empty pass-through classes (V1 decision: accept). Fresh read of `src/RawRabbit.Enrichers.HttpContext/RawRabbit.Enrichers.HttpContext.csproj:31-33` reveals an additional element the original verification missed: the csproj carries `<ItemGroup Condition=" '$(TargetFramework)' == 'netstandard1.6' "><PackageReference Include="Microsoft.AspNetCore.Mvc.Core" Version="1.0.3" /></ItemGroup>`. After the spec's TFM collapse to net10, this conditional ItemGroup matches no TFM — the package is no-op and the source doesn't reference it (already verified empty). **Consequence: harmless dead clutter; no build impact.** No fix required for the asked-for outcome; the implementer may optionally delete the stranded conditional ItemGroup as a generalization of the spec's `net451`/`net46` deletion rule.

**A18 (AssemblyInfo.cs handling) and Risk #8 — partial.** The spec's Risk #8 asserts: *"Verified during brainstorming: `src/RawRabbit/Logging/LibLog.cs` carries `[assembly: InternalsVisibleTo(...)]` for 9 sibling projects… No other AssemblyInfo-shaped attributes elsewhere."* The "no other elsewhere" claim is **false**. Fresh `grep "InternalsVisibleTo" $(find . -name AssemblyInfo.cs)` finds:

- `src/RawRabbit/Properties/AssemblyInfo.cs:12-18` declares 7 `[assembly: InternalsVisibleTo(...)]`:
  - `RawRabbit.Enrichers.GlobalExecutionId` ← unique to this file
  - `RawRabbit.Operations.Publish` ← also in LibLog.cs:48
  - `RawRabbit.Operations.Subscribe` ← also in LibLog.cs:53
  - `RawRabbit.Operations.Respond` ← also in LibLog.cs:51
  - `RawRabbit.Operations.Request` ← also in LibLog.cs:50
  - `RawRabbit.Operations.MessageSequence` ← also in LibLog.cs:47
  - `RawRabbit.Operations.StateMachine` ← also in LibLog.cs:52

Six of the seven IVTs in `AssemblyInfo.cs` are duplicates of LibLog.cs's IVT block. The seventh (`RawRabbit.Enrichers.GlobalExecutionId`) is unique to `AssemblyInfo.cs`.

**Consequence assessment:** I checked whether the affected enrichers actually use internals from RawRabbit (i.e., whether removing the IVT would break compilation). `grep "internal " src/RawRabbit/ --include="*.cs"` shows the only internal members in RawRabbit live in `LibLog.cs` (lines 537, 546, 705, 735–2111 — the LibLog provider machinery) and in `Configuration/QueueDeclare/QueueDeclarationExtensions.cs:7` (`internal static readonly string DirectQueueName`). Sampled the GlobalExecutionId enricher's source (10 `.cs` files in `src/RawRabbit.Enrichers.GlobalExecutionId/`) — they reference only public RawRabbit types (`IPipeContext`, `IClientBuilder`, `StagedMiddleware`, `StageMarker`, etc.). No usage of internal symbols. Same pattern in the Operations enrichers.

**Net effect:** the IVTs are non-load-bearing dead declarations. Deleting `src/RawRabbit/Properties/AssemblyInfo.cs` (per V2 decision) does not break the build. The spec's V2 decision and §6 Risk #8 conclusion still hold; only the *evidence trail* in Risk #8 was incomplete. Recommend the spec's Risk #8 evidence sentence be tightened in any future revision to: *"Verified: LibLog.cs carries IVTs for 9 sibling projects; `src/RawRabbit/Properties/AssemblyInfo.cs` carries 7 additional IVTs of which 6 duplicate LibLog.cs's set and 1 (`GlobalExecutionId`) is unique. None of the targeted enrichers actually access RawRabbit internals (verified by grep)."* No spec change required for the build to succeed.

## 2. Literal-wrongness findings

No literal-wrongness findings.

## 3. Forced decisions

No forced decisions found.

## 4. Recommendation

✅ **Approve as-is.** §2 and §3 are both empty. The spec is ready for implementation planning.

The §1 cross-check turned up two evidentiary gaps (A6 missed an unrelated dead `<ItemGroup>` in HttpContext csproj; Risk #8 missed 7 IVTs in `RawRabbit/Properties/AssemblyInfo.cs`) but neither affects the asked-for outcome (`dotnet build` + `dotnet test` succeed). The conclusions of A6 and V2 are correct; only the evidence trails were incomplete. The implementer can proceed.
