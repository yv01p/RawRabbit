# Modernization Phase 4.5 Wave 3 Design — Broker-Touching Coverage

**Source:** Phase 4.5 parent spec at `docs/specs/2026-05-13-modernization-phase-4.5-design.md` (parent on `origin/2.0`). Wave 3 is the third of five waves; Wave 1 shipped at `9af8650`, Wave 2 shipped through `9e91f10` on `origin/2.0`.

**Inherits from parent spec (do NOT re-litigate here):**
- §1 in/out-of-scope source areas; §2 test project structure (D8); §3 conventions; §4 A25 forbidden patterns; §5 per-area gate format; §6 wave structure (Wave 3 row); §7 phase-wide acceptance; §8 decisions D1–D10; §9 risks (notably R2 — broker-mock skip-when-blocked is permitted with annotation); §10 validation shape; §13 known-issues acknowledgements.
- Wave 2 conventions captured in Wave 2 §3: BOM preservation, AAA-deletion full-line removal, `ThrowsAnyAsync<T>` over `ThrowsAsync<T>` for subclass cancellations.

**Goal:** After Wave 3 ships, the 8 sub-areas listed in §1 each have ≥1 happy + ≥1 error path test per public method (per-method strict per parent F1=(a) carry-forward from Wave 2). Wave 3 lands ≥150 net-new passing tests across the broker-touching core (`Pipe/Middleware/`, `Channel/`, `Consumer/`, `Subscription/`, `Instantiation/`, `DependencyInjection/` core, `BusClient.cs`, `Common/TopologyProvider.cs`). Wave 3 introduces one new test-helper organisation pattern (DW8 — per-project static `BrokerMocks` helper) and one new convention category (`[Fact(Skip = "Phase 5/7 territory: <reason>")]` permitted per parent R2).

**Tech stack (inherited):** .NET 10 SDK 10.0.107 + C# 13; xUnit 2.9.3; xunit.runner.visualstudio 2.8.2; Microsoft.NET.Test.Sdk 18.5.1; Moq 4.20.72; RabbitMQ.Client 5.0.1 (legacy; Phase 5 modernizes). No new package dependencies for Wave 3.

---

## 1. Scope

Wave 3 = 8 sub-areas at the test-landing level, all landing in the existing `test/RawRabbit.Tests/` project (no new test projects; Wave 4-5 add the Operations + Enrichers projects).

| # | Area | Source files | Test landing dir | Test-shape pattern + mocking surface |
|---|------|-------------:|---|---|
| 1 | **Pipe/Middleware + Pipe/ root residual** | 29 in `src/RawRabbit/Pipe/Middleware/` (27 concrete instantiable middleware + abstract `Middleware` base + `MiddlewareInfo` info type; `StagedMiddleware` abstract base co-located in `StageMarkerMiddleware.cs` and covered via concrete subclasses `HeaderDeserialization`/`HeaderSerialization`) + 1 in `src/RawRabbit/Pipe/IPipeContextFactory.cs` (the `PipeContextFactory` impl class — file also contains the `IPipeContextFactory` interface) | `test/RawRabbit.Tests/Pipe/Middleware/` (NEW subdir) + `test/RawRabbit.Tests/Pipe/PipeContextFactoryTests.cs` (existing `Pipe/` subdir, NEW file) | Per-middleware: instantiate the middleware-under-test, give it a `Mock<IPipeContext>` (and `Mock<IModel>`/`Mock<IConnection>` for channel-touching middleware via `BrokerMocks` helper from DW8), call `InvokeAsync(ctx, token)`, assert on context mutations + observable side-effects. Each middleware: ≥1 happy + ≥1 error per `InvokeAsync` (and ≥1 cancellation where applicable). PipeContextFactory: ctor + `CreateContext()` happy + error |
| 2 | **Channel + Channel/Abstraction** | 7 in `src/RawRabbit/Channel/*.cs` (`AutoScalingChannelPool` co-defines `AutoScalingOptions` POCO; `StaticChannelPool` co-defines `IChannelPool` interface; plus `AutoScalingChannelPoolFactory`, `ChannelFactory`, `ConcurrentChannelQueue`, `DynamicChannelPool`, `ResilientChannelPool`) + 1 in `src/RawRabbit/Channel/Abstraction/IChannelFactory.cs` (interface — covered via impl) | `test/RawRabbit.Tests/Channel/` (existing — gap-fill alongside the 3 Wave 1 files; do NOT touch the 7 pre-existing Phase 5/7-skipped tests per parent D10) | Mock `IConnectionFactory` → `IConnection` → `IModel` chain via `BrokerMocks.MakeConnectionChain()`. Per-method strict on each pool's public surface; ChannelFactory ctor takes `(IConnectionFactory, RawRabbitConfiguration)` |
| 3 | **Consumer** | 2 in `src/RawRabbit/Consumer/` (`ConsumerFactory` impl with 4 public methods + 2 protected virtuals + 1 protected helper; co-defines `ConsumerExtensions` static class with `CancelAsync` + `OnMessage` extensions on `IBasicConsumer`; plus `IConsumerFactory` interface — covered via impl) | `test/RawRabbit.Tests/Consumer/` (NEW subdir) | Mock `IChannelFactory` → `IModel` → `IBasicConsumer` (and `EventingBasicConsumer` for cast-dependent paths in `ConsumerExtensions`). Per public method ≥1 happy + ≥1 error |
| 4 | **Subscription** | 2 in `src/RawRabbit/Subscription/` (`Subscription` impl — co-defines `ISubscription` interface; `SubscriptionRepository` impl — co-defines `ISubscriptionRepository` interface; both interfaces covered via impls) | `test/RawRabbit.Tests/Subscription/` (NEW subdir) | `Subscription`: Mock `IBasicConsumer` (and `DefaultBasicConsumer` for the cast-dependent ctor branch); `SubscriptionRepository`: pure logic (`Add`/`GetAll`) over `ConcurrentBag<ISubscription>`. Per public method ≥1 happy + ≥1 error |
| 5 | **Instantiation + Instantiation/Disposable** | 4 in `src/RawRabbit/Instantiation/` (`ClientBuilder` builder with `Action<>` props + accumulator `Register`; `InstanceFactory` impl with `Create`/`Dispose`/`ShutdownAsync`; `RawRabbitFactory` static factory class with 4 static methods; `RawRabbitOptions` pure POCO with 3 settable properties) + 1 in `Instantiation/Disposable/BusClient.cs` (`Disposable.BusClient` impl wrapping `IInstanceFactory.Create()`) | `test/RawRabbit.Tests/Instantiation/` (NEW subdir; mirror `Disposable/` subdir for the wrapper) | Mock `IDependencyResolver` to hand back required services; mock `IInstanceFactory` for `Disposable.BusClient`. Avoid `Resolve<IBusClient>()` at all costs (triggers real broker connection per Wave 2 lesson — use `Resolve<IInstanceFactory>()`). `RawRabbitOptions` is pure data — covered indirectly via consumers; NO dedicated test file per parent §3 trivial-getter convention |
| 6 | **DependencyInjection (core)** | 4 in `src/RawRabbit/DependencyInjection/` (`SimpleDependencyInjection` impl with 8 public methods — `AddTransient<T,T>` ×2, `AddSingleton<T>` ×4, `GetService<T>` ×2, `TryGetService` — plus private `CreateInstance` reflection helper; `RawRabbitDependencyRegisterExtension` static class with `AddRawRabbit` extension; `IDependencyRegister` + `IDependencyResolver` interfaces — covered via impls) | `test/RawRabbit.Tests/DependencyInjection/` (existing — Wave 2 added `Autofac/` and `Ninject/` subdirs; this wave adds root-level test files alongside) | Pure logic for `SimpleDependencyInjection` (no broker boundary — direct construction); for `RawRabbitDependencyRegisterExtension`, mock `IDependencyRegister` and verify the chain of registrations. Per public method ≥1 happy + ≥1 error |
| 7 | **Root: `BusClient.cs`** | 1 at `src/RawRabbit/BusClient.cs` (29 lines; ctor takes `(IPipeBuilderFactory, IPipeContextFactory, IChannelFactory)` — third arg stored but unused, see DW13; `InvokeAsync` orchestrates the 3-step pipe build + context create + invoke) | `test/RawRabbit.Tests/BusClientTests.cs` (NEW root file under `test/RawRabbit.Tests/`) | Mock `IPipeBuilderFactory`, `IPipeContextFactory` (use the new `PipeContextFactory` impl coverage from area 1 to inform shape), `IChannelFactory` (passes-through unused). Per public method ≥1 happy + ≥1 error. `IBusClient.cs` interface (11 lines) gets NO direct test file (covered via `BusClientTests` + Wave 3 area 5's `Disposable.BusClientTests`) per DW14 |
| 8 | **Common/TopologyProvider** (Wave 2 deferral) | 1 at `src/RawRabbit/Common/TopologyProvider.cs` (372 lines; `private IModel _channel` field; calls `channel.QueueBind` (line 136), `QueueDeclare` (line 195), `ExchangeDeclare` (line 217); `private IModel GetOrCreateChannel()` (line 308)) | `test/RawRabbit.Tests/Common/TopologyProviderTests.cs` (NEW; existing `Common/` subdir) | Mock `IModel` (and supporting `IConnectionFactory`/`IConnection` via `BrokerMocks` chain) for the `QueueDeclare`/`ExchangeDeclare`/`QueueBind` family. Per public method ≥1 happy + ≥1 error. Highest-risk area for Phase 5/7 skips per R2 — broker-mock surface is widest |

**Out of scope for Wave 3:**
- All Wave 4 areas (`src/RawRabbit.Operations.*`, 8 areas) and Wave 5 (`Enrichers.*`).
- Inherited from parent §1: IntegrationTests, PerformanceTest, MessagePack/Protobuf/ZeroFormatter/HttpContext, Compatibility.Legacy.
- The 7 pre-existing Phase 5/7-skipped tests in `test/RawRabbit.Tests/Channel/` (4 in `ChannelFactoryTests.cs`, 3 in `ChannelPoolTests.cs`) and 2 in `test/RawRabbit.Enrichers.Polly.Tests/Services/ChannelFactoryTests.cs` — preserved verbatim per parent D10. Wave 3 does NOT un-skip any.
- `src/RawRabbit/Pipe/PipeKey.cs` (28 `public const string` keys) and `src/RawRabbit/Pipe/StageMarker.cs` (9 `public const string` stage names) — pure constants; testing them would be tautology. Per parent §3 ("trivial getters/setters and primary ctors of pure data types do NOT each get tests"), no test files. Documented in §8.
- `src/RawRabbit/Pipe/IPipeContext.cs` interface (already partially covered by Wave 2 — the co-located `DictionaryExtensions` static class has its own test file in `test/RawRabbit.Tests/Pipe/DictionaryExtensionsTests.cs`).

---

## 2. Test count target (lower bound = acceptance gate)

Per area, lower-bound estimate (plan-write verification will tighten exact counts):

| # | Area | Public-method count est. | Min tests (≥1 happy + ≥1 error per method) |
|---|------|------:|------:|
| 1 | Pipe/Middleware (27 concrete middleware × ~3-5 tests each: happy + error + cancellation + state-mutation observation) + PipeContextFactory (3-5) | ~30-50 (Invoke + ctor + supporting public surface) | ~80-140 |
| 2 | Channel + Abstraction (7 testable units; `IChannelFactory` covered via impl) | ~12-20 | ~25-45 |
| 3 | Consumer (`ConsumerFactory` 4 public + `ConsumerExtensions` 2 statics) | ~6-8 | ~10-14 |
| 4 | Subscription (`Subscription` ctor+Dispose; `SubscriptionRepository` Add+GetAll) | ~4-6 | ~6-10 |
| 5 | Instantiation + Disposable (`ClientBuilder` ctor+Register; `InstanceFactory` Create+Dispose+ShutdownAsync; `RawRabbitFactory` 4 statics; `Disposable.BusClient` ctor+InvokeAsync+Dispose; `RawRabbitOptions` covered indirectly) | ~10-16 | ~20-35 |
| 6 | DependencyInjection core (`SimpleDependencyInjection` 8 public methods + `RawRabbitDependencyRegisterExtension` extension) | ~10-16 | ~20-32 |
| 7 | Root `BusClient.cs` (ctor + `InvokeAsync`) | ~2-3 | ~3-5 |
| 8 | TopologyProvider.cs (declare/bind/delete/configure family on `_channel`) | ~6-12 | ~15-30 |
| | **Total** | **~80-131** | **~179-311** |

**Acceptance gate: ≥150 net-new passing tests** across the 8 sub-areas (lower-bound floor with margin below 179 per-area sum to absorb plan-write tightening; range 179-311 informational). Plan-write per-area enumeration may relax individual area floors if a public surface turns out smaller than estimated; the wave-wide ≥150 gate is fixed.

---

## 3. Conventions reaffirmed (from parent §3 + Wave 1 + Wave 2 lessons)

Inherited from parent §3 — applied to all new tests:
- xUnit 2.9.3 + Moq 4.20.72; `<TypeUnderTest>Tests` class naming; `Should_Verb_Subject` method naming; one subdir per source area mirroring `src/`; `[Theory]/[InlineData]` where 3+ similar; `async Task` + `await` (no `.Wait()`/`.Result`); blank-line AAA (no comment markers); `Assert.Throws<T>` / `await Assert.ThrowsAsync<T>` / `await Assert.ThrowsAnyAsync<T>` for subclass cases; generic `Assert.IsType<T>(obj)` only; file-local `internal` POCOs; TAB indent; UTF-8 BOM preservation.

Inherited from Wave 1 (per Wave 2 §3): BOM preservation; AAA-comment full-deletion (no double-blanks); `ThrowsAnyAsync<T>` over `ThrowsAsync<T>` for `OperationCanceledException`-subclass cases.

**New conventions captured from Wave 2 (apply Wave 3 onward):**

- **Guarded BOM-restore loop:** any wave-wide BOM-restore step MUST guard against duplicate BOMs:
  ```bash
  for f in ...; do
    if ! head -c 3 "$f" | od -An -tx1 | grep -q "ef bb bf"; then
      printf '\xef\xbb\xbf' | cat - "$f" > /tmp/_bom && mv /tmp/_bom "$f"
    fi
  done
  ```
  Bare loop without the guard doubles/triples BOMs (Wave 2 commit `947b8ef` cleaned up 23 such files; `file(1)` reports "with BOM" even on duplicates so the gate-9 check missed it).

- **`[Collection("<Name>")]` for any test class mutating a static field:** serializes test classes within the named collection. Apply to any Wave 3 test class touching `LogProvider.LoggerFactory` (verified at A11 to be the only mutable static in Wave 3 source) or any other mutable static surfaced at plan-time. Wave 2 commits `4d952c3` + `9e91f10` set this precedent for `LogProvider.LoggerFactory`.

- **Specific exception types over `Assert.ThrowsAny<Exception>`:** where the runtime exception is stable (e.g., `DependencyResolutionException` from Autofac wrapping, `ArgumentNullException` from extension-method `this`-validation, `InvalidOperationException` from missing DI registrations), use `Assert.Throws<TSpecific>` not `ThrowsAny<Exception>`. Wave 2 commit `3ea4e53` tightened 7 such assertions in ServiceCollection tests.

- **csproj `VersionOverride` for binary-incompat test deps:** when a test project needs a different version of a transitively-pulled package than the source library uses (e.g., MEDI 1.0.2 source compile + MEDI 10.0 test runtime CS0433 collision), add a local `<PackageReference Include="..." VersionOverride="..."/>` in the test csproj. Source library version stays unchanged. Wave 2 commit `6dbeefb` applied this for `Microsoft.Extensions.DependencyInjection`. Not expected to recur in Wave 3 (all tests land in existing `test/RawRabbit.Tests/` which uses inherited Directory.Packages.props), but DW9 below permits it if surfaced.

- **`Resolve<IInstanceFactory>()` not `Resolve<IBusClient>()` in DI happy-path tests:** resolving `IBusClient` through the registered chain triggers a real RabbitMQ broker connection (production wires `IBusClient` through `IInstanceFactory.Create()` which synchronously builds a connection). Use `IInstanceFactory` whose ctor only stores the resolver. Applies to Wave 3 area 6 (DI core) and any future DI test happy-path. Wave 2 SDD Tasks 1-3 deviation #3 set this precedent.

- **`[Fact(Skip = "Phase 5/7 territory: <reason>")]` permitted** when broker mock setup fights RabbitMQ.Client 5.0.1 signatures (per parent R2). Each new skip annotated with the specific signature/method that failed; aggregate skipped count gate relaxes per parent R2. Existing 7 skips in `Channel/ChannelFactoryTests.cs` + `ChannelPoolTests.cs` use this pattern verbatim.

- **`BrokerMocks` static helper class (DW8):** new `test/RawRabbit.Tests/TestHelpers/BrokerMocks.cs` provides reusable Mock-chain factory methods (e.g., `MakeConnectionChain()` returning configured `(Mock<IConnectionFactory>, Mock<IConnection>, Mock<IModel>)` tuple). Use across all Wave 3 broker-touching tests where the standard Mock chain fits. Per-test customization via `.Setup(...)` overrides on the returned mocks remains permitted.

---

## 4. Wave 3 acceptance gates (Wave 3 done when ALL hold)

1. **Test count:** ≥150 net-new passing tests across the 8 sub-areas (lower-bound floor; range 179-311 informational).
2. **Build clean:** `dotnet build -c Release` returns 0 errors. Warning shape stable from Wave 2 close (no new categories).
3. **Per-area enumerated tests:** the per-area test method names locked at plan-write time all land and pass.
4. **`RawRabbit.Tests` aggregate:** `dotnet test test/RawRabbit.Tests --no-build -c Release` returns ≥529 passed (was 379 post-Wave-2; +≥150 from Wave 3).
5. **`ServiceCollection.Tests` aggregate:** unchanged from Wave 2 (no Wave 3 work in this project) — 21 passed / 1 skipped.
6. **xUnit analyzer warnings:** xUnit2020/2004/2007/1031 warnings = 0 in all Wave 3 new files (Wave 1+2 clean state preserved).
7. **Skipped count:** aggregate skipped across all 3 unit-test projects = **10 + N** (where N = Wave-3-newly-blocked broker-mock signature tests, per parent R2). Each new skip carries the per-test "Phase 5/7 territory: <specific signature>" annotation matching the existing 7 skips' format. N expected = 0-5; if N>0, the post-Wave-3 skipped breakdown becomes **(7 + N) in `test/RawRabbit.Tests`** + 1 in `ServiceCollection.Tests` + 2 in `Polly.Tests`.
8. **`[Theory]` use exercised in ≥1 Wave 3 area** (parent phase-wide ≥3 already satisfied by prior waves).
9. **Convention adherence:** new test files use guarded BOM-restore (no duplicate BOMs); `[Collection]` on any test class mutating static fields (none expected per A11; if surfaced at plan-time, applied); specific exception types not `ThrowsAny<Exception>`; `Resolve<IInstanceFactory>()` not `<IBusClient>()` in DI happy-paths; `BrokerMocks` helper used where its factory methods fit (≥3 Wave 3 test classes consume it per DW8).
10. **`BrokerMocks` helper landed:** new `test/RawRabbit.Tests/TestHelpers/BrokerMocks.cs` provides reusable Mock-chain factory methods used by ≥3 Wave 3 test classes (per DW8).

---

## 5. Wave-3-specific decisions

| # | Decision | Rationale | Alternatives considered |
|---|----------|-----------|-------------------------|
| **DW8** | Add `test/RawRabbit.Tests/TestHelpers/BrokerMocks.cs` static helper class with factory methods (`MakeConnectionChain()`, `MakeChannel()`, etc.) returning configured `Mock<IConnectionFactory>`/`Mock<IConnection>`/`Mock<IModel>` tuples | Wave 2's DW7 deferred shared helpers ("wait for the third real use case"); Wave 3 has ~25 broker-touching test classes that all need the same Mock chain — easily clears the 3-use threshold. Per-project static class (not a separate csproj) keeps blast radius minimal; Wave 4-5 can promote to shared csproj if their needs warrant it. Verified at A10 that `TestHelpers/` doesn't already exist (greenfield) | (a) Inline only (continues DW7 unchanged — 125+ lines duplication across 25 test classes); (c) New `test/RawRabbit.TestHelpers/` shared csproj (premature; Wave 4-5 may not need it; introduces sln-add overhead) |
| **DW9** | Mock strategy = Moq + skip-when-blocked per parent D3 + R2; new skips tagged `[Fact(Skip = "Phase 5/7 territory: <signature>")]` matching existing 7 skips' wording verbatim | Consistent with the 7 existing skips' precedent (verified at A12); matches parent R2's escape valve; zero new test infrastructure | (a) Hand-rolled fakes (`FakeChannel`/`FakeConnection`) — ~3-5 fake files to maintain alongside Phase 5 broker-layer modernization; (b) Hybrid Moq + fakes for problem signatures — adds 1-2 fake files but inconsistent across the wave |
| **DW10** | One Wave 3 spec covering all 8 sub-areas; one implementation plan with ~10 SDD tasks (one per sub-area + aggregate verification + final code review) | Matches Wave 2's executed pattern (8 SDD tasks + verification + final review across 8 areas, all shipped in one wave); per-area independence preserved at SDD-task level; minimizes brainstorm/spec/plan cycles | (b) Split into 3a/3b sub-waves — re-litigates parent §6 wave decomposition; doubles overhead; (c) One spec, plan splits into 2 phases — hybrid; phased acceptance unnecessary given SDD's per-task independence |
| **DW11** | Acceptance gate = ≥150 net-new tests floor; range 179-311 informational; plan-write enumeration tightens floor | Matches Wave 2's lower-bound-floor pattern (Wave 2 was conservative — gated ≥88, landed 359). 150 floor leaves margin below the per-area sum (179) for plan-write to drop areas where source surface turns out smaller than estimated | (b) Per-area floors only (loses wave-wide signal); (c) Wait for plan-write enumeration (defers an acceptance signal that's nice at spec time) |
| **DW12** | Wave 3 root tests for `BusClient.cs` land at `test/RawRabbit.Tests/BusClientTests.cs` (root file under `test/RawRabbit.Tests/`, no subdir) | One source file at `src/RawRabbit/` root → one test file at `test/RawRabbit.Tests/` root; mirrors source layout. Disposable wrapper goes under `Instantiation/Disposable/BusClientTests.cs` since its source lives at `src/RawRabbit/Instantiation/Disposable/BusClient.cs` | Subdir for root files (over-organization); split BusClient tests across multiple files (no source basis for split) |
| **DW13** | `BusClient.cs:14` ctor takes `IChannelFactory factory` but doesn't store/use it (apparent dead arg per A14 verification). Wave 3 tests assert ctor accepts non-null `factory` and call-through to `InvokeAsync` works regardless of factory state. Note as code-quality flag in §8 (Phase 7 backlog) but do NOT modify source (out of scope per parent D10's spirit — Wave 3 is test-only) | The dead arg is a real source oddity; testing should match production behavior, not "fix" the source. Phase 7 (or later cleanup) may remove the arg | Add a Phase 7 backlog item only (rejected — should test the actual behavior); modify source (rejected — Wave 3 is test-only) |
| **DW14** | `IBusClient.cs` (interface, 11 lines, 1 method) gets NO direct test file. Interface contract is verified through `BusClientTests.cs` (its only production implementation) and `Instantiation/Disposable/BusClientTests.cs` (its disposable wrapper) | Interfaces don't have testable behavior on their own; conformance is verified through implementing classes. Same pattern applies to `IConsumerFactory`, `IChannelFactory`, `IPipeContextFactory`, `IDependencyRegister`, `IDependencyResolver`, `ISubscription`, `ISubscriptionRepository`, `IClientBuilder`, `IInstanceFactory`, `IChannelPool` — all covered via impls per parent §3 convention | Add `*ConformanceTests.cs` files for each interface (rejected — no surface to test beyond the implementations) |
| **DW15** | `RawRabbitOptions` (pure POCO with 3 settable properties, no methods) gets NO dedicated test file; covered indirectly via `RawRabbitFactory.CreateInstanceFactory(options: ...)` and `RawRabbitFactory.CreateSingleton(options: ...)` happy-path tests | Per parent §3 convention ("Trivial getters/setters and primary ctors of pure data types do NOT each get tests"); the 3 settable properties are verified by tests that consume them | Dedicated `RawRabbitOptionsTests.cs` (rejected — would only assert default-ctor sets all 3 to null = tautology) |
| **DW16** | Pick up `PipeContextFactory` (impl class inside `src/RawRabbit/Pipe/IPipeContextFactory.cs`) in Wave 3 area 1; explicitly skip `PipeKey.cs` and `StageMarker.cs` (constants-only, per parent §3) | `PipeContextFactory` has real ctor + `CreateContext` logic and is core infrastructure used by `BusClient` (testable; needed for `BusClientTests` to mock cleanly). `PipeKey` (28 const strings) and `StageMarker` (9 const strings) testing would be tautology. See §6 FD1 | Carve a 9th sub-area (rejected — over-organization for 1 testable unit); defer all 3 orphans to Phase 7 (rejected — `PipeContextFactory` is core infrastructure that BusClient tests benefit from coverage of) |

---

## 6. Forced decisions surfaced by verification (resolved before this spec was written)

The brainstorming session's verification pass surfaced 1 forced decision (FD1) requiring user input + several mechanical refinements (FD2–FD6) applied in place:

| # | Finding | Resolution |
|---|---------|------------|
| FD1 | 3 of the 9 `src/RawRabbit/Pipe/*.cs` root files were orphaned by Wave 2 — neither in Wave 2's scope nor in Wave 2 §8 deferrals: `IPipeContextFactory.cs` (contains testable `PipeContextFactory` impl), `PipeKey.cs` (28 const strings), `StageMarker.cs` (9 const strings). Per parent §1 they were intended to be in scope for the phase | User decision (this brainstorm): **Pick up `PipeContextFactory` in Wave 3 area 1; explicitly skip `PipeKey` + `StageMarker` per parent §3 trivial-getter convention.** See DW16 above. Documented in §1 area 1 + §8 |
| FD2 | A1 enumeration: `src/RawRabbit/Pipe/Middleware/` has 29 files but only 27 are concrete instantiable middleware. `Middleware.cs` is the abstract base; `MiddlewareInfo.cs` is an info data type (not a middleware); `StagedMiddleware` is an additional abstract base co-located inside `StageMarkerMiddleware.cs` (covered via concrete subclasses `HeaderDeserializationMiddleware` and `HeaderSerializationMiddleware`) | Mechanical: §1 area 1 row + §2 area 1 estimate refined to "27 concrete middleware × 3-5 tests = ~80-140" |
| FD3 | A5 enumeration: `src/RawRabbit/Consumer/ConsumerFactory.cs` co-defines a `ConsumerExtensions` static class with 2 extensions on `IBasicConsumer` (`CancelAsync`, `OnMessage`) — additional testable surface beyond `ConsumerFactory`'s 4 public methods | Mechanical: §1 area 3 row note added; §2 area 3 estimate refined to ~10-14 |
| FD4 | A6 enumeration: `Subscription.cs` and `SubscriptionRepository.cs` each co-define their own interfaces (`ISubscription`, `ISubscriptionRepository`); per DW14 these are covered via impls (no separate interface test files) | Mechanical: §1 area 4 row note added |
| FD5 | A3 enumeration: `Channel/StaticChannelPool.cs` co-defines `IChannelPool` interface inline; `Channel/AutoScalingChannelPool.cs` co-defines `AutoScalingOptions` POCO with a `static AutoScalingOptions Default => new ...` computed property (NOT a mutable static field — false-positive in A11; safe) | Mechanical: §1 area 2 row note added; A11 confirmed empty (no mutable statics in Wave 3 source beyond the false-positive computed property) |
| FD6 | A11 broader scan: no mutable statics in Wave 3 source other than `AutoScalingOptions Default` computed property (false-positive — returns a fresh instance each call). `LogProvider.LoggerFactory` (handled in Wave 2) is the only known mutable static in `src/RawRabbit/`; not touched by any Wave 3 source area | Mechanical: §3 conventions section notes the empty result; gate 9 in §4 marks "[Collection] applied if any mutable static surfaces at plan-time" |

---

## 7. Verified assumptions

The following 17 assumptions were enumerated COLD (against the design alone, before any verification reads) and then verified empirically against HEAD `9e91f10` at spec-write time on 2026-05-13:

| # | Assumption | Evidence |
|---|---|---|
| A1 | `src/RawRabbit/Pipe/Middleware/` has 29 files, all (or nearly all) concrete instantiable Middleware subclasses | `ls src/RawRabbit/Pipe/Middleware/*.cs` returns 29 files. Spot-check: `head -25` of `SubscriptionMiddleware.cs`, `HeaderDeserializationMiddleware.cs`, `HeaderSerializationMiddleware.cs` confirmed: 27 concrete instantiable middleware; `Middleware.cs` is abstract base; `MiddlewareInfo.cs` is info type; `StagedMiddleware` (abstract base for HeaderDeserialization/HeaderSerialization) co-located in `StageMarkerMiddleware.cs`. Adjustment captured in FD2 |
| A2 | `IBusClient.cs` is a public interface with no implementation logic | Direct read confirms: 11 lines, `public interface IBusClient { Task<IPipeContext> InvokeAsync(...); }`. DW14 holds — no direct test file |
| A3 | `Channel/` source uses `IConnectionFactory`/`IConnection`/`IModel` directly | `grep -l "using RabbitMQ.Client"` returns all 7 `Channel/*.cs` + `Channel/Abstraction/IChannelFactory.cs`. `head -25` of `ChannelFactory.cs` confirms ctor `(IConnectionFactory connectionFactory, RawRabbitConfiguration config)` + fields `IConnectionFactory ConnectionFactory`, `ConcurrentBag<IModel> Channels`, `IConnection Connection`. `head -20` of `StaticChannelPool.cs` confirms `using RabbitMQ.Client;` + co-defined `IChannelPool` interface |
| A4 | `TopologyProvider.cs` touches `IModel` directly (broker-touching, fits Wave 3) | `grep -nE "_channel\.\|IModel\|RabbitMQ\.Client\|QueueDeclare\|ExchangeDeclare\|QueueBind"` returns: line 7 `using RabbitMQ.Client;`, line 28 `private IModel _channel;`, line 136 `channel.QueueBind(`, line 195 `channel.QueueDeclare(`, line 217 `channel.ExchangeDeclare(`, line 308 `private IModel GetOrCreateChannel()`. Matches handoff §3 |
| A5 | `Consumer/ConsumerFactory.cs` uses `RabbitMQ.Client`; has multiple public methods | Direct read confirms: 4 public methods (`GetConsumerAsync`, `GetConfiguredConsumerAsync`, `CreateConsumerAsync`, `ConfigureConsume`) + 2 protected virtuals + 1 protected helper + `ConsumerExtensions` static class with 2 extensions on `IBasicConsumer` (`CancelAsync`, `OnMessage`). Adjustment captured in FD3 |
| A6 | `Subscription/Subscription.cs` has dispose-callback pattern; `SubscriptionRepository.cs` is pure logic | Direct read: `Subscription` ctor takes `IBasicConsumer` (downcasts to `DefaultBasicConsumer`); `Dispose()` checks `_consumer.Model.IsOpen` + Active flag, then calls `_consumer.CancelAsync()`. `SubscriptionRepository` uses `ConcurrentBag<ISubscription>` with `Add` + `GetAll` methods. Both files co-define their interfaces. Adjustment captured in FD4 |
| A7 | `ClientBuilder.cs` is a builder with property accumulators | Direct read: 2 public `Action<>` properties (`PipeBuilderAction`, `DependencyInjection`) initialized to no-op delegates in ctor; `Register(pipe, ioc)` accumulates via `+=`. Note: interface declares `Register` with `ioc = null` default; impl class doesn't replicate the default (minor quirk, not blocking for tests) |
| A8 | `RawRabbitOptions.cs` is pure POCO | Direct read: 3 settable properties (`ClientConfiguration`, `DependencyInjection` Action, `Plugins` Action), no methods. DW15 holds — no dedicated test file |
| A9 | Test landing layout has no conflicts | `ls -d` confirmed: `test/RawRabbit.Tests/Pipe/` exists with 7 Wave 2 files; `Pipe/Middleware/` doesn't exist; `Consumer/`, `Subscription/`, `Instantiation/`, `TestHelpers/` don't exist; `Channel/` has only the 3 Wave 1 files (`ChannelFactoryTests`, `ChannelPoolTests`, `DynamicChannelPoolTests`); `DependencyInjection/` has only Autofac + Ninject Wave 2 subdirs. All Wave 3 landing dirs are greenfield |
| A10 | `test/RawRabbit.Tests/TestHelpers/` doesn't already exist | Confirmed by `ls -d test/RawRabbit.Tests/TestHelpers 2>&1` returning "No such file or directory". DW8 landing dir is greenfield |
| A11 | No surprise mutable statics in Wave 3 source beyond `LogProvider.LoggerFactory` | Initial scan returned empty; broader regex (`static \w+ \w+ { get | static \w+ \w+ =`) returned only `AutoScalingOptions Default => new ...` (a computed property returning fresh instance each call — NOT a mutable static field). No `[Collection]` decisions needed beyond Wave 2's existing `LogProvider.LoggerFactory` handling. Captured in FD6 |
| A12 | Existing skip annotation wording matches my planned format verbatim | `grep "Fact(Skip"` against `Channel/ChannelFactoryTests.cs` + `ChannelPoolTests.cs` returns exactly the format `[Fact(Skip = "Phase 5/7 territory: <specific reason>")]` — 4 in ChannelFactoryTests use the same wording about "2-arg ConnectionFactory.CreateConnection signature mismatch with 1-arg mock setups"; 3 in ChannelPoolTests use varied per-test wording about "crashes test host on net10 / Moq 4.20", "fails on net10 / Moq 4.20", "hangs on net10 / Moq 4.20". DW9 wording aligns |
| A13 | Post-Wave-2 baseline = 379 passed / 7 skipped in `RawRabbit.Tests`; 21 / 1 in `ServiceCollection.Tests`; 1 / 2 in `Polly.Tests` | Verified at session start (re-verified in CDR R1 pass): `dotnet test test/RawRabbit.Tests --no-build -c Release` returned `Failed: 0, Passed: 379, Skipped: 7`; `dotnet test test/RawRabbit.DependencyInjection.ServiceCollection.Tests --no-build -c Release` returned `Failed: 0, Passed: 21, Skipped: 1`; `dotnet test test/RawRabbit.Enrichers.Polly.Tests --no-build -c Release` returned `Failed: 0, Passed: 1, Skipped: 2`. §4 gate 4 + gate 5 + gate 7 anchored on these numbers |
| A14 | `BusClient.cs:14` ctor truly doesn't use the `factory` field | Direct read: ctor stores `_pipeBuilderFactory` and `_contextFactory` to fields; `factory` parameter is accepted but no field is assigned — the parameter is dead. Behavior confirmed; DW13 holds |
| A15 | Parent spec D3/R2/D10 + Wave 2 §3 conventions still in force at HEAD | Both specs read at session start; HEAD is `9e91f10` (working tree clean per Step 1 verification); no commits between specs and HEAD that touch them. All clauses still in force |
| A16 | `src/RawRabbit/Pipe/` root has 4 files left unaccounted by Wave 2 | `ls src/RawRabbit/Pipe/*.cs` returns 9 files: 5 covered by Wave 2 (`PipeBuilder`, `PipeBuilderFactory`, `PipeContextExtension`, `PipeContextGetExtension`, `AddPropertyPipeContextExtensions`), 1 partially covered via Wave 2's DictionaryExtensions tests (`IPipeContext.cs`), 3 orphaned (`IPipeContextFactory.cs` containing testable `PipeContextFactory` impl, `PipeKey.cs` constants, `StageMarker.cs` constants). FD1 + DW16 resolution |
| A17 | `AutoScalingChannelPool.cs` co-defines `AutoScalingOptions` POCO inline with a `static AutoScalingOptions Default => new ...` computed property | Direct read of lines 115-127: confirms `public static AutoScalingOptions Default => new AutoScalingOptions { MinimunPoolSize = 1, MaximumPoolSize = 10, ... }` — expression-body returning fresh instance each call. NOT a mutable static. False-positive on A11; safe |

---

## 8. Tasks NOT in this spec (deferred to Wave 3 implementation plan or to other waves)

Inherited from parent spec's split (spec defines shapes; plan locks names):

- **Per-area exact test method names** — derived during Wave 3 implementation plan's verification pass (one enumeration per source class)
- **Plan-time decision: which middleware classes have multiple `InvokeAsync` paths warranting more than 2-3 tests each** — plan reads each middleware, enumerates conditional branches in `InvokeAsync`
- **Plan-time decision: which middleware need `Mock<IModel>` vs only `Mock<IPipeContext>`** — plan reads each middleware's `using` declarations + `InvokeAsync` body
- **Plan-time decision: ConsumerFactory dispatch tests via `EventingBasicConsumer`** — depends on whether the cast-dependent code paths in `ConsumerExtensions.CancelAsync`/`OnMessage` are testable via Moq's typed Setup; may surface a Wave-3-specific FD if Moq can't bind the cast-result events
- **Plan-time decision: which `RawRabbitFactory` static overloads warrant separate tests vs `[Theory]` consolidation** (4 statics, 2 with optional `RawRabbitOptions` vs 2 with explicit DI-register + resolverFunc)
- **Plan-time decision: `BrokerMocks` helper API surface** — exact factory method names + tuple/named-record shape; plan-time can iterate after Task 1 lands an example
- **Wave 4 source areas:** `Operations.*` (8 projects)
- **Wave 5 source areas:** `Enrichers.*` (5 + Polly extension)
- **Phase 5 work:** un-skip the 7 pre-existing Phase-5/7-tagged tests in `Channel/` + 2 in `Polly.Tests/Services/` + N from Wave 2 (1 known: `ServiceProviderAdapterTests.Should_Self_Register_When_Constructed_From_Collection`) + N from Wave 3 (newly-blocked broker-mock signature tests, expected 0-5) when the broker layer is modernized
- **`PipeKey.cs` (28 const strings) + `StageMarker.cs` (9 const strings)** — pure constants per parent §3 convention; testing them would be tautology. NO test files in Wave 3 or any future wave
- **`IPipeContext.cs` interface** — partially covered by Wave 2 (`DictionaryExtensions` co-located inside it has its own test file). Interface-conformance test file not added per DW14 convention
- **Phase 7 items surfaced by Wave 3 (track separately):**
  - `BusClient.cs:14` dead `IChannelFactory factory` ctor parameter (per DW13) — Phase 7 may remove the arg
  - `IClientBuilder.Register(pipe, ioc = null)` interface declares a default that the impl class doesn't replicate (minor; per A7 note) — Phase 7 cosmetic
  - Any other code-quality flags surfaced during plan-time source enumeration

A new spec → new plan cycle is required to add any of the above to a future phase.

---

## 9. Known issues, accepted as out of scope

User-acknowledged on 2026-05-13 during this Wave 3 brainstorm:

1. **Per-method strict (F1=a) inherited from Wave 2.** Each public method on each broker-touching class gets ≥1 happy + ≥1 error test. Not re-litigated.
2. **Shared test-helper assembly (Wave 2's DW7) revisited as DW8** — per-project static class in `test/RawRabbit.Tests/TestHelpers/BrokerMocks.cs` only (NOT a separate `test/RawRabbit.TestHelpers/` shared csproj). Wave 4-5 may promote to shared csproj if their needs warrant it.
3. **Mock strategy = Moq + skip-when-blocked.** New `[Fact(Skip = "Phase 5/7 territory: <reason>")]` annotations permitted per parent R2; aggregate skipped count grows by N≥0.
4. **`BusClient.cs:14` dead `IChannelFactory factory` arg** preserved per DW13 (test-only wave; Phase 7 may remove).
5. **3 Pipe/ root files orphaned by Wave 2** resolved per FD1 + DW16: pick up `PipeContextFactory`; explicitly skip `PipeKey` + `StageMarker` as constants-only.
6. **27 concrete middleware count** (not 29 — `Middleware.cs` abstract base + `MiddlewareInfo.cs` info type are non-instantiable; `StagedMiddleware` abstract base co-located in `StageMarkerMiddleware.cs` is covered via concrete subclasses).
7. **Carries forward from prior phases:** see parent §13 item 5 (HttpContext stub, IntegrationTests, NU1902, Phase 2 metadata heterogeneity, MEDI 1.0.2 binary incompat in `ServiceProviderAdapter(IServiceCollection)` ctor — Phase 7 un-skip).
