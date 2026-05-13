# Modernization Phase 4.5 Wave 3 — Broker-Touching Coverage Implementation Plan

> **For agentic workers:** REQUIRED: Use `superpowers:subagent-driven-development` to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Source spec:** `docs/specs/2026-05-13-modernization-phase-4.5-wave-3-design.md` (commit SHA: `86103a672e8c2c2eba99f0006fda6cf63aabd3b1`)

**Plan scope:** Wave 3 only — 8 sub-areas of broker-touching test coverage (Pipe/Middleware + PipeContextFactory, Channel + Abstraction, Consumer, Subscription, Instantiation + Disposable, DependencyInjection core, root BusClient.cs, Common/TopologyProvider). Waves 4-5 are independent per parent spec D5; will be planned separately.

**Goal:** Land Wave 3's broker-touching test coverage across the 8 sub-areas per spec §4 acceptance gates. ~44 new test files + 1 helper class (`BrokerMocks.cs`); estimated ~179-311 net-new passing tests (vs spec gate `≥150` — ~1.2-2× margin from per-method strict enumeration). Zero source-code edits; zero csproj edits (Wave 1 wired all needed ProjectReferences; existing `RawRabbit.Tests.csproj` is the landing target for all Wave 3 tests).

**Architecture:** One test class per testable source class; one subdirectory per source area mirroring `src/` (per parent spec §3 file-layout convention). Test layout = source layout. New `BrokerMocks` static helper per Wave 3 spec DW8 lands in `test/RawRabbit.Tests/TestHelpers/BrokerMocks.cs`; consumed by Tasks 1 (channel-touching middleware), 2 (Channel), 3 (Consumer), 8 (TopologyProvider). Mock at `IPipeContext` boundary for non-broker-touching middleware; mock at `IModel`/`IConnection`/`IConnectionFactory` boundary for broker-touching code via `BrokerMocks` factory methods. New `[Fact(Skip = "Phase 5/7 territory: <signature>")]` annotations permitted per parent R2 / spec DW9; expected count 0-5 per spec gate 7.

**Tech stack (inherited from spec):** .NET 10 SDK 10.0.107 + C# 13; xUnit 2.9.3; xunit.runner.visualstudio 2.8.2; Microsoft.NET.Test.Sdk 18.5.1; Moq 4.20.72; RabbitMQ.Client 5.0.1 (legacy; Phase 5 modernizes). CPM via `Directory.Packages.props`. No new package dependencies for Wave 3.

---

## File Structure

**Create (~44 new test files + 1 helper; 0 source-code files; 0 csproj files):**

Task 1 — `test/RawRabbit.Tests/TestHelpers/` (NEW dir) + `Pipe/Middleware/` (NEW dir) + `Pipe/`:
- `TestHelpers/BrokerMocks.cs` — static helper with `MakeConnectionChain()`, `MakeChannel()`, `MakeBasicConsumer()` factory methods (DW8)
- `Pipe/Middleware/BasicPropertiesMiddlewareTests.cs` — ctor `(ISerializer, BasicPropertiesOptions=null)` + InvokeAsync sets `BasicProperties` on context
- `Pipe/Middleware/BasicPublishConfigurationMiddlewareTests.cs` — ctor `(IBasicPublishConfigurationFactory, …Options=null)` + InvokeAsync resolves config from context or factory
- `Pipe/Middleware/BasicPublishMiddlewareTests.cs` — ctor `(IExclusiveLock, BasicPublishOptions=null)` + InvokeAsync calls `IModel.BasicPublish` under lock (BROKER-TOUCHING — uses `BrokerMocks.MakeChannel()`)
- `Pipe/Middleware/BodyDeserializationMiddlewareTests.cs` — ctor `(ISerializer, MessageDeserializationOptions=null)` + InvokeAsync deserializes body bytes via serializer
- `Pipe/Middleware/BodySerializationMiddlewareTests.cs` — ctor `(ISerializer, MessageSerializationOptions=null)` + InvokeAsync serializes message → context bytes
- `Pipe/Middleware/CancellationMiddlewareTests.cs` — implicit parameterless ctor + InvokeAsync throws if `token.IsCancellationRequested`
- `Pipe/Middleware/ChannelCreationMiddlewareTests.cs` — ctor `(IChannelFactory, ChannelCreationOptions=null)` + InvokeAsync sets `Channel` in context (BROKER-TOUCHING — uses `BrokerMocks.MakeChannel()`)
- `Pipe/Middleware/ConsumeConfigurationMiddlewareTests.cs` — ctor `(IConsumeConfigurationFactory, …Options=null)` + InvokeAsync resolves consume config
- `Pipe/Middleware/ConsumerConsumeMiddlewareTests.cs` — ctor `(IConsumerFactory, BasicConsumeOptions=null)` + InvokeAsync triggers consume (BROKER-TOUCHING — uses `BrokerMocks.MakeBasicConsumer()`)
- `Pipe/Middleware/ConsumerCreationMiddlewareTests.cs` — ctor `(IConsumerFactory, ConsumerCreationOptions=null)` + InvokeAsync creates consumer via factory (uses `BrokerMocks.MakeBasicConsumer()`)
- `Pipe/Middleware/ConsumerMessageHandlerMiddlewareTests.cs` — ctor `(IPipeBuilderFactory, IPipeContextFactory, ConsumeOptions=null)` + InvokeAsync wires handler dispatch
- `Pipe/Middleware/ExceptionHandlingMiddlewareTests.cs` — ctor `(IPipeBuilderFactory, ExceptionHandlingOptions=null)` + InvokeAsync catches downstream exceptions + invokes handler
- `Pipe/Middleware/ExchangeDeclareMiddlewareTests.cs` — ctor `(ITopologyProvider, ExchangeDeclareOptions=null)` + InvokeAsync calls `topologyProvider.DeclareExchangeAsync` (mocks ITopologyProvider, no broker direct)
- `Pipe/Middleware/ExchangeDeleteMiddlewareTests.cs` — ctor `(ExchangeDeleteOptions)` + InvokeAsync calls `IModel.ExchangeDelete` (BROKER-TOUCHING — uses `BrokerMocks.MakeChannel()`)
- `Pipe/Middleware/ExplicitAckMiddlewareTests.cs` — ctor `(INamingConventions, ITopologyProvider, IChannelFactory, ExplicitAckOptions=null)` + InvokeAsync subscribes to consumer + dispatches ack/nack (BROKER-TOUCHING — uses `BrokerMocks.MakeChannel()` + `BrokerMocks.MakeBasicConsumer()`)
- `Pipe/Middleware/HandlerInvocationMiddlewareTests.cs` — ctor `(HandlerInvocationOptions=null)` + InvokeAsync invokes message handler from context
- `Pipe/Middleware/HeaderDeserializationMiddlewareTests.cs` — inherits `StagedMiddleware`; ctor `(ISerializer, HeaderDeserializationOptions=null)` + InvokeAsync deserializes header bytes; `StageMarker` property = `MessageReceived`
- `Pipe/Middleware/HeaderSerializationMiddlewareTests.cs` — inherits `StagedMiddleware`; ctor `(ISerializer, HeaderSerializationOptions=null)` + InvokeAsync serializes header into `IBasicProperties.Headers`; `StageMarker` property = `BasicPropertiesCreated`
- `Pipe/Middleware/NoOpMiddlewareTests.cs` — implicit parameterless ctor + InvokeAsync returns `Task.CompletedTask` (1-2 tests)
- `Pipe/Middleware/PooledChannelMiddlewareTests.cs` — ctor `(IChannelPoolFactory, PooledChannelOptions=null)` + InvokeAsync gets channel from pool, sets in context (BROKER-TOUCHING — mocks `IChannelPoolFactory` returning `BrokerMocks.MakeChannel()`)
- `Pipe/Middleware/QueueBindMiddlewareTests.cs` — ctor `(ITopologyProvider, QueueBindOptions=null)` + InvokeAsync calls `topologyProvider.BindQueueAsync`
- `Pipe/Middleware/QueueDeclareMiddlewareTests.cs` — ctor `(ITopologyProvider, QueueDeclareOptions=null)` + InvokeAsync calls `topologyProvider.DeclareQueueAsync`
- `Pipe/Middleware/QueueDeleteMiddlewareTests.cs` — ctor `(QueueDeleteOptions=null)` + InvokeAsync calls `IModel.QueueDelete` (BROKER-TOUCHING — uses `BrokerMocks.MakeChannel()`)
- `Pipe/Middleware/StageMarkerMiddlewareTests.cs` — ctor `(StageMarkerOptions)` + InvokeAsync no-op + `Stage` property; co-located `StagedMiddleware` abstract base covered indirectly via `HeaderDeserialization`/`HeaderSerialization` tests
- `Pipe/Middleware/SubscriptionMiddlewareTests.cs` — ctor `(ISubscriptionRepository, SubscriptionOptions=null)` + InvokeAsync registers subscription via repo (uses `BrokerMocks.MakeBasicConsumer()`)
- `Pipe/Middleware/TransientChannelMiddlewareTests.cs` — ctor `(IChannelFactory)` + InvokeAsync sets `TransientChannel` in context, dispose pattern (BROKER-TOUCHING — uses `BrokerMocks.MakeChannel()`)
- `Pipe/Middleware/UseHandlerMiddlewareTests.cs` — ctor `(Func<IPipeContext, Func<Task>, Task>)` + InvokeAsync invokes handler with next-action
- `Pipe/PipeContextFactoryTests.cs` — ctor `(RawRabbitConfiguration)` + `CreateContext(params KeyValuePair[])` happy + with additional + sets `ClientConfiguration` key in `Properties`

Task 2 — `test/RawRabbit.Tests/Channel/` (existing dir; modify 3 + create 4):
- (Modify) `ChannelFactoryTests.cs` — add 3-5 tests for non-skipped `ChannelFactory` paths (`Dispose()` happy, `ConnectAsync` happy, `Connection` field state); leave existing 4 skipped tests intact (parent D10)
- (Modify) `ChannelPoolTests.cs` — add 3-5 tests for non-skipped `StaticChannelPool` paths (ctor with seed, `GetAsync` happy when channel available, `Dispose` happy); leave existing 3 skipped tests intact (parent D10)
- (Modify) `DynamicChannelPoolTests.cs` — add 3-5 tests for `DynamicChannelPool`-specific behavior (parameterless ctor, `Add(params)` increases pool, `Add(IEnumerable)`, `Remove(int)` decreases, `Remove(params)`, `Remove(IEnumerable)`)
- (Create) `AutoScalingChannelPoolTests.cs` — ctor `(IChannelFactory, AutoScalingOptions)`, `GetAsync` happy under min size, `SetupScaling` schedules timer, `Dispose` cleanup
- (Create) `AutoScalingChannelPoolFactoryTests.cs` — ctor `(IChannelFactory, AutoScalingOptions=null)`, `GetChannelPool(name=null)` happy + named, `Dispose`
- (Create) `ConcurrentChannelQueueTests.cs` — `Enqueue` returns TCS, `TryDequeue` returns true when present + false when empty, `IsEmpty`, `Count`, `Queued` event fires
- (Create) `ResilientChannelPoolTests.cs` — 3 ctor overloads `(IChannelFactory, int)` / `(IChannelFactory)` / `(IChannelFactory, IEnumerable<IModel>)`, `GetAsync` recreates channel when closed

Task 3 — `test/RawRabbit.Tests/Consumer/` (NEW dir):
- `ConsumerFactoryTests.cs` — ctor `(IChannelFactory)` + 4 public methods (`GetConsumerAsync` happy + cached + error; `GetConfiguredConsumerAsync` similar; `CreateConsumerAsync` happy + with explicit channel + with null channel→creates via factory; `ConfigureConsume` happy + null cfg + missing queue/tag throws)
- `ConsumerExtensionsTests.cs` — `CancelAsync(this IBasicConsumer, CancellationToken)` happy + non-Eventing-consumer throws + token cancellation cancels TCS; `OnMessage(this IBasicConsumer, EventHandler, Predicate?)` happy + non-Eventing throws + abort predicate path

Task 4 — `test/RawRabbit.Tests/Subscription/` (NEW dir):
- `SubscriptionTests.cs` — ctor `(IBasicConsumer, string)` happy + ctor with `DefaultBasicConsumer` (covered via `BrokerMocks.MakeEventingConsumer()` — `EventingBasicConsumer` is a `DefaultBasicConsumer` subclass; cast in ctor reads its ConsumerTag) + ctor with non-`DefaultBasicConsumer` (covered via `Mock<IBasicConsumer>` whose proxy is NOT a DefaultBasicConsumer; cast returns null and ctor skips QueueName/ConsumerTag assignment). **Dispose tests MUST use the real `EventingBasicConsumer` from `BrokerMocks.MakeEventingConsumer()`** (per CIR R1 F2): `_consumer.CancelAsync()` is the extension method at `Consumer/ConsumerFactory.cs:125-145` which casts to `EventingBasicConsumer` and throws `NotSupportedException("Can only cancellation EventBasicConsumer")` on a Moq proxy. Verify Dispose behavior via `Mock<IModel>.Verify(c => c.BasicCancel(<tag>), Times.Once)` (the underlying broker call) — NOT via `Mock<IBasicConsumer>.Verify(c => c.CancelAsync())` (Moq cannot intercept extension methods). Dispose paths covered: when Model.IsOpen=true + Active=true → BasicCancel invoked once; when Model.IsOpen=false → BasicCancel never invoked; idempotency (Dispose × 2) → BasicCancel invoked exactly once. Plus `Active` setter; `QueueName`/`ConsumerTag` getters
- `SubscriptionRepositoryTests.cs` — parameterless ctor + `Add(ISubscription)` happy + `GetAll()` returns added items + `GetAll()` empty initially + concurrent Add safety (use `Parallel.For` with N=100)

Task 5 — `test/RawRabbit.Tests/Instantiation/` (NEW dir; mirror `Disposable/` subdir):
- `ClientBuilderTests.cs` — parameterless ctor sets `PipeBuilderAction` and `DependencyInjection` to no-op delegates (assert not null + invoking does nothing) + `Register(pipe, ioc)` accumulates via `+=`
- `InstanceFactoryTests.cs` — ctor `(IDependencyResolver)` happy; `Create()` returns `BusClient` resolved via 3 dependencies (mock `IDependencyResolver` returning `Mock<IPipeBuilderFactory>`/`Mock<IPipeContextFactory>`/`Mock<IChannelFactory>`); `Dispose()` resolves and disposes `IResourceDisposer`; `ShutdownAsync(TimeSpan? graceful=null)` disposes all subscriptions + delays + calls Dispose
- `RawRabbitFactoryTests.cs` — 4 statics: `CreateSingleton(options=null)` returns `Disposable.BusClient` non-null; `CreateSingleton(options, register, resolverFunc)` honors custom register; `CreateInstanceFactory(options=null)` returns `InstanceFactory` non-null; `CreateInstanceFactory(options, register, resolverFunc)` honors custom register. **Per spec §3 convention: avoid `Resolve<IBusClient>()` triggers — use `Mock<IDependencyResolver>` chain that does NOT actually invoke broker**
- `Disposable/BusClientTests.cs` — ctor `(IInstanceFactory)` happy (mocked factory returning Mock<IBusClient>); `InvokeAsync(pipeCfg, contextCfg, token)` delegates to inner busClient; `Dispose()` casts factory to IDisposable and disposes if available + skips if not (mock both shapes)

Task 6 — `test/RawRabbit.Tests/DependencyInjection/` (existing dir; root-level files alongside Wave 2's Autofac/Ninject subdirs):
- `SimpleDependencyInjectionTests.cs` — 8 public methods × happy/error per VP-13:
  - `AddTransient<TS,TI>(Func)`: happy + factory invoked at resolution + duplicate registration replaces
  - `AddTransient<TS,TI>()`: happy + auto-resolves TI via `GetService(typeof(TI))`
  - `AddSingleton<TS>(TService instance)`: happy + same instance returned on repeat
  - `AddSingleton<TS,TI>(Func)`: happy + Lazy<T> only instantiates once
  - `AddSingleton<TS,TI>()`: happy + Lazy<T> only instantiates once
  - `GetService<TService>(params)`: happy + throws InvalidOperationException for unregistered abstract + auto-creates non-abstract via `CreateInstance`
  - `GetService(Type, params)`: happy + throws for unregistered abstract + auto-creates non-abstract
  - `TryGetService(Type, out service, params)`: happy returns true + returns false for unregistered abstract + returns true for non-abstract via auto-create
- `RawRabbitDependencyRegisterExtensionTests.cs` — `AddRawRabbit(this IDependencyRegister, RawRabbitOptions=null)` per VP-14: returns the same `IDependencyRegister` (for chaining); registers `RawRabbitConfiguration` (default `Local` when options=null vs custom from options); honors `options.Plugins` callback when non-null; honors `options.DependencyInjection` callback when non-null. **DO NOT enumerate all 24 internal `.AddSingleton<...>()` registrations** — that would be testing-the-spec. Test the extension's behavior shape; sample-resolve 1-2 key services (e.g., `IBusClient` registration is wired) without triggering broker connection (use `Mock<IDependencyRegister>` and `.Verify()` on key registration calls, OR test against a `SimpleDependencyInjection` instance and resolve `IInstanceFactory` not `IBusClient` per spec §3 convention)

Task 7 — `test/RawRabbit.Tests/` (root file):
- `BusClientTests.cs` — root-level test file (DW12). ctor `(IPipeBuilderFactory, IPipeContextFactory, IChannelFactory)` happy (per VP-15); ctor accepts non-null `factory` arg even though stored-but-unused per DW13 (assert no exception, document intentional); `InvokeAsync(pipeCfg, contextCfg=null, token=default)`: happy invokes pipe through factory chain; null `contextCfg` is permitted (uses `?.Invoke`); cancellation token propagates to underlying pipe.InvokeAsync. Mock `IPipeBuilderFactory.Create()` returning `Mock<IPipe>`; mock `IPipeContextFactory.CreateContext()` returning `Mock<IPipeContext>`; assert pipe.InvokeAsync called with the context

Task 8 — `test/RawRabbit.Tests/Common/` (existing dir; NEW file):
- `TopologyProviderTests.cs` — ctor `(IChannelFactory)` happy (per VP-16); 6 public ITopologyProvider methods: `DeclareExchangeAsync(ExchangeDeclaration)` calls `IModel.ExchangeDeclare`, `DeclareQueueAsync(QueueDeclaration)` calls `IModel.QueueDeclare`, `BindQueueAsync(string, string, string, IDictionary)` calls `IModel.QueueBind`, `UnbindQueueAsync(...)` calls `IModel.QueueUnbind`, `IsDeclared(ExchangeDeclaration)` returns false for new + true after Declare, `IsDeclared(QueueDeclaration)` same; `Dispose()` cleanup. Use `BrokerMocks.MakeConnectionChain()` for the underlying channel chain. **Highest broker-mock surface — most likely place for new Phase 5/7 skips per gate 7's N=0-5 budget.**

Task 9 — Wave 3 phase-acceptance verification (no edits)

**Modify (3 existing files; Task 2 only):**
- `test/RawRabbit.Tests/Channel/ChannelFactoryTests.cs` — append new test methods alongside existing 4 skipped tests
- `test/RawRabbit.Tests/Channel/ChannelPoolTests.cs` — append new test methods alongside existing 3 skipped tests
- `test/RawRabbit.Tests/Channel/DynamicChannelPoolTests.cs` — append new test methods (existing tests all pass; no skips)

**Verify-only:**
- `RawRabbit.sln` unchanged across Wave 3 (no new projects scaffolded; Wave 3 uses existing `RawRabbit.Tests`).
- Existing test files outside Channel/ unchanged.
- 7 + 1 + 2 = 10 pre-existing skipped tests remain skipped per parent D10 (the 7 in `Channel/`, 1 in ServiceCollection.Tests's `ServiceProviderAdapterTests` from Wave 2, 2 in `Polly.Tests/Services/`).

---

## Inherited from spec

The following assumptions were verified by `thorough-brainstorming` at spec-write time (HEAD `2b7016b`; spec last modified at `86103a6` after CDR R1 fix) and are NOT re-verified here. Trusted as ground truth:

- **A1** — 27 concrete instantiable middleware in `src/RawRabbit/Pipe/Middleware/` (29 files: `Middleware.cs` abstract base + `MiddlewareInfo.cs` info type non-instantiable; `StagedMiddleware` abstract base co-located in `StageMarkerMiddleware.cs` covered via concrete subclasses)
- **A2** — `IBusClient.cs` is an 11-line public interface with one method (`Task<IPipeContext> InvokeAsync(...)`)
- **A3** — `Channel/` source uses `IConnectionFactory`/`IConnection`/`IModel` from RabbitMQ.Client 5.0.1
- **A4** — `TopologyProvider.cs` touches `IModel` directly via `_channel.QueueBind/QueueDeclare/ExchangeDeclare/QueueUnbind` calls (broker-touching, fits Wave 3)
- **A5** — `Consumer/ConsumerFactory.cs` has 4 public methods + 2 protected virtuals + 1 protected helper + co-defines `ConsumerExtensions` static class with `CancelAsync` + `OnMessage` extensions on `IBasicConsumer`
- **A6** — `Subscription.cs` co-defines `ISubscription` interface; `SubscriptionRepository.cs` co-defines `ISubscriptionRepository` interface; both interfaces covered via impls
- **A7** — `ClientBuilder.cs` is a builder with `Action<>` property accumulators; `Register` uses `+=`
- **A8** — `RawRabbitOptions.cs` is pure POCO (3 settable properties, no methods); covered indirectly per DW15
- **A9** — All Wave 3 test landing dirs/files greenfield (re-verified at VP-1)
- **A10** — `test/RawRabbit.Tests/TestHelpers/` doesn't already exist; greenfield for DW8 (re-verified at VP-1)
- **A11** — No mutable statics in Wave 3 source beyond known `LogProvider.LoggerFactory` (Wave 2 territory; not touched in Wave 3); `AutoScalingOptions Default` is computed property NOT mutable static
- **A12** — Skip annotation wording verbatim "`Phase 5/7 territory: <reason>`" matches existing 7 skips' format
- **A13** — Post-Wave-2 baseline: 379/7 in `RawRabbit.Tests`; 21/1 in `ServiceCollection.Tests`; 1/2 in `Polly.Tests` (re-verified in CDR R1 pass)
- **A14** — `BusClient.cs:14` ctor takes 3 args; `factory` param stored as ctor signature param but no field assignment (dead arg); behavior preserved per DW13
- **A15** — Parent + Wave 2 spec inheritance unchanged at HEAD
- **A16** — 3 Pipe/ root files orphaned by Wave 2: `IPipeContextFactory.cs` (containing `PipeContextFactory` impl, picked up by Wave 3), `PipeKey.cs` (constants — skipped per DW16), `StageMarker.cs` (constants — skipped per DW16)
- **A17** — `AutoScalingChannelPool.cs` co-defines `AutoScalingOptions` POCO with `static AutoScalingOptions Default => new ...` computed property (NOT mutable static; safe)
- **DW8** — `test/RawRabbit.Tests/TestHelpers/BrokerMocks.cs` static helper class with factory methods; per-project (not separate csproj)
- **DW9** — Mock strategy = Moq + skip-when-blocked; new `[Fact(Skip = "Phase 5/7 territory: <reason>")]` permitted per parent R2; gate 7 budget N=0-5
- **DW10** — One Wave 3 plan with ~9-10 SDD tasks (one per sub-area + aggregate verification); final code review pass after Task 9
- **DW11** — Acceptance gate floor ≥150 net-new tests; range 179-311 informational
- **DW12** — `BusClient.cs` root tests at `test/RawRabbit.Tests/BusClientTests.cs`; `Disposable/BusClient.cs` tests at `test/RawRabbit.Tests/Instantiation/Disposable/BusClientTests.cs` (mirror source layout)
- **DW13** — `BusClient.cs:14` dead `IChannelFactory factory` arg preserved (test-only; Phase 7 may remove)
- **DW14** — `IBusClient.cs` interface gets NO direct test file; covered via `BusClientTests.cs` + `Disposable/BusClientTests.cs`
- **DW15** — `RawRabbitOptions` (pure POCO) gets NO dedicated test file; covered indirectly via `RawRabbitFactory` happy-path tests
- **DW16** — Pick up `PipeContextFactory` in Wave 3 area 1; explicitly skip `PipeKey.cs` + `StageMarker.cs` constants
- **F1 = (a)** (parent F1 carry-forward) — Strict per-method, no consolidation; each public method gets ≥1 happy + ≥1 error test individually; `[Theory]` consolidation only where 3+ methods share input/expected shape (parent D6)
- **D6** (parent) — `[Theory]/[InlineData]` required where 3+ similar tests differ only in input/expected
- **D8** (parent) — One test project per separate source assembly; `RawRabbit.Tests` inherits Autofac + Ninject ProjectReferences (D8 inconsistency preserved); Wave 3 lands all tests in `RawRabbit.Tests` (no new test projects)
- **D10** (parent) — Pre-existing skipped tests are NOT touched in Phase 4.5; Wave 3 may add new skips per R2 but does not modify the existing 10 (7 + 1 + 2)

---

## Verified plan-level assumptions

Newly introduced by this plan and verified empirically against HEAD `86103a6` at plan-write time on 2026-05-13:

| # | Category | Assumption | Evidence |
|---|---|---|---|
| VP-1 | Cat 1 (file paths) | All Wave 3 test landing dirs/files greenfield: `Pipe/Middleware/`, `Consumer/`, `Subscription/`, `Instantiation/`, `Instantiation/Disposable/`, `TestHelpers/`, `BusClientTests.cs` (root), `TopologyProviderTests.cs` (in `Common/`), `PipeContextFactoryTests.cs` (in `Pipe/`), 4 new Channel test files, 2 new DI core test files | `ls -d` for each dir + `ls` for each file returned "No such file or directory" |
| VP-2 | Cat 1 (file paths) | 3 existing Channel test files exist for Task 2 modify: `ChannelFactoryTests.cs`, `ChannelPoolTests.cs`, `DynamicChannelPoolTests.cs` | `ls test/RawRabbit.Tests/Channel/*.cs` returned exactly these 3 files |
| VP-3 | Cat 1 (file paths) | All 27 source middleware + PipeContextFactory.cs + 7 Channel pool source files + IChannelFactory exist as listed | `ls src/RawRabbit/Pipe/Middleware/*.cs` returns 29 (27 concrete + 2 non-instantiable per A1); `ls src/RawRabbit/Pipe/IPipeContextFactory.cs src/RawRabbit/Channel/*.cs src/RawRabbit/Channel/Abstraction/IChannelFactory.cs` confirms all source paths |
| VP-4 | Cat 2 (signature) | Each of 27 concrete middleware overrides `Task InvokeAsync(IPipeContext context, CancellationToken token = default)` from `Middleware` base (or via `StagedMiddleware` for HeaderDeserialization/HeaderSerialization). Per-middleware ctor signatures enumerated below per File Structure section. | Direct read of each middleware file — see Appendix A summary |
| VP-5 | Cat 2 (signature) | `PipeContextFactory(RawRabbitConfiguration config)` ctor; `IPipeContext CreateContext(params KeyValuePair<string, object>[] additional)` returns new `PipeContext` with `Properties = new ConcurrentDictionary` seeded by `additional` + sets `[PipeKey.ClientConfiguration] = _config` | `src/RawRabbit/Pipe/IPipeContextFactory.cs:12-31` |
| VP-6 | Cat 2 (signature) — CRITICAL | **Production `ChannelFactory.cs:34` calls `ConnectionFactory.CreateConnection(IList<string> hostnames, string clientProvidedName)` — the 2-arg overload.** The 7 existing skipped tests setup `It.IsAny<List<string>>()` (1-arg) which doesn't intercept the 2-arg call. **Wave 3 BrokerMocks.MakeConnectionChain() MUST setup the 2-arg signature: `Setup(c => c.CreateConnection(It.IsAny<IList<string>>(), It.IsAny<string>()))`** | Direct read of `src/RawRabbit/Channel/ChannelFactory.cs:34`: `Connection = ConnectionFactory.CreateConnection(ClientConfig.Hostnames, ClientConfig.ClientProvidedName);` |
| VP-7 | Cat 2 (signature) | `IConnection.CreateModel()` returns `IModel`; `IConnection.IsOpen` is `bool`; `IConnection.CloseReason` returns `ShutdownEventArgs`; `IConnection.ConnectionShutdown` is event; `IRecoverable` extension on connection returned by RabbitMQ.Client (cast pattern in `ChannelFactory.cs:84-95`) | RabbitMQ.Client 5.0.1 IConnection API; `src/RawRabbit/Channel/ChannelFactory.cs:50, 62, 69-72, 79-82` |
| VP-8 | Cat 2 (signature) | `IModel` methods called by Wave 3 sources: `QueueDeclare(string, bool, bool, bool, IDictionary<string,object>)`, `ExchangeDeclare(string, string, bool, bool, IDictionary<string,object>)`, `QueueBind(string, string, string, IDictionary<string,object>)`, `QueueUnbind(string, string, string, IDictionary<string,object>)`, `QueueDelete(string, bool, bool)`, `ExchangeDelete(string, bool)`, `BasicConsume(...)` (named-arg version), `BasicCancel(string)`, `BasicQos(uint, ushort, bool)`, `BasicAck(ulong, bool)`, `BasicNack(ulong, bool, bool)`, `BasicReject(ulong, bool)`, `IsClosed`/`IsOpen` props | Per Explore agent enumeration: TopologyProvider lines 136/150/195/217; ConsumerFactory lines 75/84-91/143; ExplicitAckMiddleware uses BasicAck/Nack/Reject family; ExchangeDelete/QueueDelete middleware — full enumeration in Appendix A |
| VP-9 | Cat 2 (signature) | Channel pool inheritance + ctor signatures: `ChannelFactory(IConnectionFactory, RawRabbitConfiguration)`; `StaticChannelPool(IEnumerable<IModel> seed)` + co-defines `IChannelPool` interface; `DynamicChannelPool : StaticChannelPool` (parameterless + IEnumerable<IModel> ctors); `ResilientChannelPool : DynamicChannelPool` (3 ctors: `(IChannelFactory, int)`, `(IChannelFactory)`, `(IChannelFactory, IEnumerable<IModel>)`); `AutoScalingChannelPool : DynamicChannelPool` (1 ctor: `(IChannelFactory, AutoScalingOptions)`); `AutoScalingChannelPoolFactory(IChannelFactory, AutoScalingOptions=null)` + co-defines `IChannelPoolFactory`; `ConcurrentChannelQueue()` parameterless | Per Explore agent enumeration of `src/RawRabbit/Channel/*.cs` |
| VP-10 | Cat 2 (signature) | `ConsumerFactory(IChannelFactory)` ctor; 4 public methods: `GetConsumerAsync(ConsumeConfiguration, IModel=null, CancellationToken=default)`, `GetConfiguredConsumerAsync(...)` similar shape, `CreateConsumerAsync(IModel=null, CancellationToken=default)`, `ConfigureConsume(IBasicConsumer, ConsumeConfiguration)`; `ConsumerExtensions` static with `CancelAsync(this IBasicConsumer, CancellationToken=default)` + `OnMessage(this IBasicConsumer, EventHandler<BasicDeliverEventArgs>, Predicate<BasicDeliverEventArgs>=null)` | Direct read of `src/RawRabbit/Consumer/ConsumerFactory.cs:13-173` confirmed at A5 + Explore re-pin |
| VP-11 | Cat 2 (signature) | `Subscription(IBasicConsumer, string queueName)` ctor; `Active` settable bool; `QueueName`/`ConsumerTag` get-only strings; `Dispose()` checks `_consumer.Model.IsOpen` + `Active` flag, then sets Active=false + calls `_consumer.CancelAsync()`. `SubscriptionRepository()` parameterless ctor; `Add(ISubscription)` + `GetAll()` over `ConcurrentBag<ISubscription>` | Direct read of `src/RawRabbit/Subscription/{Subscription,SubscriptionRepository}.cs` confirmed at A6 |
| VP-12 | Cat 2 (signature) | Instantiation ctors/methods: `ClientBuilder()` parameterless + 2 settable Action props + `Register(Action<IPipeBuilder>, Action<IDependencyRegister>)`; `InstanceFactory(IDependencyResolver)` ctor + `Create()`/`Dispose()`/`ShutdownAsync(TimeSpan?=null)`; `RawRabbitFactory` 4 statics: `CreateSingleton(RawRabbitOptions=null)`, `CreateSingleton(RawRabbitOptions, IDependencyRegister, Func<IDependencyRegister, IDependencyResolver>)`, `CreateInstanceFactory(RawRabbitOptions=null)`, `CreateInstanceFactory(RawRabbitOptions, IDependencyRegister, Func<IDependencyRegister, IDependencyResolver>)`; `Disposable.BusClient(IInstanceFactory)` ctor + `InvokeAsync(...)` + `Dispose()` casts factory to IDisposable | Per Explore agent enumeration of `src/RawRabbit/Instantiation/*.cs` + `Disposable/BusClient.cs` |
| VP-13 | Cat 2 (signature) | `SimpleDependencyInjection` 8 public methods (each returns `IDependencyRegister` for Add* / appropriate type for Get*): `AddTransient<TS,TI>(Func<IDependencyResolver, TI>)`, `AddTransient<TS,TI>()`, `AddSingleton<TS>(TS instance)`, `AddSingleton<TS,TI>(Func<IDependencyResolver, TS>)`, `AddSingleton<TS,TI>()`, `GetService<TService>(params object[])`, `GetService(Type, params object[])`, `TryGetService(Type, out object, params object[])`. Plus private `CreateInstance` reflection helper for non-abstract auto-creation. Throws `InvalidOperationException("No registration for " + serviceType)` when serviceType is abstract and unregistered | Direct read of `src/RawRabbit/DependencyInjection/SimpleDependencyInjection.cs:8-112` |
| VP-14 | Cat 2 (signature) | `RawRabbitDependencyRegisterExtension.AddRawRabbit(this IDependencyRegister, RawRabbitOptions = null)` returns `IDependencyRegister` (chained). Internally invokes 24 `.AddSingleton<>` / `.AddTransient<>` calls registering core services (RawRabbitConfiguration, IConnectionFactory, IChannelPoolFactory, IClientPropertyProvider, ISerializer, IConsumerFactory, IChannelFactory, ISubscriptionRepository, ITopologyProvider, IPublisher/Consumer/etc. config factories, INamingConventions, IExclusiveLock, IBusClient, IResourceDisposer, IInstanceFactory, IPipeContextFactory, IExtendedPipeBuilder, IPipeBuilderFactory). Honors `options?.Plugins?.Invoke(clientBuilder)` and `options?.DependencyInjection?.Invoke(register)` callbacks before final return. **DO NOT enumerate each `.AddSingleton<>()` call as test assertion** — that's testing-the-spec; test the extension's behavior shape | Per Explore agent enumeration of `src/RawRabbit/DependencyInjection/RawRabbitDependencyRegisterExtension.cs:23-93` |
| VP-15 | Cat 2 (signature) | `BusClient(IPipeBuilderFactory pipeBuilderFactory, IPipeContextFactory contextFactory, IChannelFactory factory)` ctor — third arg `factory` accepted but NOT stored to a field (dead arg per DW13). `InvokeAsync(Action<IPipeBuilder> pipeCfg, Action<IPipeContext> contextCfg = null, CancellationToken token = default(CancellationToken))` returns `Task<IPipeContext>`; calls `_pipeBuilderFactory.Create(pipeCfg)` then `_contextFactory.CreateContext()` then `contextCfg?.Invoke(context)` then `await pipe.InvokeAsync(context, token)` then `return context` | Direct read of `src/RawRabbit/BusClient.cs:9-28` confirmed at A14 |
| VP-16 | Cat 2 (signature) | `TopologyProvider(IChannelFactory channelFactory)` ctor + co-defines `ITopologyProvider` interface inline. 7 public methods: `Task DeclareExchangeAsync(ExchangeDeclaration)`, `Task DeclareQueueAsync(QueueDeclaration)`, `Task BindQueueAsync(string, string, string, IDictionary<string,object>)`, `Task UnbindQueueAsync(...)` same shape, `bool IsDeclared(ExchangeDeclaration)`, `bool IsDeclared(QueueDeclaration)`, `void Dispose()`. Internal `_channel = _channelFactory.CreateChannelAsync().GetAwaiter().GetResult()` lazy via `private IModel GetOrCreateChannel()` at line 308 | Per Explore agent enumeration of `src/RawRabbit/Common/TopologyProvider.cs:15-372` |
| VP-17 | Cat 3 (build/test commands) | Inherited from Wave 2 VP-44 through VP-48: `dotnet build -c Release` (0 errors expected); `dotnet test test/RawRabbit.Tests --no-build -c Release [--filter "FullyQualifiedName~<class-or-namespace>"]`; lowercase imperative commit messages (no Conventional-Commits prefix); `.editorconfig` requires tab indent for `*.cs`; no active pre-commit hooks | Inherited from Wave 2 plan; same shape used throughout Wave 1+2 commits |
| VP-18 | Cat 4 (task ordering) | Task 1 introduces `BrokerMocks.cs` helper. Tasks 2 (Channel), 3 (Consumer), 8 (TopologyProvider) consume it via `using RawRabbit.Tests.TestHelpers;`. Tasks 4 (Subscription), 5 (Instantiation), 6 (DI core), 7 (BusClient root) do NOT consume `BrokerMocks` (use direct `Mock<T>()` for non-broker mocks). **Task 1 must complete before 2, 3, 8.** Tasks 4-7 may run in any order after Task 1. Task 9 (verification) depends on Tasks 1-8. Within Tasks 2-8, write paths are disjoint (each task only touches its own subdir + Task 2 modifies 3 existing files in `Channel/`) | By inspection of File Structure section + design analysis of which tasks need broker chain |
| VP-19 | Cat 5 (code-in-plan validity) | Inherited Wave 1+2 conventions per spec §3: BOM preservation; AAA-comment full-deletion (no double-blanks); `Assert.ThrowsAnyAsync<T>` over `ThrowsAsync<T>` for `OperationCanceledException`-subclass cases; guarded BOM-restore loop; `[Collection("<Name>")]` for static-state-mutating classes (none expected per A11); specific exception types over `Assert.ThrowsAny<Exception>`; `Resolve<IInstanceFactory>` not `<IBusClient>` in DI happy-paths (but Wave 3 DI core tests don't `.Resolve(...)` IBusClient — see VP-14) | Per spec §3 + Wave 2 lessons captured at DW8-DW16 |
| VP-20 | Cat 5 (code-in-plan validity) — KEY RISK | `Mock<IModel>` + `Mock<IConnection>` + `Mock<IConnectionFactory>` work cleanly with Moq 4.20.72 against RMQ.Client 5.0.1 IF the production call signature is mocked correctly (per VP-6: `CreateConnection` 2-arg). Existing 7 skipped tests show some `IModel` methods may still surface Moq+net10 issues (see `ChannelPoolTests.cs` skip messages: "crashes test host on net10 / Moq 4.20", "fails on net10 / Moq 4.20", "hangs on net10 / Moq 4.20" — all 3 in the channel-pool exhaustion path). Plan does NOT pre-specify which Wave 3 tests will need new skips; implementer marks `[Fact(Skip = "Phase 5/7 territory: <signature>")]` per parent R2 / spec DW9 as encountered. Expected Wave 3 new skip count N=0-5 per spec gate 7 | Inferred from existing 7 skips + production call signature analysis at VP-6; not pre-pinned |
| VP-21 | Cat 6 (consumer impact) | Modifying 3 existing Channel test files (`ChannelFactoryTests.cs`, `ChannelPoolTests.cs`, `DynamicChannelPoolTests.cs`) by APPENDING new `[Fact]` test methods does NOT break the existing tests. xUnit allows multiple `[Fact]` methods per class; existing methods (skipped + non-skipped) remain at their current line ranges; new methods append after the last existing method. Convention: keep the file's existing `using` declarations (don't remove) and add any new `using` statements (e.g., `using RawRabbit.Tests.TestHelpers;` for `BrokerMocks`) at the top alphabetically | Existing test class structure read of `test/RawRabbit.Tests/Channel/ChannelPoolTests.cs:1-15` confirms standard xUnit `public class <Name>` shape with `[Fact]` methods; xUnit class-level structure supports unlimited [Fact] methods |

---

## Tasks

### Task 1: Pipe/Middleware (27 concrete) + PipeContextFactory + BrokerMocks helper

**Files:**
- Create: `test/RawRabbit.Tests/TestHelpers/BrokerMocks.cs` (helper, not a test class)
- Create: 27 test files at `test/RawRabbit.Tests/Pipe/Middleware/<Name>MiddlewareTests.cs` (one per concrete middleware per VP-4)
- Create: `test/RawRabbit.Tests/Pipe/PipeContextFactoryTests.cs`

Largest task by file count (29 new files). Establishes `BrokerMocks` pattern that Tasks 2/3/8 consume. Per F1=(a) strict: each public method gets ≥1 happy + ≥1 error test. Per spec §1 area 1: per-middleware ≥1 happy + ≥1 error per `InvokeAsync` + cancellation/state-observation where applicable (~3-5 tests per middleware × 27 = ~80-140 tests for middleware alone).

- [ ] **Step 1: Create directories.**

  ```bash
  mkdir -p test/RawRabbit.Tests/TestHelpers test/RawRabbit.Tests/Pipe/Middleware
  ```

- [ ] **Step 2: Write `TestHelpers/BrokerMocks.cs`.** Static helper class with factory methods for the 4 most-used Mock chains. Pin the 2-arg `CreateConnection` signature per VP-6 — if the helper uses `It.IsAny<List<string>>()` (1-arg) the production `ChannelFactory.cs:34` call site WON'T be intercepted and Wave 3 tests inherit the same problem the 7 existing skipped tests have.

  ```csharp
  using System.Collections.Generic;
  using Moq;
  using RabbitMQ.Client;
  using RabbitMQ.Client.Events;

  namespace RawRabbit.Tests.TestHelpers
  {
  	internal static class BrokerMocks
  	{
  		public static (Mock<IConnectionFactory> factory, Mock<IConnection> connection, Mock<IModel> channel) MakeConnectionChain()
  		{
  			var channel = new Mock<IModel>();
  			channel.Setup(c => c.IsOpen).Returns(true);
  			channel.Setup(c => c.IsClosed).Returns(false);

  			var connection = new Mock<IConnection>();
  			connection.Setup(c => c.IsOpen).Returns(true);
  			connection.Setup(c => c.CreateModel()).Returns(channel.Object);

  			var factory = new Mock<IConnectionFactory>();
  			// 2-arg signature per VP-6 — matches production ChannelFactory.cs:34
  			factory.Setup(f => f.CreateConnection(It.IsAny<IList<string>>(), It.IsAny<string>()))
  				.Returns(connection.Object);

  			return (factory, connection, channel);
  		}

  		public static Mock<IModel> MakeChannel()
  		{
  			var channel = new Mock<IModel>();
  			channel.Setup(c => c.IsOpen).Returns(true);
  			channel.Setup(c => c.IsClosed).Returns(false);
  			return channel;
  		}

  		public static Mock<IBasicConsumer> MakeBasicConsumer(IModel channel = null)
  		{
  			var consumer = new Mock<IBasicConsumer>();
  			consumer.Setup(c => c.Model).Returns(channel ?? MakeChannel().Object);
  			return consumer;
  		}

  		public static EventingBasicConsumer MakeEventingConsumer(IModel channel = null)
  		{
  			// EventingBasicConsumer is concrete — used directly for cast-dependent tests in ConsumerExtensions
  			return new EventingBasicConsumer(channel ?? MakeChannel().Object);
  		}
  	}
  }
  ```

  Note: `internal static` — Wave 3 tests live in the `RawRabbit.Tests` assembly; helper doesn't need cross-assembly visibility.

- [ ] **Step 3: Write 27 middleware test files in `Pipe/Middleware/`.** One file per concrete middleware. Use the per-middleware ctor + `InvokeAsync` pattern documented in **File Structure** section above. For each middleware, write at minimum:
  - 1 happy-path test: instantiate middleware, give it `Mock<IPipeContext>` (and `BrokerMocks.MakeChannel()` for broker-touching), call `InvokeAsync(ctx, CancellationToken.None)`, assert observable mutation (context value set, mock method invoked, etc.)
  - 1 error-path test: trigger a controlled failure (missing required context state, null dependency where ctor permits null options) and assert specific exception type (NOT `Assert.ThrowsAny<Exception>` — per spec §3 convention)
  - Cancellation test where middleware honors token: pre-cancel a `CancellationTokenSource`, pass to `InvokeAsync`, assert `Assert.ThrowsAnyAsync<OperationCanceledException>` per Wave 1 lesson

  **Broker-touching middleware (use `BrokerMocks.MakeChannel()`):** BasicPublishMiddleware, ChannelCreationMiddleware, ExchangeDeleteMiddleware, ExplicitAckMiddleware, PooledChannelMiddleware, QueueDeleteMiddleware, TransientChannelMiddleware (7 of 27).

  **Non-broker middleware (mock `IPipeContext` only):** the other 20.

  **Special middleware:**
  - `CancellationMiddleware` + `NoOpMiddleware` — simplest; ~1-2 tests each (parameterless ctor, InvokeAsync no-op or cancellation-check)
  - `HeaderDeserializationMiddleware`/`HeaderSerializationMiddleware` inherit `StagedMiddleware` not `Middleware` directly; also test `StageMarker` property returns expected stage (`MessageReceived` / `BasicPropertiesCreated`)
  - `ExplicitAckMiddleware` is heaviest — ctor takes 4 deps; `InvokeAsync` subscribes to consumer Received event then dispatches BasicAck/Nack/Reject based on handler result; aim ~5-7 tests

  Per F1=(a), each middleware's `InvokeAsync` is ONE public method; ≥1 happy + ≥1 error per middleware = 54 baseline (27 × 2). Realistic with state-observation + cancellation = 80-140.

- [ ] **Step 4: Write `Pipe/PipeContextFactoryTests.cs`.** 4-5 tests per VP-5:
  - `Should_Create_Context_With_Empty_Properties_When_No_Additional` — assert `ctx.Properties` not null + contains exactly 1 key (`PipeKey.ClientConfiguration`)
  - `Should_Seed_Properties_With_Additional_KeyValuePairs` — pass 2-3 KVPs; assert all present
  - `Should_Set_ClientConfiguration_Key_To_Ctor_Config` — assert `ctx.Properties[PipeKey.ClientConfiguration]` equals the ctor-passed config
  - `Should_Throw_NullReferenceException_When_Config_Is_Null_And_CreateContext_Called` — pass null to ctor, call CreateContext, assert NRE on `_config` deref (or assert ctor accepts null silently and CreateContext stores null in ClientConfiguration key — verify exact production behavior at write-time)
  - `Should_Override_ClientConfiguration_Key_When_Additional_Provides_It` — pass `KeyValuePair(PipeKey.ClientConfiguration, otherConfig)`; assert stored value matches `_config` not `otherConfig` (the line-27 assignment runs AFTER seed)

- [ ] **Step 5: Restore BOMs on the 29 new files (guarded loop per spec §3).**

  ```bash
  for f in test/RawRabbit.Tests/TestHelpers/*.cs test/RawRabbit.Tests/Pipe/Middleware/*.cs test/RawRabbit.Tests/Pipe/PipeContextFactoryTests.cs; do
    if ! head -c 3 "$f" | od -An -tx1 | grep -q "ef bb bf"; then
      printf '\xef\xbb\xbf' | cat - "$f" > /tmp/_bom && mv /tmp/_bom "$f"
    fi
    file "$f"
  done
  ```
  Each line should end with "Unicode text, UTF-8 (with BOM) text". Do NOT run the loop twice without the guard — Wave 2 commit `947b8ef` had to clean up duplicate BOMs from a bare unguarded loop.

- [ ] **Step 6: Build + run targeted tests.**

  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Tests --no-build -c Release --filter "FullyQualifiedName~RawRabbit.Tests.Pipe"
  ```

  Expected: 0 build errors. Targeted test count: ~84-145 passed (27 middleware × 3-5 + PipeContextFactory 4-5 + the 7 Wave 2 Pipe test files at their existing counts); 0 failed; 0-N skipped (any new skips per VP-20 must use `[Fact(Skip = "Phase 5/7 territory: <signature>")]` per spec DW9).

- [ ] **Step 7: Commit.**

  ```bash
  git add test/RawRabbit.Tests/TestHelpers test/RawRabbit.Tests/Pipe/Middleware test/RawRabbit.Tests/Pipe/PipeContextFactoryTests.cs
  git commit -m "Add Wave 3 Pipe/Middleware tests + PipeContextFactory + BrokerMocks helper"
  ```

---

### Task 2: Channel + Channel/Abstraction tests (gap-fill 3 existing + 4 new)

**Files:**
- Modify: `test/RawRabbit.Tests/Channel/ChannelFactoryTests.cs` — append new `[Fact]` methods alongside existing 4 skipped tests
- Modify: `test/RawRabbit.Tests/Channel/ChannelPoolTests.cs` — append new `[Fact]` methods alongside existing 3 skipped tests
- Modify: `test/RawRabbit.Tests/Channel/DynamicChannelPoolTests.cs` — append new `[Fact]` methods
- Create: `test/RawRabbit.Tests/Channel/AutoScalingChannelPoolTests.cs`
- Create: `test/RawRabbit.Tests/Channel/AutoScalingChannelPoolFactoryTests.cs`
- Create: `test/RawRabbit.Tests/Channel/ConcurrentChannelQueueTests.cs`
- Create: `test/RawRabbit.Tests/Channel/ResilientChannelPoolTests.cs`

Gap-fill task. Channel ctor patterns and Mock chain via `BrokerMocks.MakeConnectionChain()` (per VP-6). The 7 existing skipped tests stay verbatim per parent D10.

- [ ] **Step 1: Append to `ChannelFactoryTests.cs`.** 3-5 new `[Fact]` methods after existing skipped tests:
  - `Should_Construct_Without_Connecting` — ctor takes mocks; assert no `CreateConnection` call (deferred to first `CreateChannelAsync`)
  - `Should_Open_Connection_On_First_CreateChannelAsync` — `var (factory, conn, channel) = BrokerMocks.MakeConnectionChain()`; construct `new ChannelFactory(factory.Object, RawRabbitConfiguration.Local)`; await `CreateChannelAsync()`; assert `Verify(f => f.CreateConnection(It.IsAny<IList<string>>(), It.IsAny<string>()), Times.Once())`
  - `Should_Reuse_Existing_Connection_On_Subsequent_CreateChannelAsync` — call twice; assert connection.CreateModel called twice but factory.CreateConnection called once
  - `Should_Dispose_Connection_On_Dispose` — Dispose; assert connection.Dispose called

- [ ] **Step 2: Append to `ChannelPoolTests.cs`.** 3-5 new `[Fact]` methods. Note: these are pool-tests (currently target `StaticChannelPool` per the existing class name); some skipped tests reference `IRecoverable` casts. Add tests for non-skipped paths:
  - `Should_Construct_From_Seed_IModels` — pass `new[] { BrokerMocks.MakeChannel().Object, BrokerMocks.MakeChannel().Object }`; assert state
  - `Should_Get_Channel_From_Pool` — happy non-skipped path
  - `Should_Dispose_All_Channels_On_Dispose` — assert each channel.Dispose called

- [ ] **Step 3: Append to `DynamicChannelPoolTests.cs`.** 3-5 new `[Fact]` methods for `DynamicChannelPool`-specific behavior:
  - `Should_Construct_With_Empty_Pool_When_Parameterless_Ctor` — assert empty
  - `Should_Add_Channels_Via_Params_Array` — `pool.Add(BrokerMocks.MakeChannel().Object, BrokerMocks.MakeChannel().Object)`; assert count++
  - `Should_Add_Channels_Via_IEnumerable` — same shape
  - `Should_Remove_N_Channels_Via_Int` — `pool.Remove(2)`; assert count--; assert disposed channels not in pool
  - `Should_Remove_Specific_Channels_Via_Params_Array` — `pool.Remove(specificChannel)`; assert removed

- [ ] **Step 4: Write `AutoScalingChannelPoolTests.cs`.** 4-6 tests per VP-9:
  - Ctor `(IChannelFactory, AutoScalingOptions)` happy with valid options + null factory throws
  - `GetAsync` happy when pool above min size
  - `SetupScaling` schedules timer (verify via `Mock<IChannelFactory>.Verify(...)` after a short delay or via reflection-checking the timer field — pick the simpler path)
  - `Dispose` cleanup happy

- [ ] **Step 5: Write `AutoScalingChannelPoolFactoryTests.cs`.** 4-6 tests:
  - Ctor `(IChannelFactory, AutoScalingOptions=null)` — null options uses `AutoScalingOptions.Default`
  - `GetChannelPool(name=null)` returns same pool on repeat call (caching)
  - `GetChannelPool("named")` returns separate pool per name
  - `Dispose` cleanup happy

- [ ] **Step 6: Write `ConcurrentChannelQueueTests.cs`.** 5-7 tests:
  - Parameterless ctor → empty
  - `Enqueue` returns non-null `TaskCompletionSource<IModel>` + `Queued` event fires
  - `TryDequeue` returns true + sets out param when not empty
  - `TryDequeue` returns false + null out param when empty
  - `IsEmpty` true initially, false after Enqueue, true after dequeue-all
  - `Count` reflects pending enqueues

- [ ] **Step 7: Write `ResilientChannelPoolTests.cs`.** 5-7 tests:
  - 3 ctor overloads: `(IChannelFactory, int)` — assert N channels seeded; `(IChannelFactory)` — empty seed; `(IChannelFactory, IEnumerable<IModel>)` — explicit seed
  - `GetAsync` happy when pool has open channel
  - `GetAsync` recreates channel via factory when pool exhausted (the resilient behavior)

- [ ] **Step 8: Restore BOMs on the 4 new files (guarded loop).** Skip the modified files unless their BOM was stripped (check first).

  ```bash
  for f in test/RawRabbit.Tests/Channel/{AutoScalingChannelPool,AutoScalingChannelPoolFactory,ConcurrentChannelQueue,ResilientChannelPool}Tests.cs; do
    if ! head -c 3 "$f" | od -An -tx1 | grep -q "ef bb bf"; then
      printf '\xef\xbb\xbf' | cat - "$f" > /tmp/_bom && mv /tmp/_bom "$f"
    fi
  done
  ```

- [ ] **Step 9: Build + run targeted tests.**

  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Tests --no-build -c Release --filter "FullyQualifiedName~RawRabbit.Tests.Channel"
  ```

  Expected: 0 build errors. Targeted count: existing 3 files post-modification + 4 new files = ~25-45 passed; 7 skipped (existing Phase 5/7); 0-N new skipped per VP-20.

- [ ] **Step 10: Commit.**

  ```bash
  git add test/RawRabbit.Tests/Channel
  git commit -m "Add Wave 3 Channel gap-fill tests (4 new files + extend 3 existing)"
  ```

---

### Task 3: Consumer tests

**Files:**
- Create: `test/RawRabbit.Tests/Consumer/ConsumerFactoryTests.cs`
- Create: `test/RawRabbit.Tests/Consumer/ConsumerExtensionsTests.cs`

- [ ] **Step 1: Create directory.**

  ```bash
  mkdir -p test/RawRabbit.Tests/Consumer
  ```

- [ ] **Step 2: Write `ConsumerFactoryTests.cs`.** ~8-10 tests per VP-10. ConsumerFactory has 4 public methods + caching behavior:
  - Ctor `(IChannelFactory)` happy
  - `GetConsumerAsync(cfg)` returns consumer + caches by `<queue>:<routingKey>:<autoAck>` key
  - `GetConsumerAsync(cfg, channel)` uses provided channel
  - `GetConfiguredConsumerAsync(cfg)` happy + retries when cached consumer's Model.IsClosed (per source line 51-55)
  - `CreateConsumerAsync(channel)` returns `EventingBasicConsumer` wrapping channel
  - `CreateConsumerAsync(null)` — gets channel via factory (mock `IChannelFactory.CreateChannelAsync` to return `BrokerMocks.MakeChannel().Object`)
  - `ConfigureConsume(consumer, cfg)` happy — calls `consumer.Model.BasicQos` + `BasicConsume`
  - `ConfigureConsume(consumer, null)` throws `ArgumentException("Unable to create consumer. The provided configuration is null")`
  - `ConfigureConsume(consumer, cfg-with-empty-queue)` throws `ArgumentException("Unable to create consume. No queue name provided.")`
  - `ConfigureConsume(consumer, cfg-with-empty-tag)` throws `ArgumentException("Unable to create consume. Consumer tag cannot be undefined.")`

- [ ] **Step 3: Write `ConsumerExtensionsTests.cs`.** ~6-8 tests per VP-10:
  - `CancelAsync(EventingBasicConsumer)` happy — invokes `consumer.Model.BasicCancel(tag)`; verify via `BrokerMocks.MakeChannel().Verify(c => c.BasicCancel(tag), Times.Once)`
  - `CancelAsync(non-Eventing-consumer)` throws `NotSupportedException("Can only cancellation EventBasicConsumer")` (note source typo preserved)
  - `CancelAsync` returns task that completes when `ConsumerCancelled` event fires for matching tag
  - `CancelAsync` cancels TCS when token cancelled before consumer cancel (use `CancellationTokenSource` pre-cancelled)
  - `OnMessage(EventingBasicConsumer, handler)` happy — handler invoked when `Received` event fires (use `EventingBasicConsumer` directly via `BrokerMocks.MakeEventingConsumer()` and `consumer.HandleBasicDeliver(...)` to fire event)
  - `OnMessage(non-Eventing-consumer, ...)` throws `NotSupportedException("Only supported for EventBasicConsumer")`
  - `OnMessage(consumer, handler, abort=null)` happy without abort
  - `OnMessage(consumer, handler, abort)` — abort predicate true → both handlers detached

- [ ] **Step 4: Restore BOMs on the 2 new files (guarded loop).**

- [ ] **Step 5: Build + run targeted tests.**

  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Tests --no-build -c Release --filter "FullyQualifiedName~RawRabbit.Tests.Consumer"
  ```

  Expected: ~14-18 passed; 0 failed; 0-N new skipped per VP-20.

- [ ] **Step 6: Commit.**

  ```bash
  git add test/RawRabbit.Tests/Consumer
  git commit -m "Add Wave 3 Consumer tests"
  ```

---

### Task 4: Subscription tests

**Files:**
- Create: `test/RawRabbit.Tests/Subscription/SubscriptionTests.cs`
- Create: `test/RawRabbit.Tests/Subscription/SubscriptionRepositoryTests.cs`

No `BrokerMocks` needed — these classes touch `IBasicConsumer` indirectly via Subscription.cs ctor.

- [ ] **Step 1: Create directory.**

  ```bash
  mkdir -p test/RawRabbit.Tests/Subscription
  ```

- [ ] **Step 2: Write `SubscriptionTests.cs`.** ~6-8 tests per VP-11. **Per CIR R1 F2:** Dispose tests MUST use real `EventingBasicConsumer` (via `BrokerMocks.MakeEventingConsumer()`); `Mock<IBasicConsumer>` only for the non-DefaultBasicConsumer ctor branch.
  - Ctor `(IBasicConsumer, string queueName)` with `EventingBasicConsumer` — `var channel = BrokerMocks.MakeChannel(); var consumer = BrokerMocks.MakeEventingConsumer(channel.Object); consumer.HandleBasicConsumeOk("tag1");` then `var sub = new Subscription(consumer, "queue1");` — assert `Active=true`, `QueueName="queue1"`, `ConsumerTag="tag1"`
  - Ctor with non-`DefaultBasicConsumer` — pass `Mock<IBasicConsumer>().Object`; cast-to-DefaultBasicConsumer at source line 26 returns null for non-Default consumers; ctor early-returns. Assert `Active=true`, `QueueName=null`, `ConsumerTag=null`
  - `Dispose()` happy — construct with `EventingBasicConsumer + BrokerMocks.MakeChannel()` (channel.IsOpen defaults true via helper), call `sub.Dispose();` — assert `channel.Verify(c => c.BasicCancel("tag1"), Times.Once)` AND `sub.Active` is now false. Verify via the underlying `IModel.BasicCancel` call (the extension method `_consumer.CancelAsync()` casts to `EventingBasicConsumer` and calls `consumer.Model.BasicCancel(consumerTag)`; Moq cannot verify the extension method itself, only the underlying `IModel` call)
  - `Dispose()` skips when `Model.IsOpen=false` — `channel.Setup(c => c.IsOpen).Returns(false);` then construct + Dispose; assert `channel.Verify(c => c.BasicCancel(It.IsAny<string>()), Times.Never)`
  - `Dispose()` idempotency — construct + Dispose twice; assert `BasicCancel` invoked exactly once (second call early-returns because `Active=false` after first Dispose)
  - `Active` setter — set false then true; verify state

- [ ] **Step 3: Write `SubscriptionRepositoryTests.cs`.** ~4-5 tests per VP-11:
  - Parameterless ctor — assert empty `GetAll()` returns empty list
  - `Add(ISubscription)` happy — Add 1 mock subscription; assert GetAll returns list of 1
  - `GetAll()` returns ToList'd snapshot (independent of bag mutations)
  - Concurrent `Add` safety — `Parallel.For(0, 100, _ => repo.Add(Mock.Of<ISubscription>()))`; assert `GetAll().Count == 100`

- [ ] **Step 4: Restore BOMs on the 2 new files (guarded loop).**

- [ ] **Step 5: Build + run targeted tests.**

  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Tests --no-build -c Release --filter "FullyQualifiedName~RawRabbit.Tests.Subscription"
  ```

  Expected: ~10-13 passed; 0 failed.

- [ ] **Step 6: Commit.**

  ```bash
  git add test/RawRabbit.Tests/Subscription
  git commit -m "Add Wave 3 Subscription tests"
  ```

---

### Task 5: Instantiation + Disposable tests

**Files:**
- Create: `test/RawRabbit.Tests/Instantiation/ClientBuilderTests.cs`
- Create: `test/RawRabbit.Tests/Instantiation/InstanceFactoryTests.cs`
- Create: `test/RawRabbit.Tests/Instantiation/RawRabbitFactoryTests.cs`
- Create: `test/RawRabbit.Tests/Instantiation/Disposable/BusClientTests.cs`

Per DW15, `RawRabbitOptions` (pure POCO) gets NO dedicated test file. **Per spec §3 Wave 2 lesson: avoid `Resolve<IBusClient>` — use `Mock<IDependencyResolver>` chain that doesn't trigger broker connection.**

- [ ] **Step 1: Create directories.**

  ```bash
  mkdir -p test/RawRabbit.Tests/Instantiation/Disposable
  ```

- [ ] **Step 2: Write `ClientBuilderTests.cs`.** ~4-5 tests per VP-12:
  - Parameterless ctor sets `PipeBuilderAction` to no-op `builder => { }` and `DependencyInjection` to no-op — assert both not null + invoking does nothing
  - `Register(pipe, ioc)` accumulates via `+=` — call Register twice with different actions; verify both compose (e.g., increment counter on each invocation)

- [ ] **Step 3: Write `InstanceFactoryTests.cs`.** ~6-8 tests per VP-12:
  - Ctor `(IDependencyResolver)` happy — assert `_resolver` stored
  - `Create()` happy — `Mock<IDependencyResolver>` returns mocks for `IPipeBuilderFactory`, `IPipeContextFactory`, `IChannelFactory` — assert returns non-null `BusClient` (the root one, not Disposable)
  - `Create()` propagates resolver-throws when resolver fails to provide a dependency
  - `Dispose()` happy — resolves `IResourceDisposer` from resolver and calls `.Dispose()`; verify `Mock<IResourceDisposer>.Verify(d => d.Dispose())`
  - `Dispose()` no-op when resolver returns null `IResourceDisposer` — `null?.Dispose()` short-circuit
  - `ShutdownAsync(TimeSpan? graceful=null)` happy with explicit `TimeSpan.FromMilliseconds(10)` — disposes all subscriptions from `ISubscriptionRepository.GetAll()` + delays + Dispose
  - `ShutdownAsync(null)` uses `_resolver.GetService<RawRabbitConfiguration>().GracefulShutdown` — set config to small TimeSpan to keep test fast

- [ ] **Step 4: Write `RawRabbitFactoryTests.cs`.** ~4 tests per VP-12. **Critical: do NOT call `Resolve<IBusClient>()` / equivalent path that triggers broker connection.** Use `RawRabbitFactory.CreateInstanceFactory(...)` (returns `InstanceFactory` whose ctor stores resolver — no broker). DO NOT call `.Create()` on the returned factory in test asserts (that triggers broker via the resolved `BusClient` ctor chain — see Wave 2 lesson). **Both `CreateSingleton(...)` overloads omitted from this test file (per CIR R1 F1)**: they construct `Disposable.BusClient` whose ctor calls `instanceFactory.Create()` synchronously at construction time, which resolves `IChannelFactory` whose registration at `RawRabbitDependencyRegisterExtension.cs:57-66` invokes `ConnectAsync().GetAwaiter().GetResult()` — opening a real broker connection inside the test's `new Disposable.BusClient(...)` line. The `Disposable.BusClient` wrapper itself is covered in Step 5's `Disposable/BusClientTests.cs` against `Mock<IInstanceFactory>` (no broker chain):
  - `CreateInstanceFactory(options=null)` returns non-null `InstanceFactory`
  - `CreateInstanceFactory(options-with-Plugins)` invokes `options.Plugins?.Invoke(clientBuilder)` — verify via stub Action that increments counter
  - `CreateInstanceFactory(options-with-DependencyInjection)` invokes `options.DependencyInjection?.Invoke(register)` — verify via stub Action
  - `CreateInstanceFactory(options, register, resolverFunc)` honors custom register — pass mock register; verify .AddRawRabbit was invoked

- [ ] **Step 5: Write `Disposable/BusClientTests.cs`.** ~4-5 tests per VP-12:
  - Ctor `(IInstanceFactory)` — `Mock<IInstanceFactory>.Setup(f => f.Create()).Returns(Mock<IBusClient>.Object)` — verify ctor calls `factory.Create()` once
  - `InvokeAsync(pipeCfg, contextCfg, token)` delegates to inner `IBusClient.InvokeAsync` — verify with `Mock<IBusClient>.Verify(b => b.InvokeAsync(It.IsAny<...>(), It.IsAny<...>(), It.IsAny<...>()))`
  - `Dispose()` casts factory to `IDisposable` and disposes when factory implements IDisposable — `Mock<IInstanceFactory>` that ALSO implements IDisposable via `Mock<IInstanceFactory>().As<IDisposable>().Setup(d => d.Dispose())`; verify .Dispose called
  - `Dispose()` no-op when factory is NOT IDisposable — pass plain `Mock<IInstanceFactory>` (no `.As<IDisposable>()`); verify no exception

- [ ] **Step 6: Restore BOMs on the 4 new files (guarded loop).**

- [ ] **Step 7: Build + run targeted tests.**

  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Tests --no-build -c Release --filter "FullyQualifiedName~RawRabbit.Tests.Instantiation"
  ```

  Expected: ~20-26 passed; 0 failed; 0 new skipped (no broker mocks at this layer).

- [ ] **Step 8: Commit.**

  ```bash
  git add test/RawRabbit.Tests/Instantiation
  git commit -m "Add Wave 3 Instantiation + Disposable tests"
  ```

---

### Task 6: DependencyInjection core tests

**Files:**
- Create: `test/RawRabbit.Tests/DependencyInjection/SimpleDependencyInjectionTests.cs`
- Create: `test/RawRabbit.Tests/DependencyInjection/RawRabbitDependencyRegisterExtensionTests.cs`

Pure logic for `SimpleDependencyInjection`. For `RawRabbitDependencyRegisterExtension`, test the extension's behavior shape — NOT each of the 24 internal `.AddSingleton<>()` calls (that's testing-the-spec).

- [ ] **Step 1: Write `SimpleDependencyInjectionTests.cs`.** ~16-22 tests per VP-13. F1=(a) strict per-method × happy/error:
  - `AddTransient<TS,TI>(Func)`: happy registers + factory invoked at Resolve + duplicate registration replaces existing (per source line 14-17)
  - `AddTransient<TS,TI>()`: happy registers + auto-resolves TI via internal `GetService(typeof(TI))` (per source line 24)
  - `AddSingleton<TS>(TS instance)`: happy + same instance returned on multiple Resolve calls
  - `AddSingleton<TS,TI>(Func)`: happy + Lazy<TI> only instantiates once even if Resolve called many times
  - `AddSingleton<TS,TI>()`: happy + Lazy<TI> only instantiates once
  - `GetService<TService>(params)`: happy returns registered + throws `InvalidOperationException("No registration for ...")` for unregistered abstract + auto-creates non-abstract via `CreateInstance` reflection helper (per source line 60-63)
  - `GetService(Type, params)`: happy + throws for unregistered abstract + auto-creates non-abstract
  - `TryGetService(Type, out service, params)`: returns true with service when registered + returns false with null for unregistered abstract + returns true with auto-created instance for non-abstract
  - Bonus: ctor with abstract type unregistered + non-abstract type with required ctor param that can't be resolved → exception (per source line 95: `throw new Exception($"Unable to find suitable constructor for {implementationType.Name}.")`)

- [ ] **Step 2: Write `RawRabbitDependencyRegisterExtensionTests.cs`.** ~6-10 tests per VP-14:
  - `AddRawRabbit(this IDependencyRegister)` returns the same `IDependencyRegister` (chaining contract) — pass `new SimpleDependencyInjection()`; assert returned ref-equal
  - `AddRawRabbit()` (no options) registers `RawRabbitConfiguration` → resolved value matches `RawRabbitConfiguration.Local` (Username="guest", Hostnames=["localhost"], etc.)
  - `AddRawRabbit(options-with-ClientConfiguration)` registers the custom config — resolve and assert custom values
  - `AddRawRabbit(options-with-Plugins)` invokes `options.Plugins.Invoke(clientBuilder)` — verify via stub Action that captures the IClientBuilder + checks non-null
  - `AddRawRabbit(options-with-DependencyInjection)` invokes `options.DependencyInjection.Invoke(register)` — verify via stub Action
  - `AddRawRabbit()` registers `IInstanceFactory` — resolve via `IDependencyResolver`; assert non-null. **DO NOT resolve `IBusClient`** (triggers broker connection per Wave 2 lesson).
  - `AddRawRabbit(null-this)` throws `ArgumentNullException` (extension method `this` validation; standard for `static` extensions on `null`)

- [ ] **Step 3: Restore BOMs on the 2 new files (guarded loop).**

- [ ] **Step 4: Build + run targeted tests.**

  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Tests --no-build -c Release --filter "FullyQualifiedName~RawRabbit.Tests.DependencyInjection.SimpleDependencyInjection|FullyQualifiedName~RawRabbit.Tests.DependencyInjection.RawRabbitDependencyRegisterExtension"
  ```

  Expected: ~22-32 new passed; 0 failed.

- [ ] **Step 5: Commit.**

  ```bash
  git add test/RawRabbit.Tests/DependencyInjection/SimpleDependencyInjectionTests.cs test/RawRabbit.Tests/DependencyInjection/RawRabbitDependencyRegisterExtensionTests.cs
  git commit -m "Add Wave 3 DependencyInjection core tests"
  ```

---

### Task 7: Root BusClient tests

**Files:**
- Create: `test/RawRabbit.Tests/BusClientTests.cs` (root-level file; mirrors `src/RawRabbit/BusClient.cs` location per DW12)

- [ ] **Step 1: Write `BusClientTests.cs`.** ~3-5 tests per VP-15. Mock `IPipeBuilderFactory.Create()` + `IPipeContextFactory.CreateContext()` + `IChannelFactory` (the dead arg per DW13):
  - `Should_Construct_With_Three_Dependencies` — pass 3 mocks; assert no exception
  - `Should_Construct_With_Null_ChannelFactory` — pass null for the third arg (DW13: dead-arg, stored-but-unused); assert no exception (the source ctor doesn't validate or store it)
  - `Should_InvokeAsync_Build_Pipe_From_Factory_And_Invoke_With_Context` — mock `IPipeBuilderFactory.Create(pipeCfg)` returns `Mock<IPipe>`; mock `IPipeContextFactory.CreateContext()` returns `Mock<IPipeContext>`; verify `IPipe.InvokeAsync(ctx, token)` called once + returned task contains the context
  - `Should_InvokeAsync_Honor_Null_ContextCfg` — pass `contextCfg=null`; assert no exception (source uses `?.Invoke`)
  - `Should_InvokeAsync_Propagate_CancellationToken` — pass cancelled `CancellationTokenSource.Token`; verify token reaches `IPipe.InvokeAsync` mock

- [ ] **Step 2: Restore BOM on the new file (guarded).**

- [ ] **Step 3: Build + run targeted tests.**

  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Tests --no-build -c Release --filter "FullyQualifiedName=RawRabbit.Tests.BusClientTests"
  ```

  Expected: ~3-5 passed; 0 failed.

- [ ] **Step 4: Commit.**

  ```bash
  git add test/RawRabbit.Tests/BusClientTests.cs
  git commit -m "Add Wave 3 root BusClient tests"
  ```

---

### Task 8: TopologyProvider tests

**Files:**
- Create: `test/RawRabbit.Tests/Common/TopologyProviderTests.cs`

Highest broker-mock surface in Wave 3. Use `BrokerMocks.MakeConnectionChain()` + `IChannelFactory` mock. **Most likely place for new Phase 5/7 skips per gate 7's N=0-5 budget — implementer marks skips as encountered.**

- [ ] **Step 1: Write `TopologyProviderTests.cs`.** ~14-20 tests per VP-16. 7 public methods × ≥1 happy + ≥1 error per F1=(a):
  - Ctor `(IChannelFactory)` happy
  - `DeclareExchangeAsync(ExchangeDeclaration)` calls `IModel.ExchangeDeclare(exchange.Name, exchange.ExchangeType, exchange.Durable, exchange.AutoDelete, exchange.Arguments)` (verify via Mock<IModel>.Verify)
  - `DeclareExchangeAsync` is idempotent — second call same exchange does NOT re-call ExchangeDeclare (per IsDeclared cache)
  - `DeclareQueueAsync(QueueDeclaration)` calls `IModel.QueueDeclare(queue.Name, queue.Durable, queue.Exclusive, queue.AutoDelete, queue.Arguments)` (verify)
  - `DeclareQueueAsync` idempotency
  - `BindQueueAsync(queue, exchange, routingKey, args)` calls `IModel.QueueBind(queue, exchange, routingKey, args)`
  - `UnbindQueueAsync(queue, exchange, routingKey, args)` calls `IModel.QueueUnbind(...)`
  - `IsDeclared(ExchangeDeclaration)` returns false for new + true after `DeclareExchangeAsync`
  - `IsDeclared(QueueDeclaration)` returns false for new + true after `DeclareQueueAsync`
  - `Dispose()` happy

  Setup pattern:
  ```csharp
  var (factory, conn, channel) = BrokerMocks.MakeConnectionChain();
  var channelFactory = new Mock<IChannelFactory>();
  channelFactory.Setup(f => f.CreateChannelAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(channel.Object);
  var topology = new TopologyProvider(channelFactory.Object);
  ```

  **If any test surfaces Moq+net10 issues (signature mismatches, hangs, crashes per existing skip pattern in `ChannelPoolTests.cs`):** mark with `[Fact(Skip = "Phase 5/7 territory: <specific signature/symptom>")]` and continue. Do NOT block the wave on broker-mock surface issues; implementer count of new skips lands within gate 7 budget N=0-5.

- [ ] **Step 2: Restore BOM on the new file (guarded).**

- [ ] **Step 3: Build + run targeted tests.**

  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Tests --no-build -c Release --filter "FullyQualifiedName=RawRabbit.Tests.Common.TopologyProviderTests"
  ```

  Expected: ~14-20 passed; 0 failed; 0-N new skipped (gate 7 N=0-5 budget).

- [ ] **Step 4: Commit.**

  ```bash
  git add test/RawRabbit.Tests/Common/TopologyProviderTests.cs
  git commit -m "Add Wave 3 TopologyProvider tests"
  ```

---

### Task 9: Wave 3 phase-acceptance verification

**Files:** none modified. Read-only.

Verify each spec §4 acceptance gate at the wave's final commit. Final code review pass after this task is the same shape as Wave 2's pre-push review.

- [ ] **Step 1: Aggregate test count gate (gate 1).**

  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Tests --no-build -c Release 2>&1 | tail -3
  ```

  Expected: `Failed: 0, Passed: ≥529, Skipped: 7+N` where N = Wave-3-newly-blocked broker-mock signature tests (0 ≤ N ≤ 5 per gate 7).

  Wave-wide gate: at least 150 net-new passing tests added vs post-Wave-2 baseline (379). So `≥529` total passed. Range observed: 179-311 informational.

- [ ] **Step 2: ServiceCollection.Tests unchanged (gate 5).**

  ```bash
  dotnet test test/RawRabbit.DependencyInjection.ServiceCollection.Tests --no-build -c Release 2>&1 | tail -1
  ```

  Expected: `Failed: 0, Passed: 21, Skipped: 1` — exact same as post-Wave-2.

- [ ] **Step 3: Polly.Tests unchanged (per gate 7's aggregate inclusion of all 3 unit-test projects).**

  ```bash
  dotnet test test/RawRabbit.Enrichers.Polly.Tests --no-build -c Release 2>&1 | tail -1
  ```

  Expected: `Failed: 0, Passed: 1, Skipped: 2` — exact same as post-Wave-2.

- [ ] **Step 4: Build clean (gate 2).**

  ```bash
  dotnet build -c Release 2>&1 | grep -E "Error|Warning" | tail -10
  ```

  Expected: 0 errors. Warning shape stable from Wave 2 close (no new categories introduced by Wave 3 test code).

- [ ] **Step 5: xUnit analyzer warnings (gate 6).**

  ```bash
  dotnet build -c Release 2>&1 | grep -E "xUnit2020|xUnit2004|xUnit2007|xUnit1031" | grep -v "IntegrationTests" | tail -10
  ```

  Expected: zero matches in Wave 3 new files.

- [ ] **Step 6: Skipped count gate (gate 7).**

  ```bash
  echo "Skipped count breakdown:"
  echo "RawRabbit.Tests:" && dotnet test test/RawRabbit.Tests --no-build -c Release 2>&1 | grep "Skipped:"
  echo "ServiceCollection.Tests:" && dotnet test test/RawRabbit.DependencyInjection.ServiceCollection.Tests --no-build -c Release 2>&1 | grep "Skipped:"
  echo "Polly.Tests:" && dotnet test test/RawRabbit.Enrichers.Polly.Tests --no-build -c Release 2>&1 | grep "Skipped:"
  ```

  Expected: `(7 + N) in RawRabbit.Tests + 1 in ServiceCollection.Tests + 2 in Polly.Tests` where N = Wave-3-newly-blocked. Aggregate = `10 + N`. N ≤ 5.

- [ ] **Step 7: Verify `[Theory]` use exercised (gate 8).**

  ```bash
  grep -rn "\[Theory\]\|\[InlineData" test/RawRabbit.Tests/ | grep -v "/bin/\|/obj/" | wc -l
  ```

  Expected: ≥1 Wave 3 use (parent phase-wide ≥3 already satisfied by Waves 1-2).

- [ ] **Step 8: Verify BrokerMocks helper landed + consumed by ≥3 Wave 3 test classes (gate 10).**

  ```bash
  ls test/RawRabbit.Tests/TestHelpers/BrokerMocks.cs
  grep -rln "BrokerMocks" test/RawRabbit.Tests/ | grep -v "/bin/\|/obj/\|TestHelpers/BrokerMocks.cs" | wc -l
  ```

  Expected: helper file exists; consumer count ≥3 (likely 4: Task 1 channel-touching middleware + Task 2 + Task 3 + Task 8).

- [ ] **Step 9: Verify convention adherence (gate 9) — sample BOM check + specific exception types + no `[Collection]` overlooked.**

  ```bash
  echo "BOM check on Wave 3 new files (sample):"
  for f in test/RawRabbit.Tests/TestHelpers/BrokerMocks.cs test/RawRabbit.Tests/Pipe/Middleware/BasicPropertiesMiddlewareTests.cs test/RawRabbit.Tests/Channel/AutoScalingChannelPoolTests.cs test/RawRabbit.Tests/BusClientTests.cs test/RawRabbit.Tests/Common/TopologyProviderTests.cs; do
    file "$f"
  done
  echo "ThrowsAny<Exception> incidence (should be 0 in Wave 3 files):"
  grep -rn "ThrowsAny<Exception>" test/RawRabbit.Tests/ | grep -v "/bin/\|/obj/" | wc -l
  echo "[Collection] usage (should preserve Wave 2's 3 uses; new ones only if static state mutated):"
  grep -rln "\[Collection" test/RawRabbit.Tests/ | grep -v "/bin/\|/obj/"
  ```

  Expected: each Wave 3 file shows "Unicode text, UTF-8 (with BOM) text"; `ThrowsAny<Exception>` count = 0; `[Collection]` only in 3 Wave 2 files (LogProviderTests, LogExtensionsTests, ExclusiveLockTests) unless plan-time enumeration surfaced new mutable statics.

- [ ] **Step 10: Verify no source-code edits (parent D10-style).**

  ```bash
  git diff origin/2.0..HEAD --stat -- src/ | head
  ```

  Expected: empty output. Wave 3 commits should only touch `test/` paths.

- [ ] **Step 11: Final per-area test method count summary (no additional commands; aggregate from Tasks 1-8 grep counts).** Document the per-area landed counts vs spec §2 estimate ranges. If any area landed below its lower-bound estimate, document why in the final code review.

- [ ] **Step 12: NO COMMIT.** Task 9 is verification-only. If any gate fails, fix the relevant Task and re-run Task 9.

---

## Tasks NOT in this plan

Inherited from spec §8 — preserve verbatim:

- **Per-area exact test method names beyond what the plan locked above** — derived during SDD execution per-task verification (one enumeration per source class)
- **Plan-time decision: which middleware classes have multiple `InvokeAsync` paths warranting more than 2-3 tests each** — implementer reads each middleware's branch structure during SDD and adjusts test count within the per-task estimate range
- **Plan-time decision: which middleware need `Mock<IModel>` via `BrokerMocks.MakeChannel()` vs only `Mock<IPipeContext>`** — see File Structure section above for the 7 broker-touching middleware identified at plan-write time; implementer may discover others
- **Plan-time decision: ConsumerFactory dispatch tests via `EventingBasicConsumer`** — implementer may surface a Wave-3-specific FD if Moq can't bind cast-result events; mark `[Fact(Skip = "Phase 5/7 territory: <signature>")]` per DW9
- **Plan-time decision: which `RawRabbitFactory` static overloads warrant separate tests vs `[Theory]` consolidation** — 4 statics; the 2 `(options=null)` overloads share shape with the 2 `(options, register, resolverFunc)` overloads; consolidation acceptable if shape clean
- **Wave 4 source areas:** `Operations.*` (8 projects)
- **Wave 5 source areas:** `Enrichers.*` (5 + Polly extension)
- **Phase 5 work:** un-skip the 7 pre-existing Phase-5/7-tagged tests in `Channel/` + 2 in `Polly.Tests/Services/` + 1 from Wave 2 (`ServiceProviderAdapterTests.Should_Self_Register_When_Constructed_From_Collection`) + N from Wave 3 (newly-blocked broker-mock signature tests, expected 0-5) when the broker layer is modernized
- **`PipeKey.cs` (28 const strings) + `StageMarker.cs` (9 const strings)** — pure constants per parent §3 convention; testing them would be tautology. NO test files in Wave 3 or any future wave (per DW16)
- **`IPipeContext.cs` interface** — partially covered by Wave 2 (`DictionaryExtensions` co-located inside it has its own test file). Interface-conformance test file not added per DW14 convention
- **Phase 7 items surfaced by Wave 3 (track separately):**
  - `BusClient.cs:14` dead `IChannelFactory factory` ctor parameter (per DW13) — Phase 7 may remove the arg
  - `IClientBuilder.Register(pipe, ioc = null)` interface declares a default that the impl class doesn't replicate (minor; per spec §7 A7 note) — Phase 7 cosmetic
  - Any other code-quality flags surfaced during SDD-time source enumeration

A new spec → new plan cycle is required to add any of the above to a future phase.

---

## Known issues inherited from spec

User-acknowledged on 2026-05-13 during the Wave 3 brainstorm — preserve verbatim:

1. **Per-method strict (F1=a) inherited from Wave 2.** Each public method on each broker-touching class gets ≥1 happy + ≥1 error test. Not re-litigated.
2. **Shared test-helper assembly (Wave 2's DW7) revisited as DW8** — per-project static class in `test/RawRabbit.Tests/TestHelpers/BrokerMocks.cs` only (NOT a separate `test/RawRabbit.TestHelpers/` shared csproj). Wave 4-5 may promote to shared csproj if their needs warrant it.
3. **Mock strategy = Moq + skip-when-blocked.** New `[Fact(Skip = "Phase 5/7 territory: <reason>")]` annotations permitted per parent R2; aggregate skipped count grows by N≥0.
4. **`BusClient.cs` dead `IChannelFactory factory` arg** preserved per DW13 (test-only wave; Phase 7 may remove).
5. **3 Pipe/ root files orphaned by Wave 2** resolved per FD1 + DW16: pick up `PipeContextFactory`; explicitly skip `PipeKey` + `StageMarker` as constants-only.
6. **27 concrete middleware count** (not 29 — `Middleware.cs` abstract base + `MiddlewareInfo.cs` info type are non-instantiable; `StagedMiddleware` abstract base co-located in `StageMarkerMiddleware.cs` is covered via concrete subclasses).
7. **Carries forward from prior phases:** see parent §13 item 5 (HttpContext stub, IntegrationTests, NU1902, Phase 2 metadata heterogeneity, MEDI 1.0.2 binary incompat in `ServiceProviderAdapter(IServiceCollection)` ctor — Phase 7 un-skip).
