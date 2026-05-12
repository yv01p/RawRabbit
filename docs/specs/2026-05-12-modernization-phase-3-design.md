# Modernization — Phase 3: Logging (LibLog → Microsoft.Extensions.Logging.Abstractions) + AsyncLocal fix

**Brief (verbatim):** "Let's work on Phase 3 now" — the third phase of the 7-phase RawRabbit modernization decomposition (Phase 1 spec §1).

**Repo:** `/home/yv01p/rawrabbit`, branch `2.0`, base commit `cc4bd44` (Phase 2 final).

## 1. Goal & success criteria

Replace LibLog (vendored 2391-line single-file logging façade in `src/RawRabbit/Logging/LibLog.cs`) with a thin shim over `Microsoft.Extensions.Logging.Abstractions`. Same call-site shape; no consumer-file edits needed beyond the shim file itself. Drop the now-orphan `LIBLOG_PORTABLE` define from both csprojs that carry it. As a paired correctness fix, repair the AsyncLocal `Get()/Set()` `#else return null;` no-ops in `GlobalExecutionIdRepository.cs` and `MessageContextRepository.cs` so those repositories actually work on net10.

After Phase 3 lands, on a clean clone with the .NET 10 SDK installed:

```
dotnet restore                                                          # 0 errors
dotnet build -c Release                                                 # 0 errors
dotnet test test/RawRabbit.Tests --no-build -c Release --blame-hang-timeout 15s   # 29 passed / 7 skipped / 0 failed
dotnet test test/RawRabbit.Enrichers.Polly.Tests --no-build -c Release  # 1 passed / 2 skipped / 0 failed
dotnet pack -c Release --no-build                                       # 25 nupkgs, 0 errors
```

Test counts stay at the Phase 1/2 baseline (30 passing / 9 skipped / 0 failed). Warning shape: same as Phase 2 plus possibly slightly fewer NU1701 (LibLog.cs no longer compiles, removing one source of older-net references); plus expected pack-time NU5048 (deprecated PackageIconUrl, accepted per Phase 2 §1).

**Out-of-scope for "phase 3 acceptance" (verbatim from Phase 1 spec §1 + Phase 2 §2.3):**
- AspNet.Sample wiring (dropped-from-sln; Phase 6/7 territory)
- `RawRabbit.Enrichers.HttpContext` restoration (Phase 1 V1 known issue; later phase)
- Any `.cs` source touch in broker layer (Phase 5 owns RabbitMQ.Client 7.x async rewrite)
- The 9 `[Fact(Skip="...")]` broker-layer tests (Phase 5/7)
- `test/RawRabbit.IntegrationTests` execution (requires live broker; Phase 6 turns it on)
- Bumping any package major version other than the explicit additions in §3 (MEL.Abstractions, Serilog.Extensions.Logging, transitive Serilog 4.0 → 4.2+) — Phases 4/5/7
- Converting middleware fields from static `LogProvider.For<T>()` to ctor-injected `ILogger<T>` (user picked the static-shim path during brainstorming)
- Normalizing the heterogeneous `Authors` values across src csprojs (Phase 2 §2.3 deferred; cosmetic, no-publish intent)

## 2. Architecture

The shim file (replacing `src/RawRabbit/Logging/LibLog.cs`, ~2391 → ~80 lines, same path, same `RawRabbit.Logging` namespace, same public surface):

```csharp
// 10 [assembly: InternalsVisibleTo(...)] declarations preserved verbatim
// (see §6 Verified Assumptions / C1 for the enumeration)

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace RawRabbit.Logging
{
    // ILog: marker interface that extends MEL ILogger so the shim's extension
    // methods (Info/Debug/Warn/Error/Trace/Fatal/IsXxxEnabled/XxxException)
    // hang off it without colliding with MEL's own LogInformation/LogDebug
    // extensions. Consumers can also call MEL's surface directly if they prefer.
    public interface ILog : ILogger { }

    public static class LogProvider
    {
        public static ILoggerFactory LoggerFactory { get; set; }
            = NullLoggerFactory.Instance;

        public static ILog For<T>() =>
            new LogWrapper(LoggerFactory.CreateLogger<T>());
    }

    internal sealed class LogWrapper(ILogger inner) : ILog
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull
            => inner.BeginScope(state);
        public bool IsEnabled(LogLevel level) => inner.IsEnabled(level);
        public void Log<TState>(LogLevel level, EventId eventId, TState state,
                                Exception exception,
                                Func<TState, Exception, string> formatter)
            => inner.Log(level, eventId, state, exception, formatter);
    }

    public static class LogExtensions
    {
        // Message-only variants
        public static void Info (this ILog l, string m, params object[] a) => l.LogInformation(m, a);
        public static void Debug(this ILog l, string m, params object[] a) => l.LogDebug      (m, a);
        public static void Warn (this ILog l, string m, params object[] a) => l.LogWarning    (m, a);
        public static void Error(this ILog l, string m, params object[] a) => l.LogError      (m, a);
        public static void Trace(this ILog l, string m, params object[] a) => l.LogTrace      (m, a);
        public static void Fatal(this ILog l, string m, params object[] a) => l.LogCritical   (m, a);

        // Exception variants
        public static void InfoException (this ILog l, string m, Exception ex, params object[] a) => l.LogInformation(ex, m, a);
        public static void DebugException(this ILog l, string m, Exception ex, params object[] a) => l.LogDebug      (ex, m, a);
        public static void WarnException (this ILog l, string m, Exception ex, params object[] a) => l.LogWarning    (ex, m, a);
        public static void ErrorException(this ILog l, string m, Exception ex, params object[] a) => l.LogError      (ex, m, a);
        public static void TraceException(this ILog l, string m, Exception ex, params object[] a) => l.LogTrace      (ex, m, a);
        public static void FatalException(this ILog l, string m, Exception ex, params object[] a) => l.LogCritical   (ex, m, a);

        // Enabled checks
        public static bool IsInfoEnabled (this ILog l) => l.IsEnabled(LogLevel.Information);
        public static bool IsDebugEnabled(this ILog l) => l.IsEnabled(LogLevel.Debug);
        public static bool IsWarnEnabled (this ILog l) => l.IsEnabled(LogLevel.Warning);
        public static bool IsErrorEnabled(this ILog l) => l.IsEnabled(LogLevel.Error);
        public static bool IsTraceEnabled(this ILog l) => l.IsEnabled(LogLevel.Trace);
        public static bool IsFatalEnabled(this ILog l) => l.IsEnabled(LogLevel.Critical);
    }
}
```

**Why this shape:**
- Call sites unchanged: `private readonly ILog _logger = LogProvider.For<T>();` and `_logger.Info("...", arg);` continue to compile verbatim across all ~30 consumer files.
- `ILog : ILogger` — consumers can also call MEL's standard `_logger.LogInformation(...)` if they want.
- `LogProvider.LoggerFactory` static setter mirrors LibLog's `SetCurrentLogProvider()` pattern. Defaults to `NullLoggerFactory.Instance` so unwired code behaves like NullLog (current implicit baseline).
- One package added in `src/RawRabbit`: `Microsoft.Extensions.Logging.Abstractions 10.0.0` (interface-only; Microsoft package; explicit net10 target).
- No `_logger.Name` helper, no raw `_logger.Log(...)` helper, no `LogProvider.IsLoggerAvailable` — all of the "concerning" non-Info/Debug/Warn/Error API surface I worried about turned out to be inside LibLog.cs itself, not used by consumers (verified in §6 / B1).

The AsyncLocal fix (separate from the logging shim, but same Phase 3 spec):

```csharp
// src/RawRabbit.Enrichers.GlobalExecutionId/Dependencies/GlobalExecutionIdRepository.cs
// Net diff: 38 → ~20 lines. Drop #if NETSTANDARD1_5 / #elif NET451 / #else / #endif
// blocks; drop #if NET451 using System.Runtime.Remoting.Messaging; just always
// use the AsyncLocal<string> field and Get/Set off its .Value.

// src/RawRabbit.Enrichers.MessageContext/Dependencies/MessageContextRepository.cs
// Same shape. 51 → ~25 lines.
```

## 3. In-scope file changes

| Change | Files | Count |
|---|---|---|
| Replace `src/RawRabbit/Logging/LibLog.cs` (2391 lines) with the ~80-line shim, preserving all 10 `[assembly: InternalsVisibleTo(...)]` declarations verbatim | `src/RawRabbit/Logging/LibLog.cs` | 1 (replace) |
| Add `<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />` and remove `<DefineConstants>$(DefineConstants);LIBLOG_PORTABLE</DefineConstants>` | `src/RawRabbit/RawRabbit.csproj` | 1 |
| Remove orphan `<DefineConstants>$(DefineConstants);LIBLOG_PORTABLE</DefineConstants>` (Compatibility.Legacy has zero LibLog usages) | `src/RawRabbit.Compatibility.Legacy/RawRabbit.Compatibility.Legacy.csproj` | 1 |
| Add 2 PackageVersion entries: `Microsoft.Extensions.Logging.Abstractions 10.0.0` and `Serilog.Extensions.Logging 10.0.0` | `Directory.Packages.props` | 1 |
| Add 1 line wiring `LogProvider.LoggerFactory = new SerilogLoggerFactory(Log.Logger);` between Serilog setup and bus build in `RunAsync()` | `sample/RawRabbit.ConsoleApp.Sample/Program.cs` | 1 |
| Add `<PackageReference Include="Serilog.Extensions.Logging" />` for the SerilogLoggerFactory class | `sample/RawRabbit.ConsoleApp.Sample/RawRabbit.ConsoleApp.Sample.csproj` | 1 |
| Drop `#if NETSTANDARD1_5 / #elif NET451 / #else return null; / #endif` blocks; drop `#if NET451 using System.Runtime.Remoting.Messaging;` block; always use AsyncLocal | `src/RawRabbit.Enrichers.GlobalExecutionId/Dependencies/GlobalExecutionIdRepository.cs` | 1 |
| Same pattern | `src/RawRabbit.Enrichers.MessageContext/Dependencies/MessageContextRepository.cs` | 1 |

**Test:** No new test files. Acceptance is "Phase 1/2 test counts unchanged" (29/7/0 + 1/2/0 = 30/9/0).

### Sample Program.cs wiring diff (illustrative)

**Before** (current Program.cs lines 25-29):
```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

_client = RawRabbitFactory.CreateSingleton(new RawRabbitOptions
```

**After**:
```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

LogProvider.LoggerFactory = new SerilogLoggerFactory(Log.Logger);

_client = RawRabbitFactory.CreateSingleton(new RawRabbitOptions
```

Plus `using Serilog.Extensions.Logging;` and `using RawRabbit.Logging;` added at the top.

## 4. Decisions taken

| ID | Decision | Choice | Why |
|---|---|---|---|
| 4.1 | Phase 3 scope | (b) Logging swap + AsyncLocal fix in same phase | The 2 AsyncLocal repos are mechanically broken on net10 (return null from Get on net10 because `#elif NET451 / #else return null;`); fix is dropping dead conditionals. Bundling avoids leaving a known correctness hole through Phase 4. |
| 4.2 | Logger surface at call sites | (a) Static factory shim (`ILog _logger = LogProvider.For<T>()`) preserved verbatim | Minimum churn across ~30 consumer files. Matches LibLog's existing static-provider architecture. The DI-injected `ILogger<T>` alternative would touch ~30 ctors AND the IoC-resolver path; YAGNI for personal modernization fork. |
| 4.3 | LibLog deletion strategy | (a) Replace LibLog.cs in place with shim file at same path/namespace | Keeps `using RawRabbit.Logging;` in consumers unchanged. Preserves the 10 InternalsVisibleTo declarations as standalone `[assembly: ...]` in the shim file. Single-file replacement = single-commit atomic change. |
| 4.4 | MEL.Abstractions version | (a) `10.0.0` (latest stable, released 11/11/2025) | Latest stable major; explicit net10 target; single direct dep on DI.Abstractions 10.0.0 (also Microsoft, also explicit net10). No NU1701/NU1902/NU1903 expected. |
| 4.5 | Serilog adapter approach | (a) `new SerilogLoggerFactory(Log.Logger)` from Serilog.Extensions.Logging 10.0.0 | The class still exists in 10.0.0 as `public sealed class SerilogLoggerFactory : ILoggerFactory`. Direct, 1-line wiring. Serilog README recommends `AddSerilog()` extension as more idiomatic, but that requires `Microsoft.Extensions.Logging` runtime + an IServiceCollection — needless complexity for this sample wiring. |
| 4.6 | Sample's transitive Serilog 4.0 → 4.2+ bump | (a) Accept (NuGet auto-resolves; Sinks.Console 6.1.1 stays compatible) | Serilog 4.0 → 4.2 is a minor bump within Serilog 4.x. Minor surface change in the sample's transitive dep tree, no behavior change. |
| 4.7 | Compatibility.Legacy's orphan LIBLOG_PORTABLE | (a) Delete in same Phase 3 commit as the RawRabbit.csproj equivalent | Zero LibLog usages in Compatibility.Legacy means the define has been a no-op since whenever; cleaning up costs zero. |
| 4.8 | Commit packaging | (a) 5 commits (1 props pre-stage, 1 atomic logging swap, 1 sample wiring, 2 AsyncLocal fixes one per repo) | Each commit leaves the build green; matches Phase 1/2 granularity convention. |

## 5. Risks & responses

| # | Risk | Why it might fire | Phase 3 response |
|---|------|---|---|
| 1 | `Microsoft.Extensions.Logging.Abstractions 10.0.0` introduces a transitive dep that fires NU1xxx beyond Phase 2 baseline | Microsoft package families occasionally drag in System.* satellites with NU1701-able TFMs | Read the actual warning code. Acceptable as long as no ERRORS fire. Document if new warning codes appear. |
| 2 | Serilog 4.0 → 4.2 transitive bump breaks the `WriteTo.Console().CreateLogger()` chain in the sample | Unlikely — both versions are within Serilog 4.x; the LoggerConfiguration API is stable since Serilog 1.x | Compile + run the sample's build to verify (`dotnet build sample/RawRabbit.ConsoleApp.Sample`). If it fails, escalate to user (need to decide between Serilog version bump or shim approach). |
| 3 | `dotnet pack` fires new pack-time warnings beyond Phase 2's NU5048 baseline | Adding a new PackageReference to RawRabbit could shift NU5128 (dependency mismatch) or NU5026 (missing description) shapes | Per Phase 2 §2.3, ANY pack-time NUxxxx warnings are acceptable as long as no errors. Document the actual warning code(s). |
| 4 | The shim's `notnull` constraint on `BeginScope<TState>` triggers a CS warning under the codebase's nullable context | Project default is nullable disabled; CS86xx warnings could fire | Verified in §6 / J1: the `notnull` constraint is syntactically valid in C# 8+ regardless of nullable context, and is needed to MATCH MEL ILogger's signature so the interface implementation typechecks. |
| 5 | Removing LIBLOG_PORTABLE from Compatibility.Legacy.csproj causes a build regression | Compatibility.Legacy's source might gate code on `#if LIBLOG_PORTABLE` somewhere | Verified in §6 / G1: no .cs file in src/ outside LibLog.cs gates code on LIBLOG_PORTABLE. Compatibility.Legacy specifically has zero LibLog usages anywhere. Safe to delete. |
| 6 | The AsyncLocal fix changes test outcomes | A test could have been written assuming the broken-on-net10 null-return | Verified in §6 / F1, F2: no test in `test/RawRabbit.Tests` or `test/RawRabbit.Enrichers.Polly.Tests` references either repository. Tests in `test/RawRabbit.IntegrationTests` (out of Phase 3 acceptance per §1; not exercised by the spec's success criteria) DO reference GlobalExecutionId — they assume the working AsyncLocal behavior. Phase 6's GHA work will exercise them against a live broker; the AsyncLocal fix lands correctly for that future use. No Phase 3 acceptance impact. |
| 7 | Removing `LIBLOG_PORTABLE` from RawRabbit.csproj while LibLog.cs still references it (in the moments before the file is replaced) breaks the build | If Task 2 (logging swap) is split across multiple commits with intermediate state | Risk eliminated by atomic commit: LibLog.cs replacement, Build.props add, and define removal all happen in one commit (Task 2 in §8 below). |
| 8 | Consumer code calls `LogExtensions.Info(this ILog, string, params object[])` but the args contain a single `Exception` arg, expecting it to bind to `ErrorException(string, Exception, params object[])` | LibLog had subtle overload resolution; MEL's `LogInformation(message, args)` with an Exception in args treats it as a structured log property | Verified in §6 / B1: existing call sites use `_logger.ErrorException(message, ex, args)` for exception logging — explicit method call, no overload resolution ambiguity. Shim's `ErrorException` extension routes correctly to `l.LogError(ex, message, args)`. |

## 6. Verified assumptions

These were enumerated cold against the design and verified empirically before this spec was written. Each is either confirmed against repo state or against authoritative external sources (nuget.org, Serilog GitHub).

| ID | Assumption | Evidence |
|---|---|---|
| A1 | `src/RawRabbit/Logging/LibLog.cs` exists as the LibLog vendored single-file façade | `wc -l` returned 2391 lines; header comments confirm it's the LibLog vendored fork |
| A2 | `src/RawRabbit/RawRabbit.csproj` has `<DefineConstants>$(DefineConstants);LIBLOG_PORTABLE</DefineConstants>` on its own line | `grep -n` confirmed line 14 |
| A3 | `src/RawRabbit.Compatibility.Legacy/RawRabbit.Compatibility.Legacy.csproj` has the same orphan define; project has zero LibLog usages | `grep -n` confirmed line 14; `grep -rln "LogProvider\|ILog\b\|using.*LibLog" src/RawRabbit.Compatibility.Legacy --include="*.cs"` returned 0 matches |
| A4 | `Directory.Packages.props` exists with the Phase 2 structure | `head` confirmed `<Project> / <PropertyGroup> / <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally> / <ItemGroup>` opening |
| A5/A6 | `sample/RawRabbit.ConsoleApp.Sample/Program.cs` and `RawRabbit.ConsoleApp.Sample.csproj` exist | `ls` confirmed |
| A7 | `GlobalExecutionIdRepository.cs` Get() returns `null` on net10 via `#else return null;` block | `cat` of file confirmed lines 23-25; Set() has no `#else` block (silently no-ops on net10) |
| A8 | `MessageContextRepository.cs` Get() returns `null` on net10 via the same pattern | `cat` confirmed lines 36-38; Set() and ctor have no `#else` (silently no-op on net10) |
| B1 | The ~30 LibLog consumers use ONLY: Info/Debug/Warn/Error/Trace/Fatal + XxxException variants + IsXxxEnabled + LogProvider.For<T>() | `grep -rhoE` summed all `_logger.X` and `LogProvider.X` references; `_logger.Name`, `_logger.Log(`, `Logger.Write`, `Logger.Instance`, `LogProvider.IsLoggerAvailable`, `LogLevel.X` references all turned out to be inside src/RawRabbit/Logging/LibLog.cs itself, NOT in consumer code |
| B2 | (Dissolved) The 2× `_logger.Name` usages were inside LibLog.cs (lines 1046, 1048) | `grep -rn "_logger\.Name"` returned only LibLog.cs lines |
| B3 | (Dissolved) The 2× raw `_logger.Log(` usages were inside LibLog.cs (lines 1046, 1048) — internal NLog adapter code | Same `grep` evidence |
| C1 | LibLog.cs carries 10 `[assembly: InternalsVisibleTo(...)]` declarations (Phase 1 §6 risk #8 said 9 — off by one). Sibling projects: Enrichers.RetryLater, Operations.{Get, MessageSequence, Publish, Request, Respond, StateMachine, Subscribe, Tools}, Enrichers.GlobalExecutionId | `grep -nE "InternalsVisibleTo" src/RawRabbit/Logging/LibLog.cs` returned lines 44-53 (10 lines) |
| C2 | LibLog.cs is the ONLY .cs file in src/RawRabbit/ with `[assembly: InternalsVisibleTo(...)]` declarations | `grep -rln "InternalsVisibleTo" src/RawRabbit --include="*.cs"` returned single match |
| C3 | The only LibLog public types referenced from outside LibLog.cs are `ILog` and `LogProvider`. `LogLevel` referenced 60+ times only inside LibLog.cs | Same B1 evidence |
| D1 | Microsoft.Extensions.Logging.Abstractions 10.0.0 (released 11/11/2025) targets net10.0 explicitly; single direct dep is Microsoft.Extensions.DependencyInjection.Abstractions 10.0.0; ships ILogger, ILogger<T>, ILoggerFactory, LogLevel, LoggerExtensions (Log* methods), NullLogger, NullLogger<T> | nuget.org/packages/Microsoft.Extensions.Logging.Abstractions/10.0.0 confirmed |
| D2 | Serilog.Extensions.Logging 10.0.0 still ships SerilogLoggerFactory class | github.com/serilog/serilog-extensions-logging confirmed `public sealed class SerilogLoggerFactory : ILoggerFactory` with `SerilogLoggerFactory(ILogger? logger = null, bool dispose = false, ...)` constructor still present |
| D3 | Serilog.Extensions.Logging 10.0.0 floor is Serilog 4.2.0+; sample currently has Serilog 4.0.0 transitively (via Sinks.Console 6.1.1); NuGet auto-resolves to max floor | nuget.org D2 page confirmed Serilog 4.2.0+ floor; `dotnet list package --include-transitive` showed current Serilog 4.0.0 |
| D4 | MEL.Abstractions 10.0.0 transitive dep tree on net10 is clean (no NU1701/NU1902/NU1903) | nuget.org confirmed only direct dep is DI.Abstractions 10.0.0 (Microsoft package, explicit net10 target) |
| E1 | ConsoleApp.Sample's Program.cs RunAsync() does Serilog setup (`Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();`) THEN bus build (`_client = RawRabbitFactory.CreateSingleton(...)`); insertion point for `LogProvider.LoggerFactory = ...` is between them | `cat` of Program.cs confirmed; insertion point is line 30 |
| F1 | No test in `test/RawRabbit.Tests` or `test/RawRabbit.Enrichers.Polly.Tests` references `GlobalExecutionIdRepository`. (test/RawRabbit.IntegrationTests has GlobalExecutionIdTests.cs and MessageSequenceTests.cs, but IntegrationTests is out of Phase 3 acceptance per §1.) | `grep -rln "GlobalExecutionId" test/RawRabbit.Tests test/RawRabbit.Enrichers.Polly.Tests` returned 0 matches |
| F2 | No test anywhere references `MessageContextRepository` / `IMessageContextRepository` / `MessageContext.Get` | `grep -rln` returned 0 matches |
| F4 | No other .cs file in src/ has the `#if NETSTANDARD1_5 / #elif NET451 / #else return null;` dead-conditional pattern | `grep -B5 "return null"` of `#else` blocks across src/ returned no results besides the 2 already-in-scope files |
| G1 | No .cs file outside `src/RawRabbit/Logging/LibLog.cs` gates code on `#if LIBLOG_PORTABLE` | `grep -rln "LIBLOG_PORTABLE" src/ --include="*.cs"` returned only LibLog.cs |
| I1 | Nothing in test/ or sample/ uses the LibLog public surface (no imports of `RawRabbit.Logging`, no calls to `LogProvider.X`) | `grep -rn "RawRabbit\.Logging\|LogProvider\." test/ sample/ --include="*.cs"` returned 0 matches |
| J1 | The shim's `where TState : notnull` constraint matches MEL ILogger's signature; syntactically valid in C# 8+ regardless of nullable context (project has no `<Nullable>` setting) | MEL ILogger source shows `BeginScope<TState>(TState state) where TState : notnull`; `grep "<Nullable>"` returned 0 matches in csprojs/Build.props |

## 7. Tasks NOT in this plan (deferred per Phase 1 spec §1 + Phase 2 §2.3 + brainstorming)

- AspNet.Sample wiring (out-of-sln; Phase 6/7)
- `RawRabbit.Enrichers.HttpContext` restoration (Phase 1 V1; later phase)
- Convert middleware fields to ctor-injected `ILogger<T>` (user picked static-shim path)
- Bumping any package major version other than the 2 explicit additions in §3 — Phases 4/5/7
- Investigation/fix of the 9 `[Fact(Skip)]` broker-layer tests (Phase 5/7 per Phase 1 §6 risk #5)
- CI on GitHub Actions (Phase 6)
- Heterogeneous `Authors` normalization (cosmetic, Phase 2 §2.3)
- `Compatibility.Legacy` `<VersionPrefix>` addition (cosmetic, Phase 2 §2.3)
- Adding `<GenerateAssembly*>` flags to MessagePack/Protobuf/ZeroFormatter (cosmetic IL metadata, Phase 2 §2.3)

## 8. Suggested implementation task decomposition (for the implementation plan to elaborate)

Five tasks; each leaves the build green except where noted.

| Task | Atomicity | Files |
|---|---|---|
| 1: Add MEL.Abstractions + Serilog.Extensions.Logging to central PackageVersions | Single commit | `Directory.Packages.props` (+2 PackageVersion lines) |
| 2: Replace LibLog with MEL.Abstractions shim | **Atomic by build necessity** — deleting LibLog.cs without the shim breaks ~30 consumers; adding the shim while LibLog still exists creates type clashes | `src/RawRabbit/Logging/LibLog.cs` (replace), `src/RawRabbit/RawRabbit.csproj` (-1 LIBLOG_PORTABLE define, +1 PackageReference), `src/RawRabbit.Compatibility.Legacy/RawRabbit.Compatibility.Legacy.csproj` (-1 orphan LIBLOG_PORTABLE define) |
| 3: Wire ConsoleApp.Sample to MEL via Serilog adapter | Single commit | `sample/RawRabbit.ConsoleApp.Sample/Program.cs` (+2 using lines, +1 wiring line), `sample/RawRabbit.ConsoleApp.Sample/RawRabbit.ConsoleApp.Sample.csproj` (+1 PackageReference) |
| 4: Fix GlobalExecutionId AsyncLocal Get/Set on net10 | Single commit | `src/RawRabbit.Enrichers.GlobalExecutionId/Dependencies/GlobalExecutionIdRepository.cs` (delete dead conditionals; net 38 → ~20 lines) |
| 5: Fix MessageContext AsyncLocal Get/Set on net10 | Single commit | `src/RawRabbit.Enrichers.MessageContext/Dependencies/MessageContextRepository.cs` (delete dead conditionals; net 51 → ~25 lines) |

Verification rolls into each task's per-commit `dotnet build -c Release` + `dotnet test`. Final acceptance check (per §1 success criteria) is run after Task 5.

## 9. Known issues, accepted as out of scope

- **`RawRabbit.Enrichers.HttpContext` still has no functional implementation on net10** (Phase 1 V1 known issue carried forward). Not addressed in Phase 3.
- **`test/RawRabbit.IntegrationTests` is not exercised in Phase 3 acceptance** (requires live broker; Phase 6 turns it on). The AsyncLocal fix correctness will be validated in Phase 6.
- **Serilog 4.0 → 4.2+ transitive bump in the sample.** Within-major version float; backward-compatible. User-acknowledged on 2026-05-12 during brainstorming.
- **The shim deletes LibLog's auto-detection-via-reflection of NLog/Serilog/log4net/Loupe/Microsoft.Extensions.Logging.** Consumers must explicitly call `LogProvider.LoggerFactory = ...` to get logging output. The sample is the one in-tree consumer; explicitly wired in Task 3. Any out-of-tree consumer of RawRabbit will see no logs until they wire an `ILoggerFactory`. Consistent with the user-chosen "minimal call-site churn" direction. User-acknowledged on 2026-05-12.
