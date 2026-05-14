# Modernization Phase 4.5 Wave 5 Design — `Enrichers.*` Coverage

**Source:** Phase 4.5 parent spec at `docs/specs/2026-05-13-modernization-phase-4.5-design.md` (parent on `origin/2.0`). Wave 5 is the fifth and final wave; Waves 1–4 shipped through `da11292` on `origin/2.0`.

**Inherits from parent spec (do NOT re-litigate here):**
- §1 in/out-of-scope source areas; §2 test project structure; §3 conventions; §4 A25 forbidden patterns; §5 per-area gate format and Enrichers bucket; §6 wave row 5 (`~12–20` test files, `~40–60` tests); §7 phase-wide acceptance; §8 decisions D1–D10 (notably **D4** excluding HttpContext/MessagePack/Protobuf/ZeroFormatter, **D8** one test project per source assembly with consolidation allowed, **D10** preserving existing skip annotations verbatim); §9 risks (notably R2 — broker-mock skip-when-blocked permitted with annotation); §10 validation shape; §13 known-issues acknowledgements.
- Wave 2 conventions captured in Wave 2 §3: BOM preservation, AAA-deletion full-line removal, `ThrowsAnyAsync<T>` over `ThrowsAsync<T>` for cancellations.
- Wave 3 conventions captured in Wave 3 spec + handoff: F1=(a) per-public-method strict (≥1 happy + ≥1 error per public method), `[Collection("LogProviderState")]` opt-in for `LogProvider.For<T>()`-reading test classes, namespace-collision alias pattern.
- Wave 4 conventions captured in Wave 4 spec §2 (3 mock patterns at `IPipeContext` boundary), §3 (carry-forward checklist), §7 (carry-forward dead-end list including new SDD-surfaced additions).

**Goal:** After Wave 5 ships, the 8 in-scope `Enrichers.*` source assemblies each have ≥1 happy + ≥1 error path test per public method (F1=(a) strict). Wave 5 lands ≥40 net-new passing tests across the 6 test projects (8 source assemblies → 6 test projects per parent §6, with `MessageContext` + `MessageContext.Respond` + `MessageContext.Subscribe` consolidated into a single `MessageContext.Tests`). Wave 5 introduces no new test-helper organisation pattern; tests use inline `Mock<IModel>` setups for Polly's broker-touching middleware (the only enricher in scope that touches broker types). Wave 5 closes Phase 4.5.

**Tech stack (inherited):** .NET 10 SDK 10.0.107 + C# 13; xUnit 2.9.3; xunit.runner.visualstudio 2.8.2; Microsoft.NET.Test.Sdk 18.5.1; Moq 4.20.72; RabbitMQ.Client 5.0.1 (legacy; Phase 5 modernizes); Polly 7.x (Polly.Tests inherits the package ref). No new package dependencies for Wave 5.

---

## 1. Scope

**In scope (8 source assemblies → 6 test projects; 6 SDD tasks):**

| # | Source assembly(ies) | Source files | Test project (state) | Wave 5 task |
|---|---|---:|---|---|
| 1 | `RawRabbit.Enrichers.Attributes` | 6 | `test/RawRabbit.Enrichers.Attributes.Tests/` (csproj only, 0 .cs) | T1 |
| 2 | `RawRabbit.Enrichers.GlobalExecutionId` | 11 | `test/RawRabbit.Enrichers.GlobalExecutionId.Tests/` (0 .cs) | T2 |
| 3 | `RawRabbit.Enrichers.MessageContext` (8) + `MessageContext.Respond` (2) + `MessageContext.Subscribe` (3) — **3 source assemblies consolidated** per parent §6 | 13 | `test/RawRabbit.Enrichers.MessageContext.Tests/` (0 .cs) | T3 |
| 4 | `RawRabbit.Enrichers.Polly` | 15 | `test/RawRabbit.Enrichers.Polly.Tests/` (**2 .cs / 1 active + 2 skipped — preserve verbatim per parent D10**) | T4 |
| 5 | `RawRabbit.Enrichers.QueueSuffix` | 9 | `test/RawRabbit.Enrichers.QueueSuffix.Tests/` (0 .cs) | T5 |
| 6 | `RawRabbit.Enrichers.RetryLater` | 9 | `test/RawRabbit.Enrichers.RetryLater.Tests/` (0 .cs) | T6 |

**Source-file totals:** 63 files in-scope. (Parent §5 row 5 estimated 52 — minor drift documented in §11 A19; doesn't change wave shape.)

**Project shape variance** (from §11 verification A1, A5, A12):

- **Polly (T4) — special handling required.** Polly is the **only in-scope enricher with broker-touching middleware** (5 source files reference `IModel`/`IConnection`/`IChannel`/`BasicConsumer`/`ConnectionFactory`). 8 of Polly's 9 middleware **subclass core `Pipe.Middleware.<Name>Middleware` and override protected hooks** (`DeclareQueueAsync`, `BasicPublish`, etc.) — they do NOT directly override `InvokeAsync`. Only `PolicyMiddleware : StagedMiddleware` is a "direct" middleware. T4's test pattern reflects this (see §2). Polly.Tests already has 2 .cs files at `Services/ChannelFactoryTests.cs` (2 skipped) + `Middleware/QueueDeclareMiddlewareTests.cs` (1 active); T4 mirrors the existing subdirectory structure for new tests.
- **MessageContext (T3) — 3-source-assembly consolidation per parent §6 / D8.** Test files mirror source structure under `MessageContext.Tests/{Base,Respond,Subscribe}/<TypeUnderTest>Tests.cs` to keep navigability. Namespace collision risk: `PipeContextExtensions` class declared in all 3 source assemblies (verified §11 A11) — apply `using <Alias> = ...` pattern as needed.
- **GlobalExecutionId (T2) — 6 middleware (most in any enricher).** F1=(a) strict may push T2 above the 4–12 per-enricher estimate; acceptable per parent §5 ("estimated, not capped"). 4 of GlobalExecutionId's middleware use `LogProvider.For<T>()` (verified §11 A10) → 4 test classes need `[Collection("LogProviderState")]`.
- **RetryLater (T6) — header round-trip codec.** Contains `RetryInformationProvider`/`RetryHeaders`/`RetryInformation`/`RetryInformationHeaderUpdater` — header `string` ⇄ struct codec needs explicit error-path coverage (W5-R3). 1 RetryLater middleware (`RetryLaterMiddleware`) uses `LogProvider.For<T>()` → 1 test class needs `[Collection("LogProviderState")]`.
- **Attributes / QueueSuffix — pure-`IPipeContext` middleware.** Standard 3-pattern mock approach from §2 applies cleanly; no broker mocks, no `[Collection]` opt-in.

**Out of scope** (carries forward from parent §1 + parent D4 + Wave 4 scope split):

- 4 Enrichers excluded by parent D4: `HttpContext` (no-op stub; Phase 1 V2), `MessagePack` (Phase 7), `Protobuf` (Phase 7), `ZeroFormatter` (Phase 7). Each is a moving target — testing now risks rework when the underlying decision is made (verified §11 A21 — these 4 are only consumed externally by `RawRabbit.IntegrationTests` + sample app, both Phase 6+ territory).
- Live-broker integration (Phase 6).
- `src/RawRabbit.Enrichers.*/` source modifications (test-only wave).
- Cross-project `BrokerMocks` consolidation (W5-D2 — declined; only Polly has broker types so there's nothing to deduplicate).
- Phase 7 deprecation candidates and refactor backlog (LogProvider lazy refactor, ResilientChannelPool double-enumeration, RespondConfigurationMiddleware:31 source bug from Wave 4, AckableResultOptions internal-set visibility, etc.).

**Wave 5's place in Phase 4.5:**

| Wave | Status | Through |
|---|---|---|
| 1 | ✅ Shipped | `9af8650` |
| 2 | ✅ Shipped | `9e91f10` |
| 3 | ✅ Shipped | `c5e78b4` |
| 4 | ✅ Shipped | `da11292` |
| **5** | **This spec** | **— (Enrichers.\*)** |

Phase 4.5 closes when Wave 5 ships and parent §7 acceptance gates 1–7 hold against the post-Wave-5 aggregate (≥257 phase-wide passing target; current Wave-4 baseline is 1191 → Phase 4.5 will exceed comfortably).

---

## 2. Test architecture

**Per parent D3 — mock at `IPipeContext` boundary (NOT broker boundary).** Each Enricher middleware test:

1. Instantiates the middleware-under-test directly (its public ctor signature; pass `Mock<I*>`-derived dependencies as needed)
2. Set up the data the middleware will read from `IPipeContext`. **Three patterns** are valid (Wave 4 §2 carry-forward); pick per test based on what's being asserted:
   - **`Mock<IPipeContext>` + Properties dictionary:** `var ctx = new Mock<IPipeContext>(); var props = new Dictionary<string, object> { [PipeKey.MessageContext] = mockContext, ... }; ctx.Setup(c => c.Properties).Returns(props);` — extension methods like `context.GetMessageContext()` then read from this dictionary. Use when the test asserts via `ctx.Verify(...)` on context interactions.
   - **Concrete `PipeContext`:** `var ctx = new PipeContext { Properties = new Dictionary<string, object> { [PipeKey.MessageContext] = mockContext } };` — pass `ctx` directly to `InvokeAsync`. Simpler when the test asserts on outputs / dependency mocks rather than context interactions.
   - **Bypass via options injection:** for middleware whose options expose `*Func` delegates (e.g., `QueueSuffixOptions`, `RetryLaterOptions`), construct with a custom func that returns the desired value directly. No `IPipeContext` setup needed for that read path.

   **Why three patterns, not `.Setup(c => c.GetXyz())`:** all `Get*(this IPipeContext)` calls are extension methods on `IPipeContext.Properties`, NOT interface members. `Mock<IPipeContext>().Setup(c => c.GetMessageContext())` does NOT compile (Moq cannot intercept static extension methods). Plan-write verification enumerates the `PipeKey.*` constants per source class so each test knows which dictionary keys to populate.
3. Calls `await middleware.InvokeAsync(ctx.Object, CancellationToken.None)` (or `CancellationToken` token-cancellation tests use `await Assert.ThrowsAnyAsync<OperationCanceledException>(...)`)
4. Asserts on context mutations via `.Verify(c => c.Properties.Add(...), Times.Once)` etc., AND/OR on dependency-mock invocations

**Polly's inheritance pattern (T4-specific — verified §11 A12).** 8 of Polly's 9 middleware subclass core `Pipe.Middleware.<Name>Middleware` (e.g., `Polly.Middleware.QueueDeclareMiddleware : Pipe.Middleware.QueueDeclareMiddleware`) and override `protected` hooks like `DeclareQueueAsync`, `BasicPublish`, `BindQueueAsync`, etc. — wrapping the base call in `_policy.Execute(...)` or `_policy.ExecuteAsync(...)`. The PUBLIC entry point (`InvokeAsync` on the parent class) is unchanged. Test pattern for these 8 middleware:

1. Instantiate the Polly subclass with its required dependencies (e.g., `Mock<IPolicyProvider>`, `Mock<INamingConventions>`)
2. Set up the parent's required `IPipeContext` state (e.g., for `QueueDeclareMiddleware`, populate `PipeKey.QueueDeclaration` so the parent's `InvokeAsync` reaches the protected `DeclareQueueAsync` override)
3. Call the inherited public `InvokeAsync(ctx, token)` — execution flows: parent `InvokeAsync` → parent's reach-the-hook logic → **Polly's protected override** → wraps base call in `_policy.Execute(...)` → calls `base.DeclareQueueAsync(...)`
4. Assertion target shifts: instead of "did the middleware mutate context", assert "did the policy execute" + "did the underlying broker call happen inside the policy" (e.g., via a counting policy or a `Mock<IModel>` `Verify(...)` inside a Polly callback)

`PolicyMiddleware : StagedMiddleware` (the 9th Polly middleware) is a "direct" middleware — standard 3-pattern mock approach applies. `ChannelFactory` (a service, not a middleware) is tested separately and is the source of Polly.Tests' existing 2 skipped tests (W5-R1).

**Inline broker mocks where needed.** Per §11 A9, Polly is the **sole in-scope enricher** that references broker types (`IModel`, `IConnection`, `IChannel`, `BasicConsumer`, `ConnectionFactory`) — 5 source files. T4 sets up `var ch = new Mock<IModel>(); ch.Setup(...).Returns(...);` inline (no shared helper). The other 5 in-scope enrichers (Attributes, GlobalExecutionId, MessageContext+sub, QueueSuffix, RetryLater) have **0 broker-type references** in source — pure `IPipeContext`-mock work.

**Test file granularity.** One test file per source class default. Mirrors `src/RawRabbit.Enrichers.X/<subdir>/<TypeUnderTest>.cs` → `test/RawRabbit.Enrichers.X.Tests/<subdir>/<TypeUnderTest>Tests.cs`. T3 (MessageContext consolidation) uses `Base/Respond/Subscribe` subdirs to keep navigability across the 3 source assemblies. T4 (Polly) mirrors existing `Services/` + `Middleware/` subdirs (verified at `test/RawRabbit.Enrichers.Polly.Tests/Services/ChannelFactoryTests.cs` + `Middleware/QueueDeclareMiddlewareTests.cs`).

**F1=(a) strict carry-forward.** ≥1 happy + ≥1 error path test per **public method** on every source class with public surface (Wave 2/3/4 lesson). For Polly's 8 inherited middleware, F1=(a) applies at the **protected override** level (each `DeclareQueueAsync` / `BasicPublish` / etc. needs ≥1 happy + ≥1 error). Implementer prompts MUST explicitly enumerate "public methods (and Polly protected overrides) needing error tests" per source class — Wave 4 lesson: implementers err on the side of optimism ("simplified", "deferred to phase 5/7") when mock setup is non-trivial; reviewer catches by reading source.

**`[Collection("LogProviderState")]` opt-in.** 5 in-scope `Enrichers.*` source files use `LogProvider.For<T>()` (verified §11 A10); test classes for these 5 sources MUST add the `[Collection("LogProviderState")]` attribute to serialize against `LogExtensionsTests` static mutation:

| Project | Source class needing `[Collection]` on its test |
|---|---|
| `Enrichers.GlobalExecutionId` (T2) | `Middleware/GlobalExecutionIdMiddleware`, `Middleware/WildcardRoutingKeyMiddleware`, `Middleware/AppendGlobalExecutionIdMiddleware`, `Middleware/ExecutionIdRoutingMiddleware` |
| `Enrichers.RetryLater` (T6) | `Middleware/RetryLaterMiddleware` |

LogProviderState collection grows from ≥40 (post-Wave-4) → **≥45** after Wave 5 (4 new from T2 + 1 new from T6). Phase 7 backlog: lazy `LogProvider.For` would eliminate the workaround (carries forward from Wave 3/4 §12).

**Namespace-collision alias pattern.** T3 (MessageContext consolidation) is the at-risk task. Verified collisions (§11 A11):

| Subdir under `MessageContext.Tests/` | Source class colliding with namespace last segment |
|---|---|
| `Base/` | `PipeContextExtensions.cs`, `MessageContext.cs` (class named same as namespace last segment); `MessageContextRepository.cs` |
| `Respond/` | `PipeContextExtensions.cs` (same name as in `Base/` namespace) |
| `Subscribe/` | `PipeContextExtensions.cs` (same name as in `Base/` and `Respond/`) |

If T3 test files reference the `MessageContext` class by simple name within a `Tests.Base` namespace, use the same `using <Alias> = <fully.qualified.Type>;` pattern Wave 3/4 used. Subdirectory namespaces (`Tests.Base`, `Tests.Respond`, `Tests.Subscribe`) reduce collision frequency relative to a flat layout. Plan-write verification confirms which test files trigger the collision.

---

## 3. Conventions inherited (carry-forward checklist; do NOT re-litigate)

- xUnit 2.9.3 + Moq 4.20.72 + Microsoft.NET.Test.Sdk 18.5.1 (parent §3; verified §11 A14)
- A25 forbidden patterns (parent §4): no `Assert.True(true)`, no hand-rolled try/catch around code-under-test, no non-generic `Assert.IsType`, no `/* AAA */` block comments, no `Task.Wait()` polling
- Test class naming: `<TypeUnderTest>Tests`; method naming: `Should_Verb_Subject` PascalCase with underscores (parent §3)
- `async Task` + `await`, never `.Wait()` / `.Result` (parent §3)
- BOM-mandatory on all new `.cs` files (Wave 2 lesson; verified per-file via `for f in <files>; do file "$f"; done` post-creation)
- AAA full-line removal during cleanup, not blank replacement (Wave 2 lesson) — N/A for Wave 5 (no in-place cleanup; greenfield test files only)
- `await Assert.ThrowsAnyAsync<OperationCanceledException>(...)` for cancellation tests, NOT `ThrowsAsync<>` (Wave 2/3 lesson)
- `[Theory]/[InlineData]` opportunistic where 3+ tests share input shape (parent §3 + acceptance gate 7 — phase-wide ≥3 places, not wave-local; Waves 1–4 already satisfied this gate)
- Indentation: tabs (per `.editorconfig` `indent_style = tab`; verified §11 A15)
- Skip annotation format: `[Fact(Skip = "Phase 5/7 territory: <specific signature/symptom>")]` exact (Wave 3/4 convention; Polly's existing 2 skips already match — verified §11 A6/A7)

---

## 4. Skip budget (gate 7)

- **N=0–5 fresh budget for Wave 5** (carries from parent R2 + §7).
- **Pool allocation, not per-project** (W5-D6 — matches Wave 4 W4-D4).
- **Polly's existing 2 skips do NOT count against Wave 5 budget** (W5-D4 — they're inherited per parent D10).
- Each new skip MUST use exact format above.
- Plan-write verification flags any source class likely to need a skip; SDD implementer reports each skip with justification; spec/quality reviewer validates; final wave-wide review confirms total ≤5.

Wave 5 enters with one a-priori-known skip risk (W5-R1 — Polly's `ChannelFactoryTests` skip pattern may extend to new ChannelFactory tests in T4 if the same 2-arg signature mismatch surfaces; counts against the N≤5 pool). Skips below the budget cap are fine; the budget is a ceiling, not a target.

---

## 5. Per-area gate format

Spec commits to **shape** per project (W5-D5). **Plan locks exact test-method enumeration** at plan-write verification time by reading each source file. Each project's gate:

- **Source files in scope** — full path list (locked at plan-write time)
- **Test file paths to create** — one per source class default; T3 uses `Base/Respond/Subscribe` subdirs; T4 mirrors existing `Services/`+`Middleware/` subdirs
- **Public-method coverage** — every public method on every public class gets ≥1 happy + ≥1 error test (F1=(a) strict per Wave 2/3/4 carry-forward); for Polly's 8 inherited middleware, applies at protected-override level
- **Mocking surface** — context surface per §2's three patterns; project-specific `Mock<I*>` for direct dependencies (e.g., `Mock<IPolicyProvider>`, `Mock<INamingConventions>`, `Mock<IRetryInformationProvider>`); inline `Mock<IModel>`/`Mock<IConnection>` for Polly's broker-touching middleware (T4 only)
- **`[Collection("LogProviderState")]`** — required on test classes for the 5 `LogProvider.For<T>()`-reading sources listed in §2 (4 in T2, 1 in T6)
- **Namespace alias** — required on T3 test files in `Base/` referencing the `MessageContext` class, or any T3 file referencing `PipeContextExtensions` ambiguously across the 3 sub-namespaces

Per-project test count target inherited from parent §5 Enrichers bucket: **4–12 tests per enricher** (with 6 test projects × 8 source assemblies → ≥40 wave total per acceptance §6.4). T2 (GlobalExecutionId, 6 middleware) and T4 (Polly, 9 middleware + ChannelFactory) likely exceed the 4–12 range — that's expected and acceptable.

---

## 6. Wave-wide acceptance (Wave 5 done when ALL hold)

1. **6 tasks shipped** (T1–T6) — each as a separate commit (or commit + fix-up cluster) with passing per-task spec/quality review.
2. **`dotnet test test/RawRabbit.Enrichers.<X>.Tests --no-build -c Release`** for each of the 6 projects → 0 failed; per-project skip count ≤5 in aggregate (wave-wide pool per §4); Polly's existing 2 skips inherited (do NOT count against Wave 5 budget).
3. **Aggregate `dotnet test`** (per-project loop across all 16 unit-test projects post-Wave-5: 1× RawRabbit.Tests + 1× ServiceCollection.Tests + 8× Operations.\*.Tests + 6× in-scope Enrichers.\*.Tests; matches parent §7 condition 2) → still 0 failed; aggregate skip count = 15 (existing baseline at HEAD `da11292`) + (Wave 5's actual N, where N≤5) — i.e., no Wave 5 work breaks Waves 1–4.
4. **Test count target:** ≥40 net-new passing tests across the 6 projects (parent §6 row 5 lower bound; verified §11 A19).
5. **Per-area gate satisfied** for all 6 projects — every public method has ≥1 happy + ≥1 error test (per the plan's locked enumeration); Polly's inherited middleware tested at protected-override level.
6. **Build clean:** `dotnet build -c Release` → 0 errors; warning shape stable from Wave 4 close (8 NU1902 MessagePack vulnerability warnings inherited; verified §11 A16).
7. **3-run stability:** aggregate `dotnet test` run 3× consecutively, all 3 reporting identical pass/fail/skip counts (matches Wave 3/4 final verification — race detector).
8. **Wave-wide review** dispatched after T6 — spec reviewer + code quality reviewer go over the entire wave's diff one last time before push (catches cross-task issues like the LogProvider race Wave 3 hit at Task 9).
9. **Phase 4.5 phase-wide acceptance** (parent §7 gates 1–7) re-evaluated against the post-Wave-5 aggregate. Expected outcome: all 7 gates pass (Phase 4.5 closes).

---

## 7. Carry-forward dead ends (do NOT repeat)

Lifted from Wave 4 handoff §3 + Wave 4 §7 + Wave 1–3 carry-forward (verified §11 A14 — RabbitMQ.Client still 5.0.1; A21+A22 — out-of-scope and in-scope enrichers have no unit-test conflict shape):

**Wave 4 SDD-surfaced additions (NEW, apply identically to Wave 5):**
- Implementer claim "extension method on IBusClient is unmockable" — false; mock the underlying `InvokeAsync` instead (Wave 4 T1 fix-up `2dee3ab`)
- Implementer claim "deferred to Phase 5/7 acceptance testing" / "simplified to construction-only" for non-trivial mock setup — false; if the plan enumerates the method, it MUST have actual behavior tests (Wave 4 T2/T4 fix-ups `d7862a6` + `0165744`)
- Adding null-arg tests for ctors that just store fields (false-positive class — those are happy paths in disguise) — verify against source before applying reviewer null-arg findings (Wave 4 T3 rejected 2 of 6 reviewer findings at `1b0d140`)
- `Mock<BasicGetResult>` (RMQ.Client 5.0.1 ctor not Moq-proxiable — use real instances) — N/A for Wave 5 unless Polly tests touch `BasicGetResult`; Polly's broker types are `IModel`/`IConnection`/`ConnectionFactory` per §11 A9
- `.Setup(c => c.QueueNamingConvention(typeof(X)))` on `Mock<INamingConventions>` — `QueueNamingConvention`/`RoutingKeyConvention` are `Func<Type, string>` properties, NOT methods; use `.SetupGet(c => c.QueueNamingConvention).Returns((Type t) => "...")` — applies to T4 if Polly tests mock `INamingConventions`
- `Stateless` library disallows duplicate `SetTriggerParameters<T>` for same T — N/A for Wave 5 (no Stateless in Enrichers)

**Wave 1–3 carry-forward (still applies):**
- `Mock.SetupSequence` for ResilientChannelPool ctor seed (use `Mock.Setup().ReturnsAsync()`) — does not apply to Enrichers directly but lesson generalises
- Testing `RawRabbitFactory.CreateSingleton` (triggers real broker connect at test time)
- `Mock<IBasicConsumer>` for Subscription.Dispose-style tests (use `EventingBasicConsumer`) — applies if T4 Polly tests touch consumer creation
- Resolving `IBusClient` in DI happy-path tests (broker trigger) — N/A for Wave 5 (no DI tests)
- `Assert.True(true)` placeholders / async tests with no assertion (A25 anti-pattern)
- `Assert.ThrowsAsync<>` for cancellation (use `ThrowsAnyAsync<OperationCanceledException>` per VP-25)
- Default xUnit parallelism on `LogProvider.For<T>()`-reading test classes (use `[Collection("LogProviderState")]` per §2 + §6)
- `ReadOnlyMemory<byte>` for `HandleBasicDeliver` body arg (use `byte[]` for RMQ.Client 5.0.1) — N/A for Wave 5 unless Polly tests trigger consumer delivery
- Trusting subagent commit SHAs (always verify via `git log -1 --format=%H`)
- `SendMessage` to subagents (one-shot only — bake answers into prompt; tool not available in this harness)
- `Mock<IPipeContext>().Setup(c => c.GetXyz())` — `Get*` are extension methods, not interface members; use `.Setup(c => c.Properties).Returns(dict)`
- `dotnet test` on the full solution to compute aggregate (includes `RawRabbit.IntegrationTests`, ~12 failing without broker — use per-project loop)

---

## 8. Decisions

| # | Decision | Rationale | Alternatives rejected |
|---|----------|-----------|----------------------|
| W5-D1 | 6 SDD tasks, 1 per test project | User-confirmed; mirrors W4-D1 cadence; aligns with the 6 already-scaffolded test projects (verified §11 A3) | 7 tasks (split Polly into Polly-A + Polly-B); 5 tasks (consolidate Attributes + QueueSuffix) — both add dispatch overhead or break the "1 task per test project" cadence |
| W5-D2 | Inline `Mock<IModel>`/`Mock<IConnection>` for Polly's broker-touching middleware (T4 only); **do NOT promote `BrokerMocks` to shared `test/RawRabbit.TestHelpers/` csproj** | W4-D2 deferred this to "Wave 5+ if duplication count justifies"; Wave 5 has only Polly with broker types — zero cross-project duplication. YAGNI: nothing to deduplicate. Phase 6 (live broker) may revisit when integration-test stand-up creates real reuse | Promote `BrokerMocks` proactively (no consumers; would be dead weight) |
| W5-D3 | T3 consolidates `MessageContext` + `MessageContext.Respond` + `MessageContext.Subscribe` into single `MessageContext.Tests` per parent §6 / D8 | Parent already settled this via Wave 1's scaffolding choice (verified §11 A8 — `MessageContext.Tests.csproj` already references all 3 source assemblies); `Base/Respond/Subscribe` subdirectory structure preserves source-mirror navigability and reduces namespace-collision frequency | Split into 3 test projects (would un-do parent decision; create 2 new csprojs against D8 grain) |
| W5-D4 | Polly's existing 2 `.cs` files (1 active in `Middleware/QueueDeclareMiddlewareTests.cs` + 2 skipped in `Services/ChannelFactoryTests.cs`) preserved verbatim per parent D10; new tests ADD to them | Skip annotations are accurate Phase 5/7 documentation (verified §11 A6/A7 — exact match to format); rewriting now risks losing context. Phase 5/7 reactivates and re-asserts | Modernize Polly's existing tests as part of T4 (drift from parent D10) |
| W5-D5 | Test-method enumeration deferred to plan-write verification | Carry-forward W4-D3; spec commits to shape, plan locks numbers after reading each source file. Wave 4 followed same pattern successfully | Lock per-project counts in spec (gold-plates spec; can't verify without reading source); commit to upper-bound only (under-specified) |
| W5-D6 | Skip budget pool wave-wide N=0–5 (Polly's existing 2 don't count) | Carry-forward W4-D4; Polly's 2 skips inherited per parent D10 | Per-project sub-budget (rigid; forces unused budget to be wasted) |
| W5-D7 | Wave-wide review after T6 (no separate T7) | Carry-forward W4-D6; verification is implicit pre-ship discipline, not a deliverable | T7 explicit (matches Wave 3 numbering — adds bureaucracy without changing work) |

---

## 9. Risks & responses

| # | Risk | Response |
|---|---|---|
| W5-R1 | Polly's existing `ChannelFactoryTests` skip pattern ("2-arg ConnectionFactory.CreateConnection signature mismatch with 1-arg mock setups") is a Phase 5/7 issue; ANY new ChannelFactory test in T4 may hit the same mismatch | T4 budget allows additional skips against the N≤5 wave pool; annotate exactly per format. If 2+ ChannelFactory tests need skipping, that's still <5 — acceptable. Plan-write verification reads `ConnectionFactory` consumer code in Polly to scope the skip risk before T4 dispatch |
| W5-R2 | T3 (MessageContext consolidation) surfaces namespace collisions across the 3 source assemblies (verified §11 A11: `PipeContextExtensions` declared in all 3; `MessageContext` class collides with namespace name) | Use `using <Alias> = ...` pattern from Wave 3/4 (parent §3 carry-forward); subdirectory namespaces (`Tests.Base`, `Tests.Respond`, `Tests.Subscribe`) reduce collision frequency. Plan-write verification confirms which T3 test files trigger collision |
| W5-R3 | RetryLater's `RetryInformationProvider`/`RetryHeaders`/`RetryInformation` round-trip a `string` ⇄ struct codec — easy to write happy paths but error paths (malformed header, missing required key, overflow on large counts, null inputs) easy to miss | Implementer prompt MUST enumerate "decode malformed header" + "decode missing required key" + boundary tests explicitly per F1=(a); spec reviewer cross-checks |
| W5-R4 | Same Wave 4 risks (W4-R5 reviewer false-positive rate ~3 per wave; W4-R6 F1=(a) gaps in async try/catch wrappers; W4-R7 BOM gap on new files) | Same mitigations: per-finding source verification before applying fix; explicit "public methods (and Polly protected overrides) needing error tests" per source class in implementer prompt; post-creation `for f in <files>; do file "$f"; done` |
| W5-R5 | GlobalExecutionId has 6 middleware (most in any in-scope enricher) — F1=(a) strict pushes T2's test count above the 4–12 per-enricher estimate | Acceptable per parent §5 ("estimated, not capped"); wave-wide ≥40 is the gate, not per-project. T2 may land 12–18 tests; that's still within the 40–60 wave envelope |
| W5-R6 | T4 Polly inheritance pattern (verified §11 A12): 8 of 9 middleware override protected hooks on inherited core middleware (`DeclareQueueAsync`, `BasicPublish`, etc.) NOT `InvokeAsync`. Implementer may try to call protected methods via reflection (test smell) or skip the parent's `InvokeAsync` entry-point (loses real flow coverage) | Spec §2 explicitly documents the test pattern: instantiate Polly subclass → set up parent's required `IPipeContext` state → call inherited `InvokeAsync` → assert on policy execution + base broker call inside policy. Implementer prompt for T4 calls this out explicitly. Reviewer cross-checks that tests use the inherited public entry-point, not reflection-based protected access |
| W5-R7 | Phase 4.5 closes after Wave 5 — parent §7 phase-wide acceptance gates 1–7 must hold against post-Wave-5 aggregate (≥257 phase-wide passing target) | Wave-wide review (§6.8) re-runs parent §7 gates 1–7 verification; current Wave-4 baseline = 1191 passing → Wave 5 adds ≥40 → post-Wave-5 ≥1231 → gate 2 (≥257) passes comfortably. Other gates (build clean, skip count drift, `[Theory]` ≥3 places, etc.) verified at wave close |

---

## 10. Tasks NOT in this spec

(Carries forward parent §12 + Wave 3/4 scope split + parent D4 exclusions)

- Per-project exact test-method names — plan-write verification (W5-D5)
- `src/RawRabbit.Enrichers.*/` source modifications — test-only wave (parent §1)
- `test/RawRabbit.IntegrationTests/*` cleanup — Phase 6 (live broker)
- Un-skipping the existing 15 skipped tests across the 3 stable projects (RawRabbit.Tests=12, ServiceCollection.Tests=1, Polly.Tests=2) — Phase 5/7
- HttpContext, MessagePack, Protobuf, ZeroFormatter test coverage — parent D4 (Phase 7 / Phase 1 V2); verified §11 A21 (these 4 only consumed externally by `RawRabbit.IntegrationTests` + sample app)
- `BrokerMocks` promotion to shared csproj — W5-D2 declined again; reconsider in Phase 6 (live broker) when integration-test stand-up creates real cross-project reuse
- Refactor of `LogProvider.For<T>()` static-read to lazy/injected — Phase 7 backlog from Wave 3/4 (LogProviderState collection grows ≥40 → ≥45 after Wave 5)
- Source bug at `src/RawRabbit.Operations.Respond/Middleware/RespondConfigurationMiddleware.cs:31` (uses `RequestTypeFunc` instead of `ResponseTypeFunc`) — Phase 7 backlog from Wave 4
- `AckableResultOptions<T>` `internal set` visibility relaxation — Phase 7 backlog from Wave 4
- T4 `Co-Authored-By` trailer fix on commit `0165744` — cosmetic; would require force-push (not worth it)
- All Wave 1–4 Phase 7 backlog items — remain open; none Wave 5's responsibility
- Phase 5/6/7 work generally

A new spec → new plan cycle is required to add any of the above to a future phase.

---

## 11. Verified assumptions

The following 25 assumptions were enumerated COLD (against the design alone, before any verification reads) and then verified empirically against HEAD `da11292` at spec-write time on 2026-05-14:

| # | Assumption | Evidence | Status |
|---|---|---|---|
| A1 | All 8 in-scope `src/RawRabbit.Enrichers.<X>/` source assemblies exist with file counts: Attributes=6, GlobalExecutionId=11, MessageContext=8, MessageContext.Respond=2, MessageContext.Subscribe=3, Polly=15, QueueSuffix=9, RetryLater=9 (total 63) | `find src/RawRabbit.Enrichers.{Attributes,GlobalExecutionId,MessageContext,MessageContext.Respond,MessageContext.Subscribe,Polly,QueueSuffix,RetryLater}/ -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*'` → counts match exactly; total = 63 | ✅ |
| A2 | The 4 out-of-scope `src/RawRabbit.Enrichers.<X>/` source assemblies (HttpContext, MessagePack, Protobuf, ZeroFormatter) exist (D4 exclusion is real, not phantom) | `find` per-dir → all 4 dirs present; HttpContext=4 .cs, MessagePack=3, Protobuf=2, ZeroFormatter=2 | ✅ |
| A3 | All 6 in-scope `test/RawRabbit.Enrichers.<X>.Tests/` directories exist with valid csproj | `ls test/RawRabbit.Enrichers.<X>.Tests/*.csproj` → all 6 csprojs present | ✅ |
| A4 | 5 of 6 in-scope test projects contain ZERO `.cs` files (Attributes/GlobalExecutionId/MessageContext/QueueSuffix/RetryLater) | `find test/RawRabbit.Enrichers.<X>.Tests/ -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' \| wc -l` → 0 for those 5 | ✅ |
| A5 | Polly.Tests contains EXACTLY 2 `.cs` files at `Services/ChannelFactoryTests.cs` + `Middleware/QueueDeclareMiddlewareTests.cs` | `find` → 2 files; subdirectory structure confirmed (T4 mirrors this for new tests) | ✅ |
| A6 | Polly.Tests has 1 active `[Fact]` + 2 `[Fact(Skip=...)]` tests; skip annotation matches format `[Fact(Skip = "Phase 5/7 territory: ...")]` | `grep -rn 'Skip\s*=' test/RawRabbit.Enrichers.Polly.Tests/` → 2 matches at `Services/ChannelFactoryTests.cs:16` + `:52`, both `[Fact(Skip = "Phase 5/7 territory: 2-arg ConnectionFactory.CreateConnection signature mismatch with 1-arg mock setups; tests need re-mocking when broker layer is modernized")]` | ✅ |
| A7 | ChannelFactoryTests' skip reason mentions "2-arg ConnectionFactory.CreateConnection" + "1-arg mock setups" (W5-R1 specificity) | Verbatim match in A6 evidence above | ✅ |
| A8 | Each of the 6 in-scope `Enrichers.*.Tests` csproj references RawRabbit core + its own source assembly + xunit/Moq/NET.Test.Sdk stack | `grep -E 'ProjectReference\|PackageReference' test/RawRabbit.Enrichers.<X>.Tests/*.csproj` per-project → all 6 match. MessageContext.Tests has 3 ProjectReferences (Base + Respond + Subscribe). Polly.Tests has additional `<PackageReference Include="Polly" />` | ✅ |
| A9 | Polly is the SOLE in-scope enricher whose source files reference broker types (`IModel`, `IConnection`, `IChannel`, `BasicConsumer`, `EventingBasicConsumer`, `BasicGetResult`, `ConnectionFactory`) | `grep -lE '\b(IModel\|IConnection\|IChannel\|BasicConsumer\|EventingBasicConsumer\|BasicGetResult\|ConnectionFactory)\b' src/RawRabbit.Enrichers.<X>/` per-project → Attributes=0, GlobalExecutionId=0, MessageContext=0, MessageContext.Respond=0, MessageContext.Subscribe=0, **Polly=5**, QueueSuffix=0, RetryLater=0 | ✅ |
| A10 | NO in-scope enricher source uses `LogProvider.For<T>()` | `grep -rln 'LogProvider\.For<' src/RawRabbit.Enrichers.{Attributes,...}/` → **5 files DO use it**: `GlobalExecutionId/Middleware/{GlobalExecutionId,WildcardRoutingKey,AppendGlobalExecutionId,ExecutionIdRouting}Middleware.cs` (4) + `RetryLater/Middleware/RetryLaterMiddleware.cs` (1). Drives `[Collection("LogProviderState")]` opt-in scope per §2/§6 (T2: 4 classes; T6: 1 class) | ❌ **Broken — but mechanical: spec body adjusted to enumerate the 5 sources** |
| A11 | MessageContext + MessageContext.Respond + MessageContext.Subscribe each declare a `PipeContextExtensions` class (or another colliding name) | `grep -rn 'class PipeContextExtensions' src/RawRabbit.Enrichers.MessageContext{,.Respond,.Subscribe}/` → 3 matches (one per sub-project). Additional collision: `class MessageContext` declared in `MessageContext` sub-project (collides with namespace last segment). Drives W5-R2 alias-pattern scope | ✅ |
| A12 | Sample 1 middleware per in-scope enricher: signature is `public override Task InvokeAsync(IPipeContext, CancellationToken)` (matches Wave 4 A5) | 5 of 6 enrichers match exactly: `Attributes/Middleware/{Consume,Produce}AttributeMiddleware.cs:31`, `GlobalExecutionId/Middleware/GlobalExecutionIdMiddleware.cs:39`, `MessageContext/Middleware/PublishForwardingMiddleware.cs:18`, `QueueSuffix/QueueSuffixMiddleware.cs:34`, `RetryLater/Middleware/RetryLaterMiddleware.cs:45`. **Polly does NOT match for 8 of 9 middleware** — they subclass core `Pipe.Middleware.<Name>Middleware` and override **protected** hooks (`DeclareQueueAsync`, `BasicPublish`, etc.); only `Polly.Middleware.PolicyMiddleware:25` overrides `InvokeAsync` directly | ⚠️ **Partial — material to T4: §2 documents Polly's inheritance pattern explicitly + W5-R6 risk added** |
| A13 | `IPipeContext` still at `RawRabbit.Pipe.IPipeContext` (Wave 4 A4 carry-forward) | `head src/RawRabbit/Pipe/IPipeContext.cs` → `namespace RawRabbit.Pipe { public interface IPipeContext { IDictionary<string, object> Properties { get; } } ... }` | ✅ |
| A14 | `Directory.Packages.props` matches Wave 4 A9 versions: xunit 2.9.3, runner.visualstudio 2.8.2, NET.Test.Sdk 18.5.1, Moq 4.20.72, RabbitMQ.Client 5.0.1 | `grep` confirms exact versions | ✅ |
| A15 | `.editorconfig` still mandates tabs (`indent_style = tab`) | `grep` line 12 = `indent_style = tab` | ✅ |
| A16 | `dotnet build -c Release` returns 0 errors at HEAD `da11292` | `dotnet build -c Release` → `0 Error(s)` + 8 warnings (all NU1902 MessagePack vulnerability — same shape as Wave 4 close) | ✅ |
| A17 | Aggregate skip count is exactly 15 at HEAD `da11292` | Per-project loop: `passed=1191 failed=0 skipped=15` (matches handoff baseline + Wave 4 close exactly) | ✅ |
| A18 | Parent `docs/specs/2026-05-13-modernization-phase-4.5-design.md` §8 D4 explicitly excludes HttpContext + MessagePack + Protobuf + ZeroFormatter (verbatim wording) | D4 row reads: "Skip 6 borderline projects: Enrichers.MessagePack/Protobuf/ZeroFormatter (Phase 7), Enrichers.HttpContext (no-op stub; Phase 1 V2), Compatibility.Legacy (Phase 7 deprecation candidate)". 4 of the 6 are enrichers (matches Wave 5's exclusion list) | ✅ |
| A19 | Parent §5 row 5 + §6 row 5 estimate 40–60 tests / 6 test projects for Wave 5 | Parent §5 row 5: `Enrichers (5 new test projects + Polly extension) \| 7 source assemblies → 6 test projects \| 52 \| ~12–20 \| ~40–60`. Parent §6 row 5: `\| **5. Enrichers** \| 5 new enricher test projects + Polly extension... \| ~40–60 \| ~12–20`. **Drift:** parent §5 says 7 source assemblies / 52 source files; on disk: 8 source assemblies (Attributes/GlobalExecutionId/MessageContext/Respond/Subscribe/Polly/QueueSuffix/RetryLater) / 63 source files. Likely an off-by-one in the parent table (consolidated MessageContext+Subscribe but not Respond). Test count target ≥40 still holds | ⚠️ **Partial — drift documented; Wave 5 acceptance unchanged** |
| A20 | Parent D10 wording matches "skipped methods within cleanup-target files are NOT touched" | D10 row reads: "Skipped methods within cleanup-target files are NOT touched in Wave 1. Skip annotations + their Phase 5/7 justifications are accurate documentation of broker-layer modernization debt; rewriting them now without the broker-layer change risks losing context. Phase 5/7 reactivates and re-asserts" | ✅ |
| A21 | NO non-Enricher source/test code in the repo depends on the 4 out-of-scope enrichers in a way that would break if Wave 5 doesn't add tests for them ("nothing else depends on this") | `grep -rln 'RawRabbit\.Enrichers\.<X>\b'` per-out-of-scope-enricher → all external references are in `test/RawRabbit.IntegrationTests/` (Phase 6 / live broker — out of scope per parent §1) or `sample/RawRabbit.AspNet.Sample/` (out of scope generally). Curious cross-enricher: `src/RawRabbit.Enrichers.Protobuf.csproj` references ZeroFormatter — but both are out of scope. NO unit-test conflict | ✅ |
| A22 | NO test file outside `test/RawRabbit.Enrichers.*.Tests/` exercises the 8 in-scope enrichers' source code in a way that would conflict with Wave 5 additions ("nothing else depends on this") | `grep -rln 'using RawRabbit\.Enrichers\.<X>\b' test/` per-in-scope-enricher → all external references are in `test/RawRabbit.IntegrationTests/` (Phase 6) or `test/RawRabbit.PerformanceTest/` (out of scope). NO references from `test/RawRabbit.Tests/` (the unit-test project that could conflict) | ✅ |
| A23 | RetryLater contains a `RetryInformationProvider` (or comparable) doing header `string` ⇄ struct codec (W5-R3 specificity) | `find src/RawRabbit.Enrichers.RetryLater -name '*.cs'` → has `Common/RetryInformationProvider.cs`, `Common/RetryHeaders.cs`, `Common/RetryInformation.cs`. Public types: `IRetryInformationProvider`, `RetryInformationProvider`, `IRetryInformationHeaderUpdater`, `RetryInformationHeaderUpdater`, `Retry : Acknowledgement`, `RetryHeaders`, `RetryInformation`, `RetryLaterPlugin`, `RetryLaterPipeContextExtensions`, `RetryLaterOptions`, `RetryLaterMiddleware : StagedMiddleware`, `RetryInformationExtractionOptions`, `RetryInformationExtractionMiddleware : StagedMiddleware` | ✅ |
| A24 | Spec target path `docs/specs/2026-05-14-modernization-phase-4.5-wave-5-design.md` does NOT already exist (avoid silent overwrite) | `ls` → `absent — safe to write` | ✅ |
| A25 | Working tree clean, branch `2.0`, HEAD `da11292`, 0 commits ahead of `origin/2.0` (re-confirm before write) | `git status --short` empty; `git symbolic-ref --short HEAD` = `2.0`; `git rev-parse HEAD` = `da112925a36353baa7e987b3c165b7286725db9a`; `git rev-list --count origin/2.0..HEAD` = `0` | ✅ |

**Summary:** 22/25 ✅, 2/25 ⚠️ partial (mechanical — A12 + A19 documented in spec body), 1/25 ❌ broken (A10 — 5 LogProvider sources surfaced; mechanical: §2 + §6 enumerate the 5 test classes that need `[Collection("LogProviderState")]`; spec scope unchanged).

---

## 12. Known issues, accepted as out of scope

User-acknowledged on 2026-05-14 during the Wave 5 brainstorm:

1. **Polly is the only Wave 5 task with broker-touching middleware** (5 source files reference broker types per §11 A9); inline `Mock<IModel>`/`Mock<IConnection>` setups per W5-D2. If Phase 6 later needs cross-project broker mocks, `BrokerMocks` promotion becomes justified — not Wave 5's job.
2. **Polly's existing 2 skips (in `Services/ChannelFactoryTests.cs:16` + `:52`) inherited verbatim per W5-D4**; do not count against W5 N≤5 budget.
3. **Polly's 8 broker-touching middleware use INHERITANCE over composition** — they subclass core `Pipe.Middleware.<Name>Middleware` and override protected hooks. T4 tests reach them via the inherited public `InvokeAsync` (per §2 + W5-R6); reflection-based protected access is forbidden (test smell).
4. **`MessageContext.Respond` + `MessageContext.Subscribe` consolidated into `MessageContext.Tests`** per parent §6/D8 — accepted at Wave 1 scaffolding time; T3 uses `Base/Respond/Subscribe` subdirs.
5. **HttpContext + MessagePack + Protobuf + ZeroFormatter intentionally untested** per parent D4 — Phase 7 (or Phase 1 V2 for HttpContext) will bundle test coverage with the underlying decision. §11 A21 confirms these 4 are only consumed by IntegrationTests + sample (no unit-test conflict).
6. **5 in-scope `Enrichers.*` sources extend the LogProviderState xUnit collection** from Wave 4's ≥40 → **≥45** after Wave 5 (4 from T2 GlobalExecutionId + 1 from T6 RetryLater). Phase 7 backlog item (carries from Waves 3/4): refactor `LogProvider.For` to lazy/injected (would eliminate workaround). Wave 5 inherits the workaround pattern.
7. **Carry-forward from Wave 4:** all Phase 7 backlog items remain open (LogProvider lazy refactor, ResilientChannelPool double-enumeration, IChannelFactory lazy connection, Subscription.Dispose extension-cast smell, 3 ExplicitAckMiddleware skipped paths, MEDI bump, AsAckable<TType> null-check, ResposeConsumerMiddleware typo, RespondConfigurationMiddleware:31 source bug, AckableResultOptions internal-set visibility). None are Wave 5's responsibility.
8. **Skip budget N=0–5 is a ceiling**, not a target. Wave 5 enters with one a-priori-known skip risk (W5-R1 — Polly ChannelFactory pattern); other skips not anticipated at spec-write.
9. **Test-count estimate is a range (40–60); only the lower bound (40) is the acceptance gate** (§6.4). Plan-write verification will lock per-project counts.
10. **Parent §5 row 5 has a documentation drift** (verified §11 A19): says "7 source assemblies / 52 source files"; on-disk reality = 8 assemblies / 63 files. Likely off-by-one in parent table; doesn't affect Wave 5 acceptance. Phase 4.5 close handoff may amend parent §5 if desired (cosmetic).
11. **8 NU1902 MessagePack vulnerability warnings** in `dotnet build -c Release` output (verified §11 A16) — same shape as Wave 4 close; out of scope for Wave 5 (MessagePack itself is excluded per parent D4; warnings are inherited from `IntegrationTests` + `MessagePack` source ref). Phase 7 / Phase 5 modernization will bump.
