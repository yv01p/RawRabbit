# Modernization Phase 1 Implementation Plan

> **For agentic workers:** REQUIRED: Use `superpowers:subagent-driven-development` to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Source spec:** `docs/specs/2026-05-11-modernization-phase-1-design.md` (commit SHA: `c88ac7a81356e32735cb0ef018be762b4d8c1240`)

**Goal:** After this plan executes, on a clean clone with the .NET 10 SDK installed, `dotnet restore && dotnet build -c Release && dotnet test test/RawRabbit.Tests && dotnet test test/RawRabbit.Enrichers.Polly.Tests` succeeds end-to-end with NU1701 warnings only (no errors).

**Architecture:** Bulk modernization of csproj/sln/config metadata. No library `.cs` source edits except: (a) deleting 25 hand-written `Properties/AssemblyInfo.cs` files (per spec V2), and (b) a 1-line edit in `sample/RawRabbit.ConsoleApp.Sample/Program.cs:25` to swap `.WriteTo.LiterateConsole()` → `.WriteTo.Console()` (the deprecated `Serilog.Sinks.Literate` is replaced by `Serilog.Sinks.Console`). All 25 csprojs in `src/`, the 2 SDK-style test csprojs, and the 2 retained sample csprojs collapse to a single `<TargetFramework>net10.0</TargetFramework>`. The non-SDK legacy `RawRabbit.Enrichers.Polly.Tests.csproj` is rewritten as SDK-style. `RawRabbit.PerformanceTest` and `RawRabbit.AspNet.Sample` are dropped from `RawRabbit.sln` (files preserved on disk).

**Tech stack (inherited from spec):** .NET 10 SDK; csproj `Microsoft.NET.Sdk` style; xunit 2.9.3 + Moq 4.20.72 + Microsoft.NET.Test.Sdk 18.5.1 + xunit.runner.visualstudio 2.8.2 (test stack); RabbitMQ.Client 5.0.1, Newtonsoft.Json 10.0.1 stay (Phase 4/5 territory); LibLog stays (Phase 3 territory).

---

## File Structure

**Create:**
- `global.json` — pin .NET 10 SDK
- `test/RawRabbit.Enrichers.Polly.Tests/RawRabbit.Enrichers.Polly.Tests.csproj` (SDK-style; overwrites the existing non-SDK file at the same path)

**Replace:**
- `NuGet.Config` — single nuget.org v3 source

**Modify (csproj edits, all collapse to net10.0):**
- 25 csprojs in `src/*` — TFM collapse, conditional ItemGroup deletes, conditional PropertyGroup deletes, DefineConstants prefix fix
- 2 csprojs in `test/` (`RawRabbit.Tests`, `RawRabbit.IntegrationTests`) — same TFM modernization + bump test stack
- 2 csprojs in `sample/` (`ConsoleApp.Sample`, `Messages.Sample`) — TFM modernization; `ConsoleApp.Sample` also gets Serilog swap + `Microsoft.Extensions.Configuration.*` bump
- `RawRabbit.sln` — drop `RawRabbit.PerformanceTest` and `RawRabbit.AspNet.Sample` project entries (Project blocks, ProjectConfigurationPlatforms entries, NestedProjects entries)
- `sample/RawRabbit.ConsoleApp.Sample/Program.cs:25` — 1-line: `.WriteTo.LiterateConsole()` → `.WriteTo.Console()`

**Delete:**
- 25 `Properties/AssemblyInfo.cs` files (full list inherited from spec §3 deletions list)
- `test/RawRabbit.Enrichers.Polly.Tests/packages.config`

---

## Inherited from spec

The following assumptions were verified by `thorough-brainstorming` at spec-write time and are NOT re-verified here. Trusted as ground truth:

- **A1:** .NET 10 SDK is GA-released (verified via Microsoft.NET.Test.Sdk 18.5.1 listing net10 as a target on nuget.org)
- **A2:** RabbitMQ.Client 5.0.1 nupkg consumable from net10 (targets netstandard1.5 + net451; resolves via fallback with NU1701 expected)
- **A3:** RabbitMQ.Client 5.0.1 runtime APIs work on net10 (dependency tree is BCL-system-package; no removed-API references)
- **A4:** Newtonsoft.Json 10.0.1 + APIs called from `src/RawRabbit/Serialization/JsonSerializer.cs` etc. work on net10
- **A5:** LibLog source compiles on net10 with LIBLOG_PORTABLE
- **A6:** HttpContext enricher source on net10: all functional paths gated by `#if NETSTANDARD1_6` / `#if NET451` → compiles to empty pass-through (V1 decision: accept)
- **A7:** RawRabbit.PerformanceTest is in `RawRabbit.sln`
- **A8:** All 3 sample projects are in `RawRabbit.sln`
- **A9:** AspNet.Sample is the only sample with substantive AspNetCore deps
- **A10:** Sln contains 32 csproj entries
- **A11:** Test stack target versions exist (Microsoft.NET.Test.Sdk 18.5.1, xunit 2.9.3, Moq 4.20.72) — note: the spec also listed `xunit.runner.visualstudio 3.1.5`, but this plan pins `2.8.2` instead per F2 forced decision (see "Verified plan-level assumptions" P14 below)
- **A12:** Moq 4.20+ source-compatible with existing tests
- **A13:** Old enricher dependency packages (Polly 5.3.1, ZeroFormatter 1.6.4, Ninject 3.3.4, MessagePack 1.7.3.4, protobuf-net 2.3.2, Stateless 3.0.0, Autofac 4.1.0) all resolve on net10 via netstandard fallback (NU1701 expected)
- **A14:** Microsoft.Extensions.DependencyInjection 1.0.2 resolves on net10
- **A15:** Legacy Polly.Tests source compatible with current Moq/Polly
- **A16:** xunit.runner.json shape compatible with xunit 2.9
- **A17:** No Release-config DefineConstants blocks we'd lose
- **A18:** 25 AssemblyInfo.cs files exist; all auto-generatable content (note: CDR + plan verification refined this — see P11 below)
- **A19:** Serilog.Sinks.Literate is deprecated (last update Jul 2017)
- **A20:** Messages.Sample is trivial
- **A21:** No Directory.Build.props or Directory.Packages.props exists

---

## Verified plan-level assumptions

Newly introduced by this plan and verified at plan-write time:

| # | Category | Assumption | Evidence |
|---|---|---|---|
| P1 | File path | `src/` contains exactly 25 directories, each with one csproj | `ls src/ \| wc -l` returned 25; enumerated in §File Structure |
| P2 | File content | `RawRabbit.sln` Project entries for `RawRabbit.PerformanceTest` (line 67) and `RawRabbit.AspNet.Sample` (line 25); both have matching `ProjectConfigurationPlatforms` entries (8 lines each) and `NestedProjects` entries (1 line each) | `cat RawRabbit.sln` shows: `Project("{9A19103F...}") = "RawRabbit.AspNet.Sample", "sample\…", "{313A89F7-B0DD-4559-BE59-C1684C41689F}"` and `Project("{9A19103F...}") = "RawRabbit.PerformanceTest", "test\…", "{2788DED9-2558-496C-81B1-B471CD42AB64}"`. Each GUID appears in `GlobalSection(ProjectConfigurationPlatforms)` (4 cfg pairs × 2 lines = 8 lines) and `GlobalSection(NestedProjects)` (1 line) |
| P3 | Code-in-plan | `.NET 10 SDK 10.0.203` is the latest stable as of 2026-04-21 | `https://dotnet.microsoft.com/en-us/download/dotnet/10.0` confirms 10.0.203 release date 2026-04-21 |
| P4 | Code-in-plan | `Serilog.Sinks.Console 6.1.1` is current and resolves on net10 | nuget.org: 6.1.1 (2025-11-02), targets net6.0+ (net10 computed) |
| P5 | Code-in-plan | `Microsoft.Extensions.Configuration.Json 10.0.7` + `Microsoft.Extensions.Configuration.Binder 10.0.7` are current and target net8.0+/netstandard2.0 | nuget.org: M.E.Configuration.Json 10.0.7 released 2026-04-21; same release line for Binder |
| P6 | Consumer impact | `sample/RawRabbit.ConsoleApp.Sample/Program.cs:25` is the ONLY callsite of `.WriteTo.LiterateConsole()`; no other Serilog sink-specific calls | `find sample/RawRabbit.ConsoleApp.Sample -name "*.cs"` returned only `Program.cs` and `Properties/AssemblyInfo.cs`; Program.cs:25 contains exactly `.WriteTo.LiterateConsole()` |
| P7 | Consumer impact | `Program.cs` uses standard `Microsoft.Extensions.Configuration` APIs (`new ConfigurationBuilder().SetBasePath().AddJsonFile().Build().Get<T>()`) — stable across all M.E.Configuration versions | Program.cs:33-37 confirms |
| P8 | Consumer impact | No remaining csproj has `<ProjectReference>` into `RawRabbit.PerformanceTest` or `RawRabbit.AspNet.Sample` | `grep "ProjectReference" $(find . -name "*.csproj") \| grep -E "PerformanceTest\|AspNet.Sample"` returned only references FROM those projects, never INTO them |
| P9 | Command | No `.husky/`, `.pre-commit-config.*`, `Makefile`, `build.sh` at repo root; `.git/hooks/` contains only `.sample` files | `ls .husky .pre-commit-config.* Makefile build.sh` all return No such file; `ls .git/hooks/` lists only `*.sample` |
| P10 | Command | Direct `dotnet` CLI commands work without wrapper interception | Same evidence as P9; only `.build/*.ps1` exists, which is AppVeyor-specific and not invoked by `dotnet build`/`dotnet test` |
| P11 | Consumer impact | All 25 `AssemblyInfo.cs` files contain only attributes that the SDK regenerates from csproj metadata, plus dead `[assembly: ComVisible(false)]`/`[assembly: Guid(...)]` (irrelevant for non-COM net10) and 7 dead IVTs in `src/RawRabbit/Properties/AssemblyInfo.cs` (CDR confirmed: consumers don't access internals) | `cat $(find . -name AssemblyInfo.cs)` shows: 25 files, all standard auto-genable attributes; only IVTs are in RawRabbit's; no `AssemblyKeyFile` / `AssemblyKeyName` / signing attributes anywhere |
| P12 | Consumer impact | Test code uses Moq APIs stable between 4.7 and 4.20: `Mock<T>()`, `.Setup(c => c.X)`, `.Returns(...)`, `.Throws(...)`, `.SetupSequence()`, `It.IsAny<T>()`, `.As<TInterface>()` | `grep -rnE "protected\(\)\|Mock\.Of<\|\.As<\|SetupGet\|SetupSet\|Mock<.*\.Protected\(\)" test/` found only `.As<IRecoverable>()` patterns in `test/RawRabbit.Tests/Channel/*.cs` — `.As<T>()` is stable across Moq 4.x |
| P13 | Consumer impact | xunit attribute usage limited to `[Fact]` (the only stable construct used) | `grep -rnE "\[Theory\]\|\[InlineData\|\[ClassData\|\[MemberData\|\[Fact\(.*Skip\|IClassFixture\|ICollectionFixture" test/` returned 0 results — tests use only basic `[Fact]` |
| P14 | Code-in-plan | `xunit.runner.visualstudio 2.8.2` (released 2024-07-08) is stable, supports xunit v2 test assemblies, supports net6.0+ (covers net10 via fallback) | nuget.org: 2.8.2 explicitly "designed for xUnit v2 test assemblies"; 3.x is for xunit v3 (not used here) |
| P15 | Code-in-plan | SDK-style csproj template for the rewritten Polly.Tests follows the convention of sibling SDK-style test projects (e.g., `test/RawRabbit.Tests/RawRabbit.Tests.csproj`): `<Project Sdk="Microsoft.NET.Sdk">`, `<TargetFramework>`, `<IsPackable>false</IsPackable>`, `<PackageReference>` for test stack + Polly, `<ProjectReference>` to RawRabbit + Enrichers.Polly | Sibling csproj read at plan-write time confirms convention; full template provided in Task 6 below |
| P16 | Code-in-plan | The `<ItemGroup Condition=" '$(TargetFramework)' == 'netstandard1.6' "><PackageReference Include="Microsoft.AspNetCore.Mvc.Core" Version="1.0.3" /></ItemGroup>` in `src/RawRabbit.Enrichers.HttpContext/RawRabbit.Enrichers.HttpContext.csproj:31-33` evaluates false on net10 → MSBuild skips the ItemGroup → package not pulled; harmless dead clutter | MSBuild `Condition` semantics; CDR §1 cross-check confirmed |

**Forced decisions resolved at plan-write time:**

- **F1 (Serilog modernization):** User picked option (a) — 1-line `.cs` edit in `Program.cs:25`. This is an explicit exception to the spec's "no `.cs` source edits except AssemblyInfo deletions" rule, approved by user.
- **F2 (xunit runner version pair):** User picked option (a) — pin `xunit.runner.visualstudio 2.8.2` (matching majors with xunit 2.9.3) instead of spec's literal `3.1.5`. This is an intentional deviation from spec assumption A11.

---

## Tasks

### Task 1: Repo foundation (global.json + NuGet.Config)

**Files:**
- Create: `global.json`
- Replace: `NuGet.Config`

- [ ] **Step 1: Create `global.json`**
  ```json
  {
    "sdk": {
      "version": "10.0.203",
      "rollForward": "latestFeature"
    }
  }
  ```
  Note: `10.0.203` is the latest stable as of 2026-04-21. `rollForward: latestFeature` allows any 10.0.x ≥ 10.0.203 or 10.1.x. If the implementer's machine has an older 10.0.x SDK and wants to pin lower, run `dotnet --list-sdks` and substitute that value.

- [ ] **Step 2: Replace `NuGet.Config`** with single source:
  ```xml
  <?xml version="1.0" encoding="utf-8"?>
  <configuration>
    <packageSources>
      <clear />
      <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
    </packageSources>
  </configuration>
  ```
  This drops the dead `xunit` MyGet feed and the redundant v2 nuget.org endpoint.

- [ ] **Step 3: Commit**
  ```bash
  git add global.json NuGet.Config
  git commit -m "Pin .NET 10 SDK and clean NuGet.Config"
  ```

### Task 2: Solution composition (drop PerformanceTest + AspNet.Sample from sln)

**Files:**
- Modify: `RawRabbit.sln`

- [ ] **Step 1: Remove `RawRabbit.AspNet.Sample` Project block** from `RawRabbit.sln`. Locate the lines:
  ```
  Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "RawRabbit.AspNet.Sample", "sample\RawRabbit.AspNet.Sample\RawRabbit.AspNet.Sample.csproj", "{313A89F7-B0DD-4559-BE59-C1684C41689F}"
  EndProject
  ```
  Delete both lines.

- [ ] **Step 2: Remove `RawRabbit.PerformanceTest` Project block** from `RawRabbit.sln`. Locate the lines:
  ```
  Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "RawRabbit.PerformanceTest", "test\RawRabbit.PerformanceTest\RawRabbit.PerformanceTest.csproj", "{2788DED9-2558-496C-81B1-B471CD42AB64}"
  EndProject
  ```
  Delete both lines.

- [ ] **Step 3: Remove their `ProjectConfigurationPlatforms` entries** in the `GlobalSection(ProjectConfigurationPlatforms) = postSolution` block. Delete these 8 lines (4 for AspNet.Sample, 4 for PerformanceTest):
  ```
  {313A89F7-B0DD-4559-BE59-C1684C41689F}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
  {313A89F7-B0DD-4559-BE59-C1684C41689F}.Debug|Any CPU.Build.0 = Debug|Any CPU
  {313A89F7-B0DD-4559-BE59-C1684C41689F}.Release|Any CPU.ActiveCfg = Release|Any CPU
  {313A89F7-B0DD-4559-BE59-C1684C41689F}.Release|Any CPU.Build.0 = Release|Any CPU
  {2788DED9-2558-496C-81B1-B471CD42AB64}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
  {2788DED9-2558-496C-81B1-B471CD42AB64}.Debug|Any CPU.Build.0 = Debug|Any CPU
  {2788DED9-2558-496C-81B1-B471CD42AB64}.Release|Any CPU.ActiveCfg = Release|Any CPU
  {2788DED9-2558-496C-81B1-B471CD42AB64}.Release|Any CPU.Build.0 = Release|Any CPU
  ```

- [ ] **Step 4: Remove their `NestedProjects` entries** in the `GlobalSection(NestedProjects) = preSolution` block:
  ```
  {313A89F7-B0DD-4559-BE59-C1684C41689F} = {CD0B1BD5-CEE9-4E4E-9266-19A18D5E5AF1}
  {2788DED9-2558-496C-81B1-B471CD42AB64} = {2F91E22A-AEBA-4BEF-9A03-C8232830F697}
  ```

- [ ] **Step 5: Verify.** Run `grep -E "PerformanceTest|AspNet.Sample" RawRabbit.sln` — should return no results. Run `grep -cE "313A89F7|2788DED9" RawRabbit.sln` — should return 0.

- [ ] **Step 6: Commit**
  ```bash
  git add RawRabbit.sln
  git commit -m "Drop PerformanceTest and AspNet.Sample from solution"
  ```

### Task 3: Delete 25 hand-written AssemblyInfo.cs files

**Files:** 25 deletions (full list in spec §3 deletions list).

- [ ] **Step 1: Delete all 25 files**
  ```bash
  git rm \
    src/RawRabbit/Properties/AssemblyInfo.cs \
    src/RawRabbit.Compatibility.Legacy/Properties/AssemblyInfo.cs \
    src/RawRabbit.DependencyInjection.Autofac/Properties/AssemblyInfo.cs \
    src/RawRabbit.DependencyInjection.Ninject/Properties/AssemblyInfo.cs \
    src/RawRabbit.DependencyInjection.ServiceCollection/Properties/AssemblyInfo.cs \
    src/RawRabbit.Enrichers.Attributes/Properties/AssemblyInfo.cs \
    src/RawRabbit.Enrichers.GlobalExecutionId/Properties/AssemblyInfo.cs \
    src/RawRabbit.Enrichers.HttpContext/Properties/AssemblyInfo.cs \
    src/RawRabbit.Enrichers.MessageContext/Properties/AssemblyInfo.cs \
    src/RawRabbit.Enrichers.MessageContext.Respond/Properties/AssemblyInfo.cs \
    src/RawRabbit.Enrichers.MessageContext.Subscribe/Properties/AssemblyInfo.cs \
    src/RawRabbit.Enrichers.Polly/Properties/AssemblyInfo.cs \
    src/RawRabbit.Enrichers.QueueSuffix/Properties/AssemblyInfo.cs \
    src/RawRabbit.Operations.Get/Properties/AssemblyInfo.cs \
    src/RawRabbit.Operations.MessageSequence/Properties/AssemblyInfo.cs \
    src/RawRabbit.Operations.Publish/Properties/AssemblyInfo.cs \
    src/RawRabbit.Operations.Request/Properties/AssemblyInfo.cs \
    src/RawRabbit.Operations.Respond/Properties/AssemblyInfo.cs \
    src/RawRabbit.Operations.StateMachine/Properties/AssemblyInfo.cs \
    src/RawRabbit.Operations.Subscribe/Properties/AssemblyInfo.cs \
    sample/RawRabbit.ConsoleApp.Sample/Properties/AssemblyInfo.cs \
    sample/RawRabbit.Messages.Sample/Properties/AssemblyInfo.cs \
    test/RawRabbit.Enrichers.Polly.Tests/Properties/AssemblyInfo.cs \
    test/RawRabbit.IntegrationTests/Properties/AssemblyInfo.cs \
    test/RawRabbit.Tests/Properties/AssemblyInfo.cs
  ```

- [ ] **Step 2: Verify.** `find . -name "AssemblyInfo.cs" -not -path "./.git/*"` should return no results.

- [ ] **Step 3: Commit**
  ```bash
  git commit -m "Delete legacy AssemblyInfo.cs files"
  ```

### Task 4: Modernize 25 library csprojs in `src/*` to net10.0

**Files:**
- Modify: 25 csprojs in `src/`:
  - `src/RawRabbit/RawRabbit.csproj`
  - `src/RawRabbit.Compatibility.Legacy/RawRabbit.Compatibility.Legacy.csproj`
  - `src/RawRabbit.DependencyInjection.Autofac/RawRabbit.DependencyInjection.Autofac.csproj`
  - `src/RawRabbit.DependencyInjection.Ninject/RawRabbit.DependencyInjection.Ninject.csproj`
  - `src/RawRabbit.DependencyInjection.ServiceCollection/RawRabbit.DependencyInjection.ServiceCollection.csproj`
  - `src/RawRabbit.Enrichers.Attributes/RawRabbit.Enrichers.Attributes.csproj`
  - `src/RawRabbit.Enrichers.GlobalExecutionId/RawRabbit.Enrichers.GlobalExecutionId.csproj`
  - `src/RawRabbit.Enrichers.HttpContext/RawRabbit.Enrichers.HttpContext.csproj`
  - `src/RawRabbit.Enrichers.MessageContext/RawRabbit.Enrichers.MessageContext.csproj`
  - `src/RawRabbit.Enrichers.MessageContext.Respond/RawRabbit.Enrichers.MessageContext.Respond.csproj`
  - `src/RawRabbit.Enrichers.MessageContext.Subscribe/RawRabbit.Enrichers.MessageContext.Subscribe.csproj`
  - `src/RawRabbit.Enrichers.MessagePack/RawRabbit.Enrichers.MessagePack.csproj`
  - `src/RawRabbit.Enrichers.Polly/RawRabbit.Enrichers.Polly.csproj`
  - `src/RawRabbit.Enrichers.Protobuf/RawRabbit.Enrichers.Protobuf.csproj`
  - `src/RawRabbit.Enrichers.QueueSuffix/RawRabbit.Enrichers.QueueSuffix.csproj`
  - `src/RawRabbit.Enrichers.RetryLater/RawRabbit.Enrichers.RetryLater.csproj`
  - `src/RawRabbit.Enrichers.ZeroFormatter/RawRabbit.Enrichers.ZeroFormatter.csproj`
  - `src/RawRabbit.Operations.Get/RawRabbit.Operations.Get.csproj`
  - `src/RawRabbit.Operations.MessageSequence/RawRabbit.Operations.MessageSequence.csproj`
  - `src/RawRabbit.Operations.Publish/RawRabbit.Operations.Publish.csproj`
  - `src/RawRabbit.Operations.Request/RawRabbit.Operations.Request.csproj`
  - `src/RawRabbit.Operations.Respond/RawRabbit.Operations.Respond.csproj`
  - `src/RawRabbit.Operations.StateMachine/RawRabbit.Operations.StateMachine.csproj`
  - `src/RawRabbit.Operations.Subscribe/RawRabbit.Operations.Subscribe.csproj`
  - `src/RawRabbit.Operations.Tools/RawRabbit.Operations.Tools.csproj`

- [ ] **Step 1: For each csproj, apply the following transformations.** All transformations are independent per csproj; order within a csproj doesn't matter.

  **(a) Replace TFM line.** Replace the `<TargetFrameworks>...</TargetFrameworks>` element with `<TargetFramework>net10.0</TargetFramework>` (note: `Frameworks` plural → `Framework` singular). Removes all of these patterns:
  - `<TargetFrameworks>netstandard1.5;net451</TargetFrameworks>` (22 csprojs)
  - `<TargetFrameworks>netstandard1.6;net451</TargetFrameworks>` (HttpContext, MessagePack, ZeroFormatter)
  - `<TargetFrameworks>netstandard2.0;net451</TargetFrameworks>` (Ninject)

  **(b) Delete TFM-conditional ItemGroups.** Delete every `<ItemGroup Condition=" '$(TargetFramework)' == 'XXX' ">…</ItemGroup>` block where `XXX` is `net451`, `net46`, `netstandard1.5`, `netstandard1.6`, or `netstandard2.0`. (P16-confirmed: these conditions are false on net10 anyway, so deletion is hygiene.) Examples:
  ```xml
  <ItemGroup Condition=" '$(TargetFramework)' == 'net451' ">
    <Reference Include="System" />
    <Reference Include="Microsoft.CSharp" />
  </ItemGroup>
  ```
  ```xml
  <ItemGroup Condition=" '$(TargetFramework)' == 'netstandard1.6' ">
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Core" Version="1.0.3" />
  </ItemGroup>
  ```

  **(c) Delete TFM-conditional PropertyGroups.** Delete every `<PropertyGroup Condition="'$(Configuration)|$(TargetFramework)|$(Platform)'=='Debug|XXX|AnyCPU'">…</PropertyGroup>` block where `XXX` is `netstandard1.5`, `netstandard1.6`, or `netstandard2.0`. These contain `<DefineConstants>TRACE;DEBUG;NETSTANDARD1_5;LIBLOG_PORTABLE</DefineConstants>` patterns that no longer apply.

  **(d) Fix unconditional `<DefineConstants>`.** Only in `src/RawRabbit/RawRabbit.csproj` and `src/RawRabbit.Compatibility.Legacy/RawRabbit.Compatibility.Legacy.csproj`: rewrite `<DefineConstants>LIBLOG_PORTABLE</DefineConstants>` to `<DefineConstants>$(DefineConstants);LIBLOG_PORTABLE</DefineConstants>` so the SDK-injected constants for net10 are preserved.

  Leave all `<ProjectReference>`, `<PackageReference>` (top-level), top-level metadata (`<Description>`, `<AssemblyTitle>`, `<VersionPrefix>`, `<Authors>`, `<PackageId>`, `<PackageTags>`, `<PackageIconUrl>`, `<PackageProjectUrl>`, `<GenerateAssembly*Attribute>` flags) UNCHANGED. (Spec §3 explicitly defers metadata work to Phase 2.)

  Example: `src/RawRabbit/RawRabbit.csproj` BEFORE:
  ```xml
  <Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
      <Description>A modern framework for communication over RabbitMq.</Description>
      <AssemblyTitle>RawRabbit</AssemblyTitle>
      <VersionPrefix>2.0.0</VersionPrefix>
      <Authors>pardahlman;enrique-avalon</Authors>
      <TargetFrameworks>netstandard1.5;net451</TargetFrameworks>
      <AssemblyName>RawRabbit</AssemblyName>
      <PackageId>RawRabbit</PackageId>
      <PackageTags>rabbitmq;rawrabbit;amqp</PackageTags>
      <PackageIconUrl>http://pardahlman.se/raw/icon.png</PackageIconUrl>
      <PackageProjectUrl>https://github.com/pardahlman/RawRabbit</PackageProjectUrl>
      <GenerateAssemblyConfigurationAttribute>false</GenerateAssemblyConfigurationAttribute>
      <GenerateAssemblyCompanyAttribute>false</GenerateAssemblyCompanyAttribute>
      <GenerateAssemblyProductAttribute>false</GenerateAssemblyProductAttribute>
      <DefineConstants>LIBLOG_PORTABLE</DefineConstants>
    </PropertyGroup>
    <ItemGroup>
      <PackageReference Include="RabbitMQ.Client" Version="5.0.1" />
      <PackageReference Include="Newtonsoft.Json" Version="10.0.1" />
    </ItemGroup>
    <ItemGroup Condition=" '$(TargetFramework)' == 'net451' ">
      <Reference Include="System" />
      <Reference Include="Microsoft.CSharp" />
    </ItemGroup>
  </Project>
  ```

  AFTER:
  ```xml
  <Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
      <Description>A modern framework for communication over RabbitMq.</Description>
      <AssemblyTitle>RawRabbit</AssemblyTitle>
      <VersionPrefix>2.0.0</VersionPrefix>
      <Authors>pardahlman;enrique-avalon</Authors>
      <TargetFramework>net10.0</TargetFramework>
      <AssemblyName>RawRabbit</AssemblyName>
      <PackageId>RawRabbit</PackageId>
      <PackageTags>rabbitmq;rawrabbit;amqp</PackageTags>
      <PackageIconUrl>http://pardahlman.se/raw/icon.png</PackageIconUrl>
      <PackageProjectUrl>https://github.com/pardahlman/RawRabbit</PackageProjectUrl>
      <GenerateAssemblyConfigurationAttribute>false</GenerateAssemblyConfigurationAttribute>
      <GenerateAssemblyCompanyAttribute>false</GenerateAssemblyCompanyAttribute>
      <GenerateAssemblyProductAttribute>false</GenerateAssemblyProductAttribute>
      <DefineConstants>$(DefineConstants);LIBLOG_PORTABLE</DefineConstants>
    </PropertyGroup>
    <ItemGroup>
      <PackageReference Include="RabbitMQ.Client" Version="5.0.1" />
      <PackageReference Include="Newtonsoft.Json" Version="10.0.1" />
    </ItemGroup>
  </Project>
  ```

- [ ] **Step 2: Verify.**
  - `grep -lE "TargetFrameworks|net451|net46\b|netstandard1\.|netstandard2\.0" $(find src -name "*.csproj")` should return no results.
  - `grep -E "TargetFramework>net10\.0" $(find src -name "*.csproj") | wc -l` should return 25.
  - `grep -h "DefineConstants" $(find src -name "*.csproj") | sort -u` should show only `<DefineConstants>$(DefineConstants);LIBLOG_PORTABLE</DefineConstants>` (or no DefineConstants for csprojs that didn't have one).

- [ ] **Step 3: Commit**
  ```bash
  git add src/
  git commit -m "Modernize src/ csprojs to net10.0"
  ```

### Task 5: Modernize SDK-style test csprojs and bump test stack

**Files:**
- Modify: `test/RawRabbit.Tests/RawRabbit.Tests.csproj`
- Modify: `test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj`

(Note: `test/RawRabbit.PerformanceTest/` is dropped from sln per Task 2; its csproj is left untouched on disk per spec decision 3.2(b). The legacy non-SDK `test/RawRabbit.Enrichers.Polly.Tests/RawRabbit.Enrichers.Polly.Tests.csproj` is rewritten in Task 6, not here.)

- [ ] **Step 1: Apply Task 4's transformations (a)+(b)+(c)+(d)** to both csprojs. The current `<TargetFramework>net46</TargetFramework>` becomes `<TargetFramework>net10.0</TargetFramework>`. The `Condition=" '$(TargetFramework)' == 'net46' "` ItemGroup gets deleted.

- [ ] **Step 2: Bump test-stack `<PackageReference>` versions** in both csprojs:
  ```xml
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.5.1" />
  <PackageReference Include="xunit" Version="2.9.3" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  <PackageReference Include="Moq" Version="4.20.72" />
  ```
  Note `xunit.runner.visualstudio 2.8.2` (per F2 decision) — NOT 3.x.

- [ ] **Step 3: Verify.**
  - `grep -lE "Microsoft\.NETCore\.Platforms" test/RawRabbit.Tests/RawRabbit.Tests.csproj test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj` should return no results (this old transitive shouldn't be needed on net10).
  - `grep -E "Microsoft\.NET\.Test\.Sdk.*18\.5\.1|xunit.*2\.9\.3|xunit\.runner\.visualstudio.*2\.8\.2|Moq.*4\.20\.72" test/RawRabbit.Tests/RawRabbit.Tests.csproj` should show all 4 lines.

- [ ] **Step 4: Commit**
  ```bash
  git add test/RawRabbit.Tests/ test/RawRabbit.IntegrationTests/
  git commit -m "Modernize test csprojs to net10.0 and bump test stack"
  ```

### Task 6: Convert legacy non-SDK Polly.Tests csproj to SDK-style

**Files:**
- Modify (full overwrite): `test/RawRabbit.Enrichers.Polly.Tests/RawRabbit.Enrichers.Polly.Tests.csproj`
- Delete: `test/RawRabbit.Enrichers.Polly.Tests/packages.config`

- [ ] **Step 1: Overwrite the csproj** with SDK-style content matching the sibling test-project convention plus the Polly.Tests-specific deps. New file content:
  ```xml
  <Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
      <TargetFramework>net10.0</TargetFramework>
      <IsPackable>false</IsPackable>
      <GenerateAssemblyConfigurationAttribute>false</GenerateAssemblyConfigurationAttribute>
      <GenerateAssemblyCompanyAttribute>false</GenerateAssemblyCompanyAttribute>
      <GenerateAssemblyProductAttribute>false</GenerateAssemblyProductAttribute>
    </PropertyGroup>

    <ItemGroup>
      <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.5.1" />
      <PackageReference Include="xunit" Version="2.9.3" />
      <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
      <PackageReference Include="Moq" Version="4.20.72" />
      <PackageReference Include="Polly" Version="5.3.1" />
    </ItemGroup>

    <ItemGroup>
      <ProjectReference Include="..\..\src\RawRabbit\RawRabbit.csproj" />
      <ProjectReference Include="..\..\src\RawRabbit.Enrichers.Polly\RawRabbit.Enrichers.Polly.csproj" />
    </ItemGroup>

  </Project>
  ```

  Notes:
  - `Polly 5.3.1` matches the rest of the codebase per Phase 1's "no major version bumps" rule.
  - `Castle.Core` and `RabbitMQ.Client` come transitively (Castle.Core via Moq; RabbitMQ.Client via the RawRabbit ProjectReference). No explicit `<PackageReference>` needed.
  - `Microsoft.Diagnostics.Tracing.EventSource.Redist` (in old packages.config) is a netfx-era dependency, not needed on net10.
  - SDK-style auto-includes all `.cs` files under the project; the old `<Compile Include>` enumeration disappears.
  - VS service GUIDs / xunit MSBuild targets imports / NuGet build-import error trap (lines 93-99 of the old csproj) all disappear — modern SDK handles these natively.

- [ ] **Step 2: Delete the companion packages.config**
  ```bash
  git rm test/RawRabbit.Enrichers.Polly.Tests/packages.config
  ```

- [ ] **Step 3: Verify.**
  - `head -1 test/RawRabbit.Enrichers.Polly.Tests/RawRabbit.Enrichers.Polly.Tests.csproj` should show `<Project Sdk="Microsoft.NET.Sdk">`.
  - `ls test/RawRabbit.Enrichers.Polly.Tests/packages.config` should fail "No such file".

- [ ] **Step 4: Commit**
  ```bash
  git add test/RawRabbit.Enrichers.Polly.Tests/RawRabbit.Enrichers.Polly.Tests.csproj
  git commit -m "Convert RawRabbit.Enrichers.Polly.Tests to SDK-style csproj"
  ```

### Task 7: Modernize the two retained sample csprojs

**Files:**
- Modify: `sample/RawRabbit.ConsoleApp.Sample/RawRabbit.ConsoleApp.Sample.csproj`
- Modify: `sample/RawRabbit.ConsoleApp.Sample/Program.cs:25` (1-line edit per F1 decision)
- Modify: `sample/RawRabbit.Messages.Sample/RawRabbit.Messages.Sample.csproj`

- [ ] **Step 1: Modernize `Messages.Sample` csproj.** Apply Task 4 transformations (a)+(b). The TFM goes from `netstandard1.5;net451` to `net10.0`; the `net451` conditional ItemGroup is deleted. No DefineConstants block exists in this csproj.

- [ ] **Step 2: Modernize `ConsoleApp.Sample` csproj.** Apply (a)+(b). The current TFM `netcoreapp1.0` becomes `net10.0`. Also:
  - Remove `<PackageTargetFallback>$(PackageTargetFallback);dnxcore50</PackageTargetFallback>` (legacy netcoreapp 1.x compat)
  - Remove `<RuntimeFrameworkVersion>1.0.4</RuntimeFrameworkVersion>` (no longer applicable)
  - Update `<PackageReference>` versions:
    - `Microsoft.Extensions.Configuration.Binder` 1.1.2 → 10.0.7
    - `Microsoft.Extensions.Configuration.Json` 1.0.2 → 10.0.7
    - REPLACE `Serilog.Sinks.Literate` 3.0.0 with `Serilog.Sinks.Console` 6.1.1

- [ ] **Step 3: Edit `sample/RawRabbit.ConsoleApp.Sample/Program.cs:25`** (per F1 decision). Change:
  ```csharp
  .WriteTo.LiterateConsole()
  ```
  to:
  ```csharp
  .WriteTo.Console()
  ```

- [ ] **Step 4: Verify.**
  - `grep -E "TargetFramework>net10\.0" sample/RawRabbit.ConsoleApp.Sample/RawRabbit.ConsoleApp.Sample.csproj sample/RawRabbit.Messages.Sample/RawRabbit.Messages.Sample.csproj` shows both files matching.
  - `grep -E "Serilog\.Sinks\.Literate|LiterateConsole" sample/RawRabbit.ConsoleApp.Sample/` (recursive) returns no results.
  - `grep "Serilog\.Sinks\.Console" sample/RawRabbit.ConsoleApp.Sample/RawRabbit.ConsoleApp.Sample.csproj` shows the new package.

- [ ] **Step 5: Commit**
  ```bash
  git add sample/
  git commit -m "Modernize sample csprojs to net10.0 and swap Serilog sink"
  ```

### Task 8: Verify build green

**Files:** none modified; verification only.

- [ ] **Step 1: Confirm SDK selection**
  ```bash
  dotnet --version
  ```
  Expect: 10.0.x (driven by `global.json`).

- [ ] **Step 2: Restore**
  ```bash
  dotnet restore
  ```
  Expect: completes with 0 errors. NU1701 warnings expected on RabbitMQ.Client 5.0.1, Newtonsoft.Json 10.0.1, Polly 5.3.1, ZeroFormatter, Ninject, MessagePack, Stateless, Autofac, protobuf-net, Microsoft.Extensions.DependencyInjection — these are the spec-anticipated warnings (see V3 decision).

- [ ] **Step 3: Build**
  ```bash
  dotnet build -c Release
  ```
  Expect: 0 errors. Warnings only (NU1701 + possibly some legitimate compiler warnings from the old .cs source).

- [ ] **Step 4: Run unit tests**
  ```bash
  dotnet test test/RawRabbit.Tests
  dotnet test test/RawRabbit.Enrichers.Polly.Tests
  ```
  Expect: tests run and pass (or skip explicitly). Zero "no tests discovered" cases.

- [ ] **Step 5: If all four steps pass, Phase 1 is complete.** No commit for this task — it's pure verification. If any step fails, consult spec §6 (Risks & responses) for the recovery template.

---

## Tasks NOT in this plan

Inherited verbatim from spec §3 ("Explicitly NOT touched in Phase 1"):

- `RabbitMQ.Client` version → stays at 5.0.1 (Phase 5 territory)
- `Newtonsoft.Json` version → stays at 10.0.1 (Phase 4 territory)
- `Polly`, `Stateless`, `protobuf-net`, `MessagePack`, `ZeroFormatter`, `Ninject`, `Autofac` versions → stay (Phase 7)
- `<PackageIconUrl>`, `<PackageProjectUrl>`, `<Authors>`, `<VersionPrefix>` metadata → stays (Phase 2)
- `Directory.Build.props`, `Directory.Packages.props` → not introduced (Phase 2)
- LibLog source file (`src/RawRabbit/Logging/LibLog.cs`) → untouched (Phase 3)
- All `.cs` source files → untouched (except: the 25 AssemblyInfo.cs deletions explicitly approved by V2; and the 1-line edit in `sample/RawRabbit.ConsoleApp.Sample/Program.cs:25` per F1)
- `.build/*.ps1`, `.build/appveyor.yml` → untouched (Phase 6 will replace them)
- `README.md`, `RELEASENOTES.md`, `docs/` → untouched

A new spec → new plan cycle is required to add any of these.

## Known issues inherited from spec

`RawRabbit.Enrichers.HttpContext` compiles to empty pass-through classes on net10 after Phase 1 (V1 decision). All four `.cs` files in this enricher gate every functional code path behind `#if NETSTANDARD1_6` or `#if NET451`. Since neither define is set on net10, the project compiles to:
- `AspNetCoreHttpContextMiddleware` with no field, no constructor body, an empty `InvokeAsync` (only `return Next.InvokeAsync(...)`)
- `NetFxHttpContextMiddleware` with the same empty pass-through
- `PipeContextHttpExtensions.UseHttpContext` and `.GetHttpContext` — methods do not exist
- `HttpContextPlugin.UseHttpContext()` extension — registers a middleware with no http accessor injection

Consumers calling the plugin will get a no-op. No tests exercise this enricher. The fix — adding a `#if NET10_0_OR_GREATER` branch wired to current `Microsoft.AspNetCore.Http` — is deferred to a later phase. Accepted by user during brainstorming on 2026-05-11.
