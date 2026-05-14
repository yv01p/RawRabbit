# Modernization Phase 4.5 Wave 5 — `Enrichers.*` Coverage Implementation Plan

> **For agentic workers:** REQUIRED: Use `superpowers:subagent-driven-development` to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Source spec:** `docs/specs/2026-05-14-modernization-phase-4.5-wave-5-design.md` (commit SHA: `481ac2d`)

**Plan scope:** Wave 5 only — 6 SDD tasks, one per in-scope `Enrichers.*` test project per spec W5-D1. Waves 1–4 already shipped through `da11292` on `origin/2.0`. Wave 5 closes Phase 4.5.

**Goal:** Land per-public-method test coverage (F1=(a) strict per Wave 2/3/4 carry-forward) across the 8 in-scope `Enrichers.*` source assemblies via the 6 already-scaffolded `test/RawRabbit.Enrichers.*.Tests/` projects (3 source assemblies consolidated into `MessageContext.Tests` per parent §6/D8). 44 new test files (T1=5 + T2=8 + T3=9 + T4=10 + T5=5 + T6=7); estimated ~84–120 net-new passing tests (well above ≥40 wave floor per spec §6.4 — F1=(a) strict + co-located `*Options` POCO + `StageMarker` per-middleware tests inflate above parent §5's ~40–60 estimate; parent §5 said "estimated, not capped"). Zero source modifications; zero csproj modifications; zero new test projects.

**Architecture:** One test class per testable source class; subdir layout mirrors `src/`. Mock at `IPipeContext` boundary per parent D3 — three patterns per spec §2 (Mock<IPipeContext> + Properties dict, concrete `PipeContext`, `*Func` options injection). Inline `Mock<IModel>` for Polly's broker-touching middleware only (T4) per spec §2; the other 5 enrichers (T1, T2, T3, T5, T6) have zero broker-type references in source per spec §11 A9. `[Collection("LogProviderState")]` opt-in on the 5 LogProvider-reading test classes per spec §2 + §6 (4 in T2, 1 in T6). Namespace-alias `using <Alias> = <Type>;` on T3 test files in `Base/` if they reference the `MessageContext` class by simple name. Plugin-only files, constants-only files, enum-only files, and pure interfaces skipped per **W5-P1** (analogous to Wave 4 W4-D8). Polly's 8 broker-touching inherited middleware tested via the inherited public `InvokeAsync` entry-point with `IPipeContext` set up to reach the protected hook; assertion target shifts to "policy executed + base broker call ran inside policy" per spec §2 + W5-R6. `[Fact(Skip = "Phase 5/7 territory: <signature>")]` permitted within wave-pool budget N=0–5 per spec §4 (Polly's existing 2 skips inherited verbatim per W5-D4, do NOT count against budget).

**Tech stack (inherited from spec):** .NET 10 SDK 10.0.107 + C# 13; xUnit 2.9.3; xunit.runner.visualstudio 2.8.2; Microsoft.NET.Test.Sdk 18.5.1; Moq 4.20.72; RabbitMQ.Client 5.0.1 (legacy; Phase 5 modernizes); Polly 7.x (Polly.Tests inherits via existing PackageReference). CPM via `Directory.Packages.props`. No new package dependencies for Wave 5.

---

## File Structure

**Create only.** Wave 5 lands ~30–45 new test files across 6 already-scaffolded test projects. Zero source modifications; zero csproj modifications; zero solution-file modifications.

### W5-P1 plan-level skip convention (extends Wave 4 W4-D8)

The following file categories are skipped from F1=(a) coverage; they have no behavior to test independently:

- **Plugin-only files** (`*Plugin.cs` whose only content is `IClientBuilder` extension methods registering middleware) — behavior covered transitively when the registered middleware is tested
- **Constants-only files** (`PipeKey.cs`, `PolicyKeys.cs`, `RetryKey.cs`, `PropertyHeaders.cs`, `RetryHeaders.cs` — `public const string X = "..."` declarations only)
- **Enum-only files** (e.g., `MessageContextSubscibeStage.cs` — note the project's typo, preserve verbatim if referenced)
- **Pure interfaces with no behavior** (covered via implementations per Wave 3 DW14): `IMessageContext`, `IMessageContextRepository`, `IRetryInformationProvider`, `IRetryInformationHeaderUpdater`
- **Static plugin classes that ONLY expose `Use<X>` extension methods** without behavior (e.g., `HostNameQueueSuffix` — static class with only `UseHostQueueSuffix` plugin extension)

### Co-located `*Options` POCO testing convention

Each middleware in scope (T1/T2/T3 Base/T5/T6) declares a co-located `<Name>Options` POCO class in the same source file. Per Wave 4 cadence: **the `*Options` POCO is tested in the same test file as its middleware** (e.g., `ConsumeAttributeMiddlewareTests.cs` covers both `ConsumeAttributeMiddleware` ctor + `InvokeAsync` AND `ConsumeAttributeOptions` POCO defaults). Implementer prompt enumerates the public surface of each `*Options` class.

### Co-located static extension class testing convention

When a middleware source file co-declares a `public static class <Name>Extensions` with public methods reading/writing `IPipeContext.Properties` (not pure `IClientBuilder` plugin extensions), the extensions are tested in the same test file as the middleware. Single occurrence in Wave 5: `WildcardRoutingKeyExtensions` co-located with `WildcardRoutingKeyMiddleware` (T2).

### `StagedMiddleware` `StageMarker` F1=(a) coverage

Every `StagedMiddleware` subclass overrides a public abstract `StageMarker` property. Per F1=(a) strict, each middleware test must include a `Should_Have_<Stage>_StageMarker` test verifying the override returns the expected stage constant. Plan-write enumeration locks the expected stage per middleware via source read.

---

### Task 1 (T1) — `test/RawRabbit.Enrichers.Attributes.Tests/` (~5 test files, ~10–14 tests)

**Files:**
- Create: `test/RawRabbit.Enrichers.Attributes.Tests/ExchangeAttributeTests.cs`
- Create: `test/RawRabbit.Enrichers.Attributes.Tests/QueueAttributeTests.cs`
- Create: `test/RawRabbit.Enrichers.Attributes.Tests/RoutingAttributeTests.cs`
- Create: `test/RawRabbit.Enrichers.Attributes.Tests/Middleware/ConsumeAttributeMiddlewareTests.cs`
- Create: `test/RawRabbit.Enrichers.Attributes.Tests/Middleware/ProduceAttributeMiddlewareTests.cs`

**Per-source coverage:**

- `ExchangeAttributeTests.cs` — POCO attribute. Public surface: `Name` (string), `Type` (`ExchangeType` enum), `Durable` (setter-only writes to `NullableDurability`), `AutoDelete` (setter-only writes to `NullableAutoDelete`), plus underlying `NullableDurability` / `NullableAutoDelete` if exposed. Test ctor + property get/set defaults. **Verify exact public properties at SDD-time via source read** (plan-write sampled top properties only; full enumeration locked at SDD).
- `QueueAttributeTests.cs` — POCO attribute. Public surface includes `Name` (string), `MessageTtl` (int), `MaxPriority` (byte), `DeadLeterExchange` (string — note project typo, preserve), `Mode` (string), plus durability/exclusivity properties. Test ctor + property get/set defaults.
- `RoutingAttributeTests.cs` — POCO attribute. Public surface: `RoutingKey` (string), `PrefetchCount` (ushort), `NoAck` (bool — backed by `NullableAutoAck`), `AutoAck` (bool — same backing). Test the dual-property symmetry.
- `Middleware/ConsumeAttributeMiddlewareTests.cs` — covers BOTH `ConsumeAttributeMiddleware : StagedMiddleware` ctor `(ConsumeAttributeOptions options = null)` + `InvokeAsync(IPipeContext, CancellationToken)` AND co-located `ConsumeAttributeOptions` POCO defaults. F1=(a): happy (apply attributes when message type has them), error (handle null/missing message type / no attributes), `StageMarker` returns expected stage.
- `Middleware/ProduceAttributeMiddlewareTests.cs` — analog for outbound; covers `ProduceAttributeMiddleware` + `ProduceAttributeOptions`. F1=(a) + StageMarker.

**Skip per W5-P1:** `AttributePlugin.cs`

**Notes:** No LogProvider, no broker types, no namespace collision. Pure `IPipeContext`-mock work (3 patterns per spec §2).

- [ ] **Step 1: Implement T1 test files (Sonnet implementer dispatch)**
  - Read each source file for exact public surface enumeration
  - Write 5 test files following the pattern above
  - F1=(a) strict per public method on every public class
  - BOM on every new `.cs` file (UTF-8 with BOM)
  - Indentation: tabs (per `.editorconfig`)

- [ ] **Step 2: Verify T1 acceptance gate locally**
  ```bash
  dotnet build test/RawRabbit.Enrichers.Attributes.Tests -c Release
  dotnet test test/RawRabbit.Enrichers.Attributes.Tests --no-build -c Release
  for f in test/RawRabbit.Enrichers.Attributes.Tests/**/*.cs; do file "$f"; done | grep -v "UTF-8 Unicode (with BOM)" || echo "BOM OK"
  ```
  Expected: 0 errors; 0 failed; ≥10 passed; all .cs files BOM-marked.

- [ ] **Step 3: Spec reviewer + code quality reviewer dispatches**
  - Spec reviewer: verify F1=(a) public-method coverage matches enumerated source classes
  - Code quality reviewer: verify A25 anti-patterns avoided, BOM, xUnit conventions, `async Task` not `.Wait()`

- [ ] **Step 4: Apply fix-up commits if reviewers surface real findings (verify against source before applying)**

- [ ] **Step 5: Commit T1**
  ```bash
  git add test/RawRabbit.Enrichers.Attributes.Tests/ExchangeAttributeTests.cs \
          test/RawRabbit.Enrichers.Attributes.Tests/QueueAttributeTests.cs \
          test/RawRabbit.Enrichers.Attributes.Tests/RoutingAttributeTests.cs \
          test/RawRabbit.Enrichers.Attributes.Tests/Middleware/ConsumeAttributeMiddlewareTests.cs \
          test/RawRabbit.Enrichers.Attributes.Tests/Middleware/ProduceAttributeMiddlewareTests.cs
  git commit -m "$(cat <<'EOF'
add wave 5 task 1: enrichers.attributes tests (5 files, <N> tests)

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
  ```

---

### Task 2 (T2) — `test/RawRabbit.Enrichers.GlobalExecutionId.Tests/` (~8 test files, ~14–20 tests)

**Files:**
- Create: `test/RawRabbit.Enrichers.GlobalExecutionId.Tests/Dependencies/GlobalExecutionIdRepositoryTests.cs`
- Create: `test/RawRabbit.Enrichers.GlobalExecutionId.Tests/PipeContextExtensionsTests.cs`
- Create: `test/RawRabbit.Enrichers.GlobalExecutionId.Tests/Middleware/GlobalExecutionIdMiddlewareTests.cs`  **[Collection("LogProviderState")]**
- Create: `test/RawRabbit.Enrichers.GlobalExecutionId.Tests/Middleware/AppendGlobalExecutionIdMiddlewareTests.cs`  **[Collection("LogProviderState")]**
- Create: `test/RawRabbit.Enrichers.GlobalExecutionId.Tests/Middleware/ExecutionIdRoutingMiddlewareTests.cs`  **[Collection("LogProviderState")]**
- Create: `test/RawRabbit.Enrichers.GlobalExecutionId.Tests/Middleware/WildcardRoutingKeyMiddlewareTests.cs`  **[Collection("LogProviderState")]**
- Create: `test/RawRabbit.Enrichers.GlobalExecutionId.Tests/Middleware/PersistGlobalExecutionIdMiddlewareTests.cs`
- Create: `test/RawRabbit.Enrichers.GlobalExecutionId.Tests/Middleware/PublishHeaderAppenderMiddlewareTests.cs`

**Per-source coverage:**

- `Dependencies/GlobalExecutionIdRepositoryTests.cs` — `GlobalExecutionIdRepository` class (likely AsyncLocal-backed). Test get/set/clear semantics + thread-isolation via parallel async ops if AsyncLocal-based.
- `PipeContextExtensionsTests.cs` — `static class PipeContextExtensions` with `GetGlobalExecutionId(this IPipeContext)` + companion setter/use methods. Use concrete `PipeContext` per spec §2 pattern 2.
- `Middleware/GlobalExecutionIdMiddlewareTests.cs` — **`[Collection("LogProviderState")]`**. `GlobalExecutionIdMiddleware : StagedMiddleware` ctor `(GlobalExecutionOptions options = null)` + `InvokeAsync` resolves/creates execution id from context or repo + co-located `GlobalExecutionOptions` POCO. F1=(a) + `StageMarker`.
- `Middleware/AppendGlobalExecutionIdMiddlewareTests.cs` — **`[Collection("LogProviderState")]`**. `AppendGlobalExecutionIdMiddleware : StagedMiddleware` ctor `(AppendGlobalExecutionIdOptions options = null)` + `InvokeAsync` appends execution id to outbound headers + co-located `AppendGlobalExecutionIdOptions`. F1=(a) + `StageMarker`.
- `Middleware/ExecutionIdRoutingMiddlewareTests.cs` — **`[Collection("LogProviderState")]`**. `ExecutionIdRoutingMiddleware : StagedMiddleware` ctor `(ExecutionIdRoutingOptions options = null)` + `InvokeAsync` updates routing key with execution id + co-located `ExecutionIdRoutingOptions`. F1=(a) + `StageMarker`.
- `Middleware/WildcardRoutingKeyMiddlewareTests.cs` — **`[Collection("LogProviderState")]`**. `WildcardRoutingKeyMiddleware : StagedMiddleware` ctor `(WildcardRoutingKeyOptions options = null)` + `InvokeAsync` replaces wildcard segments + co-located `WildcardRoutingKeyOptions`. F1=(a) + `StageMarker`. **Also covers co-located `WildcardRoutingKeyExtensions` (`UseWildcardRoutingSuffix<TPipeContext>(this TPipeContext, bool withWildCard = true)` writes `SubscribeWithWildCard` flag to `context.Properties`; `GetWildcardRoutingSuffixActive(this IPipeContext)` reads with default `true`)** per the co-located static extension convention; concrete `PipeContext` pattern.
- `Middleware/PersistGlobalExecutionIdMiddlewareTests.cs` — `PersistGlobalExecutionIdMiddleware : StagedMiddleware` ctor `(PersistGlobalExecutionIdOptions options = null)` + `InvokeAsync` persists execution id to repo on consume + co-located `PersistGlobalExecutionIdOptions`. F1=(a) + `StageMarker`.
- `Middleware/PublishHeaderAppenderMiddlewareTests.cs` — `PublishHeaderAppenderMiddleware : StagedMiddleware` ctor `(PublishHeaderAppenderOptions options = null)` + `InvokeAsync` appends execution id to publish headers + co-located `PublishHeaderAppenderOptions`. F1=(a) + `StageMarker`.

**Skip per W5-P1:** `GlobalExecutionIdPlugin.cs`, `PipeKey.cs` (constants), `PropertyHeaders.cs` (constants)

**Notes:** 4 LogProvider consumers per spec §2 → 4 `[Collection("LogProviderState")]` opt-ins. No broker types. No namespace collision.

- [ ] **Step 1: Implement T2 test files (Sonnet implementer dispatch)**
- [ ] **Step 2: Verify T2 acceptance gate locally**
  ```bash
  dotnet build test/RawRabbit.Enrichers.GlobalExecutionId.Tests -c Release
  dotnet test test/RawRabbit.Enrichers.GlobalExecutionId.Tests --no-build -c Release
  grep -rn '\[Collection("LogProviderState")\]' test/RawRabbit.Enrichers.GlobalExecutionId.Tests/ | wc -l  # expect ≥4
  ```
- [ ] **Step 3: Spec + code quality reviewer dispatches**
- [ ] **Step 4: Apply fix-ups if reviewers surface real findings**
- [ ] **Step 5: Commit T2** (analogous to T1 step 5; commit message: `add wave 5 task 2: enrichers.globalexecutionid tests (8 files, <N> tests)`)

---

### Task 3 (T3) — `test/RawRabbit.Enrichers.MessageContext.Tests/` (~8 test files across `Base/`+`Respond/`+`Subscribe/`, ~14–20 tests)

**Files:**
- Create: `test/RawRabbit.Enrichers.MessageContext.Tests/Base/Context/MessageContextTests.cs`  **[namespace alias for `MessageContext` class]**
- Create: `test/RawRabbit.Enrichers.MessageContext.Tests/Base/Dependencies/MessageContextRepositoryTests.cs`
- Create: `test/RawRabbit.Enrichers.MessageContext.Tests/Base/PipeContextExtensionsTests.cs`
- Create: `test/RawRabbit.Enrichers.MessageContext.Tests/Base/Middleware/PublishForwardingMiddlewareTests.cs`
- Create: `test/RawRabbit.Enrichers.MessageContext.Tests/Base/Middleware/ConsumeForwardingMiddlewareTests.cs`
- Create: `test/RawRabbit.Enrichers.MessageContext.Tests/Respond/PipeContextExtensionsTests.cs`
- Create: `test/RawRabbit.Enrichers.MessageContext.Tests/Respond/RespondMessageContextExtensionTests.cs`
- Create: `test/RawRabbit.Enrichers.MessageContext.Tests/Subscribe/PipeContextExtensionsTests.cs`
- Create: `test/RawRabbit.Enrichers.MessageContext.Tests/Subscribe/SubscribeMessageContextExtensionTests.cs`

**Per-source coverage:**

**`Base/` subdir (`RawRabbit.Enrichers.MessageContext` source assembly):**

- `Base/Context/MessageContextTests.cs` — `MessageContext : IMessageContext` POCO with `Guid GlobalRequestId` + other properties. **Namespace alias required** (`using MC = RawRabbit.Enrichers.MessageContext.Context.MessageContext;`) per W5-R2 — class name collides with namespace last segment. Test ctor + property get/set.
- `Base/Dependencies/MessageContextRepositoryTests.cs` — `MessageContextRepository : IMessageContextRepository`. Test get/set/clear (likely AsyncLocal-backed; same shape as T2's repo). `IMessageContextRepository` itself skipped per W5-P1 (interface; covered indirectly).
- `Base/PipeContextExtensionsTests.cs` — `static class PipeContextExtensions` with `UseMessageContext<TPipeContext>(this TPipeContext, object msgContext)` (returns generic `TPipeContext`). Use concrete `PipeContext` per spec §2 pattern 2.
- `Base/Middleware/PublishForwardingMiddlewareTests.cs` — `PublishForwardingMiddleware : StagedMiddleware` ctor `(IMessageContextRepository repo)` + `InvokeAsync` forwards context on publish. **Note: ctor takes `IMessageContextRepository` directly, NOT options.** Test with `Mock<IMessageContextRepository>`. F1=(a) + `StageMarker`.
- `Base/Middleware/ConsumeForwardingMiddlewareTests.cs` — `ConsumeForwardingMiddleware : StagedMiddleware` ctor `(IMessageContextRepository repo)` + `InvokeAsync` extracts context on consume. Same mock pattern. F1=(a) + `StageMarker`.

**`Respond/` subdir (`RawRabbit.Enrichers.MessageContext.Respond` source assembly):**

- `Respond/PipeContextExtensionsTests.cs` — `static class PipeContextExtensions` with `AddMessageContextType<TMessageContext>(this IPipeContext)` + `GetMessageContextType(this IPipeContext)`. Concrete `PipeContext` pattern.
- `Respond/RespondMessageContextExtensionTests.cs` — `static class RespondMessageContextExtension` with `RespondPipe` field (Action<IPipeBuilder>) + 2 `RespondAsync<TRequest, TResponse, TMessageContext>` overloads (return `Task<IPipeContext>`). Per VP-21 IBusClient-extension test pattern: `Mock<IBusClient>.Setup(b => b.InvokeAsync(...)).ReturnsAsync(mockContext.Object); var result = await mockBus.Object.RespondAsync<...>(...); mockBus.Verify(...);`.

**`Subscribe/` subdir (`RawRabbit.Enrichers.MessageContext.Subscribe` source assembly):**

- `Subscribe/PipeContextExtensionsTests.cs` — `static class PipeContextExtensions` with `UseMessageContext(this ISubscribeContext, Func<IPipeContext, object>)` + `AddMessageContextType<TMessageContext>` + `GetMessageContextResolver(this IPipeContext)` + `GetMessageContextType(this IPipeContext)`. Concrete context pattern.
- `Subscribe/SubscribeMessageContextExtensionTests.cs` — `static class SubscribeMessageContextExtension` with `ConsumePipe` + `SubscribePipe` Action<IPipeBuilder> fields + 2 `SubscribeAsync<TMessage, TMessageContext>` overloads (return `Task<IPipeContext>`). Per VP-21 IBusClient-extension pattern.

**Skip per W5-P1:** `MessageContextPlugin.cs` (Base), `ContextForwardPlugin.cs` (Base), `IMessageContext.cs` (interface — DW14), `IMessageContextRepository` (interface — DW14, covered via repo impl), `MessageContextSubscibeStage.cs` (Subscribe — enum-only; preserve typo verbatim if referenced)

**Notes:** No LogProvider. No broker types. **Heavy namespace-collision risk** (3 source assemblies declare `PipeContextExtensions`; `MessageContext` class collides with namespace name). Subdirectory namespaces (`Tests.Base`, `Tests.Respond`, `Tests.Subscribe`) put each `PipeContextExtensions` test in a distinct namespace, eliminating cross-file collision. Alias only needed for `MessageContext` class reference in `Base/Context/MessageContextTests.cs`.

- [ ] **Step 1-5:** analogous to T1 (implement → verify → review → fix-up if needed → commit)
  Commit message: `add wave 5 task 3: enrichers.messagecontext tests (9 files, <N> tests)`

---

### Task 4 (T4) — `test/RawRabbit.Enrichers.Polly.Tests/` (~11 new test files, ~22–32 new tests; plus 1 active + 2 skipped inherited per W5-D4)

**Files:**

**New test files:**
- Create: `test/RawRabbit.Enrichers.Polly.Tests/PipeContextExtensionsTests.cs`
- Create: `test/RawRabbit.Enrichers.Polly.Tests/Middleware/PolicyMiddlewareTests.cs`
- Create: `test/RawRabbit.Enrichers.Polly.Tests/Middleware/BasicPublishMiddlewareTests.cs`
- Create: `test/RawRabbit.Enrichers.Polly.Tests/Middleware/ConsumerCreationMiddlewareTests.cs`
- Create: `test/RawRabbit.Enrichers.Polly.Tests/Middleware/ExchangeDeclareMiddlewareTests.cs`
- Create: `test/RawRabbit.Enrichers.Polly.Tests/Middleware/ExplicitAckMiddlewareTests.cs`
- Create: `test/RawRabbit.Enrichers.Polly.Tests/Middleware/HandlerInvocationMiddlewareTests.cs`
- Create: `test/RawRabbit.Enrichers.Polly.Tests/Middleware/PooledChannelMiddlewareTests.cs`
- Create: `test/RawRabbit.Enrichers.Polly.Tests/Middleware/QueueBindMiddlewareTests.cs`
- Create: `test/RawRabbit.Enrichers.Polly.Tests/Middleware/TransientChannelMiddlewareTests.cs`

**Existing files to preserve verbatim per W5-D4 (do NOT modify):**
- `test/RawRabbit.Enrichers.Polly.Tests/Services/ChannelFactoryTests.cs` (2 skipped — `[Fact(Skip = "Phase 5/7 territory: 2-arg ConnectionFactory.CreateConnection signature mismatch with 1-arg mock setups; tests need re-mocking when broker layer is modernized")]`)
- `test/RawRabbit.Enrichers.Polly.Tests/Middleware/QueueDeclareMiddlewareTests.cs` (1 active)

**Optional ChannelFactory ctor null-arg additions:** Plan-write defers per W5-D5; if T4 implementer adds ctor null-arg tests for `ChannelFactory(IConnectionFactory, RawRabbitConfiguration, ConnectionPolicies=null)` (no broker setup needed for ctor), add to existing `Services/ChannelFactoryTests.cs`. Skip if the 2-arg `ConnectionFactory.CreateConnection` signature surfaces any new test (W5-R1; counts against N≤5 wave pool).

**Per-source coverage:**

- `PipeContextExtensionsTests.cs` — `static class PipeContextExtensions` with 2 methods: `GetPolicy(this IPipeContext, string policyName = null)` returns `Policy` + `UsePolicy<TPipeContext>(this TPipeContext, Policy, string)` returns `TPipeContext`. Concrete `PipeContext` pattern; verify Properties dict round-trip.
- `Middleware/PolicyMiddlewareTests.cs` — `PolicyMiddleware : StagedMiddleware` ctor `(PolicyOptions options = null)` + `InvokeAsync` invokes `PolicyAction(context)` (delegate from options) + forwards via `Next.InvokeAsync`. F1=(a): happy (PolicyAction invoked + Next called), error (null PolicyAction handled), `StageMarker` returns `StageMarker.Initialized`. Co-located `PolicyOptions` POCO (with `PolicyAction` Action<IPipeContext> + `ConnectionPolicies` properties) tested in same file.
- `Middleware/BasicPublishMiddlewareTests.cs` — `BasicPublishMiddleware : Pipe.Middleware.BasicPublishMiddleware` (inherits from core middleware) ctor `(IExclusiveLock exclusive, BasicPublishOptions options = null)`. **Polly inheritance pattern per spec §2**: instantiate Polly subclass with `Mock<IExclusiveLock>`, populate `IPipeContext.Properties` with policy via `context.UsePolicy(...)` + `PipeKey.Channel` = `Mock<IModel>.Object`, call inherited `InvokeAsync`, assert `Mock<IModel>.Verify(c => c.BasicPublish(...), Times.Once)` ran inside the policy execution. F1=(a) at protected-override level. **Broker-touching: inline `Mock<IModel>` setup.**
- `Middleware/ConsumerCreationMiddlewareTests.cs` — `ConsumerCreationMiddleware : Pipe.Middleware.ConsumerCreationMiddleware` ctor `(IConsumerFactory consumerFactory, ConsumerCreationOptions options = null)`. Inheritance pattern; `Mock<IConsumerFactory>` + policy in context; assert factory call + policy execution. F1=(a) at protected-override level.
- `Middleware/ExchangeDeclareMiddlewareTests.cs` — `ExchangeDeclareMiddleware : Pipe.Middleware.ExchangeDeclareMiddleware` ctor `(ITopologyProvider topologyProvider, ExchangeDeclareOptions options = null)`. Inheritance pattern; `Mock<ITopologyProvider>` + policy in context; assert topology call + policy. F1=(a) at protected-override level.
- `Middleware/ExplicitAckMiddlewareTests.cs` — `ExplicitAckMiddleware : Pipe.Middleware.ExplicitAckMiddleware` ctor `(INamingConventions conventions, ITopologyProvider topology, IChannelFactory channelFactory, ExplicitAckOptions options = null)`. Heavier dep set: `Mock<INamingConventions>` (use `.SetupGet(c => c.QueueNamingConvention).Returns((Type t) => "...")` per Wave 4 carry-forward), `Mock<ITopologyProvider>`, `Mock<IChannelFactory>` + policy. Inheritance pattern. F1=(a) at protected-override level.
- `Middleware/HandlerInvocationMiddlewareTests.cs` — `HandlerInvocationMiddleware : Pipe.Middleware.HandlerInvocationMiddleware` ctor `(HandlerInvocationOptions options = null)`. Inheritance pattern; only options dep + policy in context. F1=(a) at protected-override level.
- `Middleware/PooledChannelMiddlewareTests.cs` — `PooledChannelMiddleware : Pipe.Middleware.PooledChannelMiddleware` ctor `(IChannelPoolFactory poolFactory, PooledChannelOptions options = null)`. Inheritance pattern. **Broker-touching: protected `GetChannelAsync` returns `Task<IModel>` — inline `Mock<IModel>` setup needed.** F1=(a) at protected-override level.
- `Middleware/QueueBindMiddlewareTests.cs` — `QueueBindMiddleware : Pipe.Middleware.QueueBindMiddleware` ctor `(ITopologyProvider topologyProvider, QueueBindOptions options = null)`. Inheritance pattern. F1=(a) at protected-override level.
- `Middleware/TransientChannelMiddlewareTests.cs` — `TransientChannelMiddleware : Pipe.Middleware.TransientChannelMiddleware` ctor `(IChannelFactory factory)` (NO options ctor — single dep). Inheritance pattern. **Broker-touching: protected `CreateChannelAsync` returns `Task<IModel>` — inline `Mock<IModel>` setup needed.** F1=(a) at protected-override level.

**Polly inheritance test pattern (canonical example for the 9 inherited middleware) — see spec §2 Polly section + W5-R6 risk:**

```csharp
[Fact]
public async Task Should_Execute_DeclareQueue_Inside_Policy()
{
    // Arrange: instantiate Polly subclass + parent's required deps
    var mockTopology = new Mock<ITopologyProvider>();
    mockTopology.Setup(t => t.DeclareQueueAsync(It.IsAny<QueueDeclaration>()))
                .Returns(Task.CompletedTask);
    var middleware = new RawRabbit.Enrichers.Polly.Middleware.QueueDeclareMiddleware(mockTopology.Object);

    // Set up parent's IPipeContext state to reach the protected DeclareQueueAsync hook.
    // Use Policy.NoOpAsync() so policy.ExecuteAsync(action) invokes the action exactly once.
    // Sync policies (Policy.Handle<>().Retry(...)) throw InvalidOperationException when
    // invoked via ExecuteAsync — Polly v7 strictly separates sync/async APIs.
    var queueDeclaration = new QueueDeclaration { Name = "test.queue" };
    var ctx = new PipeContext { Properties = new Dictionary<string, object>() };
    ctx.UsePolicy(Policy.NoOpAsync(), PolicyKeys.QueueDeclare);
    ctx.Properties[PipeKey.QueueDeclaration] = queueDeclaration;

    // Act: call inherited InvokeAsync (parent reaches protected DeclareQueueAsync,
    // which Polly's override wraps in policy.ExecuteAsync(action))
    await middleware.InvokeAsync(ctx, CancellationToken.None);

    // Assert: the inner topology call ran inside the policy's ExecuteAsync.
    // (Verify proves policy executed: no other code path reaches Topology.DeclareQueueAsync.)
    mockTopology.Verify(t => t.DeclareQueueAsync(queueDeclaration), Times.Once);
}
```

(Plan-write enumerates exact `PipeKey` constants + parent setup per source class at SDD-time; pattern above is the shape.)

**Skip per W5-P1:** `PollyPlugin.cs`, `PolicyKeys.cs` (constants), `RetryKey.cs` (constants)

**Notes:** NO LogProvider in Polly source (spec §11 A10 verified). 5 broker-type-touching files (3 middleware: TransientChannel, PooledChannel, BasicPublish need `Mock<IModel>`; ChannelFactory is service-level; RetryKey is constants-only/skipped). 9 inherited middleware + 1 direct `PolicyMiddleware` = 10 middleware total; 1 already tested (`QueueDeclareMiddlewareTests.cs`) → 9 new test files in Middleware/ + `PipeContextExtensionsTests.cs` = 10 new test files. **W5-R1**: any new ChannelFactory test that hits the 2-arg `ConnectionFactory.CreateConnection` signature mismatch → annotate `[Fact(Skip = "Phase 5/7 territory: <signature>")]` against N≤5 wave pool.

- [ ] **Step 1: Implement T4 test files (Sonnet implementer dispatch — largest task)**
- [ ] **Step 2: Verify T4 acceptance gate locally**
  ```bash
  dotnet build test/RawRabbit.Enrichers.Polly.Tests -c Release
  dotnet test test/RawRabbit.Enrichers.Polly.Tests --no-build -c Release
  # Polly's existing 2 skips inherited; new tests should ADD to (active + 2 skipped) baseline
  ```
- [ ] **Step 3: Spec + code quality reviewer dispatches**
- [ ] **Step 4: Apply fix-ups if reviewers surface real findings (Wave 4 lesson: implementer "deferred to Phase 5/7" / "simplified to construction-only" claims are dead-ends per spec §7; verify against source before applying reviewer null-arg findings — false-positive class for ctors that just store args)**
- [ ] **Step 5: Commit T4** (`add wave 5 task 4: enrichers.polly tests (10 files, <N> tests)`)

---

### Task 5 (T5) — `test/RawRabbit.Enrichers.QueueSuffix.Tests/` (~5 test files, ~10–14 tests)

**Files:**
- Create: `test/RawRabbit.Enrichers.QueueSuffix.Tests/QueueSuffixOptionsTests.cs`
- Create: `test/RawRabbit.Enrichers.QueueSuffix.Tests/QueueSuffixMiddlewareTests.cs`
- Create: `test/RawRabbit.Enrichers.QueueSuffix.Tests/ApplicationNamePipeExtensionTests.cs`
- Create: `test/RawRabbit.Enrichers.QueueSuffix.Tests/CustomQueueSuffixExtensionsTests.cs`
- Create: `test/RawRabbit.Enrichers.QueueSuffix.Tests/HostNamePipeExtensionsTests.cs`

**Per-source coverage:**

- `QueueSuffixOptionsTests.cs` — POCO with **7 `Func<>`/`Action<>` fields** (NOT properties): `QueueDeclareFunc`, `CustomSuffixFunc`, `ContextSuffixOverrideFunc`, `ActiveFunc`, `SkipSuffixFunc`, `ConsumeConfigFunc`, `AppendSuffixAction`. Test default values (all should be `null` for un-assigned) + delegate-set behavior (assign each, verify field round-trip).
- `QueueSuffixMiddlewareTests.cs` — `QueueSuffixMiddleware : StagedMiddleware` ctor `(QueueSuffixOptions options = null)` + `InvokeAsync` reads queue declaration via `options.QueueDeclareFunc`, applies suffix via `options.CustomSuffixFunc` / `options.AppendSuffixAction`, optionally skips via `options.SkipSuffixFunc` + `options.ActiveFunc`. **Use options-injection pattern (spec §2 pattern 3): construct `new QueueSuffixOptions { CustomSuffixFunc = ctx => "test-suffix", ... }` for each test.** F1=(a) + `StageMarker`.
- `ApplicationNamePipeExtensionTests.cs` — `static class ApplicationNamePipeExtension` with `UseApplicationQueueSuffix<TPipeContext>(this TPipeContext, bool use = true)` (returns generic) + `GetApplicationSuffixFlag(this IPipeContext)`. Concrete `PipeContext` pattern.
- `CustomQueueSuffixExtensionsTests.cs` — `static class CustomQueueSuffixExtensions` with 2 `UseCustomQueueSuffix<TPipeContext>` overloads (string prefix; bool activated). Concrete `PipeContext` pattern; test both overloads.
- `HostNamePipeExtensionsTests.cs` — `static class HostNamePipeExtensions` with `GetHostnameQueueSuffixFlag(this IPipeContext)` + `UseHostnameQueueSuffix<TPipeContext>(this TPipeContext, bool activated)`. Concrete `PipeContext` pattern.

**Skip per W5-P1:** `QueueSuffixPlugin.cs`, `ApplicationQueueSuffixPlugin.cs`, `CustomQueueSuffixPlugin.cs`, `HostNameQueueSuffix.cs` (static plugin class with only `UseHostQueueSuffix(this IClientBuilder)` extension — no behavior beyond plugin registration)

**Notes:** No LogProvider. No broker types. No namespace collision. Pure `IPipeContext`-mock work via options-injection (spec §2 pattern 3) for the middleware + concrete `PipeContext` (pattern 2) for the extensions.

- [ ] **Step 1-5:** analogous (commit message: `add wave 5 task 5: enrichers.queuesuffix tests (5 files, <N> tests)`)

---

### Task 6 (T6) — `test/RawRabbit.Enrichers.RetryLater.Tests/` (~6 test files, ~14–20 tests)

**Files:**
- Create: `test/RawRabbit.Enrichers.RetryLater.Tests/Common/RetryTests.cs`
- Create: `test/RawRabbit.Enrichers.RetryLater.Tests/Common/RetryInformationTests.cs`
- Create: `test/RawRabbit.Enrichers.RetryLater.Tests/Common/RetryInformationProviderTests.cs`
- Create: `test/RawRabbit.Enrichers.RetryLater.Tests/Common/RetryInformationHeaderUpdaterTests.cs`
- Create: `test/RawRabbit.Enrichers.RetryLater.Tests/Common/RetryLaterPipeContextExtensionsTests.cs`
- Create: `test/RawRabbit.Enrichers.RetryLater.Tests/Middleware/RetryInformationExtractionMiddlewareTests.cs`
- Create: `test/RawRabbit.Enrichers.RetryLater.Tests/Middleware/RetryLaterMiddlewareTests.cs`  **[Collection("LogProviderState")]**

**Per-source coverage:**

- `Common/RetryTests.cs` — `Retry : Acknowledgement` ctor `(TimeSpan span)` + `Span` property + static factory `In(TimeSpan)`. **`Acknowledgement` base verified at `src/RawRabbit/Common/Acknowledgement.cs:3` `public abstract class Acknowledgement { }`** (empty marker class). Test ctor + factory + property.
- `Common/RetryInformationTests.cs` — POCO with `NumberOfRetries` (int) + `OriginalDelivered` (DateTime) properties. Test ctor + property defaults.
- `Common/RetryInformationProviderTests.cs` — `RetryInformationProvider : IRetryInformationProvider` with `Get(BasicDeliverEventArgs args)` returning `RetryInformation`. **W5-R3 critical**: enumerate per F1=(a):
  - Happy: round-trip headers (set headers → Get → assert RetryInformation matches)
  - Error: malformed header (e.g., `x-number-of-retries` is non-integer string)
  - Error: missing required key
  - Error: null `BasicDeliverEventArgs.BasicProperties`
  - Boundary: max int retry count
  - Boundary: zero retry count
  
  **Test setup uses `new BasicDeliverEventArgs(...)` directly (NOT `Mock<>`)** — `BasicDeliverEventArgs` is a RMQ.Client data POCO; same pattern as Wave 4 used for `BasicGetResult`.
- `Common/RetryInformationHeaderUpdaterTests.cs` — `RetryInformationHeaderUpdater : IRetryInformationHeaderUpdater` with 2 `AddOrUpdate(BasicDeliverEventArgs args[, RetryInformation retryInfo])` overloads. F1=(a): happy (read existing → increment → write back), error (null args, null BasicProperties).
- `Common/RetryLaterPipeContextExtensionsTests.cs` — `static class RetryLaterPipeContextExtensions` with `GetRetryInformation(this IPipeContext)` + companion methods. Concrete `PipeContext` pattern.
- `Middleware/RetryInformationExtractionMiddlewareTests.cs` — `RetryInformationExtractionMiddleware : StagedMiddleware` ctor `(IRetryInformationProvider retryProvider, RetryInformationExtractionOptions options = null)` + `InvokeAsync` uses provider to extract retry info from inbound `BasicDeliverEventArgs`. Mock `IRetryInformationProvider`. F1=(a) + `StageMarker`. Co-located `RetryInformationExtractionOptions` POCO covered in same file.
- `Middleware/RetryLaterMiddlewareTests.cs` — **`[Collection("LogProviderState")]`** (LogProvider). `RetryLaterMiddleware : StagedMiddleware` ctor `(ITopologyProvider topology, INamingConventions conventions, IChannelFactory channelFactory, IRetryInformationHeaderUpdater headerUpdater, RetryLaterOptions options = null)` — heavy dep set (4 mocks + options). `InvokeAsync` increments retry count, sets dead-letter routing, schedules requeue. Use `Mock<INamingConventions>` with `.SetupGet(...).Returns(...)` Func-property pattern (Wave 4 carry-forward dead-end). F1=(a) + `StageMarker`. Co-located `RetryLaterOptions` POCO covered in same file.

**Skip per W5-P1:** `RetryLaterPlugin.cs`, `Common/RetryHeaders.cs` (constants-only — `x-number-of-retries`, `x-original-delivered`), interfaces `IRetryInformationProvider` + `IRetryInformationHeaderUpdater` (covered indirectly via implementations per DW14)

**Notes:** 1 LogProvider consumer (`RetryLaterMiddleware`) → 1 `[Collection("LogProviderState")]` opt-in. **`BasicDeliverEventArgs` from RMQ.Client used by RetryInformationProvider + Updater + RetryInformationExtractionMiddleware** — instantiate directly, NOT mocked (data POCO; same pattern as Wave 4 `BasicGetResult`). No connection-layer broker types (`IModel`/`IConnection` not referenced in RetryLater source per spec §11 A9 verification). **W5-R3 codec error-path coverage is non-negotiable** — implementer prompt MUST enumerate the malformed-header / missing-key / boundary tests above.

- [ ] **Step 1: Implement T6 test files (Sonnet implementer dispatch)**
- [ ] **Step 2: Verify T6 acceptance gate locally**
  ```bash
  dotnet build test/RawRabbit.Enrichers.RetryLater.Tests -c Release
  dotnet test test/RawRabbit.Enrichers.RetryLater.Tests --no-build -c Release
  grep -rn '\[Collection("LogProviderState")\]' test/RawRabbit.Enrichers.RetryLater.Tests/  # expect exactly 1 (RetryLaterMiddlewareTests.cs)
  ```
- [ ] **Step 3: Spec + code quality reviewer dispatches**
- [ ] **Step 4: Apply fix-ups if reviewers surface real findings**
- [ ] **Step 5: Commit T6** (`add wave 5 task 6: enrichers.retrylater tests (7 files, <N> tests)`)

---

### Task 7 — Wave-wide review + 3-run stability + push (per W5-D7; not a separate SDD task)

After T6 ships:

- [ ] **Step 1: Wave-wide spec reviewer dispatch**
  Spec reviewer sub-agent goes over the entire Wave 5 diff (all T1-T6 test files) one last time before push (catches cross-task issues like the LogProvider race Wave 3 hit at Task 9).

- [ ] **Step 2: Wave-wide code quality reviewer dispatch**
  Same shape; A25 anti-pattern + BOM + `async Task` final sweep.

- [ ] **Step 3: 3-run stability check (race detector per spec §6.7)**
  ```bash
  for run in 1 2 3; do
    echo "=== run $run ==="
    for proj in test/RawRabbit.Tests \
                test/RawRabbit.DependencyInjection.ServiceCollection.Tests \
                test/RawRabbit.Enrichers.Attributes.Tests \
                test/RawRabbit.Enrichers.GlobalExecutionId.Tests \
                test/RawRabbit.Enrichers.MessageContext.Tests \
                test/RawRabbit.Enrichers.Polly.Tests \
                test/RawRabbit.Enrichers.QueueSuffix.Tests \
                test/RawRabbit.Enrichers.RetryLater.Tests \
                test/RawRabbit.Operations.Get.Tests \
                test/RawRabbit.Operations.MessageSequence.Tests \
                test/RawRabbit.Operations.Publish.Tests \
                test/RawRabbit.Operations.Request.Tests \
                test/RawRabbit.Operations.Respond.Tests \
                test/RawRabbit.Operations.StateMachine.Tests \
                test/RawRabbit.Operations.Subscribe.Tests \
                test/RawRabbit.Operations.Tools.Tests; do
      dotnet test "$proj" --no-build -c Release 2>&1 | grep "Passed!" | tail -1
    done
  done
  ```
  Expected: identical pass/fail/skip counts across all 3 runs (16 unit-test projects × 3 runs = 48 lines of Passed!).

- [ ] **Step 4: Phase 4.5 phase-wide acceptance check (parent §7 gates 1–7)**
  - Gate 1: Wave 1 shipped ✅ (already done)
  - Gate 2: Test count ≥217 net-new + final aggregate ≥257 → expected ≥1231 (1191 baseline + ≥40 W5)
  - Gate 3: Per-area gates satisfied for all 19 active areas (W1-W4 already verified; W5 verified by per-task gates above)
  - Gate 4: Build clean (0 errors; warning shape stable from W4 close)
  - Gate 5: xUnit analyzer cleanup (WAS verified during W1-W4; no W5 work introduces new categories)
  - Gate 6: Skipped tests preserved (15 baseline + W5 N where N≤5)
  - Gate 7: `[Theory]/[InlineData]` ≥3 phase-wide (already 13 files at W4 close per CDR R1 §1)
  
  All 7 gates expected to pass; record actual values.

- [ ] **Step 5: Ask user for explicit push confirmation**
  Per Wave 4 cadence + system-prompt rule on shared-state actions. Use `AskUserQuestion` with options: "Push Wave 5 to origin/2.0" / "Hold for review" / "Discuss findings".

- [ ] **Step 6: Push to origin/2.0**
  ```bash
  git push origin 2.0
  ```
  Verify with `git rev-list --count origin/2.0..HEAD` → 0 after push.

- [ ] **Step 7: Create handoff at `handoffs/<timestamp>_phase-4.5-wave-5-shipped.md`**
  Per Wave 4 close cadence; document the wave + Phase 4.5 close + Phase 5 transition state.

---

## Inherited from spec

The following 25 assumptions were verified by `superpowers:thorough-brainstorming` at spec-write time and are NOT re-verified here. Trusted as ground truth (full evidence in spec §11):

- **A1**: 8 in-scope `src/RawRabbit.Enrichers.<X>/` source assemblies exist with file counts (Attributes=6, GlobalExecutionId=11, MessageContext=8, MessageContext.Respond=2, MessageContext.Subscribe=3, Polly=15, QueueSuffix=9, RetryLater=9; total 63).
- **A2**: 4 out-of-scope source assemblies (HttpContext, MessagePack, Protobuf, ZeroFormatter) exist (D4 exclusion is real).
- **A3**: 6 in-scope `test/RawRabbit.Enrichers.<X>.Tests/` directories exist with valid csproj.
- **A4**: 5 of 6 in-scope test projects contain ZERO `.cs` files; only Polly.Tests has 2.
- **A5**: Polly.Tests contains exactly `Services/ChannelFactoryTests.cs` + `Middleware/QueueDeclareMiddlewareTests.cs`.
- **A6**: Polly.Tests has 1 active `[Fact]` + 2 `[Fact(Skip=...)]`; skip annotation matches exact format.
- **A7**: ChannelFactoryTests skip text matches "2-arg ConnectionFactory.CreateConnection" + "1-arg mock setups" verbatim.
- **A8**: All 6 csproj reference RawRabbit core + own assembly + xunit/Moq/NET.Test.Sdk; MessageContext.Tests references all 3 sub-assemblies; Polly.Tests adds Polly package ref.
- **A9**: Polly is sole in-scope enricher with broker connection-layer types (`IModel`/`IConnection`/`ConnectionFactory` — `IChannel` was a phantom token in alternation, contributes 0 hits per CDR R1 §1).
- **A10** (broken→adjusted): 5 in-scope sources use `LogProvider.For<T>()` (4 GlobalExecutionId + 1 RetryLater) — per-source list locked in spec §2 + §6.
- **A11**: 3 `PipeContextExtensions` classes confirmed (one per MessageContext sub-project); `MessageContext` class collides with namespace last segment.
- **A12** (partial): Polly's 9 inherited middleware override protected hooks (NOT `InvokeAsync`); spec count "8 of 9" is off by one (actual: 9 inherited + 1 PolicyMiddleware = 10), informational only per CDR R1 §1.
- **A13**: `IPipeContext` at `src/RawRabbit/Pipe/IPipeContext.cs`.
- **A14**: Package versions: xunit 2.9.3, runner.visualstudio 2.8.2, NET.Test.Sdk 18.5.1, Moq 4.20.72, RabbitMQ.Client 5.0.1.
- **A15**: `.editorconfig` mandates tabs.
- **A16**: `dotnet build -c Release` returns 0 errors at HEAD `da11292` + 8 NU1902 MessagePack vulnerability warnings.
- **A17**: Aggregate skip count = 15 at HEAD `da11292` (RawRabbit.Tests=12, ServiceCollection.Tests=1, Polly.Tests=2).
- **A18**: Parent D4 explicitly excludes HttpContext + MessagePack + Protobuf + ZeroFormatter (verbatim).
- **A19** (drift): Parent §5 row 5 says 7 source assemblies / 52 source files; on-disk = 8 / 63 (off-by-one in parent table; ≥40 acceptance unchanged).
- **A20**: Parent D10 wording matches "skipped methods within cleanup-target files are NOT touched".
- **A21**: 4 out-of-scope enrichers consumed only by IntegrationTests + sample (no unit-test conflict).
- **A22**: 8 in-scope enrichers consumed only by IntegrationTests + PerformanceTest from outside their test projects.
- **A23**: RetryLater public types include `IRetryInformationProvider`, `RetryInformationProvider`, `IRetryInformationHeaderUpdater`, `RetryInformationHeaderUpdater`, `Retry : Acknowledgement`, `RetryHeaders`, `RetryInformation`, etc.
- **A24** (now stale): Spec target path was absent at spec-write; spec now committed at `481ac2d`.
- **A25** (now stale): Working tree was clean at spec-write; current state reflects the successful spec write.

CDR R1 (`docs/criticalreviews/2026-05-14-modernization-phase-4.5-wave-5-design-critical-review-1.md`) reconfirmed all 25 under fresh read; ✅ Approve as-is.

## Verified plan-level assumptions

Newly introduced by this plan (paths, signatures, commands, ordering, code-in-plan validity) and verified empirically against HEAD `481ac2d` at plan-write time on 2026-05-14:

| # | Cat | Assumption | Evidence |
|---|---|---|---|
| P1 | 1 | Per-task source-file enumeration is exact (T1=6, T2=11, T3=13, T4=15, T5=9, T6=9) | `find` per-task at plan-draft time; counts match spec §11 A1 |
| P2 | 1 | Each new test file path's parent directory exists; no collision with Polly.Tests' 2 existing files (`Services/ChannelFactoryTests.cs` + `Middleware/QueueDeclareMiddlewareTests.cs`) | `ls` per-test-project confirms 6 dirs exist; Polly.Tests new files are at `Middleware/<Other>MiddlewareTests.cs` + `PipeContextExtensionsTests.cs` (no collision) |
| P3 | 2 | T1 source signatures (3 attributes + 2 middleware) — middleware are `: StagedMiddleware` ctor `(<Name>Options options = null)`; attribute properties: `ExchangeAttribute.{Name, Type, Durable, AutoDelete}`, `QueueAttribute.{Name, MessageTtl, MaxPriority, DeadLeterExchange (typo preserved), Mode, ...}`, `RoutingAttribute.{RoutingKey, PrefetchCount, NoAck, AutoAck}` | `grep -nE 'public class\|public\s+\w+\s+\w+\s*\{\|public.*Middleware\(' src/RawRabbit.Enrichers.Attributes/...` per-file |
| P4 | 2 | T2 source signatures: 6 middleware all `: StagedMiddleware` ctor `(<Name>Options options = null)` with co-located `<Name>Options` POCO; `GlobalExecutionIdRepository : public class`; `PipeContextExtensions.GetGlobalExecutionId(this IPipeContext)`; `PipeKey.cs` + `PropertyHeaders.cs` are constants-only (W5-P1 valid) | `grep` per-file as above |
| P5 | 2 | T3 source signatures: `MessageContext : IMessageContext` POCO with `Guid GlobalRequestId`; `MessageContextRepository : IMessageContextRepository`; 2 Base middleware `: StagedMiddleware` ctor `(IMessageContextRepository repo)` (NOT options-only); 3 `PipeContextExtensions` classes with `UseMessageContext`/`AddMessageContextType<T>`/`GetMessageContextType` etc. extensions; `RespondMessageContextExtension` static class with `RespondPipe` field + 2 `RespondAsync<TRequest, TResponse, TMessageContext>` overloads (return `Task<IPipeContext>`); `SubscribeMessageContextExtension` static class with `ConsumePipe` + `SubscribePipe` fields + 2 `SubscribeAsync<TMessage, TMessageContext>` overloads; `MessageContextSubscibeStage : enum` (W5-P1 valid) | `grep` per-file |
| P6 | 2 | T4 Polly remaining 4 inherited middleware ctors: `HandlerInvocationMiddleware(HandlerInvocationOptions options = null)`; `PooledChannelMiddleware(IChannelPoolFactory poolFactory, PooledChannelOptions options = null)`; `QueueBindMiddleware(ITopologyProvider topologyProvider, QueueBindOptions options = null)`; `TransientChannelMiddleware(IChannelFactory factory)` — single dep, NO options. PolicyMiddleware ctor `(PolicyOptions options = null)` confirmed during CDR. PolicyKeys + RetryKey are constants-only (W5-P1 valid). PollyPlugin is `static class` with 2 `UsePolly` extensions (W5-P1 valid). PipeContextExtensions: `GetPolicy(this IPipeContext, string)` + `UsePolicy<TPipeContext>(this TPipeContext, Policy, string)` | `grep` per-file |
| P7 | 2 | T5 source signatures: `QueueSuffixOptions` has 7 `Func<>`/`Action<>` **fields** (not properties): `QueueDeclareFunc`, `CustomSuffixFunc`, `ContextSuffixOverrideFunc`, `ActiveFunc`, `SkipSuffixFunc`, `ConsumeConfigFunc`, `AppendSuffixAction`; `QueueSuffixMiddleware : StagedMiddleware` ctor `(QueueSuffixOptions options = null)`; `HostNameQueueSuffix` is `static class` plugin (W5-P1 valid); 3 plugin files all `static class` (W5-P1 valid); 3 pipe extension files all `static class` with `IPipeContext` extension methods | `cat` of QueueSuffixOptions.cs + `grep` per-file |
| P8 | 2 | T6 source signatures: `Retry : Acknowledgement` with `Span` property + ctor `(TimeSpan)` + static `In(TimeSpan)`; `RetryHeaders` constants-only `x-number-of-retries`/`x-original-delivered` (W5-P1 added to skip list); `RetryInformation` POCO with `NumberOfRetries` (int) + `OriginalDelivered` (DateTime); `RetryInformationProvider : IRetryInformationProvider` with `Get(BasicDeliverEventArgs args)` returning `RetryInformation`; `RetryInformationHeaderUpdater : IRetryInformationHeaderUpdater` with 2 `AddOrUpdate(BasicDeliverEventArgs args[, RetryInformation])` overloads; `RetryInformationExtractionMiddleware : StagedMiddleware` ctor `(IRetryInformationProvider, RetryInformationExtractionOptions options = null)`; `RetryLaterMiddleware : StagedMiddleware` ctor `(ITopologyProvider, INamingConventions, IChannelFactory, IRetryInformationHeaderUpdater, RetryLaterOptions options = null)` (4 deps + options); `RetryLaterPlugin` static plugin (W5-P1 valid); `RetryLaterPipeContextExtensions.GetRetryInformation(this IPipeContext)` | `cat` of Retry.cs + `grep` per-file |
| P9 | 2 | `Acknowledgement` base class exists at `src/RawRabbit/Common/Acknowledgement.cs:3` `public abstract class Acknowledgement { }` (empty marker) — load-bearing for T6 `Retry : Acknowledgement` test | `grep -rn 'public abstract class Acknowledgement\|public class Acknowledgement\b' src/` |
| P10 | 2 | `PolicyOptions` exists with `PolicyAction` (Action<IPipeContext>) + `ConnectionPolicies` properties — confirmed during CDR via `cat` of PolicyMiddleware.cs (the file declares both `PolicyOptions` POCO and `PolicyMiddleware`); namespace is `RawRabbit` (top-level, NOT `RawRabbit.Enrichers.Polly.Middleware`) | `cat` of `src/RawRabbit.Enrichers.Polly/Middleware/PolicyMiddleware.cs` |
| P11 | 3 | Per-task `dotnet build test/<X> -c Release` + `dotnet test test/<X> --no-build -c Release` work as written | Sanity check at session start ran `dotnet test` per-project loop across all 11 then-existing projects; exit 0 for each |
| P12 | 4 | T1-T6 task ordering is independent — each test project ProjectReferences only its own source assembly + RawRabbit core (+ Polly package for Polly.Tests + 3 MessageContext sub-assemblies for MessageContext.Tests). No cross-test-project ProjectReferences. Sequential dispatch is purely SDD-protocol (W5-D1), not technical | `grep -E 'ProjectReference.*Enrichers' test/<each>.csproj` confirms per-project independence |
| P13 | 5 | All 6 in-scope `Enrichers.*.Tests` csprojs transitively get RabbitMQ.Client (only T4 actually needs it) via ProjectReference to RawRabbit.csproj which has `<PackageReference Include="RabbitMQ.Client" />`; Polly.Tests has explicit Polly PackageReference | Wave 4 precedent: `Operations.Get.Tests.csproj` has zero direct RabbitMQ.Client ref but uses `Mock<IModel>` in 3 test files; same transitive setup |
| P14 | 2 | All in-scope T2/T3/T5/T6 middleware + T4 PolicyMiddleware are `: StagedMiddleware` (12 total); T4's 9 inherited middleware are `: Pipe.Middleware.<X>Middleware` (which is `: Middleware`). `StagedMiddleware` adds public abstract `StageMarker` property — F1=(a) coverage of `StageMarker` override required for all 12 StagedMiddleware tests | `grep -nE ': StagedMiddleware\|: Middleware\b\|: Pipe\.Middleware\.' src/RawRabbit.Enrichers.<X>/Middleware/*.cs` per-project |
| P15 | 1 | Plan target path `docs/plans/2026-05-14-modernization-phase-4.5-wave-5-implementation-plan.md` did NOT exist before write | `ls` returned "No such file or directory" |
| P16 | 1 | Pre-write git state: working tree clean, branch `2.0`, HEAD `481ac2d` (the spec commit), 1 commit ahead of `origin/2.0` (the spec commit itself; will become 2 ahead after this plan commit) | `git status --short` empty; `git symbolic-ref --short HEAD` = `2.0`; `git rev-parse HEAD` = `481ac2d949c4d0bb7791e5a25361e01decb470e6`; `git rev-list --count origin/2.0..HEAD` = `1` |

**Summary:** 16/16 ✅ verified. Mechanical adjustments folded into plan body during draft (T1 attribute property names, co-located `*Options` testing convention, W5-P1 expansion to include `IMessageContextRepository`/`HostNameQueueSuffix`/`RetryHeaders`/`IRetryInformationProvider`/`IRetryInformationHeaderUpdater`, `BasicDeliverEventArgs` direct-instantiation pattern for T6, `StageMarker` F1=(a) coverage). Wave shape unchanged.

**Cat 6 (consumer impact) is N/A** — plan is create-only (no Modify entries on existing source). Per skill's conditional Cat 6 mandate: plan touches no existing code, so no consumer-impact verification required.

## Tasks NOT in this plan

(Inherited verbatim from spec §10; bullet form preserved.)

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

## Known issues inherited from spec

(Inherited verbatim from spec §12; numbered list preserved.)

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
