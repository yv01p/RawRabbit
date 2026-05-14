# Modernization Phase 4.5 Wave 4 — `Operations.*` Coverage Implementation Plan

> **For agentic workers:** REQUIRED: Use `superpowers:subagent-driven-development` to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Source spec:** `docs/specs/2026-05-14-modernization-phase-4.5-wave-4-design.md` (commit SHA: `bc9142b`)

**Plan scope:** Wave 4 only — 8 SDD tasks, one per `Operations.*` source project per spec W4-D1. Waves 1–3 already shipped through `c5e78b4` on `origin/2.0`; Wave 5 (Enrichers.*) is independent per parent §6 and will be planned separately.

**Goal:** Land per-public-method test coverage (F1=(a) strict per Wave 2/3 carry-forward) across the 8 `Operations.*` source projects via the 8 already-scaffolded `test/RawRabbit.Operations.*.Tests/` projects. ~86 new test files; estimated ~120–166 net-new passing tests (well above ≥80 wave floor per spec §6.4). Zero source modifications; zero csproj modifications; zero new test projects.

**Architecture:** One test class per testable source class; subdir layout mirrors `src/`. Mock at `IPipeContext` boundary per parent D3 — three patterns per spec §2 (Mock<IPipeContext> + Properties dict, concrete `PipeContext`, `*Func` options injection). Inline `Mock<IModel>` for the 6 broker-touching middleware identified in spec §2. `Mock<IBusClient>.Setup(b => b.InvokeAsync(...)).ReturnsAsync(mockContext.Object)` for the IBusClient-extension entry points (per VP-21 precedent at `test/RawRabbit.Tests/Instantiation/Disposable/BusClientTests.cs:33-36`). `Mock<IBusClient>` also covers MessageSequence's sync-wait per spec §11 A20. `[Collection("LogProviderState")]` opt-in on the 10 LogProvider-reading test classes per spec §2 + W4-D5. Namespace-alias `using <Alias> = <Type>;` on test files in 3 specific subdirs per spec §2 + W4-D7. Constants-only and static-pipe-builder-only files skipped per new W4-D8 convention (analogous to Wave 3 DW16). `[Fact(Skip = "Phase 5/7 territory: <signature>")]` permitted within wave-pool budget N=0–5 per spec §4.

**Tech stack (inherited from spec):** .NET 10 SDK 10.0.107 + C# 13; xUnit 2.9.3; xunit.runner.visualstudio 2.8.2; Microsoft.NET.Test.Sdk 18.5.1; Moq 4.20.72; RabbitMQ.Client 5.0.1 (legacy; Phase 5 modernizes). CPM via `Directory.Packages.props`. No new package dependencies for Wave 4.

---

## File Structure

**Create only.** Wave 4 lands ~86 new test files across 8 already-scaffolded test projects. Zero source modifications; zero csproj modifications; zero solution-file modifications.

**IBusClient-extension test setup for `Task<T>`-returning extensions.** When the extension returns a value extracted from the result context (e.g., `GetAsync` returns `result.Get<Ackable<object>>(...).AsAckable<>()`), the test MUST populate `mockContext.Properties` with the key the extension reads, so the extraction returns a meaningful value AND the test can assert on the returned value (not just on `InvokeAsync` invocation — A25 anti-pattern; Wave 3 lesson `08f3e99`).

Pattern (using `GetAsync` as exemplar):

```csharp
var mockChannel = new Mock<IModel>();
var mockGetResult = /* construct/mock BasicGetResult */;
var ackable = new Ackable<object>(mockGetResult, mockChannel.Object, deliveryTag: 1UL);
var props = new Dictionary<string, object> { [GetKey.AckableResult] = ackable };
var mockContext = new Mock<IPipeContext>();
mockContext.Setup(c => c.Properties).Returns(props);
var mockBus = new Mock<IBusClient>();
mockBus.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockContext.Object);

var result = await mockBus.Object.GetAsync();

Assert.NotNull(result);
Assert.Same(mockGetResult, result.Content);
mockBus.Verify(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>()), Times.Once);
```

Affected extensions (those returning `Task<T>` and extracting via context):

| Task | File | Extension | Return type | Extracts via | Plan-required mockContext key |
|---|---|---|---|---|---|
| T1 | `GetOperationTests.cs` | `GetAsync` | `Task<Ackable<BasicGetResult>>` | `result.Get<Ackable<object>>(GetKey.AckableResult).AsAckable<>()` | `GetKey.AckableResult` → real `Ackable<object>` |
| T1 | `GetOfTOperationTests.cs` | `GetAsync<TMessage>` | `Task<Ackable<TMessage>>` | analogous | `GetKey.AckableResult` |
| T1 | `GetManyOfTOperationTests.cs` | `GetManyAsync<TMessage>` | `Task<Ackable<List<Ackable<TMessage>>>>` | analogous | `GetKey.AckableResult` |
| T2 | `MessageSequenceExtensionTests.cs` | `ExecuteSequence<TCompleteType>` | `MessageSequence<TCompleteType>` | (verify at SDD-time which key) | (verify at SDD-time) |
| T4 | `RequestExtensionTests.cs` | `RequestAsync<TRequest, TResponse>` | `Task<TResponse>` | (verify at SDD-time which key) | (verify at SDD-time) |
| T8 | `CreateChannelExtensionTests.cs` | `CreateChannelAsync` | `Task<IModel>` | `context.GetChannel()` (`PipeKey.Channel`) — verified at probe `src/RawRabbit.Operations.Tools/CreateChannelExtension.cs:18-19` | `PipeKey.Channel` → `Mock<IModel>.Object` |
| T8 | `CreateConsumerExtensionTests.cs` | `CreateConsumerAsync` | `Task<IBasicConsumer>` | analogous (`PipeKey.Consumer` likely; verify at SDD) | `PipeKey.Consumer` → `Mock<IBasicConsumer>.Object` |

Per-task, the keys to populate are listed in the table above. SDD implementer reads each affected extension's source body to confirm the exact `PipeKey` it reads. For `Task`-only extensions (no extraction — `PublishAsync`, `SubscribeAsync`, `BasicPublishAsync`, `BindQueueAsync`, `DeclareExchangeAsync`, `DeclareQueueAsync`, `DeleteExchangeAsync`, `DeleteQueueAsync`, `BasicConsumeAsync` — 9 of the wave's 17 IBusClient extensions), the existing pattern (Setup InvokeAsync + Verify invocation) is sufficient; the new guidance only applies to `Task<T>`-returning extensions.

### Task 1 (T1) — `test/RawRabbit.Operations.Get.Tests/` (~9 test files, ~14–18 tests)

- `Middleware/AckableResultMiddlewareTests.cs` — `AckableResultMiddleware<TResult>` ctor `(AckableResultOptions<TResult>=null)` + `AckableResultMiddleware` ctor `(AckableResultOptions)` + `InvokeAsync` reads `BasicGetResult` from context, computes `Ackable`, sets `GetKey.AckableResult` in Properties. **Broker-touching: inline `Mock<IModel>` via `*Func` options injection (`ChannelFunc`, `DeliveryTagFunc`).**
- `Middleware/BasicGetMiddleware.cs` → `Middleware/BasicGetMiddlewareTests.cs` — ctor `(BasicGetOptions=null)` + `InvokeAsync` calls `IModel.BasicGet(...)` and stores `BasicGetResult` in Properties. **Broker-touching: inline `Mock<IModel>.Setup(c => c.BasicGet(...)).Returns(mockResult)`.**
- `Middleware/ConventionNamingMiddlewareTests.cs` — ctor `(INamingConventions, ConventionNamingOptions=null)` + `InvokeAsync` resolves naming via injected `INamingConventions`.
- `Middleware/GetConfigurationMiddlewareTests.cs` — ctor `(GetConfigurationOptions=null)` + `InvokeAsync` builds `GetConfiguration` from context's `ConfigurationAction`.
- `GetOperationTests.cs` — static IBusClient extension `GetAsync(this IBusClient, Action<IGetConfigurationBuilder>=null, CancellationToken=default)` per VP-21 pattern; verify `Mock<IBusClient>.InvokeAsync` called with `UntypedGetPipe`. **Skip the static `UntypedGetPipe` field per W4-D8.**
- `GetOfTOperationTests.cs` — static `GetAsync<TMessage>(...)` analog; verify InvokeAsync with `DeserializedBodyGetPipe`.
- `GetManyOfTOperationTests.cs` — static `GetManyAsync<TMessage>(this IBusClient, int batchSize, ...)` returning `Ackable<List<Ackable<TMessage>>>`.
- `GetPipeExtensionsTests.cs` — 2 IPipeContext extensions: `GetGetConfiguration` + `GetBasicGetResult`. Use concrete `PipeContext` per spec §2 pattern 2.
- `Model/AckableExtensionsTests.cs` — `AsAckable<TType>(this Ackable<object>)` cast-extension.
- `Model/AckableOfTTests.cs` — `Ackable<TType>` ctors (2 overloads — params + Func) + `Ack`/`Nack`/`Reject`/`Dispose` methods. **Broker-touching: inline `Mock<IModel>` for `IModel.BasicAck`/`BasicNack`/`BasicReject` verifies.**

**Skip per W4-D8:** `Model/GetKey.cs` (constants only).

**Notes:** No LogProvider consumers in Get sources (per spec §2 table). No namespace collision (per spec §2 table). 2 broker-touching middleware (AckableResult, BasicGet) + 1 broker-touching POCO (Ackable<TType>).

### Task 2 (T2) — `test/RawRabbit.Operations.MessageSequence.Tests/` (~10 test files, ~12–16 tests)

- `Configuration/StepOptionBuilderTests.cs` — ctor `()` + `AbortsExecution(bool=true)` + `IsOptional(bool=true)` fluent methods + `Configuration` property reflects state.
- `MessageSequenceExtensionTests.cs` — static `ExecuteSequence<TCompleteType>(this IBusClient, Func<...>)` extension. Verify `Mock<IBusClient>.InvokeAsync` invocation pattern.
- `Model/ExecutionResultTests.cs` — POCO; verify property get/set (`StepId`, `Time`, `Type`).
- `Model/ExecutionStateTests.cs` — POCO + parameterless ctor; verify property defaults (`Skipped`/`Completed`/`HandlerTasks` non-null lists).
- `Model/MessageSequenceTests.cs` — POCO; verify property get/set. **Namespace alias needed:** `using ModelMessageSequence = RawRabbit.Operations.MessageSequence.Model.MessageSequence<object>;` per W4-D7 (collides with parent namespace `RawRabbit.Operations.MessageSequence`). Tests live at `test/RawRabbit.Operations.MessageSequence.Tests/Model/MessageSequenceTests.cs`.
- `Model/StepDefinitionTests.cs` — POCO + parameterless ctor; `Id` is `private set` so verify it's settable internally only; verify defaults.
- `Model/StepOptionTests.cs` — POCO + static `StepOption Default`; verify `Default` returns expected defaults.
- `StateMachine/MessageSequenceTests.cs` — `[Collection("LogProviderState")]` (LogProvider per spec §2). **Namespace alias needed:** `using SmMessageSequence = RawRabbit.Operations.MessageSequence.StateMachine.MessageSequence;` per W4-D7. ctor `(IBusClient, INamingConventions, RawRabbitConfiguration, SequenceModel=null)` per VP-19. **Sync-wait at source line 235 (`_client.CreateChannelAsync().GetAwaiter().GetResult()`) and line 245 (`_client.InvokeAsync(...).GetAwaiter().GetResult()`)** — `Mock<IBusClient>.Setup(b => b.InvokeAsync(...)).ReturnsAsync(mockContext)` where `mockContext.Properties[PipeKey.Channel]` returns `Mock<IModel>.Object`. Cover ctor happy + `PublishAsync<TMessage>` + `When<TMessage, TMessageContext>` + `Complete<TMessage>` per F1=(a). **Plan-time decision deferred to SDD: `ConfigureState` and `Initialize` are protected — covered indirectly via public methods.**
- `StateMachine/SequenceModelTests.cs` — POCO inheriting `Model<SequenceState>`; verify property get/set.
- `Trigger/MessageAndContextTriggerExtensionTests.cs` — static `FromMessage<TStateMachine, TMessage, TMessageContext>(...)` extension; verify `TriggerConfigurer.From(...)` invocation. **Skip the static `ConsumePipe` + `SubscribePipe` fields per W4-D8.**

**Skip:** 3 interfaces in `Configuration/Abstraction/` (per Wave 3 DW14: interfaces covered indirectly via implementations).

**Notes:** 2-source namespace collision per spec §2 table (alias on tests in `Model/` and `StateMachine/`). 1 LogProvider consumer (`StateMachine/MessageSequence`). 1 sync-wait IBusClient consumer (same file; covered by `Mock<IBusClient>` per spec §11 A20 + VP-19).

### Task 3 (T3) — `test/RawRabbit.Operations.Publish.Tests/` (~6 test files, ~10–14 tests)

- `Context/PublishContextTests.cs` — ctor `(IPipeContext)`; verify Properties pass-through to inner.
- `Context/PublishContextExtensionsTests.cs` — `UsePublishConfiguration(this IPublishContext, Action<IPublisherConfigurationBuilder>)` extension; verify configuration delegate invocation.
- `Middleware/PublishAcknowledgeMiddlewareTests.cs` — `[Collection("LogProviderState")]` (LogProvider). ctor `(IExclusiveLock, PublishAcknowledgeOptions=null)` + `InvokeAsync` reads `IModel` from context, sets up confirm-mode/timeout. **Broker-touching: inline `Mock<IModel>.Setup(c => c.ConfirmSelect)`/.WaitForConfirms` verifies.** Co-located: `PublishAcknowledgePipeGetExtensions.GetPublishAcknowledgeTimeout` + `PublishAcknowledgePipeUseExtensions.UsePublishAcknowledge` (2 overloads) — covered in same test file or separate small file at SDD discretion.
- `Middleware/PublishConfigurationMiddlewareTests.cs` — `[Collection("LogProviderState")]` (LogProvider). ctor `(IPublisherConfigurationFactory, PublishConfigurationOptions=null)` + `InvokeAsync` resolves config from context strings or message type.
- `Middleware/ReturnCallbackMiddlewareTests.cs` — `[Collection("LogProviderState")]` (LogProvider). ctor `(ReturnCallbackOptions=null)` + `InvokeAsync` reads `IModel` + `EventHandler<BasicReturnEventArgs>` from context, attaches handler. **Broker-touching: inline `Mock<IModel>` for `BasicReturn += handler` verifies.**
- `PublishMessageExtensionTests.cs` — static `PublishAsync<TMessage>(this IBusClient, TMessage, Action<IPublishContext>=null, CancellationToken=default)` per VP-21 pattern; verify InvokeAsync called with `PublishPipeAction`. **Skip the static `PublishPipeAction` field per W4-D8.**

**Skip per W4-D8:** `PublishKey.cs` (constants), `PublishStage.cs` (enum-only — no behavior).

**Notes:** 3 LogProvider consumers (all 3 middleware). 2 broker-touching middleware (PublishAcknowledge, ReturnCallback). No namespace collision.

### Task 4 (T4) — `test/RawRabbit.Operations.Request.Tests/` (~12 test files, ~18–24 tests)

- `Configuration/RequestConfigurationTests.cs` — POCO + co-located `RequestConfigurationExtensions.ToDirectRpc(this RequestConfiguration)` extension; verify property defaults + ToDirectRpc transformation.
- `Configuration/RequestConfigurationBuilderTests.cs` — ctor `(RequestConfiguration initial)` + `PublishRequest` + `ConsumeResponse` fluent methods + `Config` property.
- `Configuration/RequestConfigurationFactoryTests.cs` — ctor `(IPublisherConfigurationFactory, IConsumerConfigurationFactory)` + 3 `Create` overloads (typed, type-args, string-args).
- `Context/RequestContextTests.cs` — ctor `(IPipeContext)`; Properties pass-through.
- `Context/RequestContextExtensionsTests.cs` — `UseRequestConfiguration(this IRequestContext, Action<IRequestConfigurationBuilder>)` in `namespace RawRabbit`.
- `Core/PipeContextExtensionsTests.cs` — 8 IPipeContext extensions (GetResponseMessageType, GetCorrelationId, etc.). Use concrete `PipeContext` per spec §2 pattern 2.
- `Middleware/BasicPropertiesMiddlewareTests.cs` — ctor `(ISerializer, BasicPropertiesOptions)` + inherits `Pipe.Middleware.BasicPropertiesMiddleware`; test the Request-specific override only (not the base — Wave 3 covers).
- `Middleware/RequestConfigurationMiddlewareTests.cs` — 2 ctor overloads (factory variants) + `InvokeAsync` resolves config.
- `Middleware/RequestTimeoutMiddlewareTests.cs` — ctor `(RequestTimeoutOptions=null)` + `InvokeAsync`. Co-located: `RequestTimeoutExtensions.UseRequestTimeout` + `GetRequestTimeout` — covered in same file.
- `Middleware/ResponderExceptionMiddlewareTests.cs` — `[Collection("LogProviderState")]` (LogProvider). ctor `(ISerializer, ResponderExceptionOptions=null)` + `InvokeAsync` extracts exception info from `BasicDeliverEventArgs` headers.
- `Middleware/ResponseConsumeMiddlewareTests.cs` — `[Collection("LogProviderState")]` (LogProvider). ctor `(IConsumerFactory, IPipeBuilderFactory, ResponseConsumerOptions)` + `InvokeAsync` registers response consumer. Co-located: `ResposeConsumerMiddlewareExtensions` (note the project's typo `Respose` — preserve it; don't fix).
- `RequestExtensionTests.cs` — static `RequestAsync<TRequest, TResponse>(this IBusClient, TRequest=default, Action<IRequestContext>=null, CancellationToken=default)` per VP-21 pattern. **Skip the static `RequestPipe` field per W4-D8.**

**Skip:** 2 interfaces in `Configuration/Abstraction/` (DW14). `Core/RequestKey.cs` per W4-D8.

**Notes:** 2 LogProvider consumers (ResponderException, ResponseConsume). No broker-touching middleware (per spec §2 table — Request middleware operate on response args/headers, not direct broker calls). No namespace collision.

### Task 5 (T5) — `test/RawRabbit.Operations.Respond.Tests/` (~16 test files, ~25–32 tests) — LARGEST

- `Acknowledgement/AckTests.cs` — `Ack` (untyped) + `Response` property; ctor inherits `Common.Ack`.
- `Acknowledgement/AckOfTTests.cs` — `Ack<TResponse>` ctor `(TResponse response)` + `Response` property + `AsUntyped()` override.
- `Acknowledgement/NackTests.cs` — `Nack<TResponse>` ctor `(bool requeue=true)` + `Requeue` property + `AsUntyped()`.
- `Acknowledgement/RejectTests.cs` — `Reject<TResponse>` ctor `(bool requeue=true)` + `Requeue` property + `AsUntyped()`.
- `Acknowledgement/RespondTests.cs` — **Namespace alias needed:** `using AckRespond = RawRabbit.Operations.Respond.Acknowledgement.Respond;` per W4-D7 (project name = class name). Static `Respond` class with 3 factories (`Ack<TResponse>`, `Nack<TResponse>`, `Reject<TResponse>`). Tests verify each factory returns the right typed acknowledgement subclass.
- `Acknowledgement/TypedAcknowlegementTests.cs` — Abstract; covered via concrete subclasses (Ack/Nack/Reject). **Spec §3/DW14 convention: no direct test file for abstract base.** **OMIT this test file.**
- `Configuration/RespondConfigurationTests.cs` — POCO inheriting `ConsumerConfiguration`; verify property defaults.
- `Configuration/RespondConfigurationBuilderTests.cs` — ctor `(ConsumerConfiguration initial)` inheriting `ConsumerConfigurationBuilder`; co-defined `IRespondConfigurationBuilder` interface — covered via impl.
- `Configuration/RespondConfigurationFactoryTests.cs` — ctor `(IConsumerConfigurationFactory)` + 2 `Create` overloads; co-defined `IRespondConfigurationFactory` interface — covered via impl.
- `Context/RespondContextTests.cs` — ctor `(IPipeContext)`; Properties pass-through.
- `Context/RespondContextExtensionsTests.cs` — `UseRespondConfiguration(this IRespondContext, Action<IRespondConfigurationBuilder>)` in `namespace RawRabbit`.
- `Core/PipeContextExtensionsTests.cs` — 6 IPipeContext extensions. Use concrete `PipeContext` per spec §2 pattern 2.
- `Middleware/ReplyToExtractionMiddlewareTests.cs` — `[Collection("LogProviderState")]` (LogProvider). ctor `(ReplyToExtractionOptions=null)` + `InvokeAsync` extracts `PublicationAddress` from `BasicDeliverEventArgs.BasicProperties.ReplyTo`.
- `Middleware/RespondConfigurationMiddlewareTests.cs` — 2 ctor overloads (factory variants) + `InvokeAsync` resolves respond config.
- `Middleware/RespondExceptionMiddlewareTests.cs` — ctor `(IPipeBuilderFactory, RespondExceptionOptions=null)` + `OnExceptionAsync` (overrides `ExceptionHandlingMiddleware.OnExceptionAsync`); inner-pipe invocation pattern. **Broker-touching via base class; the override itself doesn't directly call `IModel`.**
- `Middleware/ResponseHandlerOptionFactoryTests.cs` — static `Create(HandlerInvocationOptions=null)` returning configured options.
- `RespondExtensionTests.cs` — static `RespondAsync<TRequest, TResponse>(this IBusClient, ...)` 2 overloads (Func<TRequest, Task<TResponse>> + Func<TRequest, Task<TypedAcknowlegement<TResponse>>>) per VP-21 pattern. **Skip the static `ConsumePipe` + `RespondPipe` fields per W4-D8.**

**Skip per W4-D8:** `Core/RespondKey.cs`, `Core/RespondStage.cs` (constants only).

**Notes:** 1-source namespace collision (Acknowledgement/Respond.cs) → alias on `Acknowledgement/RespondTests.cs` only. 1 LogProvider consumer (ReplyToExtraction). 1 broker-touching middleware via base (RespondException). Largest task by file count and test count.

### Task 6 (T6) — `test/RawRabbit.Operations.StateMachine.Tests/` (~16 test files, ~20–26 tests)

- `Context/StateMachineContextTests.cs` — `IStateMachineContext` interface + `StateMachineContext` ctor `(IPipeContext)`.
- `Core/GlobalLockTests.cs` — `[Collection("LogProviderState")]` per spec §2 (LogProvider on `ProcessGlobalLock` co-class). `IGlobalLock` interface + `GlobalLock` concrete (ctor `(Func<Guid, Func<Task>, CancellationToken, Task>=null)`) + `ProcessGlobalLock` concrete (ctor `()`); both implement `ExecuteAsync(Guid, Func<Task>, CancellationToken=default)`. Cover happy + 2nd-call-blocks-until-1st-completes (concurrent test for ProcessGlobalLock).
- `Core/ModelRepositoryTests.cs` — `IModelRepository` interface + `ModelRepository` concrete (ctor `(Func<Guid, Task<Model>>=null, Func<Model, Task>=null)`) + `GetAsync` + `AddOrUpdateAsync`.
- `Core/StateMachineActivatorTests.cs` — ctor `(IModelRepository, IDependencyResolver)` + `ActivateAsync(Guid, Type)` happy + `PersistAsync(StateMachineBase)` happy. Mock both deps.
- `Middleware/GlobalLockMiddlewareTests.cs` — ctor `(IGlobalLock)` + `InvokeAsync` invokes injected lock around `Next.InvokeAsync`.
- `Middleware/ModelIdMiddlewareTests.cs` — ctor `(ModelIdOptions=null)` + `InvokeAsync` extracts model ID from context's correlation func.
- `Middleware/PersistModelMiddlewareTests.cs` — ctor `(IStateMachineActivator)` + `InvokeAsync` calls `_activator.PersistAsync(...)` after `Next.InvokeAsync`.
- `Middleware/RetrieveStateMachineMiddlewareTests.cs` — ctor `(IStateMachineActivator, RetrieveStateMachineOptions=null)` + `InvokeAsync` calls `_activator.ActivateAsync(...)`.
- `ModelTests.cs` — `Model` (`Id` Guid prop) + `Model<TState>` (`State` TState prop). Concrete `TestModel : Model<int>` file-local internal class for testing the abstract base.
- `PipeContextExtensionsTests.cs` — 9 IPipeContext extensions (`GetStateMachine`, `GetModelId`, `GetContextAction`, `GetPipeBuilderAction`, `GetIdCorrelationFunc`, `GetLazyCorrelationArgs`, `GetLazyHandlerArgs`, `UseLazyCorrelationArgs`, `UseLazyHandlerArgs`). Use concrete `PipeContext` per spec §2 pattern 2.
- `StateMachineBaseTests.cs` — Per W4-R9: file-local internal `TestStateMachine : StateMachineBase<int, string, TestModel>` subclass. ctor `(TModel=null)` happy (default Model from `Initialize()`) + ctor with provided model. `TriggerAsync(object)` happy + `TriggerAsync<TPayload>(object, TPayload)` happy + `GetDto()` returns the model. **Plan-time decision: testing the protected `ConfigureState` indirectly via `TriggerAsync` calls (state changes observable via `GetDto().State`).**
- `StateMachineExtensionTests.cs` — static `RegisterStateMachineAsync<TTriggerConfiguration>()` extension; verify registration logic.
- `StateMachinePlugin.cs` → `StateMachinePluginTests.cs` (in `namespace RawRabbit`) — static `UseStateMachine(Func<Guid, Task<Model>>=null, Func<Model, Task>=null, Func<Guid, Func<Task>, CancellationToken, Task>=null)` extension on `IClientBuilder`; verify it registers via the builder.
- `Trigger/TriggerConfigurationCollectionTests.cs` — Abstract; file-local internal `TestTriggerCollection : TriggerConfigurationCollection` subclass implementing `ConfigureTriggers`. Verify `GetTriggerConfiguration()` returns expected list after config.
- `Trigger/TriggerConfigurerTests.cs` — ctor `()` + `From(Action<IPipeBuilder>, Action<IPipeContext>)` fluent + `TriggerConfiguration` POCO co-class. Verify chain returns same instance.
- `Trigger/TriggerFromMessageExtensionTests.cs` — static `FromMessage<TStateMachine, TMessage>` 2 overloads. **Skip the static `ConsumePipe` + `SubscribePipe` fields per W4-D8.**

**Skip per W4-D8:** `StateMachineKey.cs` (constants only).

**Notes:** 1 LogProvider consumer (Core/GlobalLock — actually ProcessGlobalLock co-class). No namespace collision (StateMachine project has no class named "StateMachine"). Abstract `StateMachineBase<>` per W4-R9 → file-local concrete subclass. No broker-touching middleware (StateMachine pipeline operates on model lifecycle, not direct broker).

### Task 7 (T7) — `test/RawRabbit.Operations.Subscribe.Tests/` (~6 test files, ~10–14 tests) — SMALLEST

- `Context/SubscribeContextTests.cs` — `ISubscribeContext` interface + `SubscribeContext` ctor `(IPipeContext)`.
- `Context/SubscribeContextExtensionsTests.cs` — `UseSubscribeConfiguration(this ISubscribeContext, Action<IConsumerConfigurationBuilder>)` in `namespace RawRabbit`.
- `Middleware/SubscribeInvocationMiddlewareTests.cs` — parameterless ctor + inherits `HandlerInvocationMiddleware`; test the Subscribe-specific override only.
- `Middleware/SubscriptionConfigurationMiddlewareTests.cs` — `[Collection("LogProviderState")]` (LogProvider). ctor `(IConsumerConfigurationFactory, SubscriptionConfigurationOptions=null)` + `InvokeAsync` resolves consumer config from context.
- `Middleware/SubscriptionExceptionMiddlewareTests.cs` — `[Collection("LogProviderState")]` (LogProvider). ctor `(IPipeBuilderFactory, IChannelFactory, ITopologyProvider, INamingConventions, SubscriptionExceptionOptions)` + `OnExceptionAsync` extracts channel + dispatches via inner pipe. **Broker-touching: inline `Mock<IModel>` for `BasicPublish`/`BasicAck` verifies.**
- `SubscribeMessageExtensionTests.cs` — static `SubscribeAsync<TMessage>(this IBusClient, ...)` 2 overloads (Func<TMessage, Task> + Func<TMessage, Task<Acknowledgement>>) per VP-21 pattern. **Skip the static `ConsumePipe` + `SubscribePipe` fields per W4-D8.**

**Skip per W4-D8:** `Stages/SubscribeStage.cs` (enum-only).

**Notes:** 2 LogProvider consumers (SubscriptionConfiguration, SubscriptionException). 1 broker-touching middleware (SubscriptionException). No namespace collision.

### Task 8 (T8) — `test/RawRabbit.Operations.Tools.Tests/` (~11 test files, ~14–22 tests)

Per W4-R2 plan-time decision: **9 separate per-extension test files** (mirror per-source-class rule used by other 7 tasks).

- `BasicConsumeExtensionTests.cs` — static `BasicConsumeAsync(this IBusClient, Func<BasicDeliverEventArgs, Task<Acknowledgement>>, Action<IPipeContext>)` per VP-21 pattern.
- `BasicPublishExtensionTests.cs` — static `BasicPublishAsync` 2 overloads (object + BasicPublishConfiguration). **Skip the static `PublishPipe` field per W4-D8.**
- `BindQueueExtensionTests.cs` — static `BindQueueAsync` 2 overloads (string-args + typed). **Skip the static `BindQueueAction` field per W4-D8.**
- `CreateChannelExtensionTests.cs` — static `CreateChannelAsync(this IBusClient, ChannelCreationOptions=null, CancellationToken=default)` returning `Task<IModel>`. **Skip `CreateChannelPipe` field.**
- `CreateConsumerExtensionTests.cs` — static `CreateConsumerAsync(this IBusClient, ConsumeConfiguration=null, CancellationToken=default)` returning `Task<IBasicConsumer>`. **Skip `ConsumerAction` field.**
- `DeclareExchangeExtensionTests.cs` — static `DeclareExchangeAsync` 2 overloads. **Skip `DeclareExchangeAction` field.**
- `DeclareQueueExtensionTests.cs` — static `DeclareQueueAsync` 2 overloads. **Skip `DeclareQueueAction` field.**
- `DeleteExchangeExtensionTests.cs` — static `DeleteExchangeAsync` 2 overloads. **Skip `DeleteExchangePipe` field.**
- `DeleteQueueExtensionTests.cs` — static `DeleteQueueAsync` 2 overloads. **Skip `DeletePipe` field.**
- `Middleware/ExchangeDeclarationMiddlewareTests.cs` — ctor `(IExchangeDeclarationFactory, ExchangeDeclarationOptions=null)` + `InvokeAsync`.
- `Middleware/QueueDeclarationMiddlewareTests.cs` — ctor `(IQueueConfigurationFactory, QueueDeclarationOptions=null)` + `InvokeAsync`.

**Notes:** 0 LogProvider consumers. 0 broker-touching middleware (Tools middleware delegate to factories, not direct broker calls). 9 IBusClient extensions tested via VP-21 `Mock<IBusClient>.Setup(b => b.InvokeAsync(...)).ReturnsAsync(mockContext)` pattern.

### Wave totals (estimate)

- **~86 test files** across 8 already-scaffolded test projects (T1=9 + T2=10 + T3=6 + T4=12 + T5=16 + T6=16 + T7=6 + T8=11)
- **~120–166 net-new passing tests** (well above ≥80 wave floor per spec §6.4; avg 1.4-1.9 tests/file consistent with parent §5's "8-15 tests per project")

---

## Inherited from spec

The following assumptions were verified by `thorough-brainstorming` at spec-write time (HEAD `bc9142b` after CDR R1 → UDD R1) and are NOT re-verified here. Trusted as ground truth:

- **A1** — All 8 `src/RawRabbit.Operations.*` projects exist with the exact file counts in spec §1 (Get=11, MessageSequence=13, Publish=8, Request=15, Respond=19, StateMachine=17, Subscribe=7, Tools=11; total 101)
- **A2** — All 8 `test/RawRabbit.Operations.*.Tests/` projects exist with valid csproj and 0 .cs files
- **A3** — Each Operations.*.Tests csproj has correct ProjectReferences (RawRabbit core + own source assembly)
- **A4** — `IPipeContext` exists at `src/RawRabbit/Pipe/IPipeContext.cs:5`
- **A5** — Operations middleware contract shape is `public override Task InvokeAsync(IPipeContext context, CancellationToken token)` (uniform via inheritance from `Middleware` abstract base)
- **A6** — 6 broker-touching Operations middleware enumerated in spec §2 table
- **A7** — 10 `LogProvider.For<T>()` consumers enumerated in spec §2 + W4-D5 table
- **A8** — 2 projects + 3 source files have namespace-collision risk per spec §2 + W4-D7 (MessageSequence ×2, Respond ×1)
- **A9** — Package versions: xunit 2.9.3, runner.visualstudio 2.8.2, NET.Test.Sdk 18.5.1, Moq 4.20.72, RabbitMQ.Client 5.0.1
- **A10** — `.editorconfig` mandates tabs (`indent_style = tab`)
- **A11** — Aggregate skip baseline = 15 (RawRabbit.Tests=12 + ServiceCollection.Tests=1 + Polly.Tests=2)
- **A12** — `dotnet build -c Release` returns 0 errors at HEAD `bc9142b`
- **A13** — Empty Operations.*.Tests projects exit 0 with "No test is available" — Wave 1 scaffolding correct
- **A14** — RabbitMQ.Client = 5.0.1 (carry-forward dead end about `HandleBasicDeliver` `byte[]` still applies)
- **A15** — StateMachine has no Timer/Task.Delay/Thread.Sleep code; standard middleware-mock pattern suffices (W4-R1 closed)
- **A20** — `Operations.*` sources do NOT contain `RawRabbitFactory.CreateSingleton`/`new RawRabbitFactory`; 1 sync-wait `IBusClient` consumer at `MessageSequence/StateMachine/MessageSequence.cs:235,245` covered by `Mock<IBusClient>` per spec §5 mocking-surface bullet 2
- **W4-D1 through W4-D7** — all 7 spec-locked Wave 4 decisions inherited as-is
- **F1=(a) strict** — ≥1 happy + ≥1 error path test per public method on every source class with public surface (Wave 2/3 carry-forward)
- **D6** (parent) — `[Theory]/[InlineData]` opportunistic where 3+ similar tests differ only in input/expected
- **D8** (parent) — One test project per separate source assembly; 8 Operations.*.Tests csprojs scaffolded by Wave 1
- **D10** (parent) — Pre-existing skipped tests are NOT touched in Phase 4.5

---

## Verified plan-level assumptions

Newly introduced by this plan and verified empirically against HEAD `bc9142b` at plan-write time on 2026-05-14:

| # | Category | Assumption | Evidence |
|---|---|---|---|
| VP-1 | Cat 1 (file paths) | Each test project csproj auto-discovers .cs files in any subdirectory (SDK-style) | Inherited from spec §11 A2 (csproj is `<Project Sdk="Microsoft.NET.Sdk">`); Wave 1 verified A13 (`dotnet test` against empty project succeeds) |
| VP-2 | Cat 1 (file paths) | All ~70 destination test file paths are greenfield | Inherited from spec §11 A2 + A19 (`find test/RawRabbit.Operations.*.Tests/ -name '*.cs'` returns 0) |
| VP-3 | Cat 2 (signatures) | T1 Get source enumeration — see §"Per-task file structure" T1 | 8 parallel Explore agent enumerations of `src/RawRabbit.Operations.Get/` confirmed: 3 IBusClient extensions in `namespace RawRabbit` (GetOperation, GetOfTOperation, GetManyOfTOperation) + 4 middleware (AckableResultMiddleware<TResult>, BasicGetMiddleware, ConventionNamingMiddleware, GetConfigurationMiddleware) + 2 model types (Ackable<TType>, AckableExtensions) + 1 PipeContextExtensions (GetPipeExtensions) + 1 constants-only file (GetKey.cs); 0 LogProvider consumers |
| VP-4 | Cat 2 (signatures) | T2 MessageSequence source enumeration — see T2 | Explore: 1 builder (StepOptionBuilder), 1 IBusClient extension (MessageSequenceExtension), 5 POCO models + 2 StateMachine classes (MessageSequence with LogProvider + IBusClient sync-wait at lines 235,245; SequenceModel POCO), 1 trigger extension, 3 interfaces in Configuration/Abstraction (skip per DW14) |
| VP-5 | Cat 2 (signatures) | T3 Publish source enumeration — see T3 | Explore: 1 PublishContext, 1 PublishContextExtensions in `namespace RawRabbit`, 3 middleware all LogProvider (PublishConfiguration, PublishAcknowledge, ReturnCallback; latter 2 broker-touching), 1 IBusClient extension (PublishMessageExtension), constants/enum (PublishKey, PublishStage) skipped per W4-D8 |
| VP-6 | Cat 2 (signatures) | T4 Request source enumeration — see T4 | Explore: 3 Configuration classes + 2 Context classes + 1 PipeContextExtensions (8 ext methods) + 5 middleware (2 LogProvider: ResponderException, ResponseConsume; BasicProperties inherits Pipe.Middleware version; co-located Extensions classes preserved) + 1 IBusClient extension (RequestExtension); 2 interfaces + 1 constants file skipped |
| VP-7 | Cat 2 (signatures) | T5 Respond source enumeration — see T5 | Explore: 6 Acknowledgement classes (Ack/AckOfT/Nack/Reject/Respond/TypedAcknowlegement; Respond has namespace collision per A8; TypedAcknowlegement abstract → no test file per DW14) + 3 Configuration + 2 Context + 1 PipeContextExtensions + 4 middleware (1 LogProvider: ReplyToExtraction; 1 broker-touching via base: RespondException) + 1 IBusClient extension (RespondExtension); 2 constants files skipped |
| VP-8 | Cat 2 (signatures) | T6 StateMachine source enumeration — see T6 | Explore: 1 Context + 3 Core (GlobalLock has LogProvider on `ProcessGlobalLock` co-class; ModelRepository; StateMachineActivator) + 4 middleware + 1 Model (abstract base) + 1 PipeContextExtensions (9 ext methods) + 1 StateMachineBase abstract (W4-R9) + 1 StateMachineExtension + 1 StateMachinePlugin in `namespace RawRabbit` + 3 Trigger types; 1 constants file skipped; uses `Stateless` library |
| VP-9 | Cat 2 (signatures) | T7 Subscribe source enumeration — see T7 | Explore: 1 Context + 1 SubscribeContextExtensions in `namespace RawRabbit` + 3 middleware (2 LogProvider: SubscriptionConfiguration, SubscriptionException; latter broker-touching via base ExceptionHandlingMiddleware) + 1 IBusClient extension (SubscribeMessageExtension); 1 enum-only file (Stages/SubscribeStage) skipped per W4-D8 |
| VP-10 | Cat 2 (signatures) | T8 Tools source enumeration — see T8 | Explore: 9 IBusClient extensions (all in `namespace RawRabbit`; each with own static pipe-builder field skipped per W4-D8) + 2 middleware (no LogProvider, no broker-touching) |
| VP-11 | Cat 2 (signatures) | `IBusClient` interface has exactly `Task<IPipeContext> InvokeAsync(Action<IPipeBuilder>, Action<IPipeContext>=null, CancellationToken=default)` | Inherited from Wave 3 plan + spec §11 A4 lineage; precedent at `test/RawRabbit.Tests/Instantiation/Disposable/BusClientTests.cs:33-36` shows `Mock<IBusClient>.Setup(b => b.InvokeAsync(...)).ReturnsAsync(...)` works |
| VP-12 | Cat 2 (signatures) — KEY | `IPipeContext` interface declares ONLY `IDictionary<string, object> Properties { get; }`; `PipeContext` concrete class has settable `Properties { get; set; }` | Direct read of `src/RawRabbit/Pipe/IPipeContext.cs:5-12`. Both spec §2 patterns (Mock<IPipeContext>.Setup(c => c.Properties) and `new PipeContext { Properties = ... }`) work |
| VP-13 | Cat 2 (signatures) | `PipeKey` static class declares 27 string constants including `PipeKey.Channel`, `PipeKey.MessageType`, `PipeKey.DeliveryEventArgs`, `PipeKey.BasicProperties`, `PipeKey.ConfigurationAction`, `PipeKey.QueueDeclaration`, `PipeKey.ExchangeDeclaration`, etc. | Direct read of `src/RawRabbit/Pipe/PipeKey.cs` — covers all keys Wave 4 tests will populate |
| VP-14 | Cat 2 (signatures) | `RawRabbit.Pipe.Middleware.Middleware` abstract base has `Middleware Next { get; set; }` property + `abstract Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))` | Direct read of `src/RawRabbit/Pipe/Middleware/Middleware.cs:7-11` |
| VP-15 | Cat 2 (signatures) | RabbitMQ.Client 5.0.1 types `IModel`, `IBasicConsumer`, `BasicReturnEventArgs`, `BasicDeliverEventArgs`, `PublicationAddress` exist with shapes the test plan assumes | Inherited from Wave 3 plan VP-7/VP-8 (Wave 3 uses these extensively); RMQ.Client version unchanged per spec §11 A14 |
| VP-16 | Cat 3 (commands) | `dotnet build -c Release` returns 0 errors; `dotnet test test/RawRabbit.Operations.<X>.Tests --no-build -c Release [--filter "FullyQualifiedName~<class-or-namespace>"]` is a valid invocation per project | Inherited from spec §11 A12 + A13; same shape used throughout Wave 1+2+3 commits |
| VP-17 | Cat 3 (commands) | Codebase commit message convention for SDD task commits = lowercase imperative, no Conventional-Commits prefix (e.g., "add wave 3 topologyprovider tests", "fix wave 3 task 1: ..."). Plan/spec/UDD-applied commits may use longer descriptive messages but SDD task commits follow the lowercase pattern | `git log --format="%s" -20` shows last 14 SDD task commits all use lowercase imperative ("add wave 3 X", "fix wave 3 task N: ...") |
| VP-18 | Cat 3 (commands) | `for f in <files>; do file "$f"; done` BOM verification works on Linux | Standard Linux command; Wave 3 plan VP-17 inheritance + Wave 3 task fix-up at commit `92bc111` ("add missing BOMs to 5 middleware test files") confirmed the workflow |
| VP-19 | Cat 4 (ordering) | T1-T8 have disjoint write paths (each task writes only to its own `test/RawRabbit.Operations.<X>.Tests/` directory); no inter-task code dependencies; tasks may execute in any order | By inspection: 8 separate test project directories; no shared helper files (W4-D2 inline mocks per-test); no Wave-4-internal common code |
| VP-20 | Cat 4 (ordering) | Wave-wide review post-T8 reads only; no commit dependencies between tasks | By design: wave-wide review per spec §6.8 verifies aggregate state, doesn't write |
| VP-21 | Cat 5 (code-in-plan) | `[Xunit.Collection("LogProviderState")]` collection name `"LogProviderState"` matches Wave 3's existing usage exactly; new Wave 4 test classes added to this collection serialize correctly with the existing 23 Wave 3 members | `grep -l "LogProviderState" test/RawRabbit.Tests/` returns ≥5 hits across `Channel/`, `Pipe/Middleware/` confirming the collection name in active use |
| VP-22 | Cat 5 (code-in-plan) | `using <Alias> = <fully.qualified.Type>;` syntax works for the 3 namespace-collision test files (MessageSequence/Model, MessageSequence/StateMachine, Respond/Acknowledgement) | Wave 3 carry-forward: precedent established at `test/RawRabbit.Tests/Pipe/Middleware/PipeBuilderTests.cs:7` (`using MiddlewareBase = RawRabbit.Pipe.Middleware.Middleware;`) |
| VP-23 | Cat 5 (code-in-plan) | `Mock<IBusClient>.Setup(b => b.InvokeAsync(It.IsAny<Action<IPipeBuilder>>(), It.IsAny<Action<IPipeContext>>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockContext.Object)` is valid Moq syntax that intercepts calls to static IBusClient extensions | Direct read of existing precedent `test/RawRabbit.Tests/Instantiation/Disposable/BusClientTests.cs:33-36` shows the exact pattern in use |
| VP-24 | Cat 5 (code-in-plan) | `MessageSequence/StateMachine/MessageSequence` ctor `(IBusClient client, INamingConventions naming, RawRabbitConfiguration clientCfg, SequenceModel model = null)`; sync-wait at lines 235,245 | T2 Explore agent direct read; both lines call `_client.InvokeAsync(...).GetAwaiter().GetResult()` which Mock<IBusClient> intercepts via `.Setup(b => b.InvokeAsync(...)).ReturnsAsync(mockCtx)` where `mockCtx.Properties[PipeKey.Channel] = mockChannel.Object` |
| VP-25 | Cat 5 (code-in-plan) | `await Assert.ThrowsAnyAsync<OperationCanceledException>(...)` works for cancellation tests | Wave 2/3 carry-forward; Wave 3 plan VP-19 and Wave 2 §3 convention established |

**No Cat 6 (consumer impact).** Per skill: Wave 4 is **create-only** — every task has only `Create:` entries, no `Modify:`. Cat 6 not required. Confirmed by inspection of File Structure section.

---

## Tasks

### Task 1: `RawRabbit.Operations.Get` tests

**Files:**
- Create: 9 test files at `test/RawRabbit.Operations.Get.Tests/<subdir>/<TypeUnderTest>Tests.cs` (per VP-3 + File Structure T1)

**Notes:** 0 LogProvider consumers. No `[Collection]` needed. No namespace alias. 2 broker-touching middleware (AckableResult, BasicGet) + 1 broker-touching POCO (Ackable<TType>) → inline `Mock<IModel>` setups per spec §2.

- [ ] **Step 1: Create directories.**
  ```bash
  mkdir -p test/RawRabbit.Operations.Get.Tests/Middleware test/RawRabbit.Operations.Get.Tests/Model
  ```

- [ ] **Step 2: Write all 9 test files** per File Structure T1. Use the 3 patterns from spec §2 per test based on what's being asserted. For IBusClient extensions, use `Mock<IBusClient>.Setup(b => b.InvokeAsync(...)).ReturnsAsync(mockCtx)` pattern per VP-23. For broker-touching middleware, use `*Func` options injection per spec §2 pattern 3 (e.g., `new AckableResultMiddleware(new AckableResultOptions { ChannelFunc = _ => mockChannel.Object })`).

- [ ] **Step 3: Per-file BOM verification.**
  ```bash
  for f in $(find test/RawRabbit.Operations.Get.Tests -name '*.cs'); do file "$f"; done | grep -v "with BOM" && echo "MISSING BOM" || echo "All BOMs OK"
  ```

- [ ] **Step 4: Build + run targeted tests.**
  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Operations.Get.Tests --no-build -c Release 2>&1 | tail -1
  ```
  Expected: `Failed: 0, Passed: ~14-18, Skipped: 0` (no skips expected in T1).

- [ ] **Step 5: Commit.**
  ```bash
  git add test/RawRabbit.Operations.Get.Tests
  git commit -m "add wave 4 operations.get tests"
  ```

---

### Task 2: `RawRabbit.Operations.MessageSequence` tests

**Files:**
- Create: 10 test files at `test/RawRabbit.Operations.MessageSequence.Tests/<subdir>/<TypeUnderTest>Tests.cs` (per VP-4 + File Structure T2)

**Notes:** 1 `[Collection("LogProviderState")]` test class (`StateMachine/MessageSequenceTests.cs`). 3 namespace-alias test files (Model/MessageSequenceTests.cs, StateMachine/MessageSequenceTests.cs both alias the project-name collision). MessageSequence/StateMachine/MessageSequence has IBusClient sync-wait per VP-24 — `Mock<IBusClient>.Setup(b => b.InvokeAsync(...)).ReturnsAsync(mockCtx)` with `mockCtx.Properties[PipeKey.Channel] = mockChannel.Object` to satisfy lines 235 + 245. 3 interfaces in `Configuration/Abstraction/` skipped per DW14.

- [ ] **Step 1: Create directories.**
  ```bash
  mkdir -p test/RawRabbit.Operations.MessageSequence.Tests/{Configuration,Model,StateMachine,Trigger}
  ```

- [ ] **Step 2: Write all 10 test files** per File Structure T2. Apply `[Xunit.Collection("LogProviderState")]` to `StateMachine/MessageSequenceTests.cs`. Apply `using SmMessageSequence = RawRabbit.Operations.MessageSequence.StateMachine.MessageSequence;` and `using ModelMessageSequence = ...Model.MessageSequence<object>;` aliases as needed.

- [ ] **Step 3: BOM verification** (per VP-18).

- [ ] **Step 4: Build + run.**
  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Operations.MessageSequence.Tests --no-build -c Release 2>&1 | tail -1
  ```
  Expected: `Failed: 0, Passed: ~12-16, Skipped: 0` (StateMachine/MessageSequence ctor coverage may need 1-2 skips if Mock<IBusClient> sync-wait setup is intractable — within N=0-5 wave budget).

- [ ] **Step 5: Commit.**
  ```bash
  git add test/RawRabbit.Operations.MessageSequence.Tests
  git commit -m "add wave 4 operations.messagesequence tests"
  ```

---

### Task 3: `RawRabbit.Operations.Publish` tests

**Files:**
- Create: 6 test files at `test/RawRabbit.Operations.Publish.Tests/<subdir>/<TypeUnderTest>Tests.cs` (per VP-5 + File Structure T3)

**Notes:** 3 `[Collection("LogProviderState")]` test classes (all 3 middleware tests). 2 broker-touching middleware (PublishAcknowledge, ReturnCallback) → inline `Mock<IModel>`. No namespace collision.

- [ ] **Step 1: Create directories.**
  ```bash
  mkdir -p test/RawRabbit.Operations.Publish.Tests/{Context,Middleware}
  ```

- [ ] **Step 2: Write all 6 test files** per File Structure T3. Apply `[Collection("LogProviderState")]` to all 3 middleware test classes.

- [ ] **Step 3: BOM verification.**

- [ ] **Step 4: Build + run.**
  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Operations.Publish.Tests --no-build -c Release 2>&1 | tail -1
  ```
  Expected: `Failed: 0, Passed: ~10-14, Skipped: 0`.

- [ ] **Step 5: Commit.**
  ```bash
  git add test/RawRabbit.Operations.Publish.Tests
  git commit -m "add wave 4 operations.publish tests"
  ```

---

### Task 4: `RawRabbit.Operations.Request` tests

**Files:**
- Create: 12 test files at `test/RawRabbit.Operations.Request.Tests/<subdir>/<TypeUnderTest>Tests.cs` (per VP-6 + File Structure T4)

**Notes:** 2 `[Collection("LogProviderState")]` test classes (ResponderException, ResponseConsume). No broker-touching middleware. No namespace collision. 2 interfaces + 1 constants file skipped.

- [ ] **Step 1: Create directories.**
  ```bash
  mkdir -p test/RawRabbit.Operations.Request.Tests/{Configuration,Context,Core,Middleware}
  ```

- [ ] **Step 2: Write all 12 test files** per File Structure T4. Apply `[Collection("LogProviderState")]` to ResponderExceptionMiddlewareTests + ResponseConsumeMiddlewareTests.

- [ ] **Step 3: BOM verification.**

- [ ] **Step 4: Build + run.**
  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Operations.Request.Tests --no-build -c Release 2>&1 | tail -1
  ```
  Expected: `Failed: 0, Passed: ~18-24, Skipped: 0`.

- [ ] **Step 5: Commit.**
  ```bash
  git add test/RawRabbit.Operations.Request.Tests
  git commit -m "add wave 4 operations.request tests"
  ```

---

### Task 5: `RawRabbit.Operations.Respond` tests

**Files:**
- Create: 16 test files at `test/RawRabbit.Operations.Respond.Tests/<subdir>/<TypeUnderTest>Tests.cs` (per VP-7 + File Structure T5)

**Notes:** LARGEST task. 1 `[Collection("LogProviderState")]` test class (ReplyToExtraction). 1 namespace-alias test file (Acknowledgement/RespondTests.cs uses `using AckRespond = RawRabbit.Operations.Respond.Acknowledgement.Respond;`). 1 broker-touching middleware via inheritance (RespondException). TypedAcknowlegement abstract base → no test file per DW14. 2 constants files skipped.

- [ ] **Step 1: Create directories.**
  ```bash
  mkdir -p test/RawRabbit.Operations.Respond.Tests/{Acknowledgement,Configuration,Context,Core,Middleware}
  ```

- [ ] **Step 2: Write all 16 test files** per File Structure T5. Apply `[Collection("LogProviderState")]` to ReplyToExtractionMiddlewareTests. Apply namespace alias to Acknowledgement/RespondTests.cs.

- [ ] **Step 3: BOM verification.**

- [ ] **Step 4: Build + run.**
  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Operations.Respond.Tests --no-build -c Release 2>&1 | tail -1
  ```
  Expected: `Failed: 0, Passed: ~25-32, Skipped: 0`.

- [ ] **Step 5: Commit.**
  ```bash
  git add test/RawRabbit.Operations.Respond.Tests
  git commit -m "add wave 4 operations.respond tests"
  ```

---

### Task 6: `RawRabbit.Operations.StateMachine` tests

**Files:**
- Create: 16 test files at `test/RawRabbit.Operations.StateMachine.Tests/<subdir>/<TypeUnderTest>Tests.cs` (per VP-8 + File Structure T6)

**Notes:** 1 `[Collection("LogProviderState")]` test class (Core/GlobalLockTests). No namespace collision. No broker-touching middleware. W4-R9 abstract `StateMachineBase<>` → file-local internal `TestStateMachine : StateMachineBase<int, string, TestModel>` subclass for testing. Stateless library is event-driven (no timer code per A15).

- [ ] **Step 1: Create directories.**
  ```bash
  mkdir -p test/RawRabbit.Operations.StateMachine.Tests/{Context,Core,Middleware,Trigger}
  ```

- [ ] **Step 2: Write all 16 test files** per File Structure T6. Apply `[Collection("LogProviderState")]` to Core/GlobalLockTests. For StateMachineBaseTests.cs, define file-local internal `TestStateMachine : StateMachineBase<int, string, TestModel>` and `TestModel : Model<int>` to enable abstract-base testing.

- [ ] **Step 3: BOM verification.**

- [ ] **Step 4: Build + run.**
  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Operations.StateMachine.Tests --no-build -c Release 2>&1 | tail -1
  ```
  Expected: `Failed: 0, Passed: ~20-26, Skipped: 0`.

- [ ] **Step 5: Commit.**
  ```bash
  git add test/RawRabbit.Operations.StateMachine.Tests
  git commit -m "add wave 4 operations.statemachine tests"
  ```

---

### Task 7: `RawRabbit.Operations.Subscribe` tests

**Files:**
- Create: 6 test files at `test/RawRabbit.Operations.Subscribe.Tests/<subdir>/<TypeUnderTest>Tests.cs` (per VP-9 + File Structure T7)

**Notes:** SMALLEST task. 2 `[Collection("LogProviderState")]` test classes (SubscriptionConfiguration, SubscriptionException). 1 broker-touching middleware (SubscriptionException via base ExceptionHandlingMiddleware) → inline `Mock<IModel>`. No namespace collision. 1 enum file skipped.

- [ ] **Step 1: Create directories.**
  ```bash
  mkdir -p test/RawRabbit.Operations.Subscribe.Tests/{Context,Middleware}
  ```

- [ ] **Step 2: Write all 6 test files** per File Structure T7. Apply `[Collection("LogProviderState")]` to SubscriptionConfiguration + SubscriptionException middleware tests.

- [ ] **Step 3: BOM verification.**

- [ ] **Step 4: Build + run.**
  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Operations.Subscribe.Tests --no-build -c Release 2>&1 | tail -1
  ```
  Expected: `Failed: 0, Passed: ~10-14, Skipped: 0`.

- [ ] **Step 5: Commit.**
  ```bash
  git add test/RawRabbit.Operations.Subscribe.Tests
  git commit -m "add wave 4 operations.subscribe tests"
  ```

---

### Task 8: `RawRabbit.Operations.Tools` tests

**Files:**
- Create: 11 test files at `test/RawRabbit.Operations.Tools.Tests/<subdir>/<TypeUnderTest>Tests.cs` (per VP-10 + File Structure T8 + W4-R2 plan-time decision: 9 separate per-extension files)

**Notes:** 0 LogProvider consumers. 0 broker-touching middleware. 9 IBusClient extensions per VP-21 pattern. 2 plain middleware. No namespace collision.

- [ ] **Step 1: Create directories.**
  ```bash
  mkdir -p test/RawRabbit.Operations.Tools.Tests/Middleware
  ```

- [ ] **Step 2: Write all 11 test files** per File Structure T8. Use VP-23 pattern `Mock<IBusClient>.Setup(b => b.InvokeAsync(...)).ReturnsAsync(mockContext)` for the 9 extension tests.

- [ ] **Step 3: BOM verification.**

- [ ] **Step 4: Build + run.**
  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Operations.Tools.Tests --no-build -c Release 2>&1 | tail -1
  ```
  Expected: `Failed: 0, Passed: ~14-22, Skipped: 0`.

- [ ] **Step 5: Commit.**
  ```bash
  git add test/RawRabbit.Operations.Tools.Tests
  git commit -m "add wave 4 operations.tools tests"
  ```

---

### Wave-wide acceptance verification (post-T8; no separately-numbered task per W4-D6)

Per spec §6 acceptance gates, verify after T8 ships:

- [ ] **Aggregate test count:** `dotnet test 2>&1 | tail -10` over all 11 test projects (3 stable + 8 Wave 4) → `Failed: 0` everywhere; aggregate Passed delta from baseline ≥80; aggregate Skipped = 15 + N where N ≤ 5.
- [ ] **Per-project verification:** each `dotnet test test/RawRabbit.Operations.<X>.Tests --no-build -c Release` returns expected count from per-task Step 4.
- [ ] **Build clean:** `dotnet build -c Release` → 0 errors; warning shape stable from Wave 3 close.
- [ ] **3-run stability:** repeat aggregate `dotnet test` 3× consecutively, all 3 reporting identical pass/fail/skip counts (race detector per spec §6.7 — Wave 3 caught the LogProvider race here).
- [ ] **Wave-wide review** dispatched after T8 — spec reviewer + code quality reviewer go over the entire wave's diff one last time before push (spec §6.8).
- [ ] **Skip budget gate:** total Wave 4 new skips N ≤ 5 (spec §4 ceiling).
- [ ] **Source-edit gate:** `git diff origin/2.0..HEAD --stat -- src/` returns empty (Wave 4 is test-only).
- [ ] **Push to origin/2.0** after wave-wide review passes.

---

## Tasks NOT in this plan

(Carries forward parent §12 + Wave 3 scope split)

- Per-test-method exact name enumeration — SDD execution per-task verification (W4-D3); plan locks per-source-class purpose-level only
- `src/RawRabbit.Operations.*/` source modifications — test-only wave (parent §1)
- `test/RawRabbit.IntegrationTests/*` cleanup — Phase 6 (live broker)
- Un-skipping the existing 15 skipped tests across the 3 stable projects — Phase 5/7
- Performance benchmarks — separate concern
- Wave 5 (Enrichers.*) brainstorm + spec/plan/SDD lifecycle — post-Wave-4 work
- BrokerMocks promotion to shared csproj — defer to Wave 5+ if duplication count justifies (W4-D2)
- Refactor of `LogProvider.For<T>()` static-read to lazy/injected — Phase 7 backlog from Wave 3
- Phase 5/6/7 work generally
- Testing constants-only files (`*Key.cs`) and static pipe-builder fields (`UntypedGetPipe` etc.) per W4-D8 (new wave-level decision; analogous to Wave 3 DW16)
- Testing abstract base `TypedAcknowlegement<TResponse>` directly per DW14 (covered indirectly via Ack/Nack/Reject concrete subclasses)

A new spec → new plan cycle is required to add any of the above to a future phase.

---

## Known issues inherited from spec

User-acknowledged on 2026-05-14 during the Wave 4 brainstorm — preserve verbatim:

1. **6 broker-touching middleware get inline mocks** — no shared helper across the 8 Operations.*.Tests projects (W4-D2). If Wave 5 needs the same, decide at Wave 5 brainstorm whether to promote `BrokerMocks` to a shared `test/RawRabbit.TestHelpers/` csproj.
2. **10 `LogProvider.For<T>()` consumers extend the LogProviderState xUnit collection** from Wave 3's 26 → ≥36 classes after Wave 4. Phase 7 backlog item: refactor `LogProvider.For` to lazy/injected (would eliminate workaround). Wave 4 inherits the workaround pattern.
3. **Carry-forward from Wave 3:** all Phase 7 backlog items (LogProvider lazy refactor, ResilientChannelPool double-enumeration, IChannelFactory lazy connection, Subscription.Dispose extension-cast smell, 3 ExplicitAckMiddleware skipped paths) remain open. None are Wave 4's responsibility.
4. **Skip budget N=0–5 is a ceiling**, not a target. Wave 4 enters with no a-priori-known skips (W4-R1 closed; no other timer/race patterns surfaced at spec-write).
5. **Test-count estimate is a range (80–120); only the lower bound (80) is the acceptance gate.** Plan-write verification (this plan) refined the per-project counts to ~14-32 each → wave total ~123-166, well above 80.

**New plan-time addition (W4-D8 — wave-level convention this plan introduces, analogous to Wave 3 DW16):**

6. **Constants-only files (`*Key.cs`, enum-only `*Stage.cs`) and static pipe-builder fields (`UntypedGetPipe = pipe => pipe.Use<X>()` and analogs)** are NOT tested in Wave 4. Testing them would be testing the spec encoded in the field. The IBusClient-extension method on the same class IS tested per F1=(a) (verifies `Mock<IBusClient>.InvokeAsync` invocation with the expected pipe action). User-approved at plan-write 2026-05-14.
