# Modernization Phase 4.5 Wave 2 Design — Pure-Logic Coverage

**Source:** Phase 4.5 parent spec at `docs/specs/2026-05-13-modernization-phase-4.5-design.md` (committed at `9b5837d` on 2026-05-13). Wave 2 is the second of five waves; Wave 1 shipped at `9af8650` on `origin/2.0`.

**Inherits from parent spec (do NOT re-litigate here):**
- §1 in/out-of-scope source areas; §2 test project structure (D8); §3 conventions; §4 A25 forbidden patterns; §5 per-area gate format; §6 wave structure (Wave 2 row); §7 phase-wide acceptance; §8 decisions D1–D10; §9 risks; §10 validation shape; §13 known-issues acknowledgements.
- Wave 1 lessons (BOM preservation, AAA-deletion style, `Assert.ThrowsAnyAsync` for subclass cancellations) — captured as conventions in §3 below.

**Goal:** After Wave 2 ships, the 8 sub-areas listed in §1 each have ≥1 happy + ≥1 error path test per public method (per §1 strict-granularity decision). Wave 2 lands ~64-122 net-new passing tests across 5 source areas (DI ×3, Common gap-fill, Configuration + 7 sub-types, Exceptions, Logging, Pipe). Wave 2 introduces zero new mocking patterns beyond the parent spec's D3; pure-logic isolation means most tests construct objects directly and assert on output, with `Mock<IPipeContext>` reserved for the Pipe extension methods that consume one.

**Tech stack (inherited):** .NET 10 SDK 10.0.107 + C# 13; xUnit 2.9.3; xunit.runner.visualstudio 2.8.2; Microsoft.NET.Test.Sdk 18.5.1; Moq 4.20.72. No new package dependencies for Wave 2.

---

## 1. Scope

Wave 2 = 5 areas from parent spec §6 Wave 2 row, expanded to 8 sub-areas at the test-landing level (DI splits into 3; Configuration root + 7 sub-types).

**Per-method strict interpretation** (decision in this brainstorm): "Public API" = public method on a public class. Each public method gets ≥1 happy + ≥1 error test, except where multiple methods share enough input shape that `[Theory]/[InlineData]` consolidation reads more naturally (per parent spec D6). Trivial getters/setters and primary ctors of pure data types do NOT each get tests; assertions on data round-trip via the methods that consume them are sufficient.

| # | Area | Source files | Test landing location | Test-shape pattern (per parent §5 + this wave's strict-per-method) |
|---|------|-------------:|----------------------|--------------------|
| 1 | DI Autofac | `src/RawRabbit.DependencyInjection.Autofac/` (3 files: `ContainerBuilderExtension.cs`, `ContainerBuilderAdapter.cs`, `ComponentContextAdapter.cs`) | `test/RawRabbit.Tests/DependencyInjection/Autofac/` (NEW subdir under existing test project per D8 + parent §13.1 item 2) | (a) `Register*` extension produces resolvable `IBusClient`; (b) lifetimes match documented intent; (c) custom registration overrides honored; (d) DI defects throw with descriptive message |
| 2 | DI Ninject | `src/RawRabbit.DependencyInjection.Ninject/` (3 files: `KernelExtension.cs`, `NinjectAdapter.cs`, `RawRabbitModule.cs`) | `test/RawRabbit.Tests/DependencyInjection/Ninject/` (NEW subdir per D8) | Same shape as Autofac |
| 3 | DI ServiceCollection | `src/RawRabbit.DependencyInjection.ServiceCollection/` (3 files: `AddRawRabbitExtension.cs`, `ServiceCollectionAdapter.cs`, `ServiceProviderAdapter.cs`) | `test/RawRabbit.DependencyInjection.ServiceCollection.Tests/` (Wave-1-scaffolded NEW project; root) | Same shape as Autofac |
| 4 | Common gap-fill | `src/RawRabbit/Common/` (11 files beyond the 2 already covered: `Acknowledgement`, `ClientPropertyProvider`, `ExclusiveLock`, `IDictionaryExtensions`, `PropertyHeaders`, `QueueArgument`, `ResourceDisposer`, `TaskUtil`, `TopologyProvider`, `Truncator`, `TypeExtensions`) | `test/RawRabbit.Tests/Common/` (existing subdir; NEW test files alongside the 2 existing) | Per public method ≥1 happy + ≥1 error; data-only types (`Acknowledgement`, `PropertyHeaders`, `QueueArgument`) covered indirectly via consumers if no methods exist; plan-write enumeration filters |
| 5 | Configuration root + 7 sub-types | `src/RawRabbit/Configuration/RawRabbitConfiguration.cs` + 7 sub-type subdirs (`BasicPublish/`, `Consume/`, `Consumer/`, `ExchangeDeclare/`, `Get/`, `Publisher/`, `QueueDeclare/`) | `test/RawRabbit.Tests/Configuration/` (NEW subdir; 7 sub-type subdirs mirror src structure) | Per sub-type: builder defaults; parser correctness (where applicable); override merging; factory output (where applicable). Root: `RawRabbitConfiguration` instantiation + property defaults |
| 6 | Exceptions | `src/RawRabbit/Exceptions/` (4 files: `ChannelAvailabilityException`, `MessageHandlerException`, `PublishConfirmException`, `ExceptionInformation`) | `test/RawRabbit.Tests/Exceptions/` (NEW subdir) | Per exception: ctor + serialization round-trip (binary or System.Text.Json depending on ISerializable). `ExceptionInformation` if it has testable behavior beyond data |
| 7 | Logging | `src/RawRabbit/Logging/LibLog.cs` (1 file, 73 lines, Phase 3 modernization shim) | `test/RawRabbit.Tests/Logging/` (NEW subdir) | `LogProvider.For<T>()` returns wrapped logger; `LogProvider.LoggerFactory` setter accepts factory and propagates; `ILog : ILogger` conformance |
| 8 | Pipe (3 types: builder, factory, IPipeContext extensions) | `src/RawRabbit/Pipe/PipeBuilder.cs`, `Pipe/PipeBuilderFactory.cs`, and IPipeContext extensions split across `Pipe/PipeContextExtension.cs`, `Pipe/PipeContextGetExtension.cs`, `Pipe/AddPropertyPipeContextExtensions.cs` | `test/RawRabbit.Tests/Pipe/` (NEW subdir; not the `Middleware/` sub — that's Wave 3) | `PipeBuilder` ordering (stages, register, replace); `PipeBuilderFactory` resolution; `IPipeContext` extension add/get/remove correctness for the ~19 typed extensions |

**Out of scope for Wave 2** (deferred to other waves per parent spec):
- `src/RawRabbit/Pipe/Middleware/` — Wave 3 (broker-touching middleware uses one wave's pattern hardening)
- `src/RawRabbit/Channel/`, `Consumer/`, `Subscription/`, `Instantiation/`, `Instantiation/Disposable/`, `BusClient.cs`, `IBusClient.cs` — Wave 3 (broker-touching)
- `src/RawRabbit/DependencyInjection/` (the core's own DI sub-area) — Wave 3 (distinct from the 3 separate DI projects covered in Wave 2)
- `src/RawRabbit/Serialization/` — already covered (Wave 1 added 2 backfill tests; gap-fill if any deferred to plan-write inspection)
- `src/RawRabbit.Operations.*` (8 areas) — Wave 4
- `src/RawRabbit.Enrichers.*` (5 + Polly) — Wave 5
- All inherited out-of-scope from parent spec §1 (IntegrationTests, PerformanceTest, MessagePack/Protobuf/ZeroFormatter/HttpContext, Compatibility.Legacy)

---

## 2. Test count target (lower bound = acceptance gate)

Per area, lower-bound estimate (plan-write verification will tighten exact counts):

| # | Area | Public-method count est. | Min tests (1 happy + 1 error per method) | `[Theory]`-eligible groupings est. |
|---|------|------:|------:|------:|
| 1 | DI Autofac | ~3-5 | ~6-12 | 0-1 |
| 2 | DI Ninject | ~3-5 | ~6-12 | 0-1 |
| 3 | DI ServiceCollection | ~3-5 | ~6-12 | 0-1 |
| 4 | Common gap-fill (testable subset of 11 files; ~6-8 likely have testable methods) | ~6-12 | ~12-22 | 0-2 |
| 5 | Configuration root + 7 sub-types (8 testable units × 2-4 methods each) | ~12-24 | ~16-32 | 1-3 (parametric defaults across sub-types) |
| 6 | Exceptions (3 real exceptions × 2-3 tests + `ExceptionInformation` if testable) | ~3-5 | ~6-10 | 1 (parametric ctor coverage) |
| 7 | Logging (LibLog only — `LogProvider.For<T>()` + LoggerFactory setter + `ILog` interface conformance) | ~2-3 | ~2-4 | 0 |
| 8 | Pipe (`PipeBuilder` + `PipeBuilderFactory` + ~19 IPipeContext extension methods) | ~5-9 | ~10-18 | 1 (parametric extension coverage) |
| | **Total** | **~37-68** | **~64-122** | **~3-9** |

**Acceptance gate: ≥64 net-new passing tests added by Wave 2 across the 8 sub-areas.**

Plan-write verification will (a) read each source file, (b) enumerate exact public methods per area, (c) lock the per-area test count and method names. If a per-area count would land below the lower bound after enumeration (e.g., area 7 Logging has fewer testable methods than estimated), the plan documents the variance and the area's gate becomes "implement N tests as enumerated" with N being the verified count.

---

## 3. Conventions reaffirmed (from parent spec §3 + Wave 1 lessons)

Inherited from parent spec §3 — applied to all new tests:
- Test framework: xUnit 2.9.3
- Mock library: Moq 4.20.72
- Test class naming: `<TypeUnderTest>Tests`
- Test method naming: `Should_Verb_Subject` PascalCase with underscores
- File layout: one subdir per source area mirroring `src/`
- `[Theory]/[InlineData]`: required where 3+ similar tests differ only in input/expected
- `IClassFixture<T>`: allowed for shared setup that's expensive per-test (not required)
- Async tests: `async Task` + `await`, never `.Wait()` / `.Result`
- AAA: blank-line separation; no `/* Setup */ /* Test */ /* Assert */` comments
- Exception assertions: `Assert.Throws<T>` / `await Assert.ThrowsAsync<T>` / `await Assert.ThrowsAnyAsync<T>` (see Wave 1 lesson below)
- Type assertions: generic `Assert.IsType<T>(obj)` only
- Mocking: `new Mock<T>()` + `.Setup(...)/.Returns(...)` + `.Verify(...)`. Mock at `IPipeContext` boundary for area 8 Pipe extensions; otherwise direct construction (no broker boundary touched in Wave 2).
- Fixture types: file-local `internal` POCOs
- Indentation: TAB

**New conventions captured from Wave 1 (apply going forward):**
- **BOM preservation:** any source/test file with a UTF-8 BOM keeps it. The harness `Edit` tool may strip BOMs as a side effect; restore with `printf '\xef\xbb\xbf' | cat - file > tmp && mv tmp file` before commit. Wave 1's final cleanup commit `9af8650` set this precedent.
- **AAA-comment removal style:** delete the AAA line entirely; do NOT replace with a blank line. Replacing creates double-blank gaps inside method bodies (Wave 1's Task 6 hit this; the cleanup commit `9af8650` collapsed them).
- **`Assert.ThrowsAnyAsync<T>` over `Assert.ThrowsAsync<T>`** when the actual exception is a subclass of T. Specifically: `CancellationTokenSource`-driven cancellations throw `TaskCanceledException : OperationCanceledException`; xUnit's `ThrowsAsync<T>` does exact-type match and rejects the subclass. Use `ThrowsAnyAsync<OperationCanceledException>` to preserve the original `catch (OperationCanceledException)` subclass-tolerant semantic. Wave 1's Task 5 commit `2de09ea` set this precedent.

---

## 4. Wave 2 acceptance gates (Wave 2 done when ALL hold)

1. **Test count:** ≥64 net-new passing tests across the 8 sub-areas (lower bound from §2).
2. **Build clean:** `dotnet build -c Release` returns 0 errors. Warning shape stable from Wave 1 close (~114 warnings; Wave 2 may incidentally add 0-N warnings on new test code; if any new warnings appear in Wave 2 files they MUST be addressed before commit).
3. **Per-area enumerated tests:** the per-area test method names locked at plan-write time all land and pass.
4. **`RawRabbit.Tests` aggregate:** `dotnet test test/RawRabbit.Tests --no-build` returns ≥99 passed (was 41 post-Wave-1; +≥58 from Wave 2 areas 1, 2, 4, 5, 6, 7, 8 = 6+6+12+16+6+2+10). Skipped count remains 7.
5. **`ServiceCollection.Tests` aggregate:** `dotnet test test/RawRabbit.DependencyInjection.ServiceCollection.Tests --no-build` returns ≥6 passed (the new project's first tests; area 3). Skipped count = 0.
6. **xUnit analyzer warnings:** xUnit2020/2004/2007/1031 warnings = 0 in all Wave 2 new files (Wave 1's clean state preserved; Wave 2 must not regress).
7. **Skipped count unchanged:** aggregate skipped = 9 across all unit-test projects (the Phase 5/7-tagged tests; Wave 2 does NOT un-skip any).
8. **`[Theory]` use exercised in ≥1 Wave 2 area** (parent spec phase-wide ≥3 already satisfied by Wave 1's 3 uses; Wave 2 lower bar is "exercise the convention in this wave").
9. **Convention adherence:** new test files have BOM preservation correct (UTF-8 BOM where applicable), no double-blank gaps from AAA removal, no `Assert.ThrowsAsync<T>` against a subclass.

---

## 5. Wave-2-specific decisions

| # | Decision | Rationale | Alternatives considered |
|---|----------|-----------|-------------------------|
| DW1 | DI sub-areas split across landing locations: Autofac + Ninject under `test/RawRabbit.Tests/DependencyInjection/<provider>/`; ServiceCollection in its own Wave-1-scaffolded project | Per parent spec D8 + §13.1 item 2: existing `RawRabbit.Tests` ProjectReferences include Autofac + Ninject (verified in this wave's verification: `RawRabbit.Tests.csproj` lines 15-17 confirm); ServiceCollection ProjectReference deliberately absent (D8 inconsistency preserved); ServiceCollection's tests therefore live in the new sibling project | Move all 3 DI tests to separate projects (would gold-plate by adding ServiceCollection-style separation for Autofac/Ninject; rejected by D8); consolidate all 3 in `RawRabbit.Tests` (would require adding ServiceCollection ProjectReference; D8 inconsistency intentional) |
| DW2 | Configuration's 7 sub-types each get their own test subdirectory mirroring `src/` structure (8 subdirs total: 1 root + 7 sub-types) | Parent spec §3 file-layout convention ("one subdirectory per source area, mirroring `src/`"); preserves navigability (test layout = source layout) | Single flat `Configuration/` test subdir with sub-type prefix in filename (less navigable); single test file per sub-type (acceptable but verbose if a sub-type has many testable units) |
| DW3 | Exception ctor tests use `[Theory]` with `[InlineData]` per ctor signature where multiple ctor overloads share shape (likely 1 `[Theory]` consolidating 3 exceptions sharing `(string message, Exception inner)` shape) | Per parent spec D6 + Wave 1 precedent (Task 3 cluster A pattern). Reduces duplication; `[InlineData]` rows describe the variety | One `[Fact]` per exception per ctor overload (more verbose; loses convention-exercise) |
| DW4 | Pipe sub-area scope = `PipeBuilder` + `PipeBuilderFactory` + IPipeContext extensions (3 extension classes); `Pipe/Middleware/` sub-area deferred to Wave 3 | Parent spec §6 Wave 2 row separates these (Wave 2 = builder/factory/context; Wave 3 = built-in middleware). Verification confirmed: `src/RawRabbit/Pipe/` has 9 root-level `.cs` files matching parent's intent + a `Middleware/` subdir of broker-touching middleware (Wave 3 scope) | Pull `Pipe/Middleware/` into Wave 2 (re-classifies broker-mock pattern earlier than parent spec; rejected for consistency) |
| DW5 | Wave 1 lessons (BOM, AAA-deletion, `ThrowsAnyAsync`) captured in §3 as conventions for Wave 2+ | Wave 1's final cleanup commit `9af8650` established the BOM + AAA conventions; Wave 1's Task 5 commit `2de09ea` established `ThrowsAnyAsync`. Capturing in Wave 2 spec ensures Waves 3-5 inherit | Capture only as informal precedent (would lose convention reliability); amend parent spec §3 with these (heavier; deferred to a future revision pass) |
| DW6 | Logging area kept in Wave 2 with reduced count target (~2-4 tests) | Verification surfaced: `Logging/` is just `LibLog.cs` (Phase 3 shim). Surface is small but pure-logic isolated, fits Wave 2 description. Dropping would partially defeat parent spec's "every public API tested" intent | Drop Logging from Wave 2 entirely (surface so small; defer to a "misc" wave or skip — rejected because LogProvider.For<T>() is publicly used); aggressive consolidation into 1 [Theory] (rejected — only 2-3 testable methods, no shape repetition) |
| DW7 | Inline mocks/fixtures only; no shared test-helper assembly | Per Phase 4 precedent (`JsonSerializerTests` has zero helpers); per YAGNI ("wait for the third real use case"); Wave 2 is the FIRST wave with substantial new tests, so we don't yet know what duplication shapes emerge | Per-project private helpers (acceptable later if a single project's mock-construction repeats 3+ times; not preempted); shared `RawRabbit.Tests.Common` csproj (premature abstraction) |

---

## 6. Forced decisions surfaced by verification (resolved before this spec was written)

The brainstorming session's verification pass surfaced 6 findings. 4 mechanical, 2 user-decided:

| # | Finding | Resolution |
|---|---------|------------|
| FD1 | Pipe class names wrong in parent spec §6 (`StagedPipeBuilder` → actual `PipeBuilder`; `PipeFactory` → actual `PipeBuilderFactory`) | User decision (this brainstorm): **Wave 2 spec uses correct names; parent spec §6 row will be amended in a follow-up commit** to reflect actual class names AND the count reconciliation per FD-Count |
| FD2 | Logging area is just `LibLog.cs` (73 lines, Phase 3 shim) — much smaller than parent spec §1's "adapter wiring; log-level filtering" wording implies | User decision (this brainstorm): **Keep Logging in Wave 2 with reduced count target** (~2-4 tests). Parent spec §1 wording remains technically accurate (LoggerFactory setter handles log-level filtering) |
| FD-Count | Per-method strict interpretation gives ~64-122 test estimate; parent spec §6 Wave 2 row says 30-50 | User decision (this brainstorm): **Wave 2 spec commits to 64-122; parent spec §6 row will be amended** in the same follow-up commit as FD1 |
| FD3 | Exceptions count is 3 actual + 1 meta-type (`ExceptionInformation`) = 3-4 testable units, not the 5-8 implied | Mechanical: §2 area 6 row updated to ~6-10 tests |
| FD4 | Common gap-fill = 11 untested files; ~6-8 likely have testable methods (others are pure data) | Mechanical: §2 area 4 row updated to ~12-22 tests; plan-write enumeration filters data-only |
| FD5 | Configuration has root file `RawRabbitConfiguration.cs` plus 7 sub-types = 8 testable units, not 7 | Mechanical: §1 area 5 row + §2 area 5 updated to "Configuration root + 7 sub-types" with ~16-32 tests |

---

## 7. Verified assumptions

The following 13 assumptions were enumerated COLD (against the design alone, before any verification reads) and then verified empirically against HEAD `9af8650` at spec-write time on 2026-05-13:

| # | Assumption | Evidence |
|---|---|---|
| A1 | 3 DI source dirs each have a `*Extension` entry class | `ls src/RawRabbit.DependencyInjection.{Autofac,Ninject,ServiceCollection}/*.cs` confirms: Autofac → `ContainerBuilderExtension.cs` + 2 adapters; Ninject → `KernelExtension.cs` + `NinjectAdapter.cs` + `RawRabbitModule.cs`; ServiceCollection → `AddRawRabbitExtension.cs` + 2 adapters |
| A2 | `src/RawRabbit/Common/` contains source files BEYOND `ConnectionStringParser` and `NamingConventions` | `ls src/RawRabbit/Common/*.cs` returns 13 files; 11 untested (Acknowledgement, ClientPropertyProvider, ExclusiveLock, IDictionaryExtensions, PropertyHeaders, QueueArgument, ResourceDisposer, TaskUtil, TopologyProvider, Truncator, TypeExtensions) |
| A3 | `src/RawRabbit/Configuration/` exists with the 7 sub-type subdirectories matching listed names | `find src/RawRabbit/Configuration -maxdepth 1 -type d` confirms: BasicPublish, Consume, Consumer, ExchangeDeclare, Get, Publisher, QueueDeclare. Plus root file `RawRabbitConfiguration.cs` |
| A4 | `src/RawRabbit/Exceptions/` exists with N exception classes | `ls src/RawRabbit/Exceptions/*.cs` returns 4 files: ChannelAvailabilityException, ExceptionInformation (meta-type), MessageHandlerException, PublishConfirmException. Real exceptions = 3 |
| A5 | `src/RawRabbit/Logging/` exists with testable adapter/wiring source files | `ls src/RawRabbit/Logging/*.cs` returns 1 file: `LibLog.cs` (73 lines, Phase 3 modernization shim). Public surface: `LogProvider`, `ILog : ILogger`, `LogWrapper internal sealed`. Smaller than parent spec implied — see FD2 |
| A6 | `src/RawRabbit/Pipe/` contains testable builder + factory + IPipeContext extensions | `ls src/RawRabbit/Pipe/*.cs` returns 9 files. Builder: `PipeBuilder.cs` (NOT `StagedPipeBuilder`); Factory: `PipeBuilderFactory.cs` (NOT `PipeFactory`); IPipeContext extensions split across 3 files (`PipeContextExtension`, `PipeContextGetExtension`, `AddPropertyPipeContextExtensions`) with ~19 typed extension methods on `IPipeContext`. See FD1 |
| A7 | `test/RawRabbit.Tests/` does NOT yet have Wave 2 target subdirs | `find test/RawRabbit.Tests -maxdepth 1 -type d` returns only Channel/, Common/, Serialization/. None of DependencyInjection/, Configuration/, Exceptions/, Logging/, Pipe/ exist yet |
| A8 | `test/RawRabbit.Tests/Common/` contains only the 2 existing files | `ls test/RawRabbit.Tests/Common/*.cs` returns exactly: `ConnectionStringParserTests.cs`, `NamingConventionsTests.cs` |
| A9 | `test/RawRabbit.DependencyInjection.ServiceCollection.Tests/` exists with csproj wired + no `.cs` files | `ls test/RawRabbit.DependencyInjection.ServiceCollection.Tests/` returns only the csproj + bin/ + obj/ artifacts; no source files; csproj exists from Wave 1 commit `02cc56c` |
| A10 | `test/RawRabbit.Tests/RawRabbit.Tests.csproj` has ProjectReferences to Autofac + Ninject (NOT ServiceCollection) | Read of csproj lines 15-17 confirms: refs to `RawRabbit.DependencyInjection.Autofac` + `RawRabbit.DependencyInjection.Ninject` + `RawRabbit` core; ServiceCollection deliberately absent (parent spec D8 inconsistency intact) |
| A11 | `IPipeContext` interface exists with publicly-testable extension methods | `grep "public static.* this IPipeContext"` against the 3 extension files returns 19 typed extensions (`GetMessage`, `GetMessageType`, `GetMessageContext`, `GetConsumer`, `GetQueueDeclaration`, `GetConsumeThrottleAction`, `GetExchangeDeclaration`, `GetReturnCallback`, `GetConsumeConfiguration`, `GetBasicPublishConfiguration`, `GetConsumerConfiguration`, `GetPublishConfiguration`, `GetRoutingKey`, `GetSubscription`, `GetChannel`, `GetTransientChannel`, `GetBasicProperties`, `GetDeliveryEventArgs`, `GetMessageHandler`); plus `AddPropertyPipeContextExtensions` for the add side |
| A12 | Nothing in `test/` references Wave-2 layout in a conflicting way | `grep -rln 'Configuration/\|Exceptions/\|Logging/\|DependencyInjection/\|Pipe/' test/` returns only build artifact JSON in `obj/`; no source-code conflicts |
| A13 | No surprise source assemblies need Wave 2 coverage | `find src -maxdepth 1 -type d` confirms 27 source assemblies; cross-referenced against parent spec §1; 19 active areas + 6 out-of-scope + 2 already-tested all accounted for. `src/RawRabbit/` subdirs (`find src/RawRabbit -maxdepth 1 -type d`) confirm 11 sub-areas (Channel, Common, Configuration, Consumer, DependencyInjection, Exceptions, Instantiation, Logging, Pipe, Serialization, Subscription) — Wave 2 covers Common/Configuration/Exceptions/Logging/Pipe per parent spec; rest deferred to Wave 3+ |

---

## 8. Tasks NOT in this spec (deferred to Wave 2 implementation plan or to other waves)

Inherited from parent spec's split (spec defines shapes; plan locks names):

- **Per-area exact test method names** — derived during Wave 2 implementation plan's verification pass (one test enumeration per source class)
- **Plan-time decision: which Common gap-fill files have testable methods vs are pure data** — plan reads each of the 11 files, enumerates testable units, drops pure-data types from the gate
- **Plan-time decision: ExceptionInformation testability** — plan reads the file, decides whether it gets dedicated tests or rolls into the 3 real exceptions' coverage
- **Plan-time decision: which IPipeContext extensions to consolidate via `[Theory]`** — 19 typed Get* extensions may share enough shape to consolidate into 1-2 `[Theory]` methods; plan-time enumeration decides
- **Wave 3 source areas:** `Pipe/Middleware/`, `Channel/`, `Consumer/`, `Subscription/`, `Instantiation/{,Disposable/}`, `BusClient.cs`, `IBusClient.cs`, `src/RawRabbit/DependencyInjection/` (core sub-area, not the 3 separate DI projects)
- **Wave 4:** Operations.* (8 areas)
- **Wave 5:** Enrichers (5 + Polly extension)
- **Parent spec amendment** (FD1 + FD-Count): a separate follow-up commit AFTER this spec ships will amend the parent spec §6 Wave 2 row to fix Pipe class names + reconcile the 30-50 → 64-122 estimate. Not bundled into this Wave 2 spec to keep this spec focused on Wave 2's content.

A new spec → new plan cycle is required to add any of the above to a future phase.

---

## 9. Known issues, accepted as out of scope

User-acknowledged on 2026-05-13 during this Wave 2 brainstorm:

1. **Per-method strict interpretation pushes test count above parent spec's estimate.** Parent §6 says Wave 2 = 30-50 tests; this wave's 64-122 reflects honest per-method coverage. Resolved in FD-Count: Wave 2 spec commits to 64-122; parent §6 row gets amended in follow-up commit.
2. **Pipe class names in parent spec are wrong.** Resolved in FD1: Wave 2 spec uses correct names; parent §6 row gets amended in follow-up commit.
3. **Logging area is small.** Resolved in FD2: kept in Wave 2 with reduced count; parent §1 wording remains technically accurate.
4. **No shared test-helper assembly.** Per DW7: inline only; extract opportunistically only if a single project's mock-construction repeats 3+ times. Future waves may revisit.
5. **`Pipe/Middleware/` sub-area is intentionally Wave 3.** Pipe builder/factory/extensions land in Wave 2; built-in middleware in Wave 3 to benefit from one wave's broker-mock pattern hardening.
6. **Carries forward from prior phases:** see parent spec §13 item 5 (HttpContext stub, IntegrationTests, NU1902, Phase 2 metadata heterogeneity).
