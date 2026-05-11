# Threat Model Analysis — RawRabbit (branch 2.0)

## 1. Header

- **Last refreshed:** 2026-05-11 against `/home/yv01p/rawrabbit` commit `664763d` (full SHA `664763d7cdea9d4f667cc6e1c4901ca7ab8039da`, branch `2.0`).
- **Scope:** Full system, no focus narrowing.
- **Sources:**
  - Code input — `/home/yv01p/rawrabbit` (the RawRabbit .NET RabbitMQ client library, branch `2.0`, commit `664763d`).
  - No spec inputs.
- **References (security-shared):**
  - Vulnerability taxonomy — `~/.claude/skills/security-shared/vuln-taxonomy.md`
  - Severity rubric — `~/.claude/skills/security-shared/severity-and-sqs.md`
  - Refresh triggers — `~/.claude/skills/security-shared/security-triggers.md` (referenced by name in §7)

---

## 2. System overview

RawRabbit is a .NET client library that wraps the official `RabbitMQ.Client` SDK and exposes higher-level publish / subscribe / request-response / get / state-machine semantics over AMQP. Consumers register typed message handlers and the library handles connection setup, channel pooling, serialization, routing-key conventions, error republish, and a pluggable middleware ("Pipe") pipeline. Security relevance: the library sits on the trust boundary between an application process and a RabbitMQ broker — every publish writes caller data onto the wire, every subscribe deserializes broker bytes back into CLR objects, and a handful of inter-app conventions (message-context headers, error stack traces, GlobalExecutionId) propagate untrusted data between services through a shared broker.

This TMA covers the core library and all in-tree enrichers, operations, DI integrations, and serialization plugins shipped from the `2.0` branch (commit `664763d`).

---

## 3. Architecture & data flows

### 3.1 Components

| Component | Location | Role |
|---|---|---|
| **Bus facade** | `src/RawRabbit/BusClient.cs` | Single entry point `IBusClient.InvokeAsync(IPipeBuilder, IPipeContext, CancellationToken)`. All operations route through here. |
| **Pipe pipeline** | `src/RawRabbit/Pipe/` | Middleware chain assembled per operation; shared state via `IPipeContext` keyed on `PipeKey` constants. |
| **Channel/Connection management** | `src/RawRabbit/Channel/` | `ChannelFactory` opens a single `IConnection` from `RabbitMQ.Client.ConnectionFactory`; pools (`Static`/`Dynamic`/`AutoScaling`/`Resilient`) hand out `IModel`s. |
| **Configuration** | `src/RawRabbit/Configuration/RawRabbitConfiguration.cs` | Root config: hosts, port, vhost, credentials, `SslOption`, recovery intervals, naming. Optional connection-string parsing via `Common/ConnectionStringParser.cs`. |
| **Serialization (default)** | `src/RawRabbit/Serialization/JsonSerializer.cs` | Newtonsoft.Json 10.0.1, configured at DI registration with `TypeNameHandling.Auto`, `TypeNameAssemblyFormatHandling.Simple`, no custom `SerializationBinder` (`src/RawRabbit/DependencyInjection/RawRabbitDependencyRegisterExtension.cs:49-62`). |
| **Alternative serializers** | `src/RawRabbit.Enrichers.MessagePack/`, `…Protobuf/`, `…ZeroFormatter/` | Pluggable `ISerializer` replacements using MessagePack 1.7.3.4, protobuf-net 2.3.2, ZeroFormatter 1.6.4. |
| **Operations** | `src/RawRabbit.Operations.{Publish,Subscribe,Request,Respond,Get,MessageSequence,StateMachine,Tools}` | Pre-baked pipeline definitions per operation. |
| **Enrichers** | `src/RawRabbit.Enrichers.{Attributes,GlobalExecutionId,HttpContext,MessageContext{,.Subscribe,.Respond},MessagePack,Polly,Protobuf,QueueSuffix,RetryLater,ZeroFormatter}` | Optional pipeline plugins; consumer opts in. |
| **DI integrations** | `src/RawRabbit.DependencyInjection.{Autofac,Ninject,ServiceCollection}` plus core `SimpleDependencyInjection` | Wire library into host container. |
| **Logging adapter** | `src/RawRabbit/Logging/LibLog.cs` | Embedded LibLog; reflection-loads NLog/log4net/Serilog/Loupe/EntLib at runtime via `Type.GetType(...)`. |
| **Compatibility shim** | `src/RawRabbit.Compatibility.Legacy/` | 1.x API on top of 2.x pipelines. |

### 3.2 Trust boundaries (DFD-style)

```
   ┌──────────────────────┐
   │   Consumer App       │  trusted (caller code)
   │   (host process)     │
   └──────┬───────────────┘
          │ B1 caller config, message instances, plugin delegates
          ▼
   ┌──────────────────────┐
   │   RawRabbit library  │  trusted (in-proc)
   │   (Pipe pipeline)    │
   └──────┬───────────────┘
          │ B2 AMQP frames (TLS optional, default OFF)
          ▼
   ┌──────────────────────┐
   │   RabbitMQ broker    │  semi-trusted (shared infra)
   │   (exchanges/queues) │
   └──────┬───────────────┘
          │ B3 inbound bytes from any peer with publish rights
          ▼
   ┌──────────────────────┐
   │   RawRabbit library  │  deserialization happens HERE
   │   (consume side)     │
   └──────┬───────────────┘
          │ B4 typed CLR object + propagated headers
          ▼
   ┌──────────────────────┐
   │   Consumer App       │  handler invoked
   │   (handler code)     │
   └──────────────────────┘
```

| Boundary | What crosses | What's assumed about each side |
|---|---|---|
| **B1 — App ↔ library** | `RawRabbitConfiguration` (incl. credentials, `SslOption`), message instances of caller-defined types, plugin registration delegates, optional connection-string | Caller is fully trusted. No validation on routing/queue/exchange names beyond length truncation (`src/RawRabbit/Common/NamingConventions.cs:131`). Connection-string parser writes arbitrary `RawRabbitConfiguration` properties via reflection from query params (`src/RawRabbit/Common/ConnectionStringParser.cs:42-60`). |
| **B2 — Library ↔ broker (publish)** | Username/password (PLAIN by default unless caller enables SSL), vhost, `client_properties` (incl. broker username, machine name), AMQP frames carrying serialized message body and headers | TLS off by default (`RawRabbitConfiguration.cs:94` — `Ssl = new SslOption { Enabled = false }`). The library does not set certificate-validation overrides; all SSL behavior is whatever the caller passes through. |
| **B3 — Broker → library (inbound)** | `BasicDeliverEventArgs.Body` raw bytes from any peer with publish rights to the bound exchange, `BasicProperties.Headers` raw values | Library treats inbound bytes as authentic and deserializes to the CLR `Type` recorded at subscribe-registration time (`PipeKey.MessageType` = `typeof(TMessage)` from `SubscribeAsync<TMessage>`). Default JSON config has `TypeNameHandling.Auto` and no `SerializationBinder`. Header values are deserialized with `HeaderType` defaulting to `typeof(object)` (`src/RawRabbit/Pipe/Middleware/HeaderDeserializationMiddleware.cs:33`). |
| **B4 — Library → app handler** | Typed CLR object + propagated headers (`message_context`, `GlobalExecutionId`, `exception_type`, `exception_stacktrace`, `host`, `sent`, etc.) | Handler code is caller-provided; library's responsibility ends at object construction. |
| **B5 — Inter-app via headers** | Caller-defined `MessageContext` payload, GlobalExecutionId GUID, exception metadata on republish-to-error-exchange | Any peer that can publish to the same exchange can forge these headers. Headers are JSON-serialized through the same `TypeNameHandling.Auto` configuration. |
| **B6 — Local logging sinks** | Log lines (no message bodies/passwords logged by core) | LibLog dynamically resolves whatever logging framework is present; no file sinks configured by core. |

### 3.3 Data flows

| Flow | Source | Sink | Crosses | Path |
|---|---|---|---|---|
| **F1** — Publish | Caller `T` instance | Broker exchange | B1, B2 | `PublishMessageExtension.PublishAsync<T>` (`src/RawRabbit.Operations.Publish/PublishMessageExtension.cs:40`) → `BodySerializationMiddleware` (`src/RawRabbit/Pipe/Middleware/BodySerializationMiddleware.cs:41-43`) → `JsonSerializer.Serialize` → `IModel.BasicPublish`. |
| **F2** — Subscribe / consume | Broker bytes | Caller handler | B3, B4 | `SubscribeMessageExtension.SubscribeAsync<TMessage>` (`src/RawRabbit.Operations.Subscribe/SubscribeMessageExtension.cs:43,52`) → consumer event delivers `BasicDeliverEventArgs` → `BodyDeserializationMiddleware.GetMessage` (`src/RawRabbit/Pipe/Middleware/BodyDeserializationMiddleware.cs:74-79`) → `SubscribeInvocationMiddleware` calls handler. |
| **F3** — RPC request | Caller `TRequest` | Broker → caller `TResponse` | B1→B2→B3→B4 | `RequestExtension.RequestAsync<TRequest,TResponse>` (`src/RawRabbit.Operations.Request/RequestExtension.cs:60`); response uses `amq.rabbitmq.reply-to` direct-reply queue. |
| **F4** — RPC respond | Broker bytes | Handler → response back to broker | B3→B4→B2 | `RespondExtension` (`src/RawRabbit.Operations.Respond/RespondExtension.cs`). |
| **F5** — Headers in/out | Caller objects ↔ broker header bytes | both ways | B2 / B3, B5 | Serialize: `HeaderSerializationMiddleware`. Deserialize: `HeaderDeserializationMiddleware.GetHeaderObject` (`src/RawRabbit/Pipe/Middleware/HeaderDeserializationMiddleware.cs:52-87`) — default header CLR type is `object`. |
| **F6** — Error republish | Local exception | `default_error_exchange` with original routing key | B5 | `SubscriptionExceptionMiddleware` (`src/RawRabbit.Operations.Subscribe/Middleware/SubscriptionExceptionMiddleware.cs:88-92`) writes `exception_type`, `exception_stacktrace`, `host` headers. |
| **F7** — Naming → routing/queue/exchange | `Type.Namespace` + `Type.Name` + `Environment.GetCommandLineArgs()` | AMQP entity names | B1→B2 | `NamingConventions` (`src/RawRabbit/Common/NamingConventions.cs:42-50,117-134`) — values lower-cased, truncated to 254 chars, otherwise unescaped. |
| **F8** — GlobalExecutionId routing | `Guid.NewGuid()` or AsyncLocal | Routing-key suffix and header | B1→B2, B5 | `AppendGlobalExecutionIdMiddleware` and `ExecutionIdRoutingMiddleware` (`src/RawRabbit.Enrichers.GlobalExecutionId/Middleware/`). |
| **F9** — MessageContext header | Caller-defined object | `message_context` header bytes | B5 | `MessageContextPlugin` (`src/RawRabbit.Enrichers.MessageContext/MessageContextPlugin.cs:11-26`) serializes via JSON; receivers deserialize via `HeaderDeserializationMiddleware`. |
| **F10** — Connection negotiation `client_properties` | `Environment.MachineName`, assembly `CodeBase`, broker username | Broker `client_properties.broker_username` etc. | B2 | `ClientPropertyProvider.GetClientProperties` (`src/RawRabbit/Common/ClientPropertyProvider.cs:15-32`). |
| **F11** — HTTP context capture | `IHttpContextAccessor.HttpContext` | Pipe context (in-proc) | B1 (intra-app) | `AspNetCoreHttpContextMiddleware` / `NetFxHttpContextMiddleware`. |

---

## 4. Threat actors

| # | Actor | Capabilities | Motivation | In scope |
|---|---|---|---|---|
| **TA-1** | **Malicious co-tenant on the broker** | Has publish rights to one or more exchanges that a RawRabbit-using app subscribes to (peer microservice that has been compromised, untrusted producer in a multi-tenant fabric, or any account with broker publish permission obtained via misconfig). Can craft arbitrary message bodies and headers. | Code execution on the consumer process; tampering with downstream business logic; lateral movement via inter-service trust assumptions. | **Yes** — primary actor for B3/B5. The broker authenticates publishers but does not validate their payloads on behalf of consumers. Library-side deserialization is the line of defense. |
| **TA-2** | **Network attacker on the AMQP path** | Passive sniffing or active MITM between consumer process and broker. | Credential capture; payload interception; payload tampering; impersonating the broker. | **Yes** when TLS is off (the library default). Out of scope when caller has correctly enabled `SslOption.Enabled = true` with proper validation. |
| **TA-3** | **Compromised dependency / supply chain** | Malicious or backdoored release of `Newtonsoft.Json`, `MessagePack`, `protobuf-net`, `ZeroFormatter`, `RabbitMQ.Client`, `Polly`, `Autofac`, `Ninject`, or transitive dependency. | Indiscriminate compromise of consuming applications. | **Yes** — library pins old versions on out-of-support TFMs (`netstandard1.5`/`net451`), so update path is non-trivial and consumers may be stuck. |
| **TA-4** | **Compromised broker operator** | Broker has been replaced or compromised; can return any bytes on a `BasicDeliver`, observe client connection metadata. | Code execution on consumer (via B3 deserialization); fingerprinting of consuming apps. | **Yes** — B3 risks identical to TA-1. B2 metadata leakage (machine name, assembly path via `client_properties`) is realized here. |
| **TA-5** | **Insider — privileged broker user** | Direct broker management UI access; can read message contents and rebind exchanges. | Data exfiltration; payload injection. | Out of scope — broker access is a broker-side authorization concern, not a library concern. |
| **TA-6** | **Consumer-app developer (insider, regular user)** | Uses RawRabbit API as documented or misuses it. | None malicious assumed. | Out of scope — caller is trusted by the library's design. |
| **TA-7** | **External opportunistic / internet-scanning** | Random scanning, no specific knowledge of the consumer app. | Generic compromise. | Out of scope — RawRabbit exposes no internet-facing surface; broker exposure is a deployment concern. |
| **TA-8** | **Nation-state / APT** | Sustained targeting with novel zero-days. | High-value targets. | Out of scope for a general-purpose OSS library. The library should defend against TA-1–TA-4; APT-grade defense is a deployment-level concern. |

---

## 5. Threat analysis (STRIDE-per-element)

Organized by trust boundary and the elements that traverse it. Each subsection lists STRIDE threats that apply, the existing mitigation (if any) per the recon, and residual risk.

### 5.1 B3 — Inbound deserialization (`BodyDeserializationMiddleware`, `HeaderDeserializationMiddleware`)

This is the highest-risk surface in the system: TA-1, TA-3, and TA-4 all converge here.

| STRIDE | Threat | Existing mitigation | Residual |
|---|---|---|---|
| **S** Spoofing | Any peer with publish rights to the bound exchange can forge any message identity. No payload-level signing. | None in library. Broker authn is producer-side only. | **Open** — see Finding F-01. (Not on its own a high-severity threat for a typical broker fabric, but it underpins TA-1/TA-3/TA-4 practicality.) |
| **T** Tampering | Bytes on the wire (B2) can be tampered with when TLS is off. Headers can be re-written by any forging peer. | None in library; relies on caller enabling TLS. | **Open** — see Finding F-03. |
| **R** Repudiation | No payload signing → publishers can deny. | None. | Accepted — out of scope for a broker client library. |
| **I** Information disclosure | Cleartext AMQP exposes message bodies on path. | Caller-provided `SslOption`. | **Open** — see Finding F-03. |
| **D** Denial of service | (a) Maliciously huge JSON payload → memory exhaustion in `JToken.ToObject`. (b) Pathological JSON nesting → stack exhaustion. (c) Republish-loop if `default_error_exchange` is mis-configured to feed back to source queue. | Newtonsoft.Json default `MaxDepth = 64` mitigates (b). No size cap or read-throttling for (a). No loop break for (c). | **Open** — Finding F-08 (low). |
| **E** Elevation of privilege / RCE | **`TypeNameHandling.Auto` with no `SerializationBinder` enables Newtonsoft.Json gadget-chain RCE for any inbound message body whose declared type contains a polymorphic field (`object`, an interface, an abstract base, `dynamic`).** Header deserialization is even worse: `HeaderType` defaults to `typeof(object)`, so EVERY header value is fully polymorphic regardless of consumer type choices. | None — this is a deliberate JSON.NET configuration choice in `RawRabbitDependencyRegisterExtension.cs:51-57`. | **Open** — Findings F-01 and F-02 (Critical). |

### 5.2 B2 — AMQP transport (publish + connection setup)

| STRIDE | Threat | Existing mitigation | Residual |
|---|---|---|---|
| **S** Spoofing | TA-2 MITM impersonating broker. | Caller-provided `SslOption` with cert validation. | **Open by default** — Finding F-03 (TLS off in library defaults). |
| **T** Tampering | TA-2 modifies AMQP frames in flight. | Same. | **Open by default** — F-03. |
| **R** Repudiation | N/A at transport layer. | — | — |
| **I** Information disclosure | (a) Credentials in cleartext on PLAIN auth without TLS. (b) `client_properties` sends `Environment.MachineName` and assembly `CodeBase` to broker on every connection (TA-4 fingerprinting). | (a) Caller-provided TLS. (b) None — by design. | (a) **Open** — F-03. (b) **Open** — Finding F-09 (Low). |
| **D** Denial of service | Connection exhaustion if broker becomes unhealthy and recovery loop reconnects aggressively. | Configurable `RecoveryInterval` and `RequestedHeartbeat`. | Accepted. |
| **E** Elevation | N/A. | — | — |

### 5.3 B1 — App ↔ library configuration boundary

| STRIDE | Threat | Existing mitigation | Residual |
|---|---|---|---|
| **S/T/R** | N/A — caller is trusted. | — | — |
| **I** Information disclosure | `RawRabbitConfiguration.Password` held in plain memory and string fields. Reachable by anyone with process introspection (memory dump, debugger). | None — typical .NET pattern. Not using `SecureString`. | Accepted (out-of-band: a process-level threat, not the library's responsibility). |
| **E** Elevation via mass-assignment | `ConnectionStringParser.Parse` walks query parameters and assigns to **any** public instance property of `RawRabbitConfiguration` via reflection (`Common/ConnectionStringParser.cs:42-60`). If a caller ever passes a user-influenced connection string, an attacker can rewrite `Hostnames`, `Port`, `VirtualHost`, `Ssl`, `Username`, `Password`, `RouteWithGlobalId`, etc. | Caller controls input — but the parser does not allow-list which properties are user-settable. | **Open** — Finding F-04 (Medium, conditional on caller behavior). |

### 5.4 B5 — Inter-app via headers (MessageContext, GlobalExecutionId, exception headers)

| STRIDE | Threat | Existing mitigation | Residual |
|---|---|---|---|
| **S** Spoofing | A forging peer can set arbitrary `message_context`, `GlobalExecutionId`, `exception_*` headers on any message. Downstream consumers that trust these for identity, tracing, or routing decisions are misled. | None — library treats all header bytes as authentic. | **Open** — see Finding F-05 (consumer-side trust; library implication is documentation/design). |
| **T** Tampering | Same. | None. | **Open** — F-05. |
| **R** Repudiation | Headers cannot be attributed to a specific publisher post-hoc. | None. | Accepted. |
| **I** Information disclosure | `SubscriptionExceptionMiddleware` writes `Exception.StackTrace`, `Exception.GetType().FullName`, and `Environment.MachineName` to headers when republishing failed messages (`src/RawRabbit.Operations.Subscribe/Middleware/SubscriptionExceptionMiddleware.cs:88-92`). Any peer subscribed to the error exchange (a monitor service, an auditor, or anyone with broker read perms on `default_error_exchange`) sees internal type names, file paths, and host topology. | None — all errors leak. | **Open** — Finding F-06 (Medium). |
| **D** Denial of service | Republish loop if `default_error_exchange` binds back to source queue (configuration concern). Also: handler exception triggered by attacker-controlled payload causes the same payload to be republished repeatedly (infinite cost on attacker side, but quickly fills error queues). | Caller-configured exchange topology. | **Open** — Finding F-08 (Low). |
| **E** Elevation via deserialization | Header bytes are deserialized through the same JSON serializer with `TypeNameHandling.Auto` and `HeaderType = typeof(object)` — same gadget-chain RCE surface as B3, but for headers, it is unconditional regardless of consumer type discipline. | None. | **Open** — Finding F-02. |

### 5.5 B6 — Logging adapter (LibLog reflection-based discovery)

| STRIDE | Threat | Existing mitigation | Residual |
|---|---|---|---|
| **E** Elevation via DLL planting | LibLog calls `Type.GetType("Serilog.Log, Serilog")` and similar across NLog/log4net/Loupe/EntLib (`src/RawRabbit/Logging/LibLog.cs` — ~25 sites). If an attacker can drop a DLL with one of those simple names onto the assembly probing path, RawRabbit's first log call instantiates the attacker's type. | Standard .NET assembly probing rules (GAC + base directory + `privatePath`). | Accepted — host-process security boundary, not library-attacker-controllable in normal deployments. (Move from "open" to "accepted" because the threat actor would need filesystem write to the app directory, at which point RCE is already achieved.) |

### 5.6 Cross-cutting — supply-chain (TA-3)

| STRIDE | Threat | Existing mitigation | Residual |
|---|---|---|---|
| **E/T** | Pinned old versions of Newtonsoft.Json (10.0.1, 2017), MessagePack (1.7.3.4, 2017), protobuf-net (2.3.2, 2017), ZeroFormatter (1.6.4, 2017 — abandoned project), RabbitMQ.Client (5.0.1), Autofac (4.1.0), Ninject (3.3.4). TFMs `netstandard1.5` and `net451` are out of Microsoft support. Consumer apps inherit the entire pinned set. | NuGet manifest-based versioning. No lockfile, no checksum pinning per the recon's `csproj` inspection. | **Open** — Finding F-07 (Medium). |

---

## 6. Findings — prioritized roadmap

Each finding is tagged **Implemented** (vuln-shaped — a real condition in shipped code) or **Planned** (constraint-shaped — applies to future work). Severity per `~/.claude/skills/security-shared/severity-and-sqs.md`. Order: severity × exploitability × blast radius.

### F-01 — Newtonsoft.Json `TypeNameHandling.Auto` enables RCE via inbound message body — **Critical** — Implemented

**Where:** `src/RawRabbit/DependencyInjection/RawRabbitDependencyRegisterExtension.cs:49-62` configures the default `JsonSerializer` with:

- `TypeNameHandling = TypeNameHandling.Auto`
- `TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple`
- No `SerializationBinder`

**Reachable from:** every consumer that uses the default JSON serializer (i.e., everyone unless they replace `ISerializer`) and subscribes to any message type with at least one `object`-typed field, interface field, abstract base field, `dynamic`, or `IDictionary`.

**Attack path:** TA-1 (malicious co-tenant with publish rights to a subscribed exchange) crafts a JSON body where a polymorphic field carries a `$type` directive pointing at a known JSON.NET gadget — e.g., `System.Windows.Data.ObjectDataProvider`, `System.Diagnostics.Process`, `System.IO.FileInfo`, or any of the documented Newtonsoft.Json gadget chains. JSON.NET, with no `SerializationBinder` filter, instantiates the attacker-chosen type. RCE on the consumer process under the consumer process's identity.

**Taxonomy:** vuln-taxonomy §10 (Deserialization — unsafe deserializers).

**Notes:**

- Type identity for the BODY itself comes from `PipeKey.MessageType = typeof(TMessage)` and is NOT taken from a header. So a fully sealed concrete `TMessage` with no polymorphic fields blocks F-01 for that subscription. F-02 below is unconditional.
- Microsoft and the JSON.NET maintainers have explicitly warned against `TypeNameHandling != None` without a custom `SerializationBinder` since 2017 (CVE-2017-9785-class advisories on .NET formatters; JSON.NET project README). The configuration here is from before that consensus solidified in widespread practice.

**Direction (informational, not a fix):** any future refactor must either (a) drop `TypeNameHandling`, (b) install a strict allow-list `SerializationBinder` scoped to caller-registered message types, or (c) replace JSON.NET as the default with a serializer that does not perform polymorphic instantiation by default. Each option breaks consumers who rely on polymorphic message contracts; pick one explicitly.

---

### F-02 — Header deserialization with `HeaderType = typeof(object)` enables RCE on EVERY header value — **Critical** — Implemented

**Where:** `src/RawRabbit/Pipe/Middleware/HeaderDeserializationMiddleware.cs:33` defaults `HeaderType` to `typeof(object)`. Combined with the `TypeNameHandling.Auto` configuration (F-01), JSON.NET's `Deserialize<object>(bytes)` follows any `$type` marker present in the header bytes.

**Reachable from:** every consumer that runs ANY enricher that calls `HeaderDeserializationMiddleware` — this includes the `MessageContext`, `GlobalExecutionId`, `MessageContext.Subscribe`, and `MessageContext.Respond` enrichers, and any third-party plugin that reads a header through `IPipeContext.GetHeader<object>(...)`.

**Why this is worse than F-01:** F-02 is unconditional on consumer type discipline. Even a consumer who registers `SubscribeAsync<SealedDtoWithPrimitivesOnly>` is exposed if any registered enricher reads a header. The library's "header" abstraction itself is the gadget surface.

**Attack path:** TA-1 publishes a message with `BasicProperties.Headers["message_context"]` containing JSON with a top-level `$type` directive. Consumer's `MessageContextMiddleware` calls `HeaderDeserializationMiddleware` → `JsonSerializer.Deserialize<object>(headerBytes)` → JSON.NET instantiates attacker-chosen type. RCE.

**Taxonomy:** vuln-taxonomy §10.

**Direction (informational):** require callers to declare a concrete CLR type for every header they read; reject `typeof(object)`. Or scope a strict `SerializationBinder` to headers separately from bodies.

---

### F-03 — TLS off by default; library ships permissive transport posture — **High** — Implemented

**Where:** `src/RawRabbit/Configuration/RawRabbitConfiguration.cs:94` initializes `Ssl = new SslOption { Enabled = false }`. `RawRabbitConfiguration.Local` (`:110-117`) — the implicit default if a caller passes nothing — keeps this off. `ClientPropertyProvider` (F-09 below) sends machine name, assembly path, and broker username over the same cleartext channel.

**Attack path:** TA-2 sniffs PLAIN-auth credentials and message bodies on AMQP traffic. Or actively MITMs and replaces broker bytes — which then converges with F-01/F-02 since broker bytes terminate at deserialization.

**Why this is library-side, not just deployment:** the library ships defaults that consumers will use unless they explicitly override. A consumer who follows the README's quick-start path gets cleartext AMQP and stays there until explicitly changed. The "secure-by-default" line is a real one for a security-sensitive client library.

**Taxonomy:** vuln-taxonomy §3 (Cryptographic failures: missing encryption in transit).

**Direction (informational):** flip `Enabled = true` as the default; require explicit opt-out for cleartext, with a documented security warning. Provide a documented `SslOption` template that includes `ServerName` and `Version` set to TLS 1.2+ — RabbitMQ.Client `SslOption` defaults are weak on older TFMs.

---

### F-04 — `ConnectionStringParser` mass-assigns arbitrary `RawRabbitConfiguration` properties via reflection — **Medium** — Implemented

**Where:** `src/RawRabbit/Common/ConnectionStringParser.cs:42-60`. The parser walks every `?key=value` pair in the connection string and sets the matching public instance property on `RawRabbitConfiguration` via reflection + `Convert.ChangeType`. There is no allow-list; `Hostnames`, `Port`, `VirtualHost`, `Username`, `Password`, `Ssl`, `RouteWithGlobalId`, `RecoveryInterval`, etc. are all settable.

**Attack path:** any caller who constructs a connection string that interpolates user input (an admin UI form field, a multi-tenant config bootstrap) gives an attacker the ability to rewrite the broker target host or disable TLS. Conditional on caller pattern, but the failure mode is silent and has high blast radius (full broker hijack).

**Taxonomy:** vuln-taxonomy §6 (mass assignment) and §2 (broken access control via predictable property names).

**Direction (informational):** allow-list the parseable parameters explicitly. Reject unknown query keys instead of silently reflecting them.

---

### F-05 — Inter-app headers (`message_context`, `GlobalExecutionId`, `exception_*`) carry no integrity or origin attestation — **Medium** — Implemented (design-level)

**Where:** `src/RawRabbit.Enrichers.MessageContext/MessageContextPlugin.cs:11-26`, `src/RawRabbit.Enrichers.GlobalExecutionId/Middleware/AppendGlobalExecutionIdMiddleware.cs:56-59`, `src/RawRabbit.Operations.Subscribe/Middleware/SubscriptionExceptionMiddleware.cs:88-92`. All inter-app context propagation rides as plain JSON header bytes; any peer with publish rights to a shared exchange can forge any header.

**Attack path:** TA-1 publishes with a forged `message_context` header carrying a fabricated user identity, tenant ID, or trace ID. A downstream consumer that trusts `message_context` for authorization or audit logging accepts the forged identity. This is a CONSUMER-side authz break, but the library's design — exposing `message_context` as a first-class "context" abstraction without integrity — invites it.

**Taxonomy:** vuln-taxonomy §2 (broken access control), §7 (auth & session — token integrity).

**Direction (informational):** documentation must state explicitly that `message_context` is NOT authenticated and must not be used for authorization. Optional: provide a signed-context enricher that wraps `message_context` in HMAC keyed off a shared secret per environment.

---

### F-06 — Error republish leaks full stack traces and machine names through `default_error_exchange` — **Medium** — Implemented

**Where:** `src/RawRabbit.Operations.Subscribe/Middleware/SubscriptionExceptionMiddleware.cs:88-92` adds the following to the republished message's headers:

- `exception_type` — `Exception.GetType().FullName` (reveals internal type names, plugin types loaded)
- `exception_stacktrace` — `Exception.StackTrace` (reveals method names, file paths if PDBs loaded, internal call graph)
- `host` — `Environment.MachineName`

**Attack path:** TA-1 (or any peer with read permission on `default_error_exchange`) consumes error messages and accumulates internal-structure intelligence: third-party libraries in use, method signatures, host naming convention, possibly absolute paths. Combine with F-01/F-02 to refine gadget chain choice.

**Taxonomy:** vuln-taxonomy §9 (error handling — verbose stack traces).

**Direction (informational):** make stack-trace inclusion opt-in or environment-gated. Default to including only `exception_type` (without namespace) and a stable correlation ID; full stack stays on the local logger.

---

### F-07 — Pinned, stale, partly-abandoned dependencies on out-of-support TFMs — **Medium** — Implemented

**Where:** `*.csproj` across `src/`. Key versions and TFMs:

| Package | Pinned version | Released | Status |
|---|---|---|---|
| Newtonsoft.Json | 10.0.1 | Apr 2017 | Maintained (current 13.x); 10.x has known polymorphic-deserialization risks (F-01 root cause) |
| MessagePack | 1.7.3.4 | 2017 | v1 superseded by v2 with safer defaults |
| protobuf-net | 2.3.2 | 2017 | Maintained (current 3.x) |
| ZeroFormatter | 1.6.4 | 2017 | Abandoned project (no commits since 2017) |
| RabbitMQ.Client | 5.0.1 | 2017 | Current 6.x/7.x |
| Polly | 5.3.1 | 2017 | Current 8.x |
| Autofac | 4.1.0 | 2017 | Current 8.x |
| TFMs | `netstandard1.5`, `net451` | — | Both out of Microsoft support |

**Attack path:** TA-3 — any future CVE in a pinned dependency has no near-term remediation path because the library still targets `netstandard1.5`/`net451`, neither of which is supported by current versions of any of these libraries. Consumer apps inheriting RawRabbit's dependency closure are stuck on the pinned versions.

**Taxonomy:** vuln-taxonomy §5 (vulnerable components & supply chain).

**Note:** This is NOT a CVE-hunt finding (CVE hunting is `critical-security-review`'s job, not TMA). It is the architectural condition that makes consumers exposed to whatever future CVE lands.

**Direction (informational):** retarget to current TFMs (`netstandard2.0` / `net6`+) and bump the dependency baseline, accepting the breaking change as part of any subsequent major release.

---

### F-08 — Deserialization DoS surface (no body-size cap, possible republish loops) — **Low** — Implemented

**Where:** `src/RawRabbit/Pipe/Middleware/BodyDeserializationMiddleware.cs` invokes `ISerializer.Deserialize` on raw `BasicDeliverEventArgs.Body` with no size guard. Newtonsoft.Json's default `MaxDepth = 64` blocks pathological nesting. There is no read-byte limit. Republish to `default_error_exchange` happens unconditionally on handler exception (`SubscriptionExceptionMiddleware.cs:88-92`); if the error exchange is bound back to the original queue (a configuration mistake), an attacker payload that always throws on deserialization can be amplified into an unbounded loop.

**Attack path:** TA-1 sends a 1 GB JSON body → consumer's deserializer allocates strings until OOM. Or TA-1 sends a payload that always throws on Newtonsoft deserialization → if error topology mis-configured, consumer republishes to itself indefinitely.

**Taxonomy:** vuln-taxonomy §1 (input validation — message-queue boundary) overlapping §10 (deserialization).

**Direction (informational):** size-cap inbound bodies before deserialization; bound the republish chain (count via a `republish_count` header that aborts after N).

---

### F-09 — `client_properties` discloses machine name and assembly path to the broker on every connection — **Low** — Implemented

**Where:** `src/RawRabbit/Common/ClientPropertyProvider.cs:15-32` populates `client_properties` with `Environment.MachineName`, the assembly's `CodeBase` (file path of `RawRabbit.dll`), and the broker username being authenticated as.

**Attack path:** TA-4 (compromised broker) accumulates host topology and deployment-path fingerprints from every consumer, useful for refining further attacks.

**Taxonomy:** vuln-taxonomy §3 (data exposure).

**Direction (informational):** make `ClientPropertyProvider` callers-overridable; default to a stable string (e.g., library name + version) without host-specific data. Consumers who want richer fingerprinting opt in.

---

### Summary

| Finding | Severity | Tag | Surface | Taxonomy |
|---|---|---|---|---|
| F-01 — `TypeNameHandling.Auto` body RCE | **Critical** | Implemented | B3 | §10 |
| F-02 — `HeaderType = object` header RCE | **Critical** | Implemented | B3, B5 | §10 |
| F-03 — TLS off by default | **High** | Implemented | B2 | §3 |
| F-04 — Connection-string mass-assignment | **Medium** | Implemented | B1 | §2, §6 |
| F-05 — Inter-app headers lack integrity | **Medium** | Implemented (design) | B5 | §2, §7 |
| F-06 — Error republish leaks stack traces | **Medium** | Implemented | B5 | §9 |
| F-07 — Stale pinned deps on out-of-support TFMs | **Medium** | Implemented | cross-cutting | §5 |
| F-08 — Deserialization DoS (no size cap, loop risk) | **Low** | Implemented | B3, B5 | §1, §10 |
| F-09 — `client_properties` discloses host metadata | **Low** | Implemented | B2 | §3 |

The two Critical findings (F-01, F-02) share a root cause and would typically be remediated together. The High finding (F-03) compounds them by removing transport-layer defense; in practice F-01/F-02 are often only reachable to TA-1 if F-03 is closed (because TA-2 is otherwise able to compromise integrity directly). All other findings are independent.

---

## 7. When to refresh

This document is a refresh artifact, not an iterative review. Regenerate (overwriting in place) when:

- **Major architectural change** — new operation type, new serializer plugin, new transport, new auth method, new inter-service trust assumption (e.g., adopting a service mesh or moving off RabbitMQ).
- **Security incident** — a real-world incident attributed to RawRabbit or a peer microservice in the same broker fabric, especially if it touches deserialization, header handling, or transport.
- **Dependency baseline change** — bumping Newtonsoft.Json (any major), retargeting to a current TFM, replacing the default serializer, or any change to `RawRabbitConfiguration` defaults.
- **First production deployment** — this TMA is a static artifact and should be reconfirmed before the library backs new production workloads.
- **Quarterly check-in** — calendar-based review to catch drift against the documented architecture and to incorporate new threat-actor intelligence.
- **New triggers in the security-trigger list** — see `~/.claude/skills/security-shared/security-triggers.md` for the canonical taxonomy of refresh triggers shared across the security family.

Refreshing TMA does NOT auto-trigger CSR (`critical-security-review`). After a refresh, the user decides whether to invoke CSR for a code-level pass against the updated threat model.
