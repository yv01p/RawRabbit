# Modernization Phase 4.5 Design — Comprehensive Test Suite

**Source phases:** This spec acts on findings from the test audit performed at Phase 4 spec-write time (recorded in `docs/specs/2026-05-12-modernization-phase-4-design.md` §1 D8 deferral and A25). Phase 4 itself shipped the 10 serializer round-trip tests and committed at `b1c188c` (`origin/2.0`) on 2026-05-13.

**Goal:** After this phase ships across its waves, every public API surface in the in-scope source assemblies has at least one happy-path test and at least one error-path test, executed by `dotnet test` against the unit-test projects on a clean clone with the .NET 10 SDK installed (no live RabbitMQ broker required). The pre-existing 5:1 source-to-test ratio drops materially; existing tests no longer perpetuate the A25 anti-patterns enumerated in §4; a `[Theory]/[InlineData]` convention is established for parameterized tests.

**Architecture:** One unit-test project per separate source assembly (matches the existing `Polly.Tests` precedent), 16 total once Phase 4.5 lands all waves: 2 existing (`RawRabbit.Tests`, `RawRabbit.Enrichers.Polly.Tests`) + 14 new. Tests mock at the broker boundary (`IChannel`, `IConnection`) for code that calls RabbitMQ.Client directly (Channel/, Consumer/); at `IPipeContext` for `Operations.*` and enricher middleware. See §8 D3 for the full policy. No coverage-measurement tooling; per-area acceptance gates enumerate expected tests by name. Implementation is wave-driven: Wave 1 is mandatory-first (quality foundation + test project scaffolding); Waves 2–5 are independent and may ship in any order.

**Tech stack (inherited):** .NET 10 SDK 10.0.107 + C# 13 (default for net10); xUnit 2.9.3; xunit.runner.visualstudio 2.8.2; Microsoft.NET.Test.Sdk 18.5.1; Moq 4.20.72. All package versions in `Directory.Packages.props` from Phase 1/2/3/4. No new package dependencies for Phase 4.5.

---

## 1. Goal & out-of-scope

**In scope (19 active source areas, ~280 source files):**

- `src/RawRabbit/` core (118 files across ~10 sub-areas: Channel, Common, Configuration with 7 sub-types, Consumer, DependencyInjection, Exceptions, Instantiation with Disposable sub, Logging, Pipe with Middleware sub, Subscription) plus root files `BusClient.cs` and `IBusClient.cs`. Existing `Channel/` + `Common/` + `Serialization/` test directories receive gap-fill; the rest receive net-new coverage.
- 3 DI projects: `RawRabbit.DependencyInjection.Autofac` (3 files), `Ninject` (3 files), `ServiceCollection` (3 files)
- 8 `Operations.*` projects: `Get` (11), `MessageSequence` (13), `Publish` (8), `Request` (15), `Respond` (19), `StateMachine` (17), `Subscribe` (7), `Tools` (11)
- 7 enrichers: `Attributes` (6), `GlobalExecutionId` (11), `MessageContext` (8) + `MessageContext.Respond` (2) + `MessageContext.Subscribe` (3) consolidated into one test project, `Polly` (15 — extends existing 1-pass + 2-skip test project), `QueueSuffix` (9), `RetryLater` (9)

**Out of scope (carries forward from prior phases unless noted):**

- `test/RawRabbit.IntegrationTests/*` (51 files) — requires a live broker; Phase 6.
- `test/RawRabbit.Tests/Channel/` 7 currently-skipped tests + `test/RawRabbit.Enrichers.Polly.Tests/Services/ChannelFactoryTests.cs` 2 skipped tests — all tagged "Phase 5/7 territory" (broker-layer modernization).
- `test/RawRabbit.PerformanceTest/*` (4 files) — benchmarks; separate concern.
- `RawRabbit.Enrichers.MessagePack`, `RawRabbit.Enrichers.Protobuf`, `RawRabbit.Enrichers.ZeroFormatter` — alternative `ISerializer` enrichers; depend on libraries with known vulnerabilities (NU1902 against MessagePack 1.7.3.4) or upstream-abandoned dependencies (ZeroFormatter); Phase 7 will revisit and possibly deprecate. Tests written now are tests against a moving target.
- `RawRabbit.Enrichers.HttpContext` — documented as no-op stub on net10 (Phase 1 V1 known issue); testing the stub asserts only "method does nothing." Real implementation deferred to Phase 1 V2.
- `RawRabbit.Compatibility.Legacy` (26 files) — explicit deprecation candidate per Phase 7. Investing in mock fixtures for code that may not exist next phase is high-effort low-value.

**Decomposition context (Phase 4.5's place):**

| Phase | Status | Description |
|-------|--------|-------------|
| 1 | ✅ Shipped | Build sanity on net10 (Phase 1 V1) |
| 2 | ✅ Shipped | Project metadata cleanup |
| 3 | ✅ Shipped | Logging modernization |
| 4 | ✅ Shipped | `Newtonsoft.Json` → `System.Text.Json` |
| **4.5** | **This spec** | **Comprehensive unit-test suite (consumes Phase 4 D8 audit findings)** |
| 5 | Pending | RabbitMQ.Client modernization (broker layer) |
| 6 | Pending | Integration tests + live broker |
| 7 | Pending | Alt-serializer story; Compatibility.Legacy deprecation; final modernization |

Phase 4.5 deliberately precedes Phase 5/6/7 so the modernized broker-layer changes (Phase 5) and integration coverage (Phase 6) land on top of unit-test scaffolding rather than in a vacuum. R6 acknowledges that some Phase 4.5 tests may need revision when later phases change the broker contract.

---

## 2. Test project structure (forced decision; surfaced by verification)

**Existing (pre-4.5; preserved as-is):**

| Project | Source assemblies tested | Status after 4.5 |
|---|---|---|
| `test/RawRabbit.Tests/` | `RawRabbit` core + Autofac DI + Ninject DI (existing ProjectReferences) | Receives gap-fill tests for core sub-areas; Autofac and Ninject get new tests added here (NOT moved to separate projects, preserving the existing inconsistency rather than gold-plating) |
| `test/RawRabbit.Enrichers.Polly.Tests/` | `RawRabbit.Enrichers.Polly` | Receives gap-fill tests beyond the existing 1-pass + 2-skip |

**New (14 projects to scaffold in Wave 1):**

| New test project | Tests source assembly |
|---|---|
| `test/RawRabbit.DependencyInjection.ServiceCollection.Tests/` | `RawRabbit.DependencyInjection.ServiceCollection` |
| `test/RawRabbit.Operations.Get.Tests/` | `RawRabbit.Operations.Get` |
| `test/RawRabbit.Operations.MessageSequence.Tests/` | `RawRabbit.Operations.MessageSequence` |
| `test/RawRabbit.Operations.Publish.Tests/` | `RawRabbit.Operations.Publish` |
| `test/RawRabbit.Operations.Request.Tests/` | `RawRabbit.Operations.Request` |
| `test/RawRabbit.Operations.Respond.Tests/` | `RawRabbit.Operations.Respond` |
| `test/RawRabbit.Operations.StateMachine.Tests/` | `RawRabbit.Operations.StateMachine` |
| `test/RawRabbit.Operations.Subscribe.Tests/` | `RawRabbit.Operations.Subscribe` |
| `test/RawRabbit.Operations.Tools.Tests/` | `RawRabbit.Operations.Tools` |
| `test/RawRabbit.Enrichers.Attributes.Tests/` | `RawRabbit.Enrichers.Attributes` |
| `test/RawRabbit.Enrichers.GlobalExecutionId.Tests/` | `RawRabbit.Enrichers.GlobalExecutionId` |
| `test/RawRabbit.Enrichers.MessageContext.Tests/` | `RawRabbit.Enrichers.MessageContext` + `MessageContext.Respond` + `MessageContext.Subscribe` (3 source assemblies, 1 consolidated test project) |
| `test/RawRabbit.Enrichers.QueueSuffix.Tests/` | `RawRabbit.Enrichers.QueueSuffix` |
| `test/RawRabbit.Enrichers.RetryLater.Tests/` | `RawRabbit.Enrichers.RetryLater` |

Each new test project is SDK-style (`<Project Sdk="Microsoft.NET.Sdk">`), uses CPM-style `<PackageReference>`s for `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `Moq` (versions inherited from `Directory.Packages.props`), and one or more `<ProjectReference>`s pointing at the source assembly under test (and any cross-cutting dependencies it requires — e.g., `RawRabbit` core).

Each new project is added to `RawRabbit.sln` via `dotnet sln add test/<ProjectName>/<ProjectName>.csproj`. Wave 1's acceptance includes verifying `dotnet sln list | wc -l` reports 14 more projects after the wave than before.

**Net result:** 16 total test projects after Phase 4.5 (2 existing + 14 new).

---

## 3. Conventions to establish

| Convention | Decision |
|---|---|
| Test framework | xUnit 2.9.3 (existing) |
| Mock library | Moq 4.20.72 (existing) |
| File layout | One subdirectory per source area under each test project, mirroring `src/`. Existing `test/RawRabbit.Tests/{Channel,Common,Serialization}/` and `test/RawRabbit.Enrichers.Polly.Tests/{Middleware,Services}/` are precedents. |
| Test class naming | `<TypeUnderTest>Tests` (matches existing — e.g., `JsonSerializerTests`, `ChannelPoolTests`) |
| Test method naming | `Should_Verb_Subject` PascalCase with underscores (matches Phase 4) |
| `[Theory]/[InlineData]` | **NEW convention.** Required where 3+ similar tests differ only in input/expected output. Existing tests retrofit during Wave 1's cleanup. The convention must be exercised in at least 3 places across the new tests to prove it landed (per §7 phase-wide acceptance condition 7). |
| `IClassFixture<T>` | Allowed for shared test setup that's expensive to construct per-test (e.g., `JsonSerializerOptions`). Not required. |
| Async tests | `async Task` return type, `await` rather than `.Wait()` / `.Result`. Eliminates the A25 race-prone-timing anti-pattern. |
| AAA structure | No `/* Setup */ /* Test */ /* Assert */` block comments. Visual separation via blank lines only (matches Phase 4's `JsonSerializerTests.cs`). |
| Exception assertions | `Assert.Throws<T>(...)` or `await Assert.ThrowsAsync<T>(...)`. No hand-rolled try/catch around code-under-test. No `Assert.True(true, e.Message)` placeholder. |
| Type assertions | Generic `Assert.IsType<T>(obj)`. Never non-generic `Assert.IsType(typeof(X), obj)`. |
| Mocking style | `new Mock<T>()` constructor, `.Setup(...)/.Returns(...)`, `.Verify(...)`. Mock at the broker boundary (`IChannel`, `IConnection`) for code that touches `RabbitMQ.Client`. |
| Fixture types | File-local `internal` classes for test POCOs (matches Phase 4 `SimplePoco`/`WithDefaults`/etc.) |
| Indent | Tabs (per repo `.editorconfig` — `indent_style = tab` for `[*]`). |
| Coverage tooling | None. Per-area acceptance gates enumerate expected test methods by name. |

---

## 4. A25 anti-patterns (FORBIDDEN in new and cleaned-up tests)

1. `Assert.True(true)` / `Assert.True(true, "msg")` placeholder assertions
2. Hand-rolled `try { ... } catch { ... }` around code-under-test (use `Assert.Throws<T>`)
3. Non-generic `Assert.IsType(typeof(X), obj)` form (use generic `Assert.IsType<T>(obj)`)
4. Race-prone `Task.Wait(timespan)` + `IsCompleted` checks (use `async Task` + `await`)
5. `/* Setup */ /* Test */ /* Assert */` AAA block comments (use blank-line separation)

**Total A25 hits to remove during Wave 1 (per file):**

| File | Assert.True(true) | non-generic IsType | AAA comments | try/catch | Wait/Sleep |
|---|---:|---:|---:|---:|---:|
| `Channel/ChannelFactoryTests.cs` | 2 | 0 | 0 | 0 | 0 |
| `Channel/ChannelPoolTests.cs` | 5 | 0 | 0 | 0 | 0 |
| `Channel/DynamicChannelPoolTests.cs` | 1 | 0 | 9 | 0 | 0 |
| `Common/ConnectionStringParserTests.cs` | 0 | 2 | 45 | 0 | 0 |
| `Common/NamingConventionsTests.cs` | 0 | 0 | 15 | 0 | 0 |
| `Polly.Tests/Middleware/QueueDeclareMiddlewareTests.cs` | 0 | 0 | 2 | 0 | 0 |
| `Polly.Tests/Services/ChannelFactoryTests.cs` | 0 | 0 | 0 | 0 | 0 |
| **Total** | **8** | **2** | **71** | **0** | **0** |

Skipped methods within the above files (9 total — see §1 out of scope) are NOT modified by Wave 1 cleanup; their A25 hits stay in place until Phase 5/7 reactivates them.

---

## 5. Per-area acceptance gates

Each of the 19 active areas has a per-area acceptance gate. The gate format is:

- **Source files in scope** (path list)
- **Test file path(s) to create** (one file per source class is the default; sub-areas may consolidate)
- **Test method count target** (lower bound; ≥1 happy path + ≥1 error path per public API)
- **Mocking surface** (which interfaces get `Mock<T>`)
- **Forced decisions** (areas where source code's design forces a test-design choice)

The **method-name enumeration** for each area is derived during the implementation plan's verification pass — it requires reading each source file to know what public APIs exist. The spec commits to the SHAPE; the plan locks the exact methods. This intentional split mirrors Phase 4's spec (which committed to 10 tests by behavior; the plan's verification pinned the exact method names).

**Bucket patterns (apply per area type):**

**DI registration projects** (3 areas): per project, 1 test file per non-trivial source file. Verify (a) the `Register*` extension produces a resolvable `IBusClient`, (b) lifetime decisions (singleton vs transient) match documented intent, (c) custom registration overrides are honored, (d) DI container surface defects throw with descriptive message. Estimated 5–8 tests per project → ~15–25 total.

**Operations projects** (8 areas): per project, per-middleware happy + error path; per public extension method on `IBusClient`; per Builder/Stage/Key type if present. Mock at `IPipeContext` boundary — instantiate the middleware-under-test, give it a `Mock<IPipeContext>`, call `Invoke(ctx, next)`, assert on context mutations. Tests do not touch a real broker. Estimated 8–15 tests per project → ~80–120 total.

**Enrichers** (7 areas): per enricher, per-middleware context manipulation correct; side effects observable on the next pipe context; configuration override paths work; missing config falls back to documented default. Mock at `IPipeContext` boundary (same shape as Operations.*). Polly extends existing 1-pass + 2-skip. Estimated 4–12 tests per enricher → ~40–60 total.

**Core `src/RawRabbit/` sub-areas** (~10 sub-areas + 2 root files):
- `Channel/` (gap-fill non-skipped paths beyond existing tests; do NOT un-skip Phase 5/7 tests)
- `Channel/Abstraction/` (interface contract tests where applicable)
- `Common/` (gap-fill beyond existing `ConnectionStringParser` + `NamingConventions`)
- `Configuration/` + 7 sub-types (BasicPublish, Consume, Consumer, ExchangeDeclare, Get, Publisher, QueueDeclare): builder defaults; parser correctness; override merging; factory output
- `Consumer/`: subscription consumer dispatch; ack/nack paths (mocked)
- `DependencyInjection/` (the core's own, distinct from the 3 separate DI projects): registration extension; container abstraction
- `Exceptions/`: each custom exception's ctor and serialization round-trip
- `Instantiation/` + `Instantiation/Disposable/`: bus client instantiation; lifecycle disposal
- `Logging/`: adapter wiring; log-level filtering
- `Pipe/`: `PipeBuilder` ordering; `PipeBuilderFactory` resolution; `IPipeContext` add/get/remove
- `Pipe/Middleware/`: per built-in middleware happy + error path
- `Subscription/`: lifecycle; disposal
- Root: `BusClient.cs` + `IBusClient.cs` — `BusClient` instantiation, dispose, extension-method dispatch

Estimated ~80–100 tests across the core sub-areas.

**Quality cleanup of 7 existing test files** (Wave 1 detail per §4 table above): convert anti-patterns; consolidate via `[Theory]` where 3+ tests share input shape; preserve all skip annotations and their justifications.

**Phase 4 backfill** (Wave 1): add 2 tests to `test/RawRabbit.Tests/Serialization/JsonSerializerTests.cs`:
- `Should_Pass_Through_Raw_String_Without_Json_Encoding` — `serializer.Serialize("hello")` produces `Encoding.UTF8.GetBytes("hello")`, NOT `"\"hello\""`. Symmetric: `Deserialize(typeof(string), Encoding.UTF8.GetBytes("hello"))` returns `"hello"`. Pins the `if (obj is string str) return str;` early-return at `JsonSerializer.cs:23-26` and the symmetric `if (type == typeof(string)) return str;` at `:32-35`.
- `Should_Replace_Existing_Collection_Property_During_Deserialization` — pin Phase 4 spec §6 item 4 (`ObjectCreationHandling` collection-replacement): declare a POCO with `List<int> Items { get; set; } = new() { 99 };`, deserialize `{"items":[1,2,3]}`, assert `Items` equals `[1,2,3]` (not `[99,1,2,3]`).

**Phase 4 cosmetics** (Wave 1):
- Rename `Should_Deserialize_Pascal_Case_Json_Into_Pascal_Case_Properties` → `Should_Deserialize_Case_Insensitively_Despite_CamelCase_Policy`
- Restore UTF-8 BOM on `src/RawRabbit/Serialization/JsonSerializer.cs` (lost during Phase 4 Task 1; other files in `src/RawRabbit/Serialization/` retain BOM)
- Drop unused `using System.Collections.Generic;` from `test/RawRabbit.Tests/Serialization/JsonSerializerTests.cs:2`

### Per-area estimate summary

| Bucket | Areas | Source files | Est. test files | Est. tests |
|---|---|---:|---:|---:|
| Core RawRabbit/ | ~10 sub-areas + 2 root | 118 | ~25–40 | ~80–100 |
| DI projects (Autofac, Ninject in RawRabbit.Tests; ServiceCollection in own project) | 3 | 9 | ~6 | ~15–25 |
| Operations.* (8 new test projects) | 8 | 101 | ~24–40 | ~80–120 |
| Enrichers (5 new test projects + Polly extension) | 7 source assemblies → 6 test projects | 52 | ~12–20 | ~40–60 |
| Wave 1 cleanup (7 existing files) | (in place) | (in place) | 0 new | (delta: -8 Assert.True, -2 IsType, -71 AAA; net test count ≈ 0) |
| Wave 1 backfill | Serialization | (in place) | 0 new | +2 (test 11, test 12) |
| Wave 1 cosmetics | Serialization + JsonSerializer.cs | (in place) | 0 | 0 |
| Wave 1 scaffolding | (14 new csprojs) | n/a | 14 csprojs | 0 (empty) |
| **Total** | **19 active areas + 14 csprojs** | **~280** | **~67–106 new test files + 14 new csprojs** | **~217–307 new tests** |

---

## 6. Wave structure

Wave 1 is **mandatory-first** (establishes conventions in existing code + scaffolds new test projects before any new tests land in them). Waves 2–5 are **independent** — implementation plan(s) may sequence them in any order (the recommended order below is for rationale, not enforcement).

| Wave | Scope | Mandatory? | Est. tests added | Est. files added |
|------|-------|:---:|---:|---:|
| **1. Quality Foundation + Scaffolding** | A25 cleanup of 7 existing test files; 2 JsonSerializer backfill tests; 3 Phase 4 cosmetics; create 14 new test project skeletons (csproj + ProjectReferences + sln entry; empty test class per project acceptable) | **Yes (first)** | +2 | 14 csprojs |
| **2. Pure-logic isolated** | 3 DI projects + Common gap-fill + Configuration (+ 7 sub-types) + Exceptions + Logging + Pipe (`PipeBuilder`, `PipeBuilderFactory`, `IPipeContext` extension methods) | No (any time after 1) | ~64–122 | ~10–15 |
| **3. Pipe/Middleware + Consumer + Subscription + Instantiation + DI core** | Built-in middleware (each happy + error); subscription lifecycle; consumer dispatch (mocked); `BusClient` lifecycle; the core's own `DependencyInjection` extensions; `IPipeContext` mutation surface | No (any time after 1) | ~40–60 | ~10–15 |
| **4. Operations.\*** | 8 Operations projects via the 8 new test projects: per-middleware + extension method tests; Builder/Stage/Key type tests where present | No (any time after 1) | ~80–120 | ~24–40 |
| **5. Enrichers** | 5 new enricher test projects + Polly extension: per-middleware + config + extension method tests; Polly adds tests beyond existing 1+2-skip | No (any time after 1) | ~40–60 | ~12–20 |

**Rationale for the recommended order** (not enforced — plans may pull any non-Wave-1 wave when ready):

- Wave 1 establishes the conventions inside the existing codebase before new tests proliferate (avoids "we already have 200 new tests, retrofitting is a separate effort").
- Wave 2 is highest-density-per-test-effort (pure functions, no async, easy mocking) — good for momentum + convention validation.
- Wave 3 introduces the broker-mock pattern in code with low intrinsic complexity.
- Wave 4 applies the broker-mock pattern at scale; benefits from Wave 3's pattern hardening.
- Wave 5 is smallest per area but spans 7 source assemblies (6 test projects); deferred so conventions are well-settled before fan-out.

---

## 7. Phase-wide acceptance (Phase 4.5 done when ALL hold)

1. **Wave 1 shipped (mandatory):** 7 existing test files contain zero A25 anti-patterns (per §4 totals); 2 new `JsonSerializer` tests pass; 3 Phase 4 cosmetics applied; 14 new test projects scaffolded and visible in `dotnet sln list`.
2. **Test count:** ≥217 net-new passing tests added across waves 1–5 (lower bound of estimate). Final aggregate test count across all 16 unit-test projects ≥257 passing. Skipped count remains exactly 9 (the existing Phase 5/7-tagged tests; Phase 4.5 does NOT un-skip any).
3. **Per-area gate satisfied** for all 19 active areas: each area's enumerated test methods (locked at plan-write time) land and pass.
4. **Build clean:** `dotnet build -c Release` returns 0 errors. Pre-existing warning shape preserved (no new warning categories introduced by Phase 4.5 code).
5. **xUnit analyzer cleanup:** xUnit2020/2004/2007/1031 warnings reduced to zero in the cleaned-up + new files. Pre-existing warnings in `IntegrationTests/` remain (out of scope per §1).
6. **Skipped tests preserved:** the existing 9 skipped tests retain their skip annotations and Phase 5/7 commentary verbatim.
7. **`[Theory]/[InlineData]` convention exercised in ≥3 places** across the new tests (proves the convention landed, not just declared).

---

## 8. Decisions

| # | Decision | Rationale | Alternatives considered |
|---|----------|-----------|-------------------------|
| D1 | Single comprehensive Phase 4.5 spec; implementation in plan-driven waves | Captures full vision; matches the cadence of "user picks wave-by-wave when to ship"; mirrors Phase 1–4 spec→plan pattern | Decompose into 4–5 sub-phases (4.5a/b/c/d) each with own spec — more brainstorm overhead, slower; quality-only cleanup followed by separate coverage phases — defers the hardest scoping |
| D2 | Coverage target = "every public API surface tested at least once (≥1 happy + ≥1 error)" | API-surface metric is realistic and pragmatic; produces falsifiable per-area gates without coverage-padding incentives | High-value-paths-only (subjective per project); numeric line-coverage threshold via Coverlet (encourages padding tests, doesn't catch edge cases); quality cleanup only (defers the audit's main finding) |
| D3 | Mock at the broker boundary (`IChannel`, `IConnection`) using Moq 4.20 for `Channel/`, `Consumer/`, and any code that calls `IChannel`/`IConnection` directly; mock at `IPipeContext` for `Operations.*` middleware and enricher middleware | Matches existing pattern at the broker boundary (the 7 currently-skipped Channel tests confirm Moq has known issues there on net10 — those tests stay skipped per Phase 5/7); the `IPipeContext` boundary fits the `IMiddleware` contract shape (instantiate middleware-under-test, give it a `Mock<IPipeContext>`, call `Invoke(ctx, next)`, assert on context mutations) — much smaller per-test setup than building a full `StagedPipeBuilder` chain | Broker boundary uniformly (option α — would force every Operations test to set up a full pipe chain to test one middleware); situational per-test choice (option β — risks per-test inconsistency across the suite); defer all broker-touching coverage to Phase 6 (would shrink Phase 4.5 to a fraction of "comprehensive") |
| D4 | Skip 6 borderline projects: Enrichers.MessagePack/Protobuf/ZeroFormatter (Phase 7), Enrichers.HttpContext (no-op stub; Phase 1 V2), Compatibility.Legacy (Phase 7 deprecation candidate) | Each is a moving target — testing now risks rework when the underlying decision is made. Phase 7 (or Phase 1 V2) should bundle test coverage with the decision | Test all anyway (accept rework cost — explicitly rejected); test only some borderline groups (compromise rejected for consistency) |
| D5 | Approach B: spec defines all per-area gates; recommends wave order with rationale; plan(s) may sequence non-Wave-1 waves freely | Captures full vision; quality foundation lands first; non-Wave-1 sequencing is flexible | Strict opinionated wave order (less flexible); quality wave only + per-area mini-specs (multiplies brainstorm cycles) |
| D6 | Adopt `[Theory]/[InlineData]` as a NEW convention (zero existing usage) | Reduces line count where 3+ tests share input shape; centralizes test logic; easier to add cases. Exercised in ≥3 places per acceptance condition 7 | `[Fact]`-only (matches existing precedent but inconsistent for parameterized tests); mixed/per-author (risks codebase inconsistency) |
| D7 | No coverage tooling | Per-area gates enumerate tests by name → falsifiable without %; matches Phase 1–4 acceptance style; avoids coverage-padding incentives | Coverlet + ≥80% gate (incentivizes low-quality padding); Coverlet for reporting only (tooling overhead, weaker enforcement) |
| D8 | One test project per separate source assembly (matches Polly precedent); add 14 new test projects in Wave 1 | Existing pattern: Polly enricher has its own test project. Pre-existing inconsistency (RawRabbit.Tests references Autofac + Ninject DI but not ServiceCollection) preserved as-is rather than gold-plated. Each new test project independently buildable and ProjectReferenced | Single consolidated RawRabbit.Tests with all ProjectReferences (csproj balloons; test runner less granular); hybrid (per-Operations + per-Enricher with all DI in main — only marginally different from chosen path); defer to plan (re-litigates same question later) |
| D9 | Wave 1 scaffolds 14 empty test projects up-front (separate from new test code); Waves 2–5 fill them in | Decouples build/sln plumbing risk from test-writing; surfaces csproj/ProjectReference issues early; enables parallel development of waves once scaffolding lands | Scaffold per-wave (couples plumbing risk to each wave's commit); scaffold during Wave 2+ as needed (more friction, harder to parallelize) |
| D10 | Skipped methods within cleanup-target files are NOT touched in Wave 1 | Skip annotations + their Phase 5/7 justifications are accurate documentation of broker-layer modernization debt; rewriting them now without the broker-layer change risks losing context. Phase 5/7 reactivates and re-asserts | Modernize all tests in those files including skipped ones (premature; would lose Phase 5/7 context) |

---

## 9. Risks & responses

| # | Risk | Response |
|---|---|---|
| R1 | Test count estimate ranges (217–307) turn out off by >2x once verification surfaces actual file detail | The lower bound (217) becomes the spec's hard acceptance gate (§7 condition 2); the upper bound is informational. Plan-write-time verification re-tightens per-area numbers before locking. |
| R2 | Moq 4.20 + net10 surfaces blocking issues in new mocking code (similar to the 7 currently-skipped Channel tests) | Per-area gate allows the area to ship with one or more tests marked `[Fact(Skip = "Phase 5/7 territory: <specific reason>")]`. Same precedent as existing Channel/. Skipped count tracked; new skips count against §7 condition 2's "skipped count remains 9" — if new skips required, that condition relaxes to "skipped count = 9 + (newly-blocked count) with each new skip annotated similarly to existing precedent." |
| R3 | Some "pure-logic" Wave 2 areas turn out to have hidden broker/IO coupling | Plan-write-time verification reads each source file before per-area gates lock. If hidden coupling found, the area moves from Wave 2 to Wave 3 (or later) before the plan finalizes. |
| R4 | `[Theory]` convention adoption conflicts with existing test structure during cleanup wave | Cleanup wave converts only where 3+ tests genuinely share input/expected shape. Where forcing `[Theory]` would obscure intent, leave as `[Fact]` — convention is "required where natural," not "required everywhere." Acceptance condition 7 requires ≥3 uses, not exhaustive conversion. |
| R5 | Tests pass against mocks but miss real wire behavior | Acknowledged. Phase 6 (`test/RawRabbit.IntegrationTests/*`) is the live-broker layer that catches what mocks miss. Phase 4.5 is unit coverage; not a substitute for Phase 6. |
| R6 | Phase 5/6/7 modifies the broker layer between Phase 4.5 waves, invalidating already-written tests | Each wave can be re-verified against current HEAD before being considered "done." If a later phase's broker changes break Wave 4 tests, those test fix-ups are folded into the breaking phase's spec, NOT Phase 4.5. |
| R7 | Audit's "only Channel.* and Common.* (and now Serialization.*) have unit tests" claim turns out to be partially false | Verified: zero hidden test refs to `Operations.`, `Enrichers.`, `DependencyInjection.`, or `Pipe.` namespaces in `test/RawRabbit.Tests/`. Audit holds at HEAD `b1c188c`. R7 closed at spec-write time. |
| R8 | 14 new csprojs surface unexpected `.sln` GUID collisions or solution-folder placement issues | Wave 1 verification commands include `dotnet sln list \| wc -l` (before/after diff = +14) and `dotnet build -c Release` after each `dotnet sln add` to catch breakage early. New projects placed in the existing `test` solution folder (GUID `{2F91E22A-AEBA-4BEF-9A03-C8232830F697}` per `RawRabbit.sln`). |
| R9 | The 9 currently-skipped tests' skip reasons drift between specs (Phase 4.5 documents them; Phase 5/7 must un-skip them) | Phase 4.5 spec preserves skip reasons verbatim. Phase 5/7 specs must reference this Phase 4.5 spec when un-skipping; the un-skip work is Phase 5/7's responsibility, not Phase 4.5's. |

---

## 10. Validation steps for the implementer (per-wave)

Each wave's implementation plan defines its own per-wave acceptance commands. The shape per wave:

1. `dotnet build -c Release` → 0 errors. Warning shape stable from prior wave.
2. `dotnet test test/<Wave-N-test-project>/ --no-build -c Release` → all enumerated tests pass; skipped count matches expected.
3. (Wave 1 only) `dotnet sln list | wc -l` → 14 more than baseline; `git diff --stat` covers exactly Wave 1's planned files.
4. (Wave 1 only) Re-verify `[Theory]` convention exercised in ≥3 places by spec-write time of last wave (if any conversion happens during Wave 1's cleanup).
5. (Wave 5 / final) Aggregate `dotnet test` across all 16 unit-test projects → ≥257 passed total; skipped count = 9 (or = 9 + newly-blocked per R2).

Phase 4.5 is complete when all 7 conditions in §7 hold simultaneously at HEAD.

---

## 11. Verified assumptions

The following 18 assumptions were enumerated COLD (against the design alone, before any verification reads) and then verified empirically against HEAD `b1c188c` at spec-write time on 2026-05-13:

| # | Assumption | Evidence |
|---|---|---|
| A1 | All 19 active source-area paths exist as listed in §1 | `test -d` for each of 19 paths returned OK; full list confirmed |
| A2 | All 6 out-of-scope source paths (MessagePack, Protobuf, ZeroFormatter, HttpContext, Compatibility.Legacy, PerformanceTest) exist | `test -d` for each returned OK |
| A3 | Pre-Phase-4.5 test baseline is 40 passed / 9 skipped / 0 failed | `dotnet test test/RawRabbit.Tests --no-build -c Release` → `Failed: 0, Passed: 39, Skipped: 7`; `dotnet test test/RawRabbit.Enrichers.Polly.Tests --no-build -c Release` → `Failed: 0, Passed: 1, Skipped: 2` |
| A4 | Build clean (0 errors), warning shape ~143 | `dotnet build -c Release` → `0 Error(s)`, ~143 warnings (all pre-existing per Phase 4 final review) |
| A5 | 8 existing unit-test `.cs` files (NOT 5 — original §2 cleanup target understated) | `find test/RawRabbit.Tests test/RawRabbit.Enrichers.Polly.Tests -name "*.cs"` returned 8 files; cleanup target revised to 7 (excluding the already-clean `Serialization/JsonSerializerTests.cs`) |
| A6 | 9 skipped tests at the locations claimed, all tagged Phase 5/7 | `grep -rn "[Fact(Skip"` returned 9 hits: 4 in `Channel/ChannelFactoryTests.cs` (lines 15, 46, 77, 103), 3 in `Channel/ChannelPoolTests.cs` (lines 106, 190, 245), 2 in `Polly.Tests/Services/ChannelFactoryTests.cs` (lines 16, 52); all skip messages contain "Phase 5/7 territory" |
| A7 | A25 anti-patterns present in cited unit-test files at the per-file counts in §4 table | `grep -cE 'Assert\.True\(true\|Assert\.IsType\(typeof\|/\* (Setup\|Arrange\|Act\|Test\|Assert) \*/'` per file produced the §4 totals: 8 Assert.True(true), 2 non-generic IsType, 71 AAA comments, 0 try/catch, 0 Wait/Sleep |
| A8 | Zero `[Theory]/[InlineData]` usage anywhere in `test/` | `grep -rn "\[Theory\]\|\[InlineData"` returned 0 hits — confirms `[Theory]` adoption is a NEW convention, not retrofit-only |
| A9 | xUnit 2.9.3 + Moq 4.20.72 + Microsoft.NET.Test.Sdk 18.5.1 are the test stack versions | `Directory.Packages.props` lines 19–22 confirm exact versions |
| A10 | Each DI project has a `Register*`/`Add*`/`*Extension` entry-point class | `ls src/RawRabbit.DependencyInjection.{Autofac,Ninject,ServiceCollection}/*.cs` confirmed: `ContainerBuilderExtension.cs`, `KernelExtension.cs`, `AddRawRabbitExtension.cs` respectively |
| A11 | Operations projects each have middleware + extension method pattern (Builder/Stage/Key OPTIONAL) | Read `src/RawRabbit.Operations.Publish/` (has `PublishKey`, `PublishStage`, `PublishMessageExtension` + 3 middleware) and `src/RawRabbit.Operations.Subscribe/` (has `SubscribeMessageExtension` + 3 middleware; no Builder/Stage/Key). Pattern claim refined: middleware + extension method are required; Builder/Stage/Key are project-specific |
| A12 | Enrichers each follow Plugin + middleware pattern | Read `src/RawRabbit.Enrichers.Attributes/` (`AttributePlugin.cs` + 3 attribute classes + 2 middleware) and `src/RawRabbit.Enrichers.GlobalExecutionId/` (`GlobalExecutionIdPlugin.cs` + extensions/keys/headers + 3 middleware). Pattern broadly holds with varied internal extras |
| A13 | `src/RawRabbit/` sub-area structure differs from initial Section 2 claim | `find src/RawRabbit -maxdepth 2 -type d` returned actual subdirs: Channel (+Abstraction), Common, Configuration (+7 sub-types), Consumer, DependencyInjection, Exceptions, Instantiation (+Disposable), Logging, Pipe (+Middleware), Serialization, Subscription. Plus root files `BusClient.cs`, `IBusClient.cs`. Section 2's Connection/Context/inner-Operations sub-areas were absent and have been removed; DependencyInjection and Instantiation were added |
| A14 | Mocking at `IChannel`/`IConnection` boundary works with Moq 4.20 on net10 (for non-skipped paths) | 39 non-skipped tests in `RawRabbit.Tests` pass; 60 lines using `new Mock<` / `.Setup(` / `.Verify(` patterns in test/. Phase 5/7 issues are confined to specific signature-mismatch cases (already documented in skip annotations), NOT general broker-boundary mocking |
| A15 | Both unit-test csprojs are SDK-style; auto-discover any `.cs` file under any subdirectory | `head -3` of both csprojs confirmed `<Project Sdk="Microsoft.NET.Sdk">`. Phase 4 already proved auto-discovery for `RawRabbit.Tests` (added `Serialization/` subdir, picked up automatically) |
| A16 | Existing tests use `new Mock<T>()` / `.Setup(...)` / `.Verify(...)` style | `grep -c` returned 60 lines using these patterns across `test/RawRabbit.Tests/` + `test/RawRabbit.Enrichers.Polly.Tests/` |
| A17 | Audit's claim "only Channel.* and Common.* (and now Serialization.*) have unit tests" — no hidden test refs to other source areas | `grep -rln "Operations\.\|Enrichers\.\|DependencyInjection\.\|Pipe\."` against `test/RawRabbit.Tests/` returned 0 hits. Audit holds at HEAD `b1c188c` |
| A18 | Existing test project ProjectReferences | `RawRabbit.Tests`: refs RawRabbit + Autofac DI + Ninject DI (NOT ServiceCollection — pre-existing inconsistency, preserved by D8). `Polly.Tests`: refs RawRabbit + Polly enricher. Surfaced as a forced design decision; resolved in §2 / D8 (one new test project per separate source assembly) |

---

## 12. Tasks NOT in this spec

Inherited from §1 out-of-scope and the "design captures vision; plan sequences and enumerates" split:

- **Per-area exact test method names** — derived during the implementation plan's verification pass (one per source class), not in this spec
- **`test/RawRabbit.IntegrationTests/*` cleanup** — Phase 6 (live broker required)
- **Un-skipping the 9 Phase 5/7-tagged tests** — Phase 5/7 (broker-layer modernization)
- **Performance benchmarks (`test/RawRabbit.PerformanceTest/`)** — separate concern
- **Tests for MessagePack / Protobuf / ZeroFormatter / HttpContext / Compatibility.Legacy** — Phase 7 (or Phase 1 V2 for HttpContext)
- **Solution file restructuring beyond `dotnet sln add`** — solution folder reorganization is not in scope; new projects join the existing `test` solution folder
- **CI configuration changes** — none required; existing pipeline (if any) auto-discovers new test projects via `dotnet test` at solution scope

A new spec → new plan cycle is required to add any of the above to a future phase.

## 13. Known issues, accepted as out of scope

User-acknowledged on 2026-05-13 during the Phase 4.5 brainstorm:

1. **Pre-existing skipped test annotations remain in place.** The 9 currently-skipped tests stay skipped (D10); Phase 4.5 does not un-skip them.
2. **Pre-existing inconsistency in `RawRabbit.Tests` ProjectReferences preserved.** RawRabbit.Tests refs Autofac + Ninject DI but not ServiceCollection — Phase 4.5 does NOT fix this by either adding the missing ref or moving Autofac/Ninject to separate projects (D8). The new ServiceCollection.Tests is its own project; the inconsistency for Autofac/Ninject is intentional preservation, not gold-plating.
3. **`[Theory]` convention conversion is opportunistic, not exhaustive.** Wave 1 converts only where 3+ tests genuinely share input/expected shape (R4); other parameterized-style tests stay as `[Fact]` if conversion would obscure intent. Acceptance condition 7 requires ≥3 conversions, not all.
4. **Test-count estimates are ranges (217–307); only the lower bound is the acceptance gate.** Plan-write verification will re-tighten ranges per area; the upper bound is informational.
5. **Carries forward from prior phases:** `RawRabbit.Enrichers.HttpContext` still has no functional implementation on net10 (Phase 1 V1); `test/RawRabbit.IntegrationTests` still requires a live broker (Phase 6); MessagePack 1.7.3.4's NU1902 vulnerability warnings persist (Phase 7); the heterogeneous metadata acceptances from Phase 2 remain as documented.
