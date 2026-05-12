# Modernization Phase 3 Implementation Plan

> **For agentic workers:** REQUIRED: Use `superpowers:subagent-driven-development` to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Source spec:** `docs/specs/2026-05-12-modernization-phase-3-design.md` (commit SHA: `c60429a`)

**Goal:** After this plan executes, on a clean clone with the .NET 10 SDK installed, `dotnet restore && dotnet build -c Release && dotnet test test/RawRabbit.Tests --no-build -c Release && dotnet test test/RawRabbit.Enrichers.Polly.Tests --no-build -c Release && dotnet pack -c Release --no-build` succeeds end-to-end. Tests stay 30 passing / 9 skipped / 0 failed (Phase 1/2 baseline). `dotnet pack` produces 25 `.nupkg` files for src libraries; 0 from tests/samples. Warning shape: same as Phase 2 (NU1701/NU1902/NU1903 + xUnit analyzers; pack-time NU5048).

**Architecture:** Replace the 2391-line vendored `src/RawRabbit/Logging/LibLog.cs` single-file logging façade with an ~80-line shim over `Microsoft.Extensions.Logging.Abstractions` (same path, same `RawRabbit.Logging` namespace, same call-site shape — `interface ILog : ILogger` + `LogProvider.LoggerFactory` static + extension methods preserving `Info/Debug/Warn/Error/Trace/Fatal/IsXxxEnabled/XxxException`). Drop the now-orphan `LIBLOG_PORTABLE` define from RawRabbit.csproj and Compatibility.Legacy.csproj. Wire `ConsoleApp.Sample` to MEL via `SerilogLoggerFactory`. Drop dead `#if NETSTANDARD1_5/#elif NET451/#else return null;` conditionals in `GlobalExecutionIdRepository.cs` and `MessageContextRepository.cs` so AsyncLocal Get/Set actually works on net10.

**Tech stack (inherited from spec):** .NET 10 SDK 10.0.107 + C# 13 (default for net10); MSBuild Directory.Build.props + Directory.Packages.props from Phase 2; Microsoft.Extensions.Logging.Abstractions 10.0.0; Serilog.Extensions.Logging 10.0.0 (sample only). All other package versions stay at their Phase 1/2 values.

---

## File Structure

**Create:** none.

**Modify (8):**
- `Directory.Packages.props` — add 2 PackageVersion entries (Task 1)
- `src/RawRabbit/Logging/LibLog.cs` — replace 2391 lines with ~80-line shim (Task 2)
- `src/RawRabbit/RawRabbit.csproj` — remove LIBLOG_PORTABLE define on line 14, add MEL.Abstractions PackageReference (Task 2)
- `src/RawRabbit.Compatibility.Legacy/RawRabbit.Compatibility.Legacy.csproj` — remove orphan LIBLOG_PORTABLE define on line 14 (Task 2)
- `sample/RawRabbit.ConsoleApp.Sample/RawRabbit.ConsoleApp.Sample.csproj` — add Serilog.Extensions.Logging PackageReference (Task 3)
- `sample/RawRabbit.ConsoleApp.Sample/Program.cs` — add 2 using directives + 1 wiring line (Task 3)
- `src/RawRabbit.Enrichers.GlobalExecutionId/Dependencies/GlobalExecutionIdRepository.cs` — drop dead conditionals (Task 4)
- `src/RawRabbit.Enrichers.MessageContext/Dependencies/MessageContextRepository.cs` — drop dead conditionals (Task 5)

**Test:** No new test files. Acceptance is "Phase 1/2 test counts unchanged" (29/7/0 + 1/2/0 = 30/9/0).

---

## Inherited from spec

The following assumptions were verified by `thorough-brainstorming` at spec-write time and are NOT re-verified here. Trusted as ground truth:

- **A1:** `src/RawRabbit/Logging/LibLog.cs` exists, 2391 lines, namespace `RawRabbit.Logging` (LibLog vendored single-file façade)
- **A2:** `src/RawRabbit/RawRabbit.csproj:14` has `<DefineConstants>$(DefineConstants);LIBLOG_PORTABLE</DefineConstants>` on its own line
- **A3:** `src/RawRabbit.Compatibility.Legacy/RawRabbit.Compatibility.Legacy.csproj:14` carries an orphan LIBLOG_PORTABLE define; project has zero LibLog usages in `.cs` files
- **A4:** `Directory.Packages.props` exists at repo root with the Phase 2 structure
- **A5/A6:** `sample/RawRabbit.ConsoleApp.Sample/Program.cs` and `RawRabbit.ConsoleApp.Sample.csproj` exist
- **A7:** `GlobalExecutionIdRepository.cs:24` has `#else return null;` in Get(); Set() has no `#else` (silently no-ops on net10)
- **A8:** `MessageContextRepository.cs:37` has `#else return null;` in Get(); Set() and ctor have no `#else` (silently no-op on net10)
- **B1:** ~30 LibLog consumers use ONLY: `Info/Debug/Warn/Error/Trace/Fatal` + `XxxException` variants + 6 `IsXxxEnabled` + `LogProvider.For<T>()`. No `Func<string>` lazy-eval pattern. All other LibLog APIs (`_logger.Name`, raw `_logger.Log(`, `Logger.Write`, `Logger.Instance`, `LogProvider.IsLoggerAvailable`, `LogLevel.X` enum) live only inside LibLog.cs itself
- **C1:** LibLog.cs:44-53 carries 10 `[assembly: InternalsVisibleTo(...)]` declarations: Enrichers.RetryLater, Operations.{Get, MessageSequence, Publish, Request, Respond, StateMachine, Subscribe, Tools}, Enrichers.GlobalExecutionId. Shim must preserve all 10 verbatim
- **C2:** LibLog.cs is the ONLY .cs file in src/RawRabbit/ carrying `[assembly: InternalsVisibleTo(...)]`
- **C3:** Consumer-facing LibLog types are exactly `ILog` and `LogProvider`
- **D1:** Microsoft.Extensions.Logging.Abstractions 10.0.0 (released 11/11/2025) targets net10.0 explicitly; single direct dep on Microsoft.Extensions.DependencyInjection.Abstractions 10.0.0; ships ILogger, ILogger<T>, ILoggerFactory, LogLevel, LoggerExtensions, NullLogger, NullLogger<T>, NullLoggerFactory
- **D2:** Serilog.Extensions.Logging 10.0.0 still ships `public sealed class SerilogLoggerFactory : ILoggerFactory` with the `(ILogger? logger = null, bool dispose = false, ...)` ctor
- **D3:** Sample currently transitively gets Serilog 4.0.0 via Sinks.Console 6.1.1; SLog.Extensions.Logging 10.0.0 floor (Serilog ≥ 4.2.0) auto-resolves to 4.2.0+. Backward-compatible
- **D4:** MEL.Abstractions 10.0.0 transitive dep tree on net10 is clean (no NU1701/NU1902/NU1903)
- **E1:** Program.cs RunAsync() does Serilog setup before bus build; insertion point for the wiring line is between them
- **F1, F2:** No test in `test/RawRabbit.Tests` or `test/RawRabbit.Enrichers.Polly.Tests` references `GlobalExecutionIdRepository` / `MessageContextRepository`. (IntegrationTests references GlobalExecutionId but is out of Phase 3 acceptance per spec §1)
- **F4:** No other `.cs` file in src/ has the `#if NETSTANDARD1_5 / #elif NET451 / #else return null;` pattern
- **G1:** No `.cs` file outside src/RawRabbit/Logging/LibLog.cs gates code on `#if LIBLOG_PORTABLE`
- **I1:** Nothing in test/ or sample/ uses the LibLog public surface directly
- **J1:** No `<Nullable>` setting in csprojs/Build.props; the shim's `where TState : notnull` constraint is syntactically valid in C# 8+ regardless of nullable context

---

## Verified plan-level assumptions

Newly introduced by this plan (paths, signatures, commands, ordering, code-in-plan validity, consumer impact) and verified at plan-write time against HEAD `c60429a`:

| # | Category | Assumption | Evidence |
|---|---|---|---|
| P1 | File path / structure | `Directory.Packages.props` has a clean ItemGroup with 17 PackageVersion entries grouped by 3 comments; adding 2 entries is mechanical | `cat Directory.Packages.props` confirmed shape |
| P2 | File path / structure | `src/RawRabbit/RawRabbit.csproj` has a single `<ItemGroup>` with 2 PackageReferences (RabbitMQ.Client, Newtonsoft.Json) in CPM-style; adding MEL.Abstractions is mechanical; LIBLOG_PORTABLE define is on line 14 | `cat src/RawRabbit/RawRabbit.csproj` confirmed |
| P3 | File path / structure | `sample/RawRabbit.ConsoleApp.Sample/RawRabbit.ConsoleApp.Sample.csproj` has 3 ItemGroups; the third (PackageReferences) has 3 entries (Microsoft.Extensions.Configuration.Binder, ...Json, Serilog.Sinks.Console) all in CPM-style; adding Serilog.Extensions.Logging is mechanical | `cat sample/RawRabbit.ConsoleApp.Sample/RawRabbit.ConsoleApp.Sample.csproj` confirmed |
| P4 | Code-in-plan | C# 13 (default for .NET 10 SDK 10.0.107 per global.json; no `<LangVersion>` override anywhere) supports primary constructors (C# 12+); the shim's `internal sealed class LogWrapper(ILogger inner) : ILog` syntax compiles | `grep -rn "LangVersion"` returned 0 matches; `git log` shows commit `0a1f996 Fix net10/C#13 source compile errors` confirming C# 13 is in active use |
| P5 | Code-in-plan | Same C# 13 default supports target-typed `new()` (C# 9+) used in Tasks 4/5's `private static readonly AsyncLocal<string> ... = new();` field initializers | Same evidence as P4 |
| P6 | Command | Commit message convention is short imperative single-line subject, no Conventional-Commits prefix | `git log --oneline -15` confirmed consistent style across Phase 1/2/3-spec commits |
| P7 | Task ordering | (a) Task 2's `<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />` requires Task 1's central PackageVersion entry (NU1010 if missed); (b) Task 3's wiring `LogProvider.LoggerFactory = ...` references the new shim's static property which only exists after Task 2; (c) Task 3's `<PackageReference Include="Serilog.Extensions.Logging" />` requires Task 1; (d) Tasks 4, 5 touch independent enricher files with no logging dep — fully independent of 1-3 and of each other; (e) Task 6 verifies cumulative state | Logical analysis against task code blocks + the central package management rules in spec A4/D1 |
| P8 | Command | `dotnet test ... --blame-hang-timeout 15s` flag works on .NET 10 SDK 10.0.107's xunit runner | Phase 2 plan's Task 6 + spec §4 used this exact flag and it worked |
| P9 | Code-in-plan | Post-Phase-2, in-sln csprojs use `<PackageReference Include="X" />` shape (no inline Version); CPM resolves versions via Directory.Packages.props. Tasks 2 and 3 add new PackageReferences in this same shape | `cat src/RawRabbit/RawRabbit.csproj` and `cat sample/RawRabbit.ConsoleApp.Sample/RawRabbit.ConsoleApp.Sample.csproj` confirmed |
| P10 | Consumer impact | Removing `LIBLOG_PORTABLE` from `Compatibility.Legacy.csproj` is safe — no `.cs` file in `src/RawRabbit.Compatibility.Legacy/` gates on it | `grep -rln "LIBLOG_PORTABLE" src/RawRabbit.Compatibility.Legacy --include="*.cs"` returned 0 matches |
| P11 | Consumer impact | NuGet auto-resolution of Serilog 4.0 → 4.2+ in the sample (forced by SLog.Extensions.Logging 10.0's floor) does NOT conflict with Serilog.Sinks.Console 6.1.1's dependency range | nuget.org page for Sinks.Console 6.1.1 shows `Serilog (>= 4.0.0)` with NO upper bound; max-floor resolution to 4.2+ is safe |

---

## Tasks

### Task 1: Add MEL.Abstractions + Serilog.Extensions.Logging to central PackageVersions

**Files:**
- Modify: `Directory.Packages.props`

- [ ] **Step 1: Add 2 new `<PackageVersion>` entries to the existing `<ItemGroup>` in `Directory.Packages.props`.** Place `Microsoft.Extensions.Logging.Abstractions` after the existing "Library deps" group (group it under a new `<!-- Logging (Phase 3) -->` comment). Place `Serilog.Extensions.Logging` after the existing `Serilog.Sinks.Console` line (in the "ConsoleApp.Sample deps" group). The 2 lines:
  ```xml
  <!-- Logging (Phase 3) -->
  <PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.0" />
  ```
  and
  ```xml
  <PackageVersion Include="Serilog.Extensions.Logging" Version="10.0.0" />
  ```
  Do NOT change any existing PackageVersion entries.

- [ ] **Step 2: Verify restore still works**
  ```bash
  dotnet restore
  ```
  Expect: 0 errors. Same NU1701/NU1902/NU1903 warnings as Phase 2 baseline. The 2 new central entries are inert because no csproj references them yet.

- [ ] **Step 3: Commit**
  ```bash
  git add Directory.Packages.props
  git commit -m "Add MEL.Abstractions and Serilog.Extensions.Logging to central PackageVersions"
  ```

### Task 2: Replace LibLog with Microsoft.Extensions.Logging.Abstractions shim (atomic)

This task is **atomic by build necessity**: deleting LibLog.cs without the shim breaks ~30 consumers; adding the shim while LibLog.cs still exists creates type clashes (both define `RawRabbit.Logging.ILog`); removing `LIBLOG_PORTABLE` from RawRabbit.csproj while LibLog.cs still references it (`#if !LIBLOG_PORTABLE using System.Runtime.CompilerServices; #endif`) breaks compilation. All 3 file changes commit together.

**Files:**
- Modify: `src/RawRabbit/Logging/LibLog.cs` (replace 2391-line file with ~80-line shim)
- Modify: `src/RawRabbit/RawRabbit.csproj` (delete LIBLOG_PORTABLE define on line 14; add MEL.Abstractions PackageReference)
- Modify: `src/RawRabbit.Compatibility.Legacy/RawRabbit.Compatibility.Legacy.csproj` (delete orphan LIBLOG_PORTABLE define on line 14)

- [ ] **Step 1: Replace `src/RawRabbit/Logging/LibLog.cs` with the shim.** Full file content (preserves all 10 `[assembly: InternalsVisibleTo(...)]` declarations from the original at lines 44-53):
  ```csharp
  // Vendored shim replacing the LibLog single-file facade with a thin layer over
  // Microsoft.Extensions.Logging.Abstractions. Phase 3 of the modernization.

  using System;
  using Microsoft.Extensions.Logging;
  using Microsoft.Extensions.Logging.Abstractions;

  [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RawRabbit.Enrichers.RetryLater")]
  [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RawRabbit.Operations.Get")]
  [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RawRabbit.Operations.MessageSequence")]
  [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RawRabbit.Operations.Publish")]
  [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RawRabbit.Operations.Request")]
  [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RawRabbit.Operations.Respond")]
  [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RawRabbit.Operations.StateMachine")]
  [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RawRabbit.Operations.Subscribe")]
  [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RawRabbit.Operations.Tools")]
  [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RawRabbit.Enrichers.GlobalExecutionId")]

  namespace RawRabbit.Logging
  {
      public interface ILog : ILogger { }

      public static class LogProvider
      {
          public static ILoggerFactory LoggerFactory { get; set; } = NullLoggerFactory.Instance;

          public static ILog For<T>() => new LogWrapper(LoggerFactory.CreateLogger<T>());
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
          public static void Info (this ILog l, string m, params object[] a) => l.LogInformation(m, a);
          public static void Debug(this ILog l, string m, params object[] a) => l.LogDebug      (m, a);
          public static void Warn (this ILog l, string m, params object[] a) => l.LogWarning    (m, a);
          public static void Error(this ILog l, string m, params object[] a) => l.LogError      (m, a);
          public static void Trace(this ILog l, string m, params object[] a) => l.LogTrace      (m, a);
          public static void Fatal(this ILog l, string m, params object[] a) => l.LogCritical   (m, a);

          public static void Info (this ILog l, Exception ex, string m, params object[] a) => l.LogInformation(ex, m, a);
          public static void Debug(this ILog l, Exception ex, string m, params object[] a) => l.LogDebug      (ex, m, a);
          public static void Warn (this ILog l, Exception ex, string m, params object[] a) => l.LogWarning    (ex, m, a);
          public static void Error(this ILog l, Exception ex, string m, params object[] a) => l.LogError      (ex, m, a);
          public static void Trace(this ILog l, Exception ex, string m, params object[] a) => l.LogTrace      (ex, m, a);
          public static void Fatal(this ILog l, Exception ex, string m, params object[] a) => l.LogCritical   (ex, m, a);

          public static void InfoException (this ILog l, string m, Exception ex, params object[] a) => l.LogInformation(ex, m, a);
          public static void DebugException(this ILog l, string m, Exception ex, params object[] a) => l.LogDebug      (ex, m, a);
          public static void WarnException (this ILog l, string m, Exception ex, params object[] a) => l.LogWarning    (ex, m, a);
          public static void ErrorException(this ILog l, string m, Exception ex, params object[] a) => l.LogError      (ex, m, a);
          public static void TraceException(this ILog l, string m, Exception ex, params object[] a) => l.LogTrace      (ex, m, a);
          public static void FatalException(this ILog l, string m, Exception ex, params object[] a) => l.LogCritical   (ex, m, a);

          public static bool IsInfoEnabled (this ILog l) => l.IsEnabled(LogLevel.Information);
          public static bool IsDebugEnabled(this ILog l) => l.IsEnabled(LogLevel.Debug);
          public static bool IsWarnEnabled (this ILog l) => l.IsEnabled(LogLevel.Warning);
          public static bool IsErrorEnabled(this ILog l) => l.IsEnabled(LogLevel.Error);
          public static bool IsTraceEnabled(this ILog l) => l.IsEnabled(LogLevel.Trace);
          public static bool IsFatalEnabled(this ILog l) => l.IsEnabled(LogLevel.Critical);
      }
  }
  ```

- [ ] **Step 2: Modify `src/RawRabbit/RawRabbit.csproj`.**
  - Delete line 14: `<DefineConstants>$(DefineConstants);LIBLOG_PORTABLE</DefineConstants>`
  - In the existing `<ItemGroup>` (currently containing 2 PackageReferences), add a third entry:
    ```xml
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
    ```
  Do NOT touch any other lines (preserve `Description`, `AssemblyTitle`, `VersionPrefix`, `Authors`, `AssemblyName`, `PackageId`, `PackageTags`, the 3 `GenerateAssembly*` flags, and the 2 existing PackageReferences with their Include-only shape).

- [ ] **Step 3: Modify `src/RawRabbit.Compatibility.Legacy/RawRabbit.Compatibility.Legacy.csproj`.**
  - Delete line 14 (the orphan `<DefineConstants>$(DefineConstants);LIBLOG_PORTABLE</DefineConstants>`).
  Do NOT touch any other lines.

- [ ] **Step 4: Verify build green**
  ```bash
  dotnet build -c Release
  ```
  Expect: 0 errors. ~197 warnings (Phase 2 baseline; warning count may shift slightly because LibLog.cs no longer compiles — this is expected).

- [ ] **Step 5: Verify tests still green**
  ```bash
  dotnet test test/RawRabbit.Tests --no-build -c Release --blame-hang-timeout 15s
  dotnet test test/RawRabbit.Enrichers.Polly.Tests --no-build -c Release
  ```
  Expect: 29 passed / 7 skipped / 0 failed (RawRabbit.Tests); 1 passed / 2 skipped / 0 failed (Polly.Tests). Total 30/9/0.

- [ ] **Step 6: Commit (atomic — all 3 file changes together)**
  ```bash
  git add src/RawRabbit/Logging/LibLog.cs src/RawRabbit/RawRabbit.csproj src/RawRabbit.Compatibility.Legacy/RawRabbit.Compatibility.Legacy.csproj
  git commit -m "Replace LibLog vendored facade with Microsoft.Extensions.Logging.Abstractions shim"
  ```

### Task 3: Wire ConsoleApp.Sample to MEL via Serilog adapter

**Files:**
- Modify: `sample/RawRabbit.ConsoleApp.Sample/RawRabbit.ConsoleApp.Sample.csproj` (+1 PackageReference)
- Modify: `sample/RawRabbit.ConsoleApp.Sample/Program.cs` (+2 using directives, +1 wiring line)

- [ ] **Step 1: Modify the sample csproj.** In the third `<ItemGroup>` (the one containing the existing 3 PackageReferences for Microsoft.Extensions.Configuration.Binder, ...Json, Serilog.Sinks.Console), add a fourth entry:
  ```xml
  <PackageReference Include="Serilog.Extensions.Logging" />
  ```
  Do NOT touch any other lines.

- [ ] **Step 2: Modify `sample/RawRabbit.ConsoleApp.Sample/Program.cs`.** Add 2 using directives at the top of the file (alphabetically after the existing `using RawRabbit.*` block and the existing `using Serilog;` line):
  ```csharp
  using RawRabbit.Logging;
  using Serilog.Extensions.Logging;
  ```

- [ ] **Step 3: In `Program.cs RunAsync()`, insert the wiring line.** After the existing block:
  ```csharp
  Log.Logger = new LoggerConfiguration()
      .WriteTo.Console()
      .CreateLogger();
  ```
  and BEFORE the next statement (`_client = RawRabbitFactory.CreateSingleton(...)`), insert:
  ```csharp

  LogProvider.LoggerFactory = new SerilogLoggerFactory(Log.Logger);
  ```
  (Blank line above the new line for readability.)

- [ ] **Step 4: Verify the sample builds**
  ```bash
  dotnet build sample/RawRabbit.ConsoleApp.Sample -c Release
  ```
  Expect: 0 errors. The sample isn't run; just compiled.

- [ ] **Step 5: Verify the broader build is still green** (the new Serilog.Extensions.Logging transitively bumps Serilog 4.0 → 4.2+; verify Sinks.Console still works)
  ```bash
  dotnet build -c Release
  ```
  Expect: 0 errors. Warning shape stable.

- [ ] **Step 6: Commit**
  ```bash
  git add sample/RawRabbit.ConsoleApp.Sample/RawRabbit.ConsoleApp.Sample.csproj sample/RawRabbit.ConsoleApp.Sample/Program.cs
  git commit -m "Wire ConsoleApp.Sample to MEL via SerilogLoggerFactory adapter"
  ```

### Task 4: Fix GlobalExecutionId AsyncLocal Get/Set on net10

**Files:**
- Modify: `src/RawRabbit.Enrichers.GlobalExecutionId/Dependencies/GlobalExecutionIdRepository.cs` (38 → ~20 lines)

- [ ] **Step 1: Replace the entire file content with the simplified version.** The file becomes:
  ```csharp
  using System.Threading;

  namespace RawRabbit.Enrichers.GlobalExecutionId.Dependencies
  {
      public class GlobalExecutionIdRepository
      {
          private static readonly AsyncLocal<string> GlobalExecutionId = new();

          public static string Get()
          {
              return GlobalExecutionId.Value;
          }

          public static void Set(string id)
          {
              GlobalExecutionId.Value = id;
          }
      }
  }
  ```
  Changes from current: drop `#if NET451 using System.Runtime.Remoting.Messaging; #endif` block at top; drop conditional field declaration (NETSTANDARD1_5 / NET451 branches); drop conditional Get() and Set() bodies; field initializer uses target-typed `new()`.

- [ ] **Step 2: Verify build green**
  ```bash
  dotnet build -c Release
  ```
  Expect: 0 errors. Warning shape stable.

- [ ] **Step 3: Commit**
  ```bash
  git add src/RawRabbit.Enrichers.GlobalExecutionId/Dependencies/GlobalExecutionIdRepository.cs
  git commit -m "Fix GlobalExecutionIdRepository to use AsyncLocal on net10"
  ```

### Task 5: Fix MessageContext AsyncLocal Get/Set on net10

**Files:**
- Modify: `src/RawRabbit.Enrichers.MessageContext/Dependencies/MessageContextRepository.cs` (51 → ~25 lines)

- [ ] **Step 1: Replace the entire file content with the simplified version.** The file becomes:
  ```csharp
  using System.Threading;

  namespace RawRabbit.Enrichers.MessageContext.Dependencies
  {
      public interface IMessageContextRepository
      {
          object Get();
          void Set(object context);
      }

      public class MessageContextRepository : IMessageContextRepository
      {
          private readonly AsyncLocal<object> _msgContext = new();

          public object Get()
          {
              return _msgContext.Value;
          }

          public void Set(object context)
          {
              _msgContext.Value = context;
          }
      }
  }
  ```
  Changes from current: drop `#if NET451 using System.Runtime.Remoting.Messaging; #endif`; collapse conditional field declaration to single unconditional with target-typed `new()`; remove now-empty parameterless ctor (field initializer covers it); drop conditional Get() and Set() bodies.

- [ ] **Step 2: Verify build + tests still green**
  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Tests --no-build -c Release --blame-hang-timeout 15s
  dotnet test test/RawRabbit.Enrichers.Polly.Tests --no-build -c Release
  ```
  Expect: 0 errors; 30/9/0 tests.

- [ ] **Step 3: Commit**
  ```bash
  git add src/RawRabbit.Enrichers.MessageContext/Dependencies/MessageContextRepository.cs
  git commit -m "Fix MessageContextRepository to use AsyncLocal on net10"
  ```

### Task 6: Verify acceptance per spec §1

**Files:** none modified; verification only.

- [ ] **Step 1: Confirm SDK selection**
  ```bash
  dotnet --version
  ```
  Expect: `10.0.107` (driven by `global.json`).

- [ ] **Step 2: Restore**
  ```bash
  dotnet restore
  ```
  Expect: 0 errors. Warnings: NU1701 + NU1902 + NU1903 (Phase 1/2 baseline) only.

- [ ] **Step 3: Build**
  ```bash
  dotnet build -c Release
  ```
  Expect: 0 errors. Warning shape: same as Phase 2 (~197 warnings; possibly slightly fewer NU1701 since LibLog.cs no longer compiles).

- [ ] **Step 4: Tests stay green**
  ```bash
  dotnet test test/RawRabbit.Tests --no-build -c Release --blame-hang-timeout 15s
  dotnet test test/RawRabbit.Enrichers.Polly.Tests --no-build -c Release
  ```
  Expect: 29 passed / 7 skipped / 0 failed (RawRabbit.Tests); 1 passed / 2 skipped / 0 failed (Polly.Tests). Total 30/9/0 — matches Phase 1/2 baseline.

- [ ] **Step 5: Pack works**
  ```bash
  dotnet pack -c Release --no-build
  ```
  Expect: 0 errors. Pack-time NU5048 (deprecated PackageIconUrl) warnings expected per spec §1 / Phase 2 spec §2.3 — these are acceptable.
  ```bash
  find . -path "*/bin/Release/*.nupkg" -not -path "*/AspNet.Sample/*" -not -path "*/PerformanceTest/*" | wc -l
  ```
  Expect: `25`.

- [ ] **Step 6: LIBLOG_PORTABLE define cleanup verification**
  ```bash
  grep -c "LIBLOG_PORTABLE" src/RawRabbit/RawRabbit.csproj src/RawRabbit.Compatibility.Legacy/RawRabbit.Compatibility.Legacy.csproj
  ```
  Expect: `0` for both files (was 1/1 before Task 2).

- [ ] **Step 7: LibLog.cs replacement size verification**
  ```bash
  wc -l src/RawRabbit/Logging/LibLog.cs
  ```
  Expect: ~80 lines (was 2391). The exact count depends on exact whitespace in the shim implementation; anything under ~120 confirms replacement landed.

- [ ] **Step 8: AsyncLocal fix verification**
  ```bash
  grep -c "#if NETSTANDARD1_5\|#elif NET451\|#else" src/RawRabbit.Enrichers.GlobalExecutionId/Dependencies/GlobalExecutionIdRepository.cs src/RawRabbit.Enrichers.MessageContext/Dependencies/MessageContextRepository.cs
  ```
  Expect: `0` per file (dead conditionals removed).

- [ ] **Step 9: If all eight steps pass, Phase 3 is complete.** No commit for this task — pure verification. If any step fails, consult spec §5 (Risks & responses) for the recovery template.

---

## Tasks NOT in this plan

Inherited verbatim from spec §1 ("Out-of-scope for 'phase 3 acceptance'") and §7 ("Tasks NOT in this plan"):

- AspNet.Sample wiring (dropped-from-sln; Phase 6/7 territory)
- `RawRabbit.Enrichers.HttpContext` restoration (Phase 1 V1 known issue; later phase)
- Any `.cs` source touch in broker layer (Phase 5 owns RabbitMQ.Client 7.x async rewrite)
- The 9 `[Fact(Skip="...")]` broker-layer tests (Phase 5/7)
- `test/RawRabbit.IntegrationTests` execution (requires live broker; Phase 6 turns it on)
- Bumping any package major version other than the explicit additions in the spec's §3 (MEL.Abstractions, Serilog.Extensions.Logging, transitive Serilog 4.0 → 4.2+) — Phases 4/5/7
- Converting middleware fields from static `LogProvider.For<T>()` to ctor-injected `ILogger<T>` (user picked the static-shim path during brainstorming)
- Normalizing the heterogeneous `Authors` values across src csprojs (Phase 2 §2.3 deferred; cosmetic, no-publish intent)
- `Compatibility.Legacy` `<VersionPrefix>` addition (cosmetic, Phase 2 §2.3)
- Adding `<GenerateAssembly*>` flags to MessagePack/Protobuf/ZeroFormatter (cosmetic IL metadata, Phase 2 §2.3)
- CI on GitHub Actions (Phase 6)

A new spec → new plan cycle is required to add any of these.

## Known issues inherited from spec

In addition to Phase 1's accepted known issue (`RawRabbit.Enrichers.HttpContext` compiles to empty pass-through on net10) and Phase 2's heterogeneous metadata acceptances, Phase 3 explicitly accepts:

- **`RawRabbit.Enrichers.HttpContext` still has no functional implementation on net10** (Phase 1 V1 known issue carried forward). Not addressed in Phase 3.
- **`test/RawRabbit.IntegrationTests` is not exercised in Phase 3 acceptance** (requires live broker; Phase 6 turns it on). The AsyncLocal fix correctness will be validated in Phase 6.
- **Serilog 4.0 → 4.2+ transitive bump in the sample.** Within-major version float; backward-compatible. User-acknowledged on 2026-05-12 during brainstorming.
- **The shim deletes LibLog's auto-detection-via-reflection of NLog/Serilog/log4net/Loupe/Microsoft.Extensions.Logging.** Consumers must explicitly call `LogProvider.LoggerFactory = ...` to get logging output. The sample is the one in-tree consumer; explicitly wired in Task 3. Any out-of-tree consumer of RawRabbit will see no logs until they wire an `ILoggerFactory`. Consistent with the user-chosen "minimal call-site churn" direction. User-acknowledged on 2026-05-12.

User-acknowledged on 2026-05-12 during brainstorming.
