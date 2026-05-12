# Critical Design Review: 2026-05-12-modernization-phase-3-design (Round 1)

**Spec:** `/home/yv01p/rawrabbit/docs/specs/2026-05-12-modernization-phase-3-design.md`
**Verified Assumptions section:** present

## 1. Verified-assumptions cross-check

Fresh-read sanity check against the cited evidence in spec §6. All 24 assumptions reconfirmed:

| ID | Status |
|---|---|
| A1 | Reconfirmed — `src/RawRabbit/Logging/LibLog.cs`, 2391 lines, header confirms LibLog vendored fork |
| A2 | Reconfirmed — `src/RawRabbit/RawRabbit.csproj:14` carries the LIBLOG_PORTABLE define on its own line |
| A3 | Reconfirmed — `src/RawRabbit.Compatibility.Legacy/RawRabbit.Compatibility.Legacy.csproj:14` carries the orphan LIBLOG_PORTABLE; zero LibLog usages in the project's `.cs` files |
| A4 | Reconfirmed — `Directory.Packages.props` exists with the Phase 2 structure |
| A5/A6 | Reconfirmed — sample paths exist |
| A7 | Reconfirmed — `GlobalExecutionIdRepository.cs` Get() returns null on net10 via `#else return null;`; Set() silently no-ops on net10 |
| A8 | Reconfirmed — `MessageContextRepository.cs` Get() returns null on net10 via the same pattern; Set()/ctor silently no-op |
| B1 | Reconfirmed — exhaustive grep of `_logger.X`, `LogProvider.X`, `ILog\b`, `LogLevel.X` confirms consumer surface is exactly Info/Debug/Warn/Error/Trace/Fatal + the `XxxException` variants + the 6 `IsXxxEnabled` checks + `LogProvider.For<T>()`. No `Func<string>` lazy-eval pattern (`grep -rE '_logger\.\w+\s*\(\s*\(\)'` returned 0 hits in consumer files). All other LibLog APIs (`_logger.Name`, raw `_logger.Log(`, `Logger.Write`, `Logger.Instance`, `LogProvider.IsLoggerAvailable`, `LogLevel.X` enum references) verified to live only inside `src/RawRabbit/Logging/LibLog.cs` itself, never used by consumers |
| B2/B3 | Reconfirmed dissolved (the references were inside LibLog.cs internals) |
| C1 | Reconfirmed — `LibLog.cs:44-53` carries exactly 10 `[assembly: InternalsVisibleTo(...)]` declarations; the 10 sibling project names match the spec's enumeration verbatim |
| C2 | Reconfirmed — `LibLog.cs` is the only `.cs` file in `src/RawRabbit/` carrying `[assembly: InternalsVisibleTo(...)]` |
| C3 | Reconfirmed — only `ILog` and `LogProvider` are referenced from outside LibLog.cs |
| D1 | Reconfirmed — `Microsoft.Extensions.Logging.Abstractions 10.0.0` (released 11/11/2025) targets net10.0 explicitly; single direct dep on `Microsoft.Extensions.DependencyInjection.Abstractions 10.0.0` (also explicit net10) |
| D2 | Reconfirmed — `SerilogLoggerFactory` class still ships in `Serilog.Extensions.Logging 10.0.0` as `public sealed class SerilogLoggerFactory : ILoggerFactory` with the `(ILogger? logger = null, bool dispose = false, ...)` ctor |
| D3 | Reconfirmed — sample currently transitively gets Serilog 4.0.0; SLog.Extensions.Logging 10.0.0's floor of Serilog ≥ 4.2.0 will auto-resolve via NuGet max-floor |
| D4 | Reconfirmed — MEL.Abstractions 10.0.0 transitive dep tree on net10 is clean (single Microsoft transitive dep) |
| E1 | Reconfirmed — `Program.cs` Serilog setup precedes bus build; insertion point for the wiring line is unambiguous |
| F1 | Reconfirmed — no test in `test/RawRabbit.Tests` or `test/RawRabbit.Enrichers.Polly.Tests` references `GlobalExecutionIdRepository`. (IntegrationTests references it but is out of Phase 3 acceptance per spec §1) |
| F2 | Reconfirmed — no test anywhere references `MessageContextRepository` / `IMessageContextRepository` / `MessageContext.Get` |
| F4 | Reconfirmed — no other `.cs` file in `src/` has the `#if NETSTANDARD1_5 / #elif NET451 / #else return null;` dead-conditional pattern |
| G1 | Reconfirmed — only `LibLog.cs` gates code on `#if LIBLOG_PORTABLE` |
| I1 | Reconfirmed — nothing in `test/` or `sample/` directly imports `RawRabbit.Logging` or calls `LogProvider.X` |
| J1 | Reconfirmed — no `<Nullable>` setting in src csprojs or Build.props; the `where TState : notnull` constraint on the shim's `BeginScope<TState>` is syntactically valid in C# 8+ regardless of nullable context and is required to satisfy MEL ILogger's interface |

## 2. Literal-wrongness findings

No literal-wrongness findings.

Several candidates considered and dropped after the literal-wrongness test:

- **Visibility widening of `ILog` (internal → public).** The current LibLog.cs declares `interface ILog` under `#if LIBLOG_PUBLIC ... public ... #else internal ... #endif`; with neither LIBLOG_PUBLIC nor LIBLOG_PROVIDERS_ONLY defined in the csproj, `ILog` is currently `internal`. The spec's shim makes it `public`. Wider visibility never breaks consumers; verified that no public RawRabbit API today exposes a parameter or return of type `ILog` (`grep -rnE "public\s+\w+\s+(ILog\b|.*<ILog>)" src/` returned 0 matches). Drop.
- **`Func<string>` lazy-eval overloads.** LibLog historically supported `_logger.Info(() => $"...")`. The shim's extension methods only have `(string m, params object[] a)` signatures. Verified that consumers do NOT use the lazy-eval pattern (`grep -rE '_logger\.\w+\s*\(\s*\(\)'` returned 0 hits in consumer files). Asked-for behavior (existing call sites continue to compile) is not impacted. Drop.
- **`ErrorException` argument order.** Verified the sole consumer call (`src/RawRabbit/Common/ExclusiveLock.cs:75`) uses `(message, exception)` order, matching the shim's `(this ILog l, string m, Exception ex, params object[] a)` signature. Drop.
- **2 unused IVT entries** (`RawRabbit.Operations.Get`, `RawRabbit.Operations.Tools`). Listed in LibLog.cs IVT but no `LogProvider.For` / `ILog` consumption verified by grep. Preserving them in the shim is harmless verbosity, not literal-wrongness. Drop.
- **NuGet upper-bound conflict between Sinks.Console 6.1.1 and the auto-bumped Serilog 4.2+.** Spec risk #2 acknowledges the breakage path generically (any incompatibility between transitive Serilog 4.0→4.2+ and the existing sample) and provides an escalation template. No specific evidence of an upper-bound cap on Sinks.Console 6.1.1; without evidence, this is speculation. Drop.

## 3. Forced decisions

No forced decisions found.

Spec §4 explicitly picks every decision (4.1 scope, 4.2 logger surface, 4.3 deletion strategy, 4.4 MEL version, 4.5 Serilog adapter, 4.6 transitive Serilog bump, 4.7 Compat.Legacy define cleanup, 4.8 commit packaging). No either/or that the spec dodges.

## 5. Recommendation

✅ **Approve as-is** — §2 and §3 both empty. Spec is ready for implementation planning (`thorough-writing-plans`).
