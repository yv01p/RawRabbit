# Modernization Phase 4.5 Wave 4 Design — `Operations.*` Coverage

**Source:** Phase 4.5 parent spec at `docs/specs/2026-05-13-modernization-phase-4.5-design.md` (parent on `origin/2.0`). Wave 4 is the fourth of five waves; Waves 1–3 shipped through `c5e78b4` on `origin/2.0`.

**Inherits from parent spec (do NOT re-litigate here):**
- §1 in/out-of-scope source areas; §2 test project structure (D8 — 8 `Operations.*` test projects already scaffolded by Wave 1); §3 conventions; §4 A25 forbidden patterns; §5 per-area gate format and Operations bucket; §6 wave row 4 (`~24–40` test files, `~80–120` tests); §7 phase-wide acceptance; §8 decisions D1–D10; §9 risks (notably R2 — broker-mock skip-when-blocked permitted with annotation); §10 validation shape; §13 known-issues acknowledgements.
- Wave 2 conventions captured in Wave 2 §3: BOM preservation, AAA-deletion full-line removal, `ThrowsAnyAsync<T>` over `ThrowsAsync<T>` for cancellations.
- Wave 3 conventions captured in Wave 3 spec + handoff: F1=(a) per-public-method strict (≥1 happy + ≥1 error per public method), `[Collection("LogProviderState")]` opt-in for `LogProvider.For<T>()`-reading test classes, namespace-collision alias pattern.

**Goal:** After Wave 4 ships, the 8 `Operations.*` source projects each have ≥1 happy + ≥1 error path test per public method (F1=(a) strict). Wave 4 lands ≥80 net-new passing tests across the 8 sub-areas, each in its own already-scaffolded `test/RawRabbit.Operations.*.Tests/` project. Wave 4 introduces no new test-helper organisation pattern; tests use inline `Mock<IModel>` setups for the 6 broker-touching middleware identified in §2 (mock at `IPipeContext` boundary per parent D3).

**Tech stack (inherited):** .NET 10 SDK 10.0.107 + C# 13; xUnit 2.9.3; xunit.runner.visualstudio 2.8.2; Microsoft.NET.Test.Sdk 18.5.1; Moq 4.20.72; RabbitMQ.Client 5.0.1 (legacy; Phase 5 modernizes). No new package dependencies for Wave 4.

---

## 1. Scope

**In scope (8 sub-areas, 101 source files; 8 SDD tasks):**

| # | Source project | Source files | Test project (scaffolded; 0 .cs) | Wave 4 task |
|---|---|---:|---|---|
| 1 | `RawRabbit.Operations.Get` | 11 | `test/RawRabbit.Operations.Get.Tests/` | T1 |
| 2 | `RawRabbit.Operations.MessageSequence` | 13 | `test/RawRabbit.Operations.MessageSequence.Tests/` | T2 |
| 3 | `RawRabbit.Operations.Publish` | 8 | `test/RawRabbit.Operations.Publish.Tests/` | T3 |
| 4 | `RawRabbit.Operations.Request` | 15 | `test/RawRabbit.Operations.Request.Tests/` | T4 |
| 5 | `RawRabbit.Operations.Respond` | 19 | `test/RawRabbit.Operations.Respond.Tests/` | T5 |
| 6 | `RawRabbit.Operations.StateMachine` | 17 | `test/RawRabbit.Operations.StateMachine.Tests/` | T6 |
| 7 | `RawRabbit.Operations.Subscribe` | 7 | `test/RawRabbit.Operations.Subscribe.Tests/` | T7 |
| 8 | `RawRabbit.Operations.Tools` | 11 | `test/RawRabbit.Operations.Tools.Tests/` | T8 |

**Project shape variance** (from §11 verification A1, A22):

- **Tools (T8)** — 9 root `*Extension.cs` (one per RMQ primitive) + 2 `Middleware/`. Different shape from the other 7. Plan-write verification per W4-R2 decides per-extension vs consolidated test files.
- **Subscribe (T7) — smallest** — 7 files: `Context/`, `Middleware/`, `Stages/`, single `SubscribeMessageExtension.cs`.
- **Respond (T5) — largest** — 19 files: `Acknowledgement/`, `Configuration/`, `Context/`, `Core/`, `Middleware/`, single `RespondExtension.cs`.
- **StateMachine (T6)** — 17 files; uses `Stateless` library (event-driven, NOT timer-driven; no `Timer`/`Task.Delay`/`Thread.Sleep`). Standard middleware-mock pattern suffices; no custom test fixture needed. (W4-R1 risk closed at spec-write per A15.)
- **Get / MessageSequence / Publish / Request** — subscribe-shape variants with own `Context/`, `Middleware/`, optional `Stages/Configuration/Core/Model/`.

**Out of scope** (carries forward from parent §1 + Wave 3):

- Live-broker integration (Phase 6).
- `src/RawRabbit.Operations.*/` source modifications (test-only wave).
- Cross-project `BrokerMocks` consolidation (W4-D2 — defer to Wave 5+ if duplication count justifies).
- Phase 7 deprecation candidates and refactor backlog (LogProvider lazy refactor, ResilientChannelPool double-enumeration, etc.).
- Wave 5 (`Enrichers.*`).

**Wave 4's place in Phase 4.5:**

| Wave | Status | Through |
|---|---|---|
| 1 | ✅ Shipped | `9af8650` |
| 2 | ✅ Shipped | `9e91f10` |
| 3 | ✅ Shipped | `c5e78b4` |
| **4** | **This spec** | **— (Operations.\*)** |
| 5 | ⏳ Pending | (Enrichers.*) |

---

## 2. Test architecture

**Per parent D3 — mock at `IPipeContext` boundary (NOT broker boundary).** Each Operations middleware test:

1. Instantiates the middleware-under-test directly (its public ctor signature; pass `Mock<IPipeContext>`-derived dependencies as needed)
2. Builds a `Mock<IPipeContext>` via `new Mock<IPipeContext>()`; `.Setup(c => c.GetXyz()).Returns(...)` per the middleware's read paths
3. Calls `await middleware.InvokeAsync(ctx.Object, CancellationToken.None)` (or `CancellationToken` token-cancellation tests use `await Assert.ThrowsAnyAsync<OperationCanceledException>(...)`)
4. Asserts on context mutations via `.Verify(c => c.Properties.Add(...), Times.Once)` etc., AND/OR on dependency-mock invocations

**Inline broker mocks where needed.** 6 middleware across 4 projects reference broker types (verified §11 A6):

| Project | Broker-touching middleware |
|---|---|
| `Operations.Get` | `BasicGetMiddleware.cs`, `AckableResultMiddleware.cs` |
| `Operations.Publish` | `ReturnCallbackMiddleware.cs`, `PublishAcknowledgeMiddleware.cs` |
| `Operations.Subscribe` | `SubscriptionExceptionMiddleware.cs` |
| `Operations.Respond` | `RespondExceptionMiddleware.cs` |

For each, the test sets up `var ch = new Mock<IModel>(); ch.Setup(...).Returns(...);` inline (no shared helper). Most need 1–2 broker stubs only — inline is clearer than calling a factory method, and avoids the cross-project plumbing of promoting `BrokerMocks` to a shared csproj (W4-D2).

**Test file granularity.** One test file per source class default. Mirrors `src/RawRabbit.Operations.X/<subdir>/<TypeUnderTest>.cs` → `test/RawRabbit.Operations.X.Tests/<subdir>/<TypeUnderTest>Tests.cs`. Sub-areas may consolidate (parent §3) — Tools may consolidate per W4-R2.

**F1=(a) strict carry-forward.** ≥1 happy + ≥1 error path test per **public method** on every source class with public surface (Wave 2/3 lesson). Implementer prompts MUST explicitly enumerate "public methods needing error tests" per source class — Wave 3 missed this in Tasks 2 and 8 for async methods that wrap try/catch (caught by code reviewer, fixed in `083a075` + `b4b5bd8`).

**`[Collection("LogProviderState")]` opt-in.** 10 `Operations.*` source files use `LogProvider.For<T>()` (verified §11 A7); test classes for these 10 sources MUST add the `[Collection("LogProviderState")]` attribute to serialize against `LogExtensionsTests` static mutation:

| Project | Source class needing `[Collection]` on its test |
|---|---|
| `Operations.Publish` | `Middleware/PublishConfigurationMiddleware`, `Middleware/ReturnCallbackMiddleware`, `Middleware/PublishAcknowledgeMiddleware` |
| `Operations.Request` | `Middleware/ResponderExceptionMiddleware`, `Middleware/ResponseConsumeMiddleware` |
| `Operations.Respond` | `Middleware/ReplyToExtractionMiddleware` |
| `Operations.Subscribe` | `Middleware/SubscriptionConfigurationMiddleware`, `Middleware/SubscriptionExceptionMiddleware` |
| `Operations.MessageSequence` | `StateMachine/MessageSequence` |
| `Operations.StateMachine` | `Core/GlobalLock` |

Plan-write verification re-grep'd against HEAD; SDD implementer prompts call out the per-task subset.

**Namespace-collision alias pattern.** 2 projects have a class named the same as the project's last namespace segment (verified §11 A8):

| Project | Source classes named the same as the project (alias may be needed in tests) |
|---|---|
| `Operations.MessageSequence` | `StateMachine/MessageSequence.cs`, `Model/MessageSequence.cs` |
| `Operations.Respond` | `Acknowledgement/Respond.cs` |

If Wave 4 test files in those subdirectories collide on the class name (`Tests.StateMachine.MessageSequenceTests` referencing `MessageSequence` ambiguously), use the same `using <Alias> = <fully.qualified.Type>;` pattern Wave 3 used for `MiddlewareBase = RawRabbit.Pipe.Middleware.Middleware`. Plan-write verification confirms which test files trigger the collision.

---

## 3. Conventions inherited (carry-forward checklist; do NOT re-litigate)

- xUnit 2.9.3 + Moq 4.20.72 + Microsoft.NET.Test.Sdk 18.5.1 (parent §3)
- A25 forbidden patterns (parent §4): no `Assert.True(true)`, no hand-rolled try/catch around code-under-test, no non-generic `Assert.IsType`, no `/* AAA */` block comments, no `Task.Wait()` polling
- Test class naming: `<TypeUnderTest>Tests`; method naming: `Should_Verb_Subject` PascalCase with underscores (parent §3)
- `async Task` + `await`, never `.Wait()` / `.Result` (parent §3)
- BOM-mandatory on all new `.cs` files (Wave 2 lesson; verified per-file via `for f in <files>; do file "$f"; done` post-creation)
- AAA full-line removal during cleanup, not blank replacement (Wave 2 lesson)
- `await Assert.ThrowsAnyAsync<OperationCanceledException>(...)` for cancellation tests, NOT `ThrowsAsync<>` (Wave 2/3 lesson)
- `[Theory]/[InlineData]` opportunistic where 3+ tests share input shape (parent §3 + acceptance gate 7 — phase-wide ≥3 places, not wave-local)
- Indentation: tabs (per `.editorconfig` `indent_style = tab`)
- Skip annotation format: `[Fact(Skip = "Phase 5/7 territory: <specific signature/symptom>")]` exact (Wave 3 convention)

---

## 4. Skip budget (gate 7)

- **N=0–5 fresh budget for Wave 4** (carries from parent R2 + §7).
- **Pool allocation, not per-project** (W4-D4 — matches Wave 3, where pool distributed organically: 3 to Task 1 ExplicitAck + 2 to Task 2 timer/IRecoverable).
- Each skip MUST use exact format above.
- Plan-write verification flags any source class likely to need a skip; SDD implementer reports each skip with justification; spec/quality reviewer validates; final wave-wide review confirms total ≤5.

Wave 4 enters with no a-priori-known skips (StateMachine W4-R1 closed at A15; no other timer/race patterns surfaced at spec-write). Skips below the budget cap are fine; the budget is a ceiling, not a target.

---

## 5. Per-area gate format

Spec commits to **shape** per project (W4-D3). **Plan locks exact test-method enumeration** at plan-write verification time by reading each source file. Each project's gate:

- **Source files in scope** — full path list (locked at plan-write time)
- **Test file paths to create** — one per source class default; sub-areas may consolidate (Tools per W4-R2)
- **Public-method coverage** — every public method on every public class gets ≥1 happy + ≥1 error test (F1=(a) strict per Wave 2/3 carry-forward)
- **Mocking surface** — `Mock<IPipeContext>` always; project-specific `Mock<I*>` for direct dependencies (e.g., `Mock<IChannelFactory>`, `Mock<ISerializer>`); inline `Mock<IModel>` for the 6 broker-touching middleware in §2
- **`[Collection("LogProviderState")]`** — required on test classes for the 10 `LogProvider.For<T>()`-reading sources listed in §2
- **Namespace alias** — required on test files in `MessageSequence/Model`, `MessageSequence/StateMachine`, `Respond/Acknowledgement` if they reference the same-named class

Per-project test count target inherited from parent §5 Operations bucket: **8–15 tests per project** (with 8 projects → ≥80 wave total per acceptance §6.4).

---

## 6. Wave-wide acceptance (Wave 4 done when ALL hold)

1. **8 tasks shipped** (T1–T8) — each as a separate commit (or commit + fix-up cluster) with passing per-task spec/quality review.
2. **`dotnet test test/RawRabbit.Operations.<X>.Tests --no-build -c Release`** for each of the 8 projects → 0 failed; per-project skip count ≤5 in aggregate (wave-wide pool per §4).
3. **`dotnet test`** (full solution / each test project) → still 0 failed; aggregate skip count = 15 (existing) + (Wave 4's actual N, where N≤5) — i.e., no Wave 4 work breaks Waves 1–3.
4. **Test count target:** ≥80 net-new passing tests across the 8 projects (parent §6 row 4 lower bound; verified §11 A20).
5. **Per-area gate satisfied** for all 8 projects — every public method has ≥1 happy + ≥1 error test (per the plan's locked enumeration).
6. **Build clean:** `dotnet build -c Release` → 0 errors; warning shape stable from Wave 3 close (verified §11 A12).
7. **3-run stability:** aggregate `dotnet test` run 3× consecutively, all 3 reporting identical pass/fail/skip counts (matches Wave 3 final verification — race detector).
8. **Wave-wide review** dispatched after T8 — spec reviewer + code quality reviewer go over the entire wave's diff one last time before push (catches cross-task issues like the LogProvider race Wave 3 hit at Task 9).

---

## 7. Carry-forward dead ends (do NOT repeat)

Lifted from Wave 3 handoff §3 — applicable to Wave 4 by the same mechanisms (verified §11 A14 — RabbitMQ.Client still 5.0.1; A20 — broker-trigger pattern absent in Operations.*):

- `Mock.SetupSequence` for ResilientChannelPool ctor seed (use `Mock.Setup().ReturnsAsync()`) — does not apply to Operations.* directly but lesson generalises
- Testing `RawRabbitFactory.CreateSingleton` (triggers real broker connect at test time)
- `Mock<IBasicConsumer>` for Subscription.Dispose tests (use `EventingBasicConsumer`)
- Resolving `IBusClient` in DI happy-path tests (broker trigger)
- `Assert.True(true)` placeholders / async tests with no assertion
- `Assert.ThrowsAsync<>` for cancellation (use `ThrowsAnyAsync<OperationCanceledException>`)
- Default xUnit parallelism on `LogProvider.For<T>()`-reading test classes (use `[Collection("LogProviderState")]`)
- `ReadOnlyMemory<byte>` for `HandleBasicDeliver` body arg (use `byte[]` for RMQ.Client 5.0.1)
- Trusting subagent commit SHAs (always verify via `git log -1 --format=%H`)
- `SendMessage` to subagents (one-shot only; bake answers into prompt — Wave 3 wasted 1 dispatch on Q&A)

---

## 8. Decisions

| # | Decision | Rationale | Alternatives rejected |
|---|----------|-----------|----------------------|
| W4-D1 | 8 SDD tasks, one per `Operations.*` project | Mirrors Wave 3 cadence (8 sub-areas); simplest per-task scope; aligns with the 8 already-scaffolded test projects (verified §11 A2) | Group 4 tasks (cross-project blast); 1 mega-task (lose-everything-on-blocker risk); 9 tasks with explicit verification (verification folded into wave-wide review per W4-D6) |
| W4-D2 | Inline `Mock<IModel>` setups; no shared `BrokerMocks` helper for Wave 4 | Honors parent D3 (mock at `IPipeContext`); only 6 middleware reach for broker types (verified §11 A6); each needs 1–2 stubs; YAGNI | Duplicate `BrokerMocks` to 8 projects (8 sync sites); promote to shared csproj (scope expansion not justified by 6 call sites); defer (no clear trigger) |
| W4-D3 | Test-method enumeration deferred to plan-write verification (per parent §5) | Spec commits to shape; plan locks numbers after reading each source file. Wave 3 followed same pattern successfully | Lock per-project test counts in spec (gold-plates spec; can't verify without reading source); commit to upper-bound only (under-specified) |
| W4-D4 | Skip budget pool wave-wide (N=0–5), not per-project | Matches Wave 3 (pool distributed 3+2 organically); some projects may need 0 skips, others 1–2 — pool absorbs variance | Per-project sub-budget (rigid; forces unused budget to be wasted) |
| W4-D5 | Carry-forward `[Collection("LogProviderState")]` opt-in; identify the 10 affected sources at spec-time | 10 `Operations.*` sources verified using `LogProvider.For<T>()` (§11 A7); same race surface as Wave 3 | Disable parallelism wave-wide (over-broad); per-test mutex (heavier); ignore (race re-emerges) |
| W4-D6 | Wave-wide review after T8 (not a separately-numbered T9) | Verification is implicit pre-ship discipline, not a deliverable | T9 explicit (matches Wave 3 numbering but adds bureaucracy without changing work) |
| W4-D7 | Carry-forward namespace-alias pattern; identify the 2 affected projects (3 source files) at spec-time | 2 projects have class named same as namespace last segment (§11 A8: MessageSequence ×2, Respond ×1); apply alias only when test code references the colliding name | Wave-wide alias prophylaxis (over-broad; pollutes 5 projects that don't need it) |

---

## 9. Risks & responses

| # | Risk | Response |
|---|---|---|
| W4-R1 | StateMachine's transition machinery needs a custom test fixture beyond per-class instantiation | **Closed at spec-write per §11 A15.** StateMachine uses `Stateless` library (event-driven, no Timer/Task.Delay). Standard middleware-mock pattern suffices. |
| W4-R2 | Tools project's 9 root extension methods (one per RMQ primitive) make per-class file granularity awkward | Plan-write verification decides: per-extension test files vs single consolidated `ToolsExtensionTests.cs` (parent §3 allows sub-area consolidation). Do NOT lock at spec-time. |
| W4-R3 | Wave 4 SDD surfaces a new race or static-state mutation analogous to Wave 3's LogProvider | Same fix pattern (`[Collection]` attribute) — wave-wide review catches it; budget for 1 fix-up commit per task. The 10 known LogProvider consumers are already opted-in via §2. |
| W4-R4 | `Operations.*` middleware that touch `LogProvider.For<T>()` in their ctors expand the LogProviderState collection from 26 (Wave 3 close) to ≥36 classes | Acceptable; documented carry-forward. Phase 7 backlog item: refactor `LogProvider.For<T>()` to lazy/injected (would eliminate the workaround). Not Wave 4's job. |
| W4-R5 | Reviewer false-positive rate similar to Wave 3 (~3 per wave) | Each Critical/Important reviewer claim verified against actual file bytes / spec text before applying fixes (Wave 3 lesson). |
| W4-R6 | F1=(a) error-test gaps in async methods that wrap try/catch (Wave 3 hit this in Tasks 2 + 8) | Implementer prompt explicitly enumerates "public methods needing error tests" per source class; reviewer cross-checks. |
| W4-R7 | BOM gap on ~10–20% of new files even with guarded loop (Wave 3 hit in Task 1 — 5 of 29 files) | Implementer prompt requires `for f in <files>; do file "$f"; done` post-loop verification with explicit per-file confirmation. |
| W4-R8 | Per-project test count lower-bound (8 tests/project) doesn't satisfy F1=(a) strict for projects with many small public methods (e.g., Respond's 19 source files; Request's 15) | Plan-write verification surfaces actual per-project counts; if F1=(a) strict drives a project above 15, that's expected (parent §5 says "8–15 estimated, not capped"). The wave-wide ≥80 is the gate, not per-project. |
| W4-R9 | StateMachine's `StateMachineBase<TState, TTrigger, TModel>` is abstract; tests need concrete subclasses | Plan-write verification decides whether to provide a file-local internal `TestStateMachine` subclass per test file (parent §3 fixture-types convention) or skip ctor-level tests if the cost exceeds the value (within N=0–5 budget). |

---

## 10. Tasks NOT in this spec

(Carries forward parent §12 + Wave 3 scope split)

- Per-project exact test-method names — plan-write verification (W4-D3)
- `src/RawRabbit.Operations.*/` source modifications — test-only wave (parent §1)
- `test/RawRabbit.IntegrationTests/*` cleanup — Phase 6 (live broker)
- Un-skipping the existing 15 skipped tests across the 3 stable projects — Phase 5/7
- Performance benchmarks — separate concern
- Wave 5 (Enrichers.*) brainstorm + spec/plan/SDD lifecycle — post-Wave-4 work
- BrokerMocks promotion to shared csproj — defer to Wave 5+ if duplication count justifies (W4-D2)
- Refactor of `LogProvider.For<T>()` static-read to lazy/injected — Phase 7 backlog from Wave 3
- Phase 5/6/7 work generally

A new spec → new plan cycle is required to add any of the above to a future phase.

---

## 11. Verified assumptions

The following 20 assumptions were enumerated COLD (against the design alone, before any verification reads) and then verified empirically against HEAD `c5e78b4` at spec-write time on 2026-05-14:

| # | Assumption | Evidence |
|---|---|---|
| A1 | All 8 `src/RawRabbit.Operations.*` projects exist with the file counts in §1 | `find src/RawRabbit.Operations.*/ -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*'` → Get=11, MessageSequence=13, Publish=8, Request=15, Respond=19, StateMachine=17, Subscribe=7, Tools=11; total 101 (matches parent §1 + §6) |
| A2 | All 8 `test/RawRabbit.Operations.*.Tests/` projects exist with valid csproj and 0 .cs files | `find` → all 8 dirs present; csproj per dir; `.cs` count = 0 each |
| A3 | Each Operations.*.Tests csproj references `RawRabbit` core + its own source assembly | Read `Get.Tests` and `Tools.Tests` csprojs — both ProjectReference `..\..\src\RawRabbit\RawRabbit.csproj` + `..\..\src\RawRabbit.Operations.X\RawRabbit.Operations.X.csproj`; pattern consistent across 8 |
| A4 | `IPipeContext` exists at `RawRabbit.Pipe.IPipeContext` | `src/RawRabbit/Pipe/IPipeContext.cs:5` declares `public interface IPipeContext` |
| A5 | Operations middleware contract shape is `public override Task InvokeAsync(IPipeContext context, CancellationToken token)` | 5 sampled middleware (Get/AckableResultMiddleware, Publish/PublishAcknowledgeMiddleware, Respond/ReplyToExtractionMiddleware, StateMachine/GlobalLockMiddleware, Tools/ExchangeDeclarationMiddleware) all match shape; Tools uses `default(CancellationToken)` default param — same signature |
| A6 | Operations middleware that reach for broker types: 6 (not "~7" as design draft said) | `grep -lE "\bIChannel\b\|\bIModel\b\|\bBasicConsumer\b\|\bEventingBasicConsumer\b" src/RawRabbit.Operations.*/Middleware/*.cs` → 6 hits across 4 projects (Get×2, Publish×2, Subscribe×1, Respond×1). Spec §2 reflects 6. |
| A7 | 10 `Operations.*` sources use `LogProvider.For<T>()` — list locked in §2 W4-D5 | `grep -rln "LogProvider\.For<" src/RawRabbit.Operations.*/` → 10 hits across 6 projects. Drives `[Collection("LogProviderState")]` opt-in scope. |
| A8 | 2 projects + 3 source files have namespace-collision risk (class named same as namespace last segment) | `grep` per-project for `class <ProjectName>`: MessageSequence has `StateMachine/MessageSequence.cs` + `Model/MessageSequence.cs`; Respond has `Acknowledgement/Respond.cs`. Other 6 projects: none. Drives W4-D7 alias-pattern scope. |
| A9 | Package versions: xunit 2.9.3, runner.visualstudio 2.8.2, NET.Test.Sdk 18.5.1, Moq 4.20.72, RabbitMQ.Client 5.0.1 | `Directory.Packages.props` lines confirm exact versions |
| A10 | `.editorconfig` mandates tabs | `grep -E "indent_style" .editorconfig` → `indent_style = tab` (with per-section indent_size overrides 4/2/2/2) |
| A11 | Aggregate skip count is 15 (RawRabbit.Tests=12 + ServiceCollection.Tests=1 + Polly.Tests=2) | Spec §6.3 acceptance reflects 15 baseline (initial design draft said 12 — corrected per A11 finding). `dotnet test` confirmed: 614/12, 21/1, 1/2 |
| A12 | `dotnet build -c Release` returns 0 errors at Wave 3 close | `dotnet build -c Release` → `0 Error(s)` |
| A13 | Empty Operations.*.Tests projects exit 0 with "No test is available" — Wave 1 scaffolding correct | `dotnet test test/RawRabbit.Operations.Get.Tests --no-build -c Release` → exit 0, message "No test is available" (clean scaffold; Wave 4 tests will populate) |
| A14 | RabbitMQ.Client = 5.0.1 (carry-forward dead end about `HandleBasicDeliver` `byte[]` still applies) | `Directory.Packages.props` line: `<PackageVersion Include="RabbitMQ.Client" Version="5.0.1" />` |
| A15 | StateMachine has NO timer/Task.Delay/Thread.Sleep code — no custom test fixture needed | `grep -lE "Timer\|Task\.Delay\|Thread\.Sleep" src/RawRabbit.Operations.StateMachine/**/*.cs` → 0 hits. `StateMachineBase.cs` uses `Stateless` library (event-driven via `FireAsync`). W4-R1 closed at spec-write. |
| A16 | Git status clean, branch `2.0`, HEAD `c5e78b4` (no in-flight conflicts on Operations.*.Tests) | `git status --short` empty; `git branch --show-current` = `2.0`; `git log -1` = `c5e78b4` |
| A17 | Parent §6 row 4 lower bound for Wave 4 is 80 net-new tests | Parent §5 estimate table row: `Operations.* (8 new test projects) | 8 | 101 | ~24-40 | ~80-120`; per-project parent §5 Operations bucket: `Estimated 8-15 tests per project → ~80-120 total` |
| A18 | `Middleware` base class is at `src/RawRabbit/Pipe/Middleware/Middleware.cs:7` (not in any Operations.* namespace) | `grep -rn "public abstract class Middleware\b" src/RawRabbit/Pipe/` returns the single hit. Wave 3's `using MiddlewareBase = ...` alias pattern still applies if Operations test files reference the abstract base by simple name within a `.Middleware` namespace |
| A19 | Each Operations.*.Tests dir contains only its csproj — no leftover test files / fixtures | `find test/RawRabbit.Operations.*.Tests/ -type f -not -path '*/obj/*' -not -path '*/bin/*'` → 8 hits, all csprojs |
| A20 | `Operations.*` sources do NOT contain `RawRabbitFactory.CreateSingleton` or `new RawRabbitFactory` (broker-trigger pattern absent) | `grep -rln "RawRabbitFactory\.CreateSingleton\|new RawRabbitFactory\b" src/RawRabbit.Operations.*/` → 0 hits. Broker-trigger pattern confined to RawRabbit core. |

---

## 12. Known issues, accepted as out of scope

User-acknowledged on 2026-05-14 during the Wave 4 brainstorm:

1. **6 broker-touching middleware get inline mocks** — no shared helper across the 8 Operations.*.Tests projects (W4-D2). If Wave 5 needs the same, decide at Wave 5 brainstorm whether to promote `BrokerMocks` to a shared `test/RawRabbit.TestHelpers/` csproj.
2. **10 `LogProvider.For<T>()` consumers extend the LogProviderState xUnit collection** from Wave 3's 26 → ≥36 classes after Wave 4. Phase 7 backlog item: refactor `LogProvider.For` to lazy/injected (would eliminate workaround). Wave 4 inherits the workaround pattern.
3. **Carry-forward from Wave 3:** all Phase 7 backlog items (LogProvider lazy refactor, ResilientChannelPool double-enumeration, IChannelFactory lazy connection, Subscription.Dispose extension-cast smell, 3 ExplicitAckMiddleware skipped paths) remain open. None are Wave 4's responsibility.
4. **Skip budget N=0–5 is a ceiling**, not a target. Wave 4 enters with no a-priori-known skips (W4-R1 closed; no other timer/race patterns surfaced at spec-write).
5. **Test-count estimate is a range (80–120); only the lower bound (80) is the acceptance gate.** Plan-write verification will lock per-project counts.
