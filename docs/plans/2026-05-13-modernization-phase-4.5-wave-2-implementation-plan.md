# Modernization Phase 4.5 Wave 2 — Pure-Logic Coverage Implementation Plan

> **For agentic workers:** REQUIRED: Use `superpowers:subagent-driven-development` to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Source spec:** `docs/specs/2026-05-13-modernization-phase-4.5-wave-2-design.md` (commit SHA: `4b9477b641eea532cf074ef2807686cfe75be3d0`)

**Plan scope:** Wave 2 only — 8 sub-areas of pure-logic test coverage (DI Autofac, DI Ninject, DI ServiceCollection, Common gap-fill, Configuration root + 7 sub-types, Exceptions, Logging, Pipe). Waves 3–5 are independent per parent spec D5 / Wave 2 spec §1; will be planned separately.

**Goal:** Land Wave 2's pure-logic test coverage across the 8 sub-areas per spec §4 acceptance gates. ~52 new test files, ~278 net-new passing tests (vs spec gate `≥88` — 3.2× margin from strict-per-method enumeration). Zero source-code edits; zero csproj edits (Wave 1 wired all needed ProjectReferences).

**Architecture:** One test class per source class; one subdirectory per source area mirroring `src/` (per parent spec §3). Test layout = source layout. New tests use inline mocks/fixtures only (no shared test-helper assembly per spec DW7). Mock at `IPipeContext` boundary for area 8 Pipe extensions; otherwise direct construction (no broker boundary touched in Wave 2 — TopologyProvider deferred to Wave 3 per FD-Plan-2 below).

**Tech stack (inherited from spec):** .NET 10 SDK 10.0.107 + C# 13; xUnit 2.9.3; xunit.runner.visualstudio 2.8.2; Microsoft.NET.Test.Sdk 18.5.1; Moq 4.20.72. CPM via `Directory.Packages.props`. No new package dependencies for Wave 2.

---

## File Structure

**Create (~52 new test files; 0 source-code files; 0 csproj files):**

Task 1 — `test/RawRabbit.Tests/DependencyInjection/Autofac/`:
- `ContainerBuilderExtensionTests.cs` — 1 method × happy/error/options
- `ContainerBuilderAdapterTests.cs` — 5 methods × happy/error
- `ComponentContextAdapterTests.cs` — 1 static factory + 2 methods × happy/error

Task 2 — `test/RawRabbit.Tests/DependencyInjection/Ninject/`:
- `KernelExtensionTests.cs` — 1 method × happy/options/error
- `NinjectAdapterTests.cs` — 2 methods × happy/error
- `RawRabbitModuleTests.cs` — 1 Load override × resolves IDependencyResolver/IInstanceFactory/IBusClient

Task 3 — `test/RawRabbit.DependencyInjection.ServiceCollection.Tests/`:
- `AddRawRabbitExtensionTests.cs` — 1 method × happy/error/options-callback
- `ServiceCollectionAdapterTests.cs` — 7 methods × happy/error
- `ServiceProviderAdapterTests.cs` — 2 ctors + 2 GetService methods × happy/error

Task 4 — `test/RawRabbit.Tests/Common/` (7 NEW files alongside the 2 existing):
- `ClientPropertyProviderTests.cs` — `GetClientProperties` × cfg=null / cfg=set / property-shape
- `ExclusiveLockTests.cs` — 4 methods + ctor + Dispose
- `IDictionaryExtensionsTests.cs` — `GetOrDefault` × key-exists / key-missing
- `ResourceDisposerTests.cs` — `Dispose`, `ShutdownAsync(TimeSpan?)` (mock all 5 deps)
- `TaskUtilTests.cs` — 4 static methods × happy/with-exception
- `TruncatorTests.cs` — `Truncate` × under-254 / over-254
- `TypeExtensionsTests.cs` — `GetUserFriendlyName` × no-generic / 1-generic / multi-generic

Task 5 — `test/RawRabbit.Tests/Configuration/` (root + 7 sub-dirs):
- `RawRabbitConfigurationTests.cs` — ctor defaults + `Local` static + extension methods
- `BasicPublish/BasicPublishConfigurationBuilderTests.cs` + `…FactoryTests.cs`
- `Consume/ConsumeConfigurationBuilderTests.cs` + `…FactoryTests.cs` + `ConsumeConfigExtensionsTests.cs`
- `Consumer/ConsumerConfigurationBuilderTests.cs` + `…FactoryTests.cs`
- `ExchangeDeclare/ExchangeDeclarationBuilderTests.cs` + `ExchangeDeclarationFactoryTests.cs` + `ExchangeDeclarationTests.cs` + `ExchangeDeclarationExtensionsTests.cs`
- `Get/GetConfigurationBuilderTests.cs`
- `Publisher/PublisherConfigurationBuilderTests.cs` + `…FactoryTests.cs`
- `QueueDeclare/QueueDeclarationBuilderTests.cs` + `QueueDeclarationFactoryTests.cs` + `QueueDeclarationTests.cs` + `QueueDeclarationExtensionsTests.cs`

Task 6 — `test/RawRabbit.Tests/Exceptions/`:
- `ChannelAvailabilityExceptionTests.cs` — 1 ctor × message round-trip
- `MessageHandlerExceptionTests.cs` — 3 ctors + 3 props (`InnerExceptionType`, `InnerStackTrace`, `InnerMessage`)
- `PublishConfirmExceptionTests.cs` — 3 ctors

Task 7 — `test/RawRabbit.Tests/Logging/`:
- `LogProviderTests.cs` — `For<T>()` returns wrapped logger; `LoggerFactory` setter propagates
- `ILogConformanceTests.cs` — `ILog : ILogger` shape (`IsEnabled`, `Log`, `BeginScope`)
- `LogExtensionsTests.cs` — 24 extension methods × happy/error (per F1 (a) strict — FD-Plan-4)

Task 8 — `test/RawRabbit.Tests/Pipe/`:
- `PipeBuilderTests.cs` — 5 IPipeBuilder methods + `Build()`
- `PipeBuilderFactoryTests.cs` — `Create()` + `Create(Action<IPipeBuilder>)`
- `CachedPipeBuilderFactoryTests.cs` — `Create()` + `Create(Action)` + cache-hit
- `PipeContextExtensionTests.cs` — 23 typed `GetXxx` extensions × happy/missing-key (per F1 strict — FD-Plan-1)
- `PipeContextGetExtensionTests.cs` — `Get<T>` × key-found-typed / key-found-wrong-type / key-missing / null-context
- `AddPropertyPipeContextExtensionsTests.cs` — `UseConsumerConcurrency`, `UseConsumeSemaphore`, `UseThrottledConsume`
- `DictionaryExtensionsTests.cs` — `TryAdd`, `AddOrReplace` (defined in `IPipeContext.cs:15-35` but unenumerated in spec)

Task 9 — Wave 2 phase-acceptance verification (no edits)

**Modify:** none.

**Verify-only:**
- `RawRabbit.sln` unchanged across Wave 2 (no new projects scaffolded; Wave 1 already added 14 in commit `02cc56c`).
- Existing test files (`Common/ConnectionStringParserTests.cs`, `Common/NamingConventionsTests.cs`, etc.) unchanged.
- 9 skipped Phase-5/7-tagged tests remain skipped per parent spec D10.

---

## Inherited from spec

The following assumptions were verified by `thorough-brainstorming` at spec-write time (HEAD `9af8650` for spec; spec last modified at `4b9477b`) and are NOT re-verified here. Trusted as ground truth:

- A1 — 3 DI source dirs each have an entry-point `*Extension` class (Autofac → `ContainerBuilderExtension`; Ninject → `KernelExtension`; ServiceCollection → `AddRawRabbitExtension`)
- A2 — `src/RawRabbit/Common/` contains 11 untested files beyond `ConnectionStringParser` + `NamingConventions`
- A3 — `src/RawRabbit/Configuration/` has 7 sub-type subdirs (BasicPublish, Consume, Consumer, ExchangeDeclare, Get, Publisher, QueueDeclare) + root `RawRabbitConfiguration.cs`
- A4 — `src/RawRabbit/Exceptions/` has 4 files: 3 real exceptions + `ExceptionInformation` meta-type
- A5 — `src/RawRabbit/Logging/LibLog.cs` is the only Logging file (73 lines, Phase 3 shim); exposes `LogProvider` + `ILog` + `LogExtensions` with 24 forwarding methods
- A6 — `src/RawRabbit/Pipe/` has 9 root-level `.cs` files (excluding `Middleware/` which is Wave 3); spec lists `PipeBuilder.cs` + `PipeBuilderFactory.cs` + 3 extension files
- A7 — `test/RawRabbit.Tests/` does NOT yet have Wave 2 target subdirs (Channel/, Common/, Serialization/ exist; the rest do not)
- A8 — `test/RawRabbit.Tests/Common/` contains exactly 2 existing files (ConnectionStringParserTests, NamingConventionsTests)
- A9 — `test/RawRabbit.DependencyInjection.ServiceCollection.Tests/` has csproj wired (from Wave 1 `02cc56c`); no `.cs` files
- A10 — `RawRabbit.Tests.csproj` ProjectReferences: Autofac + Ninject + RawRabbit core (NOT ServiceCollection — D8 inconsistency preserved)
- A11 — `IPipeContext` interface exists with publicly-testable typed extension methods on it (NOTE: spec said 19; actual is 23 — see VP-15 below for plan-level correction)
- A12 — Nothing in `test/` references Wave-2 layout in a conflicting way
- A13 — No surprise source assemblies need Wave 2 coverage; cross-referenced against parent spec §1
- DW1 — DI sub-areas split: Autofac + Ninject under `test/RawRabbit.Tests/DependencyInjection/<provider>/`; ServiceCollection in its own Wave-1-scaffolded project
- DW2 — Configuration's 7 sub-types each get their own test subdirectory mirroring `src/` structure (8 subdirs total)
- DW3 — Exception ctor tests use `[Theory]` with `[InlineData]` per ctor signature where multiple ctor overloads share shape
- DW4 — Pipe sub-area scope = `PipeBuilder` + `PipeBuilderFactory` + IPipeContext extensions; `Pipe/Middleware/` deferred to Wave 3
- DW5 — Wave 1 lessons (BOM preservation, AAA-deletion style, `ThrowsAnyAsync<T>`) captured as Wave 2 conventions
- DW6 — Logging area kept in Wave 2 with strict per-method count target per CDR R1 F1 = (a)
- DW7 — Inline mocks/fixtures only; no shared test-helper assembly
- F1 (a) — Strict per-method, no consolidation, no exemption for forwarding extension methods (LibLog 24 + Pipe 23 GetXxx)
- D6 (parent) — `[Theory]/[InlineData]` required where 3+ similar tests differ only in input/expected
- D8 (parent) — One test project per separate source assembly; preserve existing `RawRabbit.Tests` ProjectReference inconsistency
- D10 (parent) — Skipped methods are NOT touched in Phase 4.5

---

## Verified plan-level assumptions

Newly introduced by this plan and verified empirically against HEAD `4b9477b` at plan-write time on 2026-05-13:

| # | Category | Assumption | Evidence |
|---|---|---|---|
| VP-1 | Cat 1 (file paths) | None of the 5 NEW test subdirs exist yet (`DependencyInjection/`, `Configuration/`, `Exceptions/`, `Logging/`, `Pipe/`) | `find test/RawRabbit.Tests -maxdepth 2 -type d` returned only Channel/, Common/, Serialization/, plus build artifact dirs |
| VP-2 | Cat 1 (file paths) | `test/RawRabbit.DependencyInjection.ServiceCollection.Tests/` exists with csproj wired + no `.cs` files | `ls test/RawRabbit.DependencyInjection.ServiceCollection.Tests/` returned only the csproj; csproj at lines 18-19 references `RawRabbit` core + `RawRabbit.DependencyInjection.ServiceCollection` |
| VP-3 | Cat 1 (file paths) | `test/RawRabbit.Tests/RawRabbit.Tests.csproj` has ProjectReferences to Autofac + Ninject (NOT ServiceCollection) | Read of csproj lines 14-18 confirms exactly 3 ProjectReference entries: RawRabbit.DependencyInjection.Autofac, RawRabbit.DependencyInjection.Ninject, RawRabbit core |
| VP-4 | Cat 2 (signature) | `ContainerBuilderExtension.RegisterRawRabbit(this ContainerBuilder, RawRabbitOptions = null)` returns `ContainerBuilder` | `src/RawRabbit.DependencyInjection.Autofac/ContainerBuilderExtension.cs:12-18` |
| VP-5 | Cat 2 (signature) | `ContainerBuilderAdapter` ctor takes `(ContainerBuilder builder)`; implements 5 `IDependencyRegister` methods (`AddTransient` × 2 overloads, `AddSingleton` × 3 overloads), all returning `IDependencyRegister` (this) | `src/RawRabbit.DependencyInjection.Autofac/ContainerBuilderAdapter.cs:7-60` |
| VP-6 | Cat 2 (signature) | `ComponentContextAdapter` exposes static `Create(IComponentContext)`, ctor `(IComponentContext)`, and 2 `GetService` overloads (`<TService>(params object[])`, `(Type, params object[])`) | `src/RawRabbit.DependencyInjection.Autofac/ComponentContextAdapter.cs:10-36` |
| VP-7 | Cat 2 (signature) | `KernelExtension.RegisterRawRabbit(this IKernel, RawRabbitOptions = null)` returns `IKernel`; binds `RawRabbitOptions` if non-null + Loads `RawRabbitModule` | `src/RawRabbit.DependencyInjection.Ninject/KernelExtension.cs:8-16` |
| VP-8 | Cat 2 (signature) | `NinjectAdapter` ctor takes `(IContext)`; implements 2 `GetService` overloads (`<TService>`, `(Type)`) | `src/RawRabbit.DependencyInjection.Ninject/NinjectAdapter.cs:10-31` |
| VP-9 | Cat 2 (signature) | `RawRabbitModule : NinjectModule`; overrides `Load()`; binds `IDependencyResolver` → `NinjectAdapter`, `IInstanceFactory` → `RawRabbitFactory.CreateInstanceFactory(...)` (singleton), `IBusClient` → factory.Create() | `src/RawRabbit.DependencyInjection.Ninject/RawRabbitModule.cs:8-25` |
| VP-10 | Cat 2 (signature) | `AddRawRabbitExtension.AddRawRabbit(this IServiceCollection, RawRabbitOptions = null)` returns `IServiceCollection`; invokes `options?.DependencyInjection?.Invoke(adapter)` | `src/RawRabbit.DependencyInjection.ServiceCollection/AddRawRabbitExtension.cs:6-15` |
| VP-11 | Cat 2 (signature) | `ServiceCollectionAdapter` exposes public `Collection { get; set; }`; ctor `(IServiceCollection)`; 7 `IDependencyRegister` methods (3 AddTransient overloads, 4 AddSingleton overloads), all returning `IDependencyRegister` (this) | `src/RawRabbit.DependencyInjection.ServiceCollection/ServiceCollectionAdapter.cs:6-56` |
| VP-12 | Cat 2 (signature) | `ServiceProviderAdapter` has 2 ctors (`(IServiceProvider)`, `(IServiceCollection)` — second self-binds via `BuildServiceProvider`) + 2 GetService overloads | `src/RawRabbit.DependencyInjection.ServiceCollection/ServiceProviderAdapter.cs:6-32` |
| VP-13 | Cat 2 (signature) | `ClientPropertyProvider.GetClientProperties(RawRabbitConfiguration cfg = null)` returns `IDictionary<string, object>` with 5 base keys (product, version, platform, client_directory, client_server) + 2 conditional (request_timeout, broker_username) when cfg ≠ null | `src/RawRabbit/Common/ClientPropertyProvider.cs:13-34` |
| VP-14 | Cat 2 (signature) + Cat 5 (lib quirk) | `ClientPropertyProvider:22` uses `Assembly.CodeBase` which is **OBSOLETE on .NET 5+** (compile-time warning CS0618; runtime returns the file:// URI for normal assemblies, may return null in single-file deployments) | `grep -rn "Assembly.CodeBase" src/RawRabbit/Common/ClientPropertyProvider.cs` confirms line 22; .NET docs: `Assembly.CodeBase` is `[Obsolete("Code Base is only included for .NET Framework compatibility.")]` |
| VP-15 | Cat 2 (signature) | `IExclusiveLock` has 4 methods (`AquireAsync`, `ReleaseAsync`, `Execute<T>`, `ExecuteAsync<T>`); `ExclusiveLock` implements all + `Dispose`; uses `LogProvider.For<ExclusiveLock>()` internally | `src/RawRabbit/Common/ExclusiveLock.cs:9-91` |
| VP-16 | Cat 2 (signature) | `IDictionaryExtensions.GetOrDefault<TKey,TValue>(this IDictionary<TKey,TValue>, TKey)` returns `TValue` (default if missing) | `src/RawRabbit/Common/IDictionaryExtensions.cs:5-11` |
| VP-17 | Cat 2 (signature) | `ResourceDisposer` ctor takes `(IChannelFactory, IConnectionFactory, ISubscriptionRepository, IChannelPoolFactory, RawRabbitConfiguration)`; methods `Dispose()` + `ShutdownAsync(TimeSpan? graceful = null)` | `src/RawRabbit/Common/ResourceDisposer.cs:24-55` |
| VP-18 | Cat 2 (signature) | `TaskUtil` has 4 public static methods: `FromCancelled<T>()`, `FromCancelled()`, `FromException(Exception)`, `FromException<T>(Exception)` | `src/RawRabbit/Common/TaskUtil.cs:8-30` |
| VP-19 | Cat 2 (signature) | `Truncator.Truncate(ref string name)` truncates to 254 chars with `"..." + last 250 chars` prefix when over | `src/RawRabbit/Common/Truncator.cs:5-11` |
| VP-20 | Cat 2 (signature) | `TypeExtensions.GetUserFriendlyName(this Type)` returns `"{Namespace}.{Name}[<generic args recursively>], {Assembly.Name}"` | `src/RawRabbit/Common/TypeExtensions.cs:8-26` |
| VP-21 | Cat 5 (scope decision) | `TopologyProvider` touches `IModel` (broker) directly via `_channel.QueueDeclare(...)`, `_channel.QueueBind(...)`, etc. — NOT pure-logic. Per FD-Plan-2 user decision: **defer to Wave 3** with rest of broker-mock cohort. NOT in this plan's area 4. | `src/RawRabbit/Common/TopologyProvider.cs:135, 150, 195, 217` show direct `IModel` API calls; `_channel = _channelFactory.CreateChannelAsync().GetAwaiter().GetResult()` at lines 315-318 |
| VP-22 | Cat 2 (signature) | `RawRabbitConfiguration` ctor sets exactly 11 properties to documented defaults (RequestTimeout=10s, PublishConfirmTimeout=1s, PersistentDeliveryMode=true, AutoCloseConnection=true, AutomaticRecovery=true, TopologyRecovery=true, RouteWithGlobalId=true, RecoveryInterval=10s, GracefulShutdown=10s, Ssl.Enabled=false, Hostnames=empty list); also initializes Exchange + Queue sub-configs | `src/RawRabbit/Configuration/RawRabbitConfiguration.cs:83-108` |
| VP-23 | Cat 2 (signature) | `RawRabbitConfiguration.Local` static returns config with VirtualHost="/", Username="guest", Password="guest", Port=5672, Hostnames=["localhost"] | `src/RawRabbit/Configuration/RawRabbitConfiguration.cs:110-117` |
| VP-24 | Cat 2 (signature) | `RawRabbitConfigurationExtensions` has 2 extension methods: `AsHighPerformance` (sets PersistentDeliveryMode=false, RouteWithGlobalId=false, Exchange.Type=Direct), `AsLegacy` (sets Exchange.Type=Direct, RouteWithGlobalId=false) | `src/RawRabbit/Configuration/RawRabbitConfiguration.cs:171-200` |
| VP-25 | Cat 2 (signature) | `BasicPublishConfigurationBuilder` has ctor `(BasicPublishConfiguration)` + 4 fluent methods: `OnExchange(string)`, `WithRoutingKey(string)`, `AsMandatory(bool=true)`, `WithProperties(Action<IBasicProperties>)` | `src/RawRabbit/Configuration/BasicPublish/BasicPublishConfigurationBuilder.cs:8-41` |
| VP-26 | Cat 2 (signature) | `BasicPublishConfigurationFactory` has 3 public Create overloads: `Create(object message)`, `Create(Type type)`, `Create()`; depends on `INamingConventions` + `ISerializer` + `RawRabbitConfiguration` | `src/RawRabbit/Configuration/BasicPublish/BasicPublishConfigurationFactory.cs:11-52` |
| VP-27 | Cat 2 (signature) | `ConsumeConfigurationBuilder` has 10 fluent methods: `OnExchange`, `FromQueue`, `WithNoAck`, `WithAutoAck`, `WithConsumerTag`, `WithRoutingKey`, `WithNoLocal`, `WithPrefetchCount`, `WithExclusive`, `WithArgument`; `OnExchange`/`FromQueue` call `Truncator.Truncate(ref ...)` | `src/RawRabbit/Configuration/Consume/ConsumeConfigurationBuilder.cs:7-81` |
| VP-28 | Cat 2 (signature) | `ConsumeConfigurationFactory` has 3 Create overloads; `ConsumeConfigExtensions.IsDirectReplyTo` returns true when `cfg.QueueName == "amq.rabbitmq.reply-to"` (case-insensitive) | `src/RawRabbit/Configuration/Consume/ConsumeConfigurationFactory.cs:7-40`; `src/RawRabbit/Configuration/Consume/ConsumeConfigExtensions.cs:6-12`; `DirectQueueName` = `internal static readonly` at `src/RawRabbit/Configuration/QueueDeclare/QueueDeclarationExtensions.cs:7` |
| VP-29 | Cat 2 (signature) | `ConsumerConfigurationBuilder` has 3 fluent methods: `OnDeclaredExchange(Action<IExchangeDeclarationBuilder>)`, `FromDeclaredQueue(Action<IQueueDeclarationBuilder>)`, `Consume(Action<IConsumeConfigurationBuilder>)`; nests sub-builders | `src/RawRabbit/Configuration/Consumer/ConsumerConfigurationBuilder.cs:8-50` |
| VP-30 | Cat 2 (signature) | `ConsumerConfigurationFactory` ctor takes 4 deps; 3 Create overloads | `src/RawRabbit/Configuration/Consumer/ConsumerConfigurationFactory.cs:9-47` |
| VP-31 | Cat 2 (signature) | `ExchangeDeclarationBuilder` has ctor `(ExchangeDeclaration = null)` (defaults to `ExchangeDeclaration.Default`) + 5 fluent methods: `WithName` (calls Truncator), `WithType`, `WithDurability`, `WithAutoDelete`, `WithArgument`; `ExchangeDeclaration` has 2 ctors + `Default` static + 5 props; `ExchangeDeclarationExtensions.IsDefaultExchange` returns true when `string.IsNullOrEmpty(declaration.Name)`; `ExchangeConfigurationFactory` (file misnamed — class is `ExchangeDeclarationFactory`) has 3 Create overloads | `src/RawRabbit/Configuration/ExchangeDeclare/ExchangeDeclarationBuilder.cs:5-44`; `ExchangeDeclaration.cs:5-31`; `ExchangeDeclarationExtensions.cs:3-10`; `ExchangeConfigurationFactory.cs:14-47` (note class name discrepancy with filename) |
| VP-32 | Cat 2 (signature) | `GetConfigurationBuilder` has ctor `(GetConfiguration = null)` + 3 fluent methods (`FromQueue`, `WithNoAck`, `WithAutoAck`) | `src/RawRabbit/Configuration/Get/GetConfigurationBuilder.cs:3-27` |
| VP-33 | Cat 2 (signature) | `PublisherConfigurationBuilder` has 6 fluent methods (`OnDeclaredExchange`, `WithReturnCallback`, `OnExchange`, `WithRoutingKey`, `AsMandatory`, `WithProperties`); the 4 inherited-shape methods return `IBasicPublishConfigurationBuilder` not `IPublisherConfigurationBuilder` (subtle return-type asymmetry); `PublisherConfigurationFactory` has 3 Create overloads | `src/RawRabbit/Configuration/Publisher/PublisherConfigurationBuilder.cs:11-66`; `PublisherConfigurationFactory.cs:8-48` |
| VP-34 | Cat 2 (signature) | `QueueDeclarationBuilder` has ctor `(QueueDeclaration = null)` (defaults to `QueueDeclaration.Default`) + 6 fluent methods; `QueueDeclaration` has 2 ctors + `Default` static + 5 props; `QueueDeclarationExtensions.IsDirectReplyTo` returns true when `queue.Name == "amq.rabbitmq.reply-to"`; `QueueDeclarationFactory` has 3 Create overloads | `src/RawRabbit/Configuration/QueueDeclare/QueueDeclarationBuilder.cs:5-51`; `QueueDeclaration.cs:5-26`; `QueueDeclarationExtensions.cs:5-13`; `QueueDeclarationFactory.cs:14-47` |
| VP-35 | Cat 2 (signature) | `ChannelAvailabilityException` has 1 ctor `(string message) : base(message)`; `MessageHandlerException` + `PublishConfirmException` each have 3 ctors: `()`, `(string message) : base(message)`, `(string message, Exception inner) : base(message, inner)`; `MessageHandlerException` has 3 settable string props (`InnerExceptionType`, `InnerStackTrace`, `InnerMessage`); `ExceptionInformation` is pure data (4 string props) — skip per spec FD3 | `src/RawRabbit/Exceptions/{ChannelAvailabilityException,MessageHandlerException,PublishConfirmException,ExceptionInformation}.cs` |
| VP-36 | Cat 2 (signature) | `LogProvider` (public static) exposes `LoggerFactory { get; set; } = NullLoggerFactory.Instance` and `For<T>() => new LogWrapper(LoggerFactory.CreateLogger<T>())`; `ILog : ILogger` (empty marker interface); `LogWrapper` is internal sealed (cannot test directly — `RawRabbit.Tests` is NOT in the InternalsVisibleTo list at LibLog.cs:8-17, only Operations.* + Enrichers.RetryLater + Enrichers.GlobalExecutionId) | `src/RawRabbit/Logging/LibLog.cs:23-41` |
| VP-37 | Cat 2 (signature) | `LogExtensions` defines exactly 24 public extension methods on `ILog`: 6 `Info/Debug/Warn/Error/Trace/Fatal(string, params object[])` + 6 `Info/Debug/…(Exception, string, params object[])` + 6 `InfoException/DebugException/…(string, Exception, params object[])` + 6 `IsInfoEnabled/IsDebugEnabled/…/IsFatalEnabled()` | `src/RawRabbit/Logging/LibLog.cs:43-72` |
| VP-38 | Cat 2 (signature) — DRIFT FROM SPEC | `PipeContextExtension` defines exactly **23** public typed `GetXxx` extensions (NOT 19 as spec A11 stated): GetMessage, GetMessageType, GetMessageContext, GetConsumer, GetQueueDeclaration, GetConsumeThrottleAction, GetExchangeDeclaration, GetReturnCallback, GetConsumeConfiguration, GetBasicPublishConfiguration, GetConsumerConfiguration, GetPublishConfiguration, GetRoutingKey, GetSubscription, GetChannel, GetTransientChannel, GetBasicProperties, GetDeliveryEventArgs, GetMessageHandler, **GetMessageHandlerArgs**, **GetMessageHandlerResult**, **GetMessageAcknowledgement**, **GetClientConfiguration**. The 4 emboldened extensions were missed in spec brainstorm. Plan uses 23. | Read of `src/RawRabbit/Pipe/PipeContextExtension.cs:18-134` line-by-line counted |
| VP-39 | Cat 2 (signature) | `PipeContextGetExtension.Get<TType>(this IPipeContext, string key, TType fallback = default)` returns fallback when `context?.Properties == null`, when key absent, or when value is wrong type | `src/RawRabbit/Pipe/PipeContextGetExtension.cs:5-17` |
| VP-40 | Cat 2 (signature) | `AddPropertyPipeContextExtensions` has 3 generic extensions on `TPipeContext : IPipeContext`: `UseConsumerConcurrency(uint)`, `UseConsumeSemaphore(SemaphoreSlim)`, `UseThrottledConsume(Action<Func<Task>, CancellationToken>)`; the first two delegate to the third | `src/RawRabbit/Pipe/AddPropertyPipeContextExtensions.cs:7-31` |
| VP-41 | Cat 2 (signature) — UNENUMERATED IN SPEC | `IPipeContext.cs:15-35` ALSO defines `public static class DictionaryExtensions` with 2 extensions on `IDictionary<TKey,TValue>`: `TryAdd` (returns false if key present) and `AddOrReplace` (removes-then-adds). NOT in spec area 8 enumeration. Plan tests these in `DictionaryExtensionsTests.cs` (Task 8). | `src/RawRabbit/Pipe/IPipeContext.cs:15-35` |
| VP-42 | Cat 2 (signature) | `PipeBuilder` exposes `IPipeBuilder` interface (5 methods: `Use(handler)`, `Use<TMW>(args)`, 2 `Replace<TC,TN>` overloads, `Remove<TMW>`) + `IExtendedPipeBuilder.Build()`; ctor takes `IDependencyResolver`; resolver also provides optional `Action<IPipeBuilder>` for additional configuration applied at Build time | `src/RawRabbit/Pipe/PipeBuilder.cs:11-137` |
| VP-43 | Cat 2 (signature) — UNENUMERATED IN SPEC | `src/RawRabbit/Pipe/PipeBuilderFactory.cs:35-56` defines **a SECOND class `CachedPipeBuilderFactory`** also implementing `IPipeBuilderFactory`; uses `ConcurrentDictionary` to cache built middleware by `Action<IPipeBuilder>` key. NOT in spec area 8 enumeration. Plan tests separately (Task 8). | Read of `PipeBuilderFactory.cs:35-56` |
| VP-44 | Cat 3 (build/test commands) | Build = `dotnet build -c Release`; targeted test = `dotnet test test/<project> --no-build -c Release [--filter "FullyQualifiedName~<class>"]` | Per Wave 1 plan VP-14 + Phase 4 baseline; same shape used throughout |
| VP-45 | Cat 3 (build/test commands) | `dotnet test test/RawRabbit.DependencyInjection.ServiceCollection.Tests --no-build -c Release` is the new project's test command | csproj at `test/RawRabbit.DependencyInjection.ServiceCollection.Tests/…csproj` is SDK-style; `dotnet test` auto-discovers any `.cs` file under any subdir per Wave 1 A15 |
| VP-46 | Cat 3 (commit conventions) | Lowercase imperative-mood commit message, no Conventional-Commits prefix (e.g., "Add Wave 2 …", "Add tests for …") | `git log --oneline -8` shows: "Fix Wave 2 spec Goal sentence …", "applied 9 fixes from …", "Amend Phase 4.5 spec …", "Add Phase 4.5 Wave 2 design spec …", "Restore BOM …", "Clean AAA comments …", "Clean A25 anti-patterns …" |
| VP-47 | Cat 3 (codebase config) | `.editorconfig` requires `indent_style = tab` for `[*]` (so `*.cs`); 2-space for csproj | `.editorconfig` lines 7-13 + 17 |
| VP-48 | Cat 3 (codebase config) | No active pre-commit hooks; no formatter modifies files at commit time | Inherited from Wave 1 plan VP-16 (no change since) |
| VP-49 | Cat 4 (task ordering) | All 9 tasks touch disjoint file sets: each Task 1-8 creates files only under its own subdir; Task 9 reads only (no edits). No inter-task dependencies among Tasks 1-8 — they can run in any order. Task 9 depends on all prior tasks completing. | By inspection of File Structure section above |
| VP-50 | Cat 5 (code-in-plan validity) | Wave 1 lessons apply: `Assert.ThrowsAnyAsync<T>` for subclass cancellations; AAA-deletion not blank-replacement; BOM preservation on first Edit/Write | Per spec §3 + DW5; Wave 1 commits `2de09ea` + `9af8650` set precedent |
| VP-51 | Cat 5 (code-in-plan validity) | New test files should have UTF-8 BOM (matches existing `test/RawRabbit.Tests/Common/{Connection,Naming}…Tests.cs` and most `src/` files); `Write` tool may strip BOMs on creation, so each Task's commit step ends with explicit BOM-restore via `printf '\xef\xbb\xbf' | cat - <file> > tmp && mv tmp <file>` per file | `file test/RawRabbit.Tests/Common/{Connection,Naming}…Tests.cs` returns "Unicode text, UTF-8 (with BOM)" — establishes convention |
| VP-52 | Cat 5 (code-in-plan validity) | `Mock<ILogger>` from Moq 4.20.72 supports verification of `Log<TState>(LogLevel, EventId, TState, Exception, Func<TState, Exception, string>)` via `It.IsAny<It.IsAnyType>()` for the generic state parameter; the standard pattern for Microsoft.Extensions.Logging mocking | Standard Moq+ILogger pattern; works on Microsoft.Extensions.Logging.Abstractions versions consumed by net10. Plan provides explicit pattern in Task 7 to avoid implementer trial-and-error |
| VP-53 | Cat 5 (code-in-plan validity) | `LogProvider.LoggerFactory` is mutable static (`get; set;`); tests must reset before/after to `NullLoggerFactory.Instance` to avoid cross-test pollution. Use `IDisposable` test-class fixture or per-test setup/teardown | `src/RawRabbit/Logging/LibLog.cs:25` confirms `public static ILoggerFactory LoggerFactory { get; set; }` |
| VP-54 | Cat 5 (code-in-plan validity) | `Pipe/IPipeContext.cs:1` does NOT have a UTF-8 BOM (per `file` output during enumeration); other Pipe files presumed similar; new test files match codebase convention (BOM, per VP-51) — source-file BOM state is independent | Established convention via VP-51 |

---

## Tasks

### Task 1: DI Autofac tests

**Files:**
- Create: `test/RawRabbit.Tests/DependencyInjection/Autofac/ContainerBuilderExtensionTests.cs`
- Create: `test/RawRabbit.Tests/DependencyInjection/Autofac/ContainerBuilderAdapterTests.cs`
- Create: `test/RawRabbit.Tests/DependencyInjection/Autofac/ComponentContextAdapterTests.cs`

Per F1 = (a): each public method gets ≥1 happy + ≥1 error test individually. Test class names = `<TypeUnderTest>Tests` (parent spec §3). Test method names = `Should_<Verb>_<Subject>` PascalCase with underscores.

- [ ] **Step 1: Create directory.**

  ```bash
  mkdir -p test/RawRabbit.Tests/DependencyInjection/Autofac
  ```

- [ ] **Step 2: Write `ContainerBuilderExtensionTests.cs`.** 3 tests covering the single `RegisterRawRabbit` extension:
  - `Should_Register_RawRabbit_And_Resolve_BusClient` — happy path; build container; resolve `IBusClient`; assert non-null
  - `Should_Honor_Custom_RawRabbitOptions` — pass non-default `RawRabbitOptions`; verify options reach the configuration
  - `Should_Throw_ArgumentNullException_When_Builder_Is_Null` — null `ContainerBuilder` → `ArgumentNullException` (extension method called via static syntax)

  Pattern:
  ```csharp
  using Autofac;
  using RawRabbit.DependencyInjection.Autofac;
  using RawRabbit.Instantiation;
  using Xunit;

  namespace RawRabbit.Tests.DependencyInjection.Autofac
  {
  	public class ContainerBuilderExtensionTests
  	{
  		[Fact]
  		public void Should_Register_RawRabbit_And_Resolve_BusClient()
  		{
  			var builder = new ContainerBuilder();

  			builder.RegisterRawRabbit();
  			var container = builder.Build();

  			Assert.NotNull(container.Resolve<IBusClient>());
  		}

  		// + 2 more tests (options-honored, null-builder)
  	}
  }
  ```

- [ ] **Step 3: Write `ContainerBuilderAdapterTests.cs`.** 10 tests covering all 5 `IDependencyRegister` methods (per VP-5):
  - `AddTransient<TS,TI>(Func)`: Should_Register_Transient_With_Factory + Should_Throw_When_Factory_Is_Null
  - `AddTransient<TS,TI>()`: Should_Register_Transient_Without_Factory + Should_Throw_When_Type_Resolution_Fails
  - `AddSingleton<TS>(TService)`: Should_Register_Singleton_Instance + Should_Throw_When_Instance_Is_Null
  - `AddSingleton<TS,TI>(Func)`: Should_Register_Singleton_With_Factory + Should_Throw_When_Factory_Is_Null
  - `AddSingleton<TS,TI>()`: Should_Register_Singleton_Without_Factory + Should_Throw_When_Type_Resolution_Fails

  Construct `new ContainerBuilderAdapter(new ContainerBuilder())`, call register method, build container, resolve, assert. For singleton lifetime: resolve twice, `Assert.Same`. For transient: resolve twice, `Assert.NotSame`.

- [ ] **Step 4: Write `ComponentContextAdapterTests.cs`.** 6 tests covering 1 static factory + 2 GetService overloads (per VP-6):
  - `Create` (static): Should_Create_Adapter_Wrapping_Context + Should_Throw_When_Context_Is_Null
  - `GetService<TService>`: Should_Resolve_Service_Generic + Should_Throw_When_Service_Not_Registered_Generic
  - `GetService(Type)`: Should_Resolve_Service_NonGeneric + Should_Throw_When_Service_Not_Registered_NonGeneric

  Construct an Autofac `IContainer` with a known service (e.g., a simple `class Foo { }`), wrap via `ComponentContextAdapter.Create(container)`, resolve and assert.

- [ ] **Step 5: Restore BOMs on the 3 new files.** Per VP-51 (Edit/Write may have stripped BOMs):

  ```bash
  for f in test/RawRabbit.Tests/DependencyInjection/Autofac/*.cs; do
    printf '\xef\xbb\xbf' | cat - "$f" > /tmp/_bom && mv /tmp/_bom "$f"
    file "$f"
  done
  # Each line should end "Unicode text, UTF-8 (with BOM) text"
  ```

- [ ] **Step 6: Build + run targeted tests.**

  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Tests --no-build -c Release --filter "FullyQualifiedName~RawRabbit.Tests.DependencyInjection.Autofac"
  ```

  Expected: 0 build errors. Targeted test count: 19 passed (3 + 10 + 6); 0 skipped; 0 failed.

- [ ] **Step 7: Commit.**

  ```bash
  git add test/RawRabbit.Tests/DependencyInjection/Autofac
  git commit -m "Add Wave 2 DI Autofac tests"
  ```

---

### Task 2: DI Ninject tests

**Files:**
- Create: `test/RawRabbit.Tests/DependencyInjection/Ninject/KernelExtensionTests.cs`
- Create: `test/RawRabbit.Tests/DependencyInjection/Ninject/NinjectAdapterTests.cs`
- Create: `test/RawRabbit.Tests/DependencyInjection/Ninject/RawRabbitModuleTests.cs`

- [ ] **Step 1: Create directory.**

  ```bash
  mkdir -p test/RawRabbit.Tests/DependencyInjection/Ninject
  ```

- [ ] **Step 2: Write `KernelExtensionTests.cs`.** 3 tests covering `RegisterRawRabbit` (per VP-7):
  - `Should_Register_RawRabbit_And_Resolve_BusClient`
  - `Should_Honor_Custom_RawRabbitOptions` — pass options; verify bound as constant
  - `Should_Throw_ArgumentNullException_When_Kernel_Is_Null`

  Use `new StandardKernel()` from Ninject; call extension; resolve; assert.

- [ ] **Step 3: Write `NinjectAdapterTests.cs`.** 4 tests covering 2 GetService overloads (per VP-8):
  - `GetService<T>`: Should_Resolve_Service_Generic + Should_Throw_When_Service_Not_Registered_Generic
  - `GetService(Type)`: Should_Resolve_Service_NonGeneric + Should_Throw_When_Service_Not_Registered_NonGeneric

  Construct via `new NinjectAdapter(<IContext>)`. For tests, build a `StandardKernel`, bind a known service, then create an `IContext` via `kernel.CreateRequest(...)` (or use kernel's internal context — verify pattern works with Ninject's actual API).

- [ ] **Step 4: Write `RawRabbitModuleTests.cs`.** 2 tests covering the `Load()` override (per VP-9):
  - `Should_Bind_IDependencyResolver_IInstanceFactory_IBusClient_When_Loaded` — happy: load module into kernel; resolve all 3 bindings
  - `Should_Throw_When_RawRabbitOptions_Not_Bound` — error: load module without binding `RawRabbitOptions` first; verify resolution of `IInstanceFactory` throws (since `context.Kernel.Get<RawRabbitOptions>()` will fail)

- [ ] **Step 5: Restore BOMs on the 3 new files.** Same pattern as Task 1 Step 5.

- [ ] **Step 6: Build + run targeted tests.**

  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Tests --no-build -c Release --filter "FullyQualifiedName~RawRabbit.Tests.DependencyInjection.Ninject"
  ```

  Expected: 0 build errors. Targeted test count: 9 passed (3 + 4 + 2); 0 skipped; 0 failed.

- [ ] **Step 7: Commit.**

  ```bash
  git add test/RawRabbit.Tests/DependencyInjection/Ninject
  git commit -m "Add Wave 2 DI Ninject tests"
  ```

---

### Task 3: DI ServiceCollection tests

**Files:**
- Create: `test/RawRabbit.DependencyInjection.ServiceCollection.Tests/AddRawRabbitExtensionTests.cs`
- Create: `test/RawRabbit.DependencyInjection.ServiceCollection.Tests/ServiceCollectionAdapterTests.cs`
- Create: `test/RawRabbit.DependencyInjection.ServiceCollection.Tests/ServiceProviderAdapterTests.cs`

This task lands tests in the Wave-1-scaffolded `RawRabbit.DependencyInjection.ServiceCollection.Tests` project (D8 inconsistency preserved). Csproj already wired (per VP-2).

- [ ] **Step 1: Write `AddRawRabbitExtensionTests.cs`.** 3 tests covering `AddRawRabbit` (per VP-10):
  - `Should_Register_RawRabbit_And_Resolve_BusClient` — happy
  - `Should_Invoke_Options_DependencyInjection_Callback` — verify `options.DependencyInjection?.Invoke(adapter)` callback is fired with the adapter
  - `Should_Throw_ArgumentNullException_When_Collection_Is_Null`

  Use `new ServiceCollection()`; resolve via `BuildServiceProvider().GetService<IBusClient>()`.

- [ ] **Step 2: Write `ServiceCollectionAdapterTests.cs`.** 14 tests covering 7 `IDependencyRegister` methods (per VP-11):
  - `AddTransient<TS,TI>()`: Should_Register_Transient_Without_Factory + Should_Throw_When_Type_Resolution_Fails
  - `AddTransient<TS>(Func)`: Should_Register_Transient_With_Factory + Should_Throw_When_Factory_Is_Null
  - `AddTransient<TS,TI>(Func)`: Should_Register_Transient_With_Factory_And_TI + Should_Throw_When_Factory_Is_Null_With_TI
  - `AddSingleton<TS>(TService instance)`: Should_Register_Singleton_Instance + Should_Throw_When_Instance_Is_Null
  - `AddSingleton<TS,TI>(Func)`: Should_Register_Singleton_With_Factory_TI + Should_Throw_When_Factory_Is_Null_TI
  - `AddSingleton<TS>(Func)`: Should_Register_Singleton_With_Factory + Should_Throw_When_Factory_Is_Null
  - `AddSingleton<TS,TI>()`: Should_Register_Singleton_Without_Factory + Should_Throw_When_Type_Resolution_Fails

- [ ] **Step 3: Write `ServiceProviderAdapterTests.cs`.** 5 tests covering 2 ctors + 2 GetService overloads (per VP-12):
  - `Should_Self_Register_When_Constructed_From_Collection` — second ctor registers self as `IDependencyResolver` singleton
  - `Should_Resolve_Service_Generic` + `Should_Activate_Service_Via_ActivatorUtilities_When_Not_Registered_Generic`
  - `Should_Resolve_Service_NonGeneric` + `Should_Handle_Null_Additional_Args` — `additional ?? new object[0]` path

- [ ] **Step 4: Restore BOMs on the 3 new files.** Same pattern as Task 1 Step 5 (path scope = `test/RawRabbit.DependencyInjection.ServiceCollection.Tests/*.cs`).

- [ ] **Step 5: Build + run the new project's tests.**

  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.DependencyInjection.ServiceCollection.Tests --no-build -c Release
  ```

  Expected: 0 build errors. Project test count: 22 passed (3 + 14 + 5); 0 skipped; 0 failed. (This is the project's first non-zero test count; spec §4 gate 5 requires ≥6 passed — landed with margin.)

- [ ] **Step 6: Commit.**

  ```bash
  git add test/RawRabbit.DependencyInjection.ServiceCollection.Tests
  git commit -m "Add Wave 2 DI ServiceCollection tests"
  ```

---

### Task 4: Common gap-fill tests (TopologyProvider deferred to Wave 3 per FD-Plan-2)

**Files:**
- Create: `test/RawRabbit.Tests/Common/ClientPropertyProviderTests.cs`
- Create: `test/RawRabbit.Tests/Common/ExclusiveLockTests.cs`
- Create: `test/RawRabbit.Tests/Common/IDictionaryExtensionsTests.cs`
- Create: `test/RawRabbit.Tests/Common/ResourceDisposerTests.cs`
- Create: `test/RawRabbit.Tests/Common/TaskUtilTests.cs`
- Create: `test/RawRabbit.Tests/Common/TruncatorTests.cs`
- Create: `test/RawRabbit.Tests/Common/TypeExtensionsTests.cs`

7 NEW files alongside the 2 existing in `test/RawRabbit.Tests/Common/`. Per VP-21: `TopologyProvider` is broker-touching (uses `IModel` API directly); deferred to Wave 3. Per spec §1 area 4 row: pure-data types (`Acknowledgement`, `PropertyHeaders`, `QueueArgument`) skipped — covered indirectly via consumers in other tests if at all.

- [ ] **Step 1: Write `ClientPropertyProviderTests.cs`.** 3 tests covering `GetClientProperties` (per VP-13 + VP-14):
  - `Should_Return_5_Base_Properties_When_Config_Null` — keys: product, version, platform, client_directory, client_server. Note per VP-14: `Assembly.CodeBase` is obsolete on .NET 5+. Test asserts presence of "client_directory" key but does NOT assert exact value (CodeBase may return null on single-file deploys; for normal test runs returns a `file://` URI). Use `Assert.Contains("client_directory", result.Keys)` not `Assert.Equal(expected, result["client_directory"])`.
  - `Should_Add_2_More_Properties_When_Config_Provided` — keys: request_timeout, broker_username
  - `Should_Throw_ArgumentNullException_When_Adding_Headers_Property_Twice` — error path when called sequentially with same instance? No — re-read: `GetClientProperties` returns a fresh `Dictionary<string, object>` each call. There is no error path. Adjust to: **`Should_Format_RequestTimeout_As_General_TimeSpan`** — verify `cfg.RequestTimeout.ToString("g")` format. Plan note: the strict-error path doesn't exist; for "error" coverage substitute property-shape verification (request_timeout format, broker_username equals cfg.Username).

- [ ] **Step 2: Write `ExclusiveLockTests.cs`.** 10 tests covering `ExclusiveLock` (per VP-15):
  - `AquireAsync(object, CancellationToken)`: Should_Acquire_Lock_For_New_Object + Should_Cancel_When_Token_Cancelled (use `Assert.ThrowsAnyAsync<OperationCanceledException>` per Wave 1 lesson)
  - `ReleaseAsync(object)`: Should_Release_Acquired_Lock + Should_Throw_When_Releasing_Unacquired_Lock — wait: `ReleaseAsync` does `semaphore.Release()` which throws `SemaphoreFullException` if released without prior acquire. Verify.
  - `Execute<T>(T, Action<T>, CancellationToken)`: Should_Execute_Action_Synchronously + Should_Log_Exception_Without_Throwing_When_Action_Throws (Execute catches exceptions and logs; verify via mock `ILog`? But `_logger = LogProvider.For<ExclusiveLock>()` — use `LogProvider.LoggerFactory` setter to install a captured-output factory; reset after)
  - `ExecuteAsync<T>(T, Func<T,Task>, CancellationToken)`: Should_ExecuteAsync_Async_Func + Should_Log_Exception_Without_Throwing_When_Func_Throws
  - `Dispose`: Should_Dispose_All_Semaphores + Should_Be_Idempotent_On_Multiple_Dispose

- [ ] **Step 3: Write `IDictionaryExtensionsTests.cs`.** 2 tests covering `GetOrDefault<TKey,TValue>` (per VP-16):
  - `Should_Return_Value_When_Key_Exists` — happy
  - `Should_Return_Default_When_Key_Missing` — error/edge

- [ ] **Step 4: Write `ResourceDisposerTests.cs`.** 4 tests covering `ResourceDisposer` (per VP-17):
  - `Dispose`: Should_Dispose_All_Owned_Resources — mock 5 deps; assert each `IDisposable` had `Dispose()` invoked; `IConnectionFactory`/`IChannelPoolFactory` are `as IDisposable`-cast, so use mocks that ALSO implement `IDisposable`
  - `Dispose`: Should_Skip_NonDisposable_Connection_Factory — mock without IDisposable; verify no throw
  - `ShutdownAsync(TimeSpan?)`: Should_Honor_Provided_Graceful_Timeout — pass `TimeSpan.Zero`; verify Task.Delay(Zero) completes immediately + Dispose called
  - `ShutdownAsync(TimeSpan?)`: Should_Use_Config_GracefulShutdown_When_Null — pass null; verify `_config.GracefulShutdown` is used; use small TimeSpan in fixture (e.g., `TimeSpan.FromMilliseconds(10)`) to keep test fast

  Construct test fixture: `new RawRabbitConfiguration { GracefulShutdown = TimeSpan.FromMilliseconds(10) }` + 4 mocks; assert via `.Verify(x => x.Dispose())`.

- [ ] **Step 5: Write `TaskUtilTests.cs`.** 8 tests covering 4 static methods (per VP-18):
  - `FromCancelled<T>()`: Should_Return_Cancelled_Task_Generic + Should_Have_TaskStatus_Canceled
  - `FromCancelled()`: Should_Return_Cancelled_Task_NonGeneric + Should_Have_TaskStatus_Canceled
  - `FromException(Exception)`: Should_Return_Faulted_Task_With_Exception + Should_Throw_Inner_When_Awaited
  - `FromException<T>(Exception)`: Should_Return_Faulted_Task_Generic + Should_Throw_Inner_When_Awaited

- [ ] **Step 6: Write `TruncatorTests.cs`.** 2 tests covering `Truncate(ref string)` (per VP-19):
  - `Should_Leave_String_Unchanged_When_Under_254_Chars` — happy
  - `Should_Truncate_With_Triple_Dot_Prefix_When_Over_254_Chars` — assert prefix `"..."`, total length 253 (3 dots + 250 chars from end)

- [ ] **Step 7: Write `TypeExtensionsTests.cs`.** 3 tests covering `GetUserFriendlyName(this Type)` (per VP-20):
  - `Should_Format_Non_Generic_Type` — e.g., `typeof(string)` → `"System.String, System.Private.CoreLib"`
  - `Should_Format_Single_Generic_Type` — e.g., `typeof(List<int>)` → `"System.Collections.Generic.List`1[[System.Int32, System.Private.CoreLib]], System.Private.CoreLib"`
  - `Should_Format_Multi_Generic_Type` — e.g., `typeof(Dictionary<string, int>)`

- [ ] **Step 8: Restore BOMs on the 7 new files.** Same pattern.

- [ ] **Step 9: Build + run targeted tests.**

  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Tests --no-build -c Release --filter "FullyQualifiedName~RawRabbit.Tests.Common"
  ```

  Expected: 0 build errors. Targeted test count: ~31 passed (3 + 10 + 2 + 4 + 8 + 2 + 3) plus the 2 pre-existing test classes (ConnectionStringParserTests + NamingConventionsTests at their post-Wave-1 row counts); 0 skipped; 0 failed.

- [ ] **Step 10: Commit.**

  ```bash
  git add test/RawRabbit.Tests/Common
  git commit -m "Add Wave 2 Common gap-fill tests (TopologyProvider deferred to Wave 3)"
  ```

---

### Task 5: Configuration tests (root + 7 sub-types)

**Files:**
- Create: `test/RawRabbit.Tests/Configuration/RawRabbitConfigurationTests.cs`
- Create: `test/RawRabbit.Tests/Configuration/BasicPublish/BasicPublishConfigurationBuilderTests.cs`
- Create: `test/RawRabbit.Tests/Configuration/BasicPublish/BasicPublishConfigurationFactoryTests.cs`
- Create: `test/RawRabbit.Tests/Configuration/Consume/ConsumeConfigurationBuilderTests.cs`
- Create: `test/RawRabbit.Tests/Configuration/Consume/ConsumeConfigurationFactoryTests.cs`
- Create: `test/RawRabbit.Tests/Configuration/Consume/ConsumeConfigExtensionsTests.cs`
- Create: `test/RawRabbit.Tests/Configuration/Consumer/ConsumerConfigurationBuilderTests.cs`
- Create: `test/RawRabbit.Tests/Configuration/Consumer/ConsumerConfigurationFactoryTests.cs`
- Create: `test/RawRabbit.Tests/Configuration/ExchangeDeclare/ExchangeDeclarationBuilderTests.cs`
- Create: `test/RawRabbit.Tests/Configuration/ExchangeDeclare/ExchangeDeclarationFactoryTests.cs`
- Create: `test/RawRabbit.Tests/Configuration/ExchangeDeclare/ExchangeDeclarationTests.cs`
- Create: `test/RawRabbit.Tests/Configuration/ExchangeDeclare/ExchangeDeclarationExtensionsTests.cs`
- Create: `test/RawRabbit.Tests/Configuration/Get/GetConfigurationBuilderTests.cs`
- Create: `test/RawRabbit.Tests/Configuration/Publisher/PublisherConfigurationBuilderTests.cs`
- Create: `test/RawRabbit.Tests/Configuration/Publisher/PublisherConfigurationFactoryTests.cs`
- Create: `test/RawRabbit.Tests/Configuration/QueueDeclare/QueueDeclarationBuilderTests.cs`
- Create: `test/RawRabbit.Tests/Configuration/QueueDeclare/QueueDeclarationFactoryTests.cs`
- Create: `test/RawRabbit.Tests/Configuration/QueueDeclare/QueueDeclarationTests.cs`
- Create: `test/RawRabbit.Tests/Configuration/QueueDeclare/QueueDeclarationExtensionsTests.cs`

**Per FD-Plan-3:** Builder fluent setters consolidated via `[Theory]` per builder where 3+ methods share shape (parent spec D6); factory and root tests use `[Fact]` per case.

- [ ] **Step 1: Create directory tree.**

  ```bash
  mkdir -p test/RawRabbit.Tests/Configuration/{BasicPublish,Consume,Consumer,ExchangeDeclare,Get,Publisher,QueueDeclare}
  ```

- [ ] **Step 2: `RawRabbitConfigurationTests.cs`** — 5 tests covering ctor defaults + `Local` + 2 extensions (per VP-22, VP-23, VP-24):
  - `Should_Initialize_Default_Values_In_Ctor` — single test asserting all 11 ctor-set properties
  - `Should_Return_Local_Config_With_Guest_Credentials` — `Local` static
  - `Should_Configure_AsHighPerformance` — verify `PersistentDeliveryMode=false`, `RouteWithGlobalId=false`, `Exchange.Type=Direct`
  - `Should_Configure_AsLegacy` — verify `Exchange.Type=Direct`, `RouteWithGlobalId=false`
  - `Should_Throw_NullReferenceException_When_AsHighPerformance_Called_On_Null` — error path

- [ ] **Step 3: `BasicPublish/BasicPublishConfigurationBuilderTests.cs`** — 3-4 tests via `[Theory]` per FD-Plan-3 (per VP-25). 1 `[Theory]` consolidating the 4 fluent setters (OnExchange, WithRoutingKey, AsMandatory, WithProperties) — but `WithProperties` takes an `Action<IBasicProperties>` so it doesn't share shape; split it out:
  - `[Theory]` cluster: 3 rows for OnExchange + WithRoutingKey + AsMandatory (each parametric over (method-name-key, value, expected-property))
  - `[Fact]` for `WithProperties` happy: invoke action; verify props set
  - `[Fact]` for `WithProperties` null-action: should not throw (null-coalescing in source line 38)
  - `[Fact]` for fluent chain: chain all 4; verify final config

  Consider 1-2 [Theory] usage opportunities per builder; aim for ≥1 [Theory] in this Task overall to satisfy spec §4 gate 8.

- [ ] **Step 4: `BasicPublish/BasicPublishConfigurationFactoryTests.cs`** — 7-8 tests covering 3 Create overloads (per VP-26):
  - `Create(object)`: Should_Create_From_Object_With_Body + Should_Fall_Through_To_Create_When_Object_Null
  - `Create(Type)`: Should_Create_From_Type_With_RoutingKey + Should_Throw_When_Type_Is_Null
  - `Create()`: Should_Create_With_Empty_BasicProperties + Should_Set_BasicProperties_Type_From_Type_Name + Should_Set_DeliveryMode_Per_Config_PersistentDelivery + Should_Set_UserId_From_Config_Username

  Mock `INamingConventions` + `ISerializer`; construct `new RawRabbitConfiguration()` directly. Verify via `Assert.Equal` on properties.

- [ ] **Step 5: `Consume/ConsumeConfigurationBuilderTests.cs`** — 4-6 tests via `[Theory]` per FD-Plan-3 (per VP-27). 10 fluent methods consolidate into 1-2 `[Theory]` clusters:
  - `[Theory]` cluster A: 5-6 rows for simple-setter methods (`WithNoAck`, `WithAutoAck`, `WithConsumerTag`, `WithRoutingKey`, `WithNoLocal`, `WithPrefetchCount`, `WithExclusive`)
  - `[Fact]` for `OnExchange` (calls Truncator + sets ExistingExchange flag)
  - `[Fact]` for `FromQueue` (calls Truncator + sets ExistingQueue flag)
  - `[Fact]` for `WithArgument` (initializes Arguments dict if null; uses TryAdd from `IPipeContext.cs` extension)
  - `[Fact]` for fluent chain

- [ ] **Step 6: `Consume/ConsumeConfigurationFactoryTests.cs`** — 4-6 tests covering 3 Create overloads (per VP-28):
  - `Create<TMessage>()` + `Create(Type)`: Should_Use_Naming_Conventions + Should_Generate_GUID_ConsumerTag
  - `Create(string, string, string)`: Should_Set_All_Three_From_Args + Should_Initialize_Empty_Arguments

- [ ] **Step 7: `Consume/ConsumeConfigExtensionsTests.cs`** — 2 tests:
  - `IsDirectReplyTo`: Should_Return_True_When_QueueName_Is_DirectReplyTo + Should_Return_False_For_Other_QueueNames

- [ ] **Step 8: `Consumer/ConsumerConfigurationBuilderTests.cs`** — 6 tests covering 3 fluent methods (per VP-29). Each method takes a sub-builder Action; nested mocking. Use `[Fact]` (don't share shape):
  - `OnDeclaredExchange`: Should_Build_Exchange + Should_Update_Consume_ExchangeName
  - `FromDeclaredQueue`: Should_Build_Queue + Should_Update_Consume_QueueName
  - `Consume`: Should_Build_Consume + Should_Null_Exchange_When_ExistingExchange_True + Should_Null_Queue_When_ExistingQueue_True

- [ ] **Step 9: `Consumer/ConsumerConfigurationFactoryTests.cs`** — 4-6 tests covering 3 Create overloads (per VP-30):
  - `Create<TMessage>()` + `Create(Type)`: Should_Use_Conventions
  - `Create(string, string, string)`: Should_Compose_From_3_Sub_Factories — mock `IQueueConfigurationFactory` + `IExchangeDeclarationFactory` + `IConsumeConfigurationFactory`; verify each `.Create(...)` is invoked with correct args and results composed into `ConsumerConfiguration`

- [ ] **Step 10: `ExchangeDeclare/` (4 files)** per VP-31:
  - `ExchangeDeclarationBuilderTests.cs`: 5-6 tests; 1 `[Theory]` for the 4 simple setters (WithType, WithDurability, WithAutoDelete, WithArgument); `[Fact]` for `WithName` (Truncator); `[Fact]` for ctor with null defaults to `ExchangeDeclaration.Default`
  - `ExchangeDeclarationFactoryTests.cs` (note: file is `ExchangeConfigurationFactory.cs` but class is `ExchangeDeclarationFactory`; test class name follows the class): 4-5 tests covering 3 Create overloads
  - `ExchangeDeclarationTests.cs`: 3 tests — default ctor + ctor from GeneralExchangeConfiguration + `Default` static
  - `ExchangeDeclarationExtensionsTests.cs`: 2 tests — `IsDefaultExchange` true for empty name + false otherwise

- [ ] **Step 11: `Get/GetConfigurationBuilderTests.cs`** — 4 tests covering 3 fluent methods + ctor defaults (per VP-32):
  - `[Fact]` for ctor: Should_Default_To_New_Configuration_When_Null
  - `[Fact]` for `FromQueue`
  - `[Fact]` for `WithNoAck` (delegates to WithAutoAck)
  - `[Fact]` for `WithAutoAck`

- [ ] **Step 12: `Publisher/PublisherConfigurationBuilderTests.cs`** — 8-10 tests covering 6 fluent methods (per VP-33). The 4 inherited-shape methods (OnExchange, WithRoutingKey, AsMandatory, WithProperties) override the BasicPublish builder's behavior with subtle differences:
  - `OnDeclaredExchange`: Should_Build_Exchange + Should_Update_ExchangeName
  - `WithReturnCallback`: Should_Compose_Callback + Should_Force_Mandatory_True + Should_Append_To_Existing_Callback
  - `OnExchange`: Should_Null_Exchange_And_Set_ExchangeName (subtle override behavior)
  - `WithRoutingKey`, `AsMandatory`: Should_Set_<Property> (1 [Theory] for these two)
  - `WithProperties`: Should_Initialize_When_Null + Should_Apply_Action

- [ ] **Step 13: `Publisher/PublisherConfigurationFactoryTests.cs`** — 6 tests covering 3 Create overloads:
  - `Create<TMessage>()` + `Create(Type)`: Should_Compose_BasicPublish_And_Exchange_Factories + Should_Carry_Through_BasicProperties_Body_Mandatory
  - `Create(string, string)`: Should_Set_ExchangeName_RoutingKey + Should_Initialize_Empty_BasicProperties

- [ ] **Step 14: `QueueDeclare/` (4 files)** per VP-34:
  - `QueueDeclarationBuilderTests.cs`: 6-7 tests; 1 `[Theory]` for the 4 simple setters (WithAutoDelete, WithDurability, WithExclusivity, WithArgument); `[Fact]` for `WithName` (Truncator); `[Fact]` for `WithNameSuffix`; `[Fact]` for ctor null defaults
  - `QueueDeclarationFactoryTests.cs`: 4-5 tests covering 3 Create overloads
  - `QueueDeclarationTests.cs`: 3 tests — default ctor + ctor from GeneralQueueConfiguration + `Default` static
  - `QueueDeclarationExtensionsTests.cs`: 2 tests — `IsDirectReplyTo` true for "amq.rabbitmq.reply-to" + false otherwise

- [ ] **Step 15: Restore BOMs on all new files.**

  ```bash
  for f in $(find test/RawRabbit.Tests/Configuration -name '*.cs'); do
    printf '\xef\xbb\xbf' | cat - "$f" > /tmp/_bom && mv /tmp/_bom "$f"
  done
  find test/RawRabbit.Tests/Configuration -name '*.cs' -exec file {} \; | grep -v "with BOM" | head -3
  # Expect: empty (all files have BOM)
  ```

- [ ] **Step 16: Build + run targeted tests.**

  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Tests --no-build -c Release --filter "FullyQualifiedName~RawRabbit.Tests.Configuration"
  ```

  Expected: 0 build errors. Targeted test count: ~55 passed (5 + ~7 BasicPublish + ~14 Consume + ~10 Consumer + ~14 ExchangeDeclare + 4 Get + ~14 Publisher + ~16 QueueDeclare; Theory rows count individually); 0 skipped; 0 failed.

- [ ] **Step 17: Commit.**

  ```bash
  git add test/RawRabbit.Tests/Configuration
  git commit -m "Add Wave 2 Configuration tests (root + 7 sub-types)"
  ```

---

### Task 6: Exceptions tests

**Files:**
- Create: `test/RawRabbit.Tests/Exceptions/ChannelAvailabilityExceptionTests.cs`
- Create: `test/RawRabbit.Tests/Exceptions/MessageHandlerExceptionTests.cs`
- Create: `test/RawRabbit.Tests/Exceptions/PublishConfirmExceptionTests.cs`

`ExceptionInformation` skipped — pure-data type per spec FD3 + VP-35.

- [ ] **Step 1: Create directory.**

  ```bash
  mkdir -p test/RawRabbit.Tests/Exceptions
  ```

- [ ] **Step 2: Write `ChannelAvailabilityExceptionTests.cs`.** 2 tests covering 1 ctor (per VP-35):
  - `Should_Set_Message_From_Ctor_Argument` — happy
  - `Should_Throw_When_Catch_Block_Catches_The_Exception` — round-trip via try/catch (verifies it inherits Exception correctly)

- [ ] **Step 3: Write `MessageHandlerExceptionTests.cs`.** 4 tests covering 3 ctors + 3 props (per VP-35 + DW3):
  - `Should_Be_Constructible_With_Default_Ctor` — `new MessageHandlerException()`; assert message null + inner null
  - `[Theory]` 2 rows for `(string)` and `(string, Exception)` ctors — assert message + inner correctly set
  - `Should_Round_Trip_Inner_Properties` — set `InnerExceptionType`, `InnerStackTrace`, `InnerMessage`; read back

- [ ] **Step 4: Write `PublishConfirmExceptionTests.cs`.** 4 tests covering 3 ctors (per VP-35 + DW3):
  - `Should_Be_Constructible_With_Default_Ctor`
  - `[Theory]` 2 rows for `(string)` and `(string, Exception)` ctors — same shape as MessageHandlerException
  - Reuses the `[Theory]` pattern; spec §4 gate 8 is satisfied via Configuration's [Theory] uses + this one

- [ ] **Step 5: Restore BOMs on the 3 new files.**

- [ ] **Step 6: Build + run targeted tests.**

  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Tests --no-build -c Release --filter "FullyQualifiedName~RawRabbit.Tests.Exceptions"
  ```

  Expected: 0 build errors. Targeted test count: 10 passed (2 + 4 + 4 — Theory rows count individually); 0 skipped; 0 failed.

- [ ] **Step 7: Commit.**

  ```bash
  git add test/RawRabbit.Tests/Exceptions
  git commit -m "Add Wave 2 Exceptions tests"
  ```

---

### Task 7: Logging tests (LibLog + 24 forwarding extensions per F1 (a) strict)

**Files:**
- Create: `test/RawRabbit.Tests/Logging/LogProviderTests.cs`
- Create: `test/RawRabbit.Tests/Logging/ILogConformanceTests.cs`
- Create: `test/RawRabbit.Tests/Logging/LogExtensionsTests.cs`

Per FD-Plan-4 (F1 strict literal): each of 24 LibLog extension methods gets ≥1 happy + ≥1 error test (≥48 tests). Per VP-53: `LogProvider.LoggerFactory` is mutable static — tests use `IDisposable` test-class fixture or per-test setup/teardown to avoid pollution.

- [ ] **Step 1: Create directory.**

  ```bash
  mkdir -p test/RawRabbit.Tests/Logging
  ```

- [ ] **Step 2: Write `LogProviderTests.cs`.** 3 tests covering `LogProvider` (per VP-36):
  - `Should_Return_Wrapped_Logger_From_For_T` — `LogProvider.For<MyType>()` returns a non-null `ILog`
  - `Should_Use_Set_LoggerFactory_When_Replaced` — set `LogProvider.LoggerFactory = mockFactory.Object`; call `For<T>()`; verify mockFactory's `CreateLogger<T>` was invoked
  - `Should_Default_To_NullLoggerFactory_Instance` — at static init time `LoggerFactory = NullLoggerFactory.Instance`; verify after reset

  Tests implement `IDisposable` to reset `LogProvider.LoggerFactory = NullLoggerFactory.Instance` after each test. Pattern:
  ```csharp
  public class LogProviderTests : IDisposable
  {
  	public LogProviderTests() => LogProvider.LoggerFactory = NullLoggerFactory.Instance;
  	public void Dispose() => LogProvider.LoggerFactory = NullLoggerFactory.Instance;
  	// tests …
  }
  ```

- [ ] **Step 3: Write `ILogConformanceTests.cs`.** 1 test verifying the marker interface contract (per VP-36):
  - `Should_Confirm_ILog_Inherits_From_ILogger` — `Assert.True(typeof(ILogger).IsAssignableFrom(typeof(ILog)))`

- [ ] **Step 4: Write `LogExtensionsTests.cs`.** 48 tests covering 24 extension methods × happy + error per F1 (a) strict (per VP-37). Mock `ILogger`; use `LogProvider.LoggerFactory` setter to wire a mockFactory that returns the mock; call extension; verify the underlying `LogXxx` was invoked.

  Mock `ILogger` pattern (per VP-52):
  ```csharp
  var mockLogger = new Mock<ILogger>();
  // Verify Log<It.IsAnyType>(level, eventId, state, exception, formatter)
  mockLogger.Verify(l => l.Log(
  	LogLevel.Information,
  	It.IsAny<EventId>(),
  	It.IsAny<It.IsAnyType>(),
  	It.IsAny<Exception>(),
  	It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
  ```

  Test method names per the 24 methods (4 shapes × 6 levels):
  - `Should_Log_<Level>_With_Message` × 6 happy + `Should_Throw_When_Logger_Is_Null_<Level>_Message` × 6 error
  - `Should_Log_<Level>_With_Exception_And_Message` × 6 happy + `Should_Throw_When_Logger_Is_Null_<Level>_Exception_Message` × 6 error
  - `Should_Log_<Level>Exception_With_Message_And_Exception` × 6 happy + `Should_Throw_When_Logger_Is_Null_<Level>Exception` × 6 error
  - `Should_Return_Bool_For_Is<Level>Enabled` × 6 happy + `Should_Throw_When_Logger_Is_Null_Is<Level>Enabled` × 6 error

  Total: 48 tests (24 happy + 24 error). Test class implements IDisposable for LoggerFactory cleanup.

  Optional consolidation: organize as 6 sub-classes per log level (1 file per level) OR one file with 6 nested classes — **plan keeps single file per VP-1 file structure**; nested test classes are an implementer-choice for organization.

- [ ] **Step 5: Restore BOMs on the 3 new files.**

- [ ] **Step 6: Build + run targeted tests.**

  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Tests --no-build -c Release --filter "FullyQualifiedName~RawRabbit.Tests.Logging"
  ```

  Expected: 0 build errors. Targeted test count: 52 passed (3 + 1 + 48); 0 skipped; 0 failed.

- [ ] **Step 7: Commit.**

  ```bash
  git add test/RawRabbit.Tests/Logging
  git commit -m "Add Wave 2 Logging tests (LibLog + 24 forwarding extensions per F1 strict)"
  ```

---

### Task 8: Pipe tests

**Files:**
- Create: `test/RawRabbit.Tests/Pipe/PipeBuilderTests.cs`
- Create: `test/RawRabbit.Tests/Pipe/PipeBuilderFactoryTests.cs`
- Create: `test/RawRabbit.Tests/Pipe/CachedPipeBuilderFactoryTests.cs`
- Create: `test/RawRabbit.Tests/Pipe/PipeContextExtensionTests.cs`
- Create: `test/RawRabbit.Tests/Pipe/PipeContextGetExtensionTests.cs`
- Create: `test/RawRabbit.Tests/Pipe/AddPropertyPipeContextExtensionsTests.cs`
- Create: `test/RawRabbit.Tests/Pipe/DictionaryExtensionsTests.cs`

Per FD-Plan-1 (F1 strict per-method): each of 23 GetXxx extensions gets ≥1 happy + ≥1 error test (≥46 tests just for `PipeContextExtensionTests.cs`).

- [ ] **Step 1: Create directory.**

  ```bash
  mkdir -p test/RawRabbit.Tests/Pipe
  ```

- [ ] **Step 2: Write `PipeBuilderTests.cs`.** 12 tests covering 5 IPipeBuilder methods + Build (per VP-42):
  - `Use(Func)`: Should_Wrap_Handler_In_UseHandlerMiddleware + Should_Throw_When_Handler_Is_Null
  - `Use<TMW>(args)`: Should_Add_Middleware_With_Args + Should_Throw_When_Resolver_Cannot_Resolve_Type
  - `Replace<TC,TN>(predicate, args)`: Should_Replace_Matching_Middleware_With_Args + Should_Skip_When_No_Match
  - `Replace<TC,TN>(predicate, argsFunc)`: Should_Replace_Via_ArgsFunc + Should_Use_Null_ArgsFunc_When_Not_Provided
  - `Remove<TMW>(predicate)`: Should_Remove_Matching_Middleware + Should_Skip_When_No_Match
  - `Build()`: Should_Sort_Staged_Middleware_By_Marker + Should_Wrap_With_Cancellation_And_NoOp_Middleware

  Mock `IDependencyResolver`; resolver.GetService for `Action<IPipeBuilder>` returns null (default) or non-null per test.

- [ ] **Step 3: Write `PipeBuilderFactoryTests.cs`.** 4 tests:
  - `Create()`: Should_Return_PipeBuilder + Should_Pass_Resolver
  - `Create(Action<IPipeBuilder>)`: Should_Apply_Action_And_Build + Should_Throw_When_Action_Is_Null

- [ ] **Step 4: Write `CachedPipeBuilderFactoryTests.cs`.** 4 tests covering caching behavior (per VP-43):
  - `Create()`: Should_Delegate_To_Fallback (returns IExtendedPipeBuilder)
  - `Create(Action)` first call: Should_Build_And_Cache
  - `Create(Action)` cache hit: Should_Return_Cached_Instance_For_Same_Action_Reference
  - `Create(Action)` distinct actions: Should_Build_Separately_For_Different_Action_References

- [ ] **Step 5: Write `PipeContextExtensionTests.cs`.** 46 tests covering 23 GetXxx extensions × happy + error per F1 strict (per VP-38). Mock `IPipeContext` returning a `Dictionary<string, object>` for `Properties`. Pattern:
  ```csharp
  [Fact]
  public void Should_Return_Stored_Message_From_GetMessage()
  {
  	var expected = new MyMessage();
  	var ctx = new Mock<IPipeContext>();
  	ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object> { [PipeKey.Message] = expected });

  	var actual = ctx.Object.GetMessage();

  	Assert.Same(expected, actual);
  }

  [Fact]
  public void Should_Return_Default_When_Message_Not_Set()
  {
  	var ctx = new Mock<IPipeContext>();
  	ctx.Setup(c => c.Properties).Returns(new Dictionary<string, object>());

  	var actual = ctx.Object.GetMessage();

  	Assert.Null(actual);
  }
  ```

  Test method names per extension: `Should_Return_Stored_<Subject>_From_<MethodName>` + `Should_Return_Default_When_<Subject>_Not_Set` for all 23 extensions:
  GetMessage, GetMessageType, GetMessageContext, GetConsumer, GetQueueDeclaration, GetConsumeThrottleAction, GetExchangeDeclaration, GetReturnCallback, GetConsumeConfiguration, GetBasicPublishConfiguration, GetConsumerConfiguration, GetPublishConfiguration, GetRoutingKey, GetSubscription, GetChannel, GetTransientChannel, GetBasicProperties, GetDeliveryEventArgs, GetMessageHandler, GetMessageHandlerArgs, GetMessageHandlerResult, GetMessageAcknowledgement, GetClientConfiguration.

  Note: `GetConsumeThrottleAction` has a non-default fallback `(func, token) => func()` — the "default" test verifies this fallback is returned (not null).

- [ ] **Step 6: Write `PipeContextGetExtensionTests.cs`.** 5 tests covering `Get<T>` (per VP-39):
  - `Should_Return_Value_When_Key_Found_And_Type_Matches`
  - `Should_Return_Fallback_When_Key_Found_But_Type_Mismatch`
  - `Should_Return_Fallback_When_Key_Missing`
  - `Should_Return_Fallback_When_Properties_Null`
  - `Should_Return_Fallback_When_Context_Null`

- [ ] **Step 7: Write `AddPropertyPipeContextExtensionsTests.cs`.** 6 tests covering 3 extensions (per VP-40):
  - `UseConsumerConcurrency(uint)`: Should_Add_Throttle_Action_To_Properties + Should_Throw_ArgumentOutOfRange_When_Concurrency_Zero
  - `UseConsumeSemaphore(SemaphoreSlim)`: Should_Add_Throttle_Action_From_Semaphore + Should_Throw_When_Semaphore_Is_Null
  - `UseThrottledConsume(Action)`: Should_Add_Throttle_Action_Directly + Should_Skip_When_Key_Already_Present (TryAdd returns false)

- [ ] **Step 8: Write `DictionaryExtensionsTests.cs`.** 4 tests covering 2 extensions (per VP-41):
  - `TryAdd`: Should_Add_When_Key_Missing_And_Return_True + Should_Return_False_When_Key_Present
  - `AddOrReplace`: Should_Add_When_Key_Missing + Should_Replace_When_Key_Present

- [ ] **Step 9: Restore BOMs on the 7 new files.**

- [ ] **Step 10: Build + run targeted tests.**

  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Tests --no-build -c Release --filter "FullyQualifiedName~RawRabbit.Tests.Pipe"
  ```

  Expected: 0 build errors. Targeted test count: ~81 passed (12 + 4 + 4 + 46 + 5 + 6 + 4); 0 skipped; 0 failed.

- [ ] **Step 11: Commit.**

  ```bash
  git add test/RawRabbit.Tests/Pipe
  git commit -m "Add Wave 2 Pipe tests (PipeBuilder + Factory + IPipeContext extensions per F1 strict)"
  ```

---

### Task 9: Wave 2 phase-acceptance verification (no commit)

**Files:** none — verification commands only.

This task confirms spec §4 acceptance gates 1-9. If any check fails, fix the issue and re-commit on the appropriate prior task; do NOT add a "fix Wave 2 acceptance" commit.

- [ ] **Step 1: Build clean (gate 2).**

  ```bash
  dotnet build -c Release
  ```

  Expected: 0 errors. Warning shape stable from Wave 1 close (~114 warnings); Wave 2 may add 0-N warnings on new test code. If new warnings appear in Wave 2 files, address before declaring done. Note: VP-14 documented `Assembly.CodeBase` may emit CS0618 in source — if it does, source-side warning is out of scope (Wave 2 doesn't modify source); only NEW test-file warnings matter.

- [ ] **Step 2: Aggregate test pass count for `RawRabbit.Tests` (gate 4).**

  ```bash
  dotnet test test/RawRabbit.Tests --no-build -c Release
  ```

  Expected: ≥123 passed (was 41 post-Wave-1; +82 minimum from Wave 2 areas 1, 2, 4, 5, 6, 7, 8 — actual Wave 2 contribution: 19 + 9 + 31 + 55 + 10 + 52 + 81 = ~257; total ~298); 7 skipped (unchanged); 0 failed.

- [ ] **Step 3: Aggregate test pass count for `ServiceCollection.Tests` (gate 5).**

  ```bash
  dotnet test test/RawRabbit.DependencyInjection.ServiceCollection.Tests --no-build -c Release
  ```

  Expected: ≥6 passed (Wave 2 contributes 22); 0 skipped; 0 failed.

- [ ] **Step 4: Wave-wide net-new test count (gate 1, ≥88).**

  ```bash
  # Aggregate Wave 2 contribution: RawRabbit.Tests delta + ServiceCollection.Tests count
  RR_DELTA=$(echo "298 - 41" | bc)   # ~257
  SC_COUNT=22
  TOTAL=$((RR_DELTA + SC_COUNT))
  echo "Wave 2 net-new tests: $TOTAL (gate ≥88)"
  # Expect: ~279, ≥88
  ```

- [ ] **Step 5: Skipped count unchanged (gate 7).**

  ```bash
  grep -rEn '\[Fact\(Skip\s*=\s*"Phase 5/7' test/RawRabbit.Tests test/RawRabbit.Enrichers.Polly.Tests | wc -l
  # Expect: 9 (4 ChannelFactoryTests + 3 ChannelPoolTests + 2 Polly Services/ChannelFactoryTests)
  ```

  Wave 2 must NOT un-skip any.

- [ ] **Step 6: `[Theory]` use exercised in ≥1 Wave 2 area (gate 8).**

  ```bash
  grep -rEn '\[Theory\]' test/RawRabbit.Tests/Configuration test/RawRabbit.Tests/Exceptions | wc -l
  # Expect: ≥3 (Configuration BasicPublish + Consume + ExchangeDeclare + Publisher + QueueDeclare builders + Exceptions ctors)
  ```

- [ ] **Step 7: xUnit analyzer warnings on Wave 2 files = 0 (gate 6).**

  ```bash
  dotnet build -c Release 2>&1 | grep -E "xUnit(2020|2004|2007|1031)" | grep -E "(test/RawRabbit\.Tests/(DependencyInjection|Configuration|Exceptions|Logging|Pipe|Common/(ClientPropertyProvider|ExclusiveLock|IDictionaryExtensions|ResourceDisposer|TaskUtil|Truncator|TypeExtensions))|test/RawRabbit\.DependencyInjection\.ServiceCollection\.Tests)"
  # Expect: empty output (no matching warnings in Wave 2 new files)
  ```

- [ ] **Step 8: BOM convention adhered to on all new files (gate 9).**

  ```bash
  find test/RawRabbit.Tests/{DependencyInjection,Configuration,Exceptions,Logging,Pipe} test/RawRabbit.Tests/Common test/RawRabbit.DependencyInjection.ServiceCollection.Tests -name '*.cs' -newer docs/specs/2026-05-13-modernization-phase-4.5-wave-2-design.md -exec file {} \; | grep -v "with BOM"
  # Expect: empty (every new .cs file has UTF-8 BOM)
  ```

  Also check for double-blank gaps (gate 9):

  ```bash
  for f in $(find test/RawRabbit.Tests/{DependencyInjection,Configuration,Exceptions,Logging,Pipe} -name '*.cs'); do
    awk 'BEGIN{prev=0} /^$/{if (prev==1) print FILENAME":"NR; prev=1; next} {prev=0}' "$f"
  done | head -5
  # Expect: empty (no double-blank gaps)
  ```

- [ ] **Step 9: No `Assert.ThrowsAsync<T>` against subclass (gate 9).**

  ```bash
  grep -rn 'Assert\.ThrowsAsync<OperationCanceledException>' test/RawRabbit.Tests/{DependencyInjection,Common,Configuration,Exceptions,Logging,Pipe} test/RawRabbit.DependencyInjection.ServiceCollection.Tests
  # Expect: empty (all subclass-tolerant async cancellation assertions use ThrowsAnyAsync)
  ```

- [ ] **Step 10: Wave 2 done.** When all Steps 1–9 hold, Wave 2 is complete. Aggregate state:
  - Tasks 1–8 each have a single commit on `2.0` (8 commits added in Wave 2)
  - 0 csproj edits, 0 source-code edits, ~52 new test files, ~278 net-new passing tests
  - Test baseline: ~298/7/0 in `RawRabbit.Tests`; 22/0/0 in `ServiceCollection.Tests`; ~321 passing aggregate added by Wave 2
  - Branch is 8 commits ahead of `origin/2.0` plus the 4 unpushed Wave-2 spec lifecycle commits already on local = 12 ahead total (modulo any user push between waves)
  - Decision point: plan Waves 3-5 (separately or combined), OR push Wave 2 to origin first

---

## Tasks NOT in this plan

Inherited from spec §8 (with Wave-2 scope clarification appended):

- **Per-area exact test method names** — derived during this plan's verification pass (one per source class), not in spec
- **Plan-time decision: which Common gap-fill files have testable methods vs are pure data** — applied: Acknowledgement, PropertyHeaders, QueueArgument skipped (pure data); TopologyProvider deferred to Wave 3 per FD-Plan-2 (broker-touching)
- **Plan-time decision: ExceptionInformation testability** — applied: skipped per spec FD3 (pure data)
- **Plan-time decision: which IPipeContext extensions to consolidate via `[Theory]`** — applied: F1 strict per-method per FD-Plan-1 (no consolidation)
- **Wave 3 source areas:** `Pipe/Middleware/`, `Channel/`, `Consumer/`, `Subscription/`, `Instantiation/{,Disposable/}`, `BusClient.cs`, `IBusClient.cs`, `src/RawRabbit/DependencyInjection/` (core sub-area), `Common/TopologyProvider.cs`
- **Wave 4:** Operations.* (8 areas)
- **Wave 5:** Enrichers (5 + Polly extension)
- **Parent spec amendment** (count reconciliation `~64-122` → `≥88`): a separate follow-up commit at user's discretion will amend the parent spec §6 Wave 2 row; explicitly deferred per Wave 2 spec §8 last bullet
- **CDR R3 of Wave 2 spec** — skipped per user decision (R2 finding was a 1-line direct fix; R3 not required before plan-write)

A new spec → new plan cycle is required to add any of the above to a future phase.

**Wave-2-specific scope clarification:**

- **Phase 4.5 Waves 3-5** are out of scope for this plan. Each wave requires its own brainstorm → spec → plan → execute cycle.
- **`src/RawRabbit/Common/TopologyProvider.cs`** is deferred to Wave 3 per FD-Plan-2; spec §1 area 4 row listed it but its IModel coupling makes it broker-touching cohort.
- **`Assembly.CodeBase` deprecation in `ClientPropertyProvider.cs:22`** — out of scope for Wave 2 (Wave 2 is test-only). Replacement (e.g., `Assembly.Location`) is a Phase-7 concern if it surfaces as a runtime error.

## Known issues inherited from spec

Inherited from spec §9 (user-acknowledged on 2026-05-13 during Wave 2 brainstorm):

1. **Per-method strict interpretation pushes test count above parent spec's estimate.** Parent §6 says Wave 2 = 30-50 tests; spec §2 commits to 88-168. Plan lands ~278 (above spec's upper bound, but still under ratio). FD-Count was resolved at brainstorm; parent spec amendment deferred to follow-up.
2. **Pipe class names in parent spec are wrong.** Resolved in spec FD1 + parent amendment commit `180eb77`.
3. **Logging area is small.** Resolved in spec FD2: kept in Wave 2 with strict per-method; lands ~52 tests.
4. **No shared test-helper assembly.** Per DW7: inline only; revisit if a project's mock-construction repeats 3+ times. Wave 2 is the first wave with substantial new tests; pattern emergence will inform Wave 3+.
5. **`Pipe/Middleware/` sub-area is intentionally Wave 3.** Pipe builder/factory/extensions land in Wave 2; built-in middleware in Wave 3 to benefit from one wave's broker-mock pattern hardening.
6. **Carries forward from prior phases:** `RawRabbit.Enrichers.HttpContext` still has no functional implementation on net10 (Phase 1 V1); `test/RawRabbit.IntegrationTests` still requires a live broker (Phase 6); MessagePack 1.7.3.4's NU1902 vulnerability warnings persist (Phase 7); Phase 2 metadata heterogeneity remains.
