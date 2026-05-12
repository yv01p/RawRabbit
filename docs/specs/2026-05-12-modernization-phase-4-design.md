# Modernization — Phase 4: Newtonsoft.Json → System.Text.Json

**Brief (verbatim, from Phase 1 spec §1):** "Serializer modernization: Newtonsoft 10 → 13, or → System.Text.Json. Phase 1 dependency."

**Repo:** `/home/yv01p/rawrabbit`, branch `2.0`, base commit `6de57f6` (post-Phase-3).

## 1. Goal & success criteria

After Phase 4 lands, on a clean clone with the .NET 10 SDK installed, this sequence succeeds:

```
dotnet restore
dotnet build -c Release
dotnet test test/RawRabbit.Tests --no-build -c Release --blame-hang-timeout 15s
dotnet test test/RawRabbit.Enrichers.Polly.Tests --no-build -c Release
dotnet pack -c Release --no-build
```

Test counts stay **30 passed / 9 skipped / 0 failed** (Phase 1/2/3 baseline). `dotnet pack` produces 25 `.nupkg` files.

`Newtonsoft.Json` no longer appears in `Directory.Packages.props`, in any `*.csproj`, or in any `*.cs` file in the repo. The 3× `NU1903` warnings against `Newtonsoft.Json 10.0.1` (currently fired against `https://github.com/advisories/GHSA-5crp-9r3c-p9vr`) disappear from the warning shape. Other warnings (NU1701/NU1902 from RabbitMQ.Client/Polly/etc.) stay at the Phase 3 baseline.

**Out of scope for "phase 4 acceptance" (carries forward from prior phases):**
- `test/RawRabbit.IntegrationTests` — requires a live broker (Phase 6).
- `RawRabbit.Enrichers.HttpContext` no-op state on net10 (Phase 1 V1 known issue).
- `RawRabbit.Enrichers.Protobuf` and `RawRabbit.Enrichers.MessagePack` — alternative `ISerializer` implementations, untouched by this phase. Their fate is Phase 7.

## 2. Decomposition context (Phase 4's place)

The 7-phase decomposition (recorded in Phase 1 spec §1) places Phase 4 as serializer modernization, dependent on Phase 1 only. Logging (Phase 3) is now complete; CPM (Phase 2) is in place. The serializer is the framework's primary data path and the last third-party runtime dependency in `RawRabbit.csproj` other than `RabbitMQ.Client`.

This spec chooses **System.Text.Json** (not Newtonsoft 13) and the **clean-defaults** wire-format profile (not Newtonsoft emulation) — both confirmed by the user during brainstorming on 2026-05-12.

## 3. In-scope file changes

| Change | Files | Count |
|---|---|---|
| Remove the `<PackageVersion Include="Newtonsoft.Json" Version="10.0.1" />` line | `Directory.Packages.props` | 1 |
| Remove the `<PackageReference Include="Newtonsoft.Json" />` line | `src/RawRabbit/RawRabbit.csproj` | 1 |
| Replace the file contents with a System.Text.Json-backed implementation (same class name, same namespace, same `StringSerializerBase` base) | `src/RawRabbit/Serialization/JsonSerializer.cs` | 1 |
| Replace the `Newtonsoft.Json.JsonSerializer { ... 11 settings ... }` literal at lines 49–62 with a `JsonSerializerOptions` literal (4 explicit properties; rest are STJ defaults). Drop `using Newtonsoft.Json;` and `using Newtonsoft.Json.Serialization;` at lines 2–3 | `src/RawRabbit/DependencyInjection/RawRabbitDependencyRegisterExtension.cs` | 1 |
| Drop the dead `using Newtonsoft.Json.Linq;` line (no `JToken`/`JObject`/`JArray` symbol is referenced anywhere in the file) | `src/RawRabbit.Enrichers.Polly/RetryKey.cs` | 1 |

**Total: 5 files modified, 0 created, 0 deleted.**

### Atomicity (forced by transitive build resolution)

All 5 changes commit together. The Polly enricher's `using Newtonsoft.Json.Linq;` compiles today only because `Newtonsoft.Json` is reachable transitively via `ProjectReference → RawRabbit.csproj`. Removing the package from `RawRabbit.csproj` makes the namespace disappear for the Polly enricher, so the dead `using` becomes a compile error in the same build the package removal happens in. There is no decomposition that keeps the build green between commits.

### New `JsonSerializer.cs` (illustrative shape)

```csharp
using System;
using System.Text.Json;

namespace RawRabbit.Serialization
{
    public class JsonSerializer : StringSerializerBase
    {
        private readonly JsonSerializerOptions _options;
        private const string _applicationJson = "application/json";
        public override string ContentType => _applicationJson;

        public JsonSerializer(JsonSerializerOptions options)
        {
            _options = options;
        }

        public override string SerializeToString(object obj)
        {
            if (obj == null)
            {
                return string.Empty;
            }
            if (obj is string str)
            {
                return str;
            }
            return System.Text.Json.JsonSerializer.Serialize(obj, obj.GetType(), _options);
        }

        public override object Deserialize(Type type, string str)
        {
            if (type == typeof(string))
            {
                return str;
            }
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }
            return System.Text.Json.JsonSerializer.Deserialize(str, type, _options);
        }
    }
}
```

Key correctness pin: `JsonSerializer.Serialize(obj, obj.GetType(), _options)` uses runtime type. The default `Serialize<T>(value, options)` overload uses declared type `T`; passing `object` would emit `{}` and silently break every consumer call in the framework. Newtonsoft's `_json.Serialize(sw, obj)` historically used runtime type — the new implementation must too.

The `string.IsNullOrEmpty(str)` guard in `Deserialize` is a behavior parity addition — STJ throws on empty input where Newtonsoft's `JsonTextReader(StringReader(""))` returned null/default.

### New DI options literal (in `RawRabbitDependencyRegisterExtension.cs`)

```csharp
.AddSingleton<ISerializer>(resolver => new Serialization.JsonSerializer(new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNameCaseInsensitive = true,
    WriteIndented = false,
}))
```

Replaces the 11-setting Newtonsoft block. Settings translation table in §4 below documents what was kept, what was collapsed into STJ defaults, and what was dropped.

## 4. Settings translation

| Newtonsoft setting (current) | STJ equivalent (new) | Notes |
|---|---|---|
| `Formatting.None` | *(STJ default — `WriteIndented = false`)* | Set explicitly for clarity |
| `ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() }` | `PropertyNamingPolicy = JsonNamingPolicy.CamelCase` | Wire shape preserved for camelCase property names |
| `NullValueHandling.Ignore` | `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull` | Direct equivalent |
| `DefaultValueHandling.Ignore` | *(rolled into above)* | STJ collapses both into one enum. Picking `WhenWritingNull` (not `WhenWritingDefault`) preserves Newtonsoft behavior for primitives (`0`/`false`/`""` still serialize) |
| `MissingMemberHandling.Ignore` | *(STJ default — unknown JSON properties silently ignored)* | No setting needed |
| `CheckAdditionalContent = true` | *(STJ default — throws on trailing content after a JSON document)* | No setting needed |
| `TypeNameAssemblyFormatHandling.Simple` | *(no equivalent; drops with `TypeNameHandling`)* | — |
| `TypeNameHandling.Auto` | *(dropped — clean STJ defaults)* | **Wire-format break.** No `$type` discriminator. Embedded polymorphism is the consumer's responsibility via `[JsonDerivedType]` on their base classes |
| `ObjectCreationHandling.Auto` | *(no equivalent; STJ always creates new collections)* | Newtonsoft `Auto` rarely fired in practice; STJ "always new" matches the typical case |
| `ReferenceLoopHandling.Serialize` | *(dropped; STJ throws on cycles)* | **Behavior change.** Cyclic message graphs now throw `JsonException("A possible object cycle was detected")` rather than serializing repeatedly |
| `PreserveReferencesHandling.Objects` | *(dropped — clean STJ defaults)* | **Wire-format break.** No `$id`/`$ref`. Same-object-twice serializes as two copies |
| *(no Newtonsoft setting; Newtonsoft is case-insensitive on deserialize by default)* | `PropertyNameCaseInsensitive = true` | Added to preserve deserialization tolerance — STJ is case-sensitive by default |

## 5. Decisions taken

| ID | Decision | Choice | Why |
|---|---|---|---|
| D1 | Serializer direction | (B) Replace with System.Text.Json | User-confirmed 2026-05-12 during brainstorming; substantive modernization, NU1903 vulnerability removal, BCL-resident |
| D2 | Wire-format fidelity | (B.1) Clean STJ defaults | User-confirmed 2026-05-12; idiomatic STJ over bespoke Newtonsoft emulation |
| D3 | Polly's dead `using Newtonsoft.Json.Linq;` | Drop in this phase | Removes the last `Newtonsoft` token in src/. Verification showed the removal is also forced by transitive build resolution (not optional) |
| D4 | Polymorphism strategy | Consumer responsibility via `[JsonDerivedType]` | Default STJ pattern; no framework-side resolver |
| D5 | Sample wiring | No change | `ConsoleApp.Sample` doesn't touch `ISerializer` directly; the DI setup wires the new STJ-backed default automatically |
| D6 | Stay on branch `2.0` | Same convention as Phases 1/2/3 | Personal fork; `2.0` is the main branch |
| D7 | Atomic vs sequential commits | Single atomic commit for all 5 file changes | Forced by transitive build resolution (see §3 atomicity note) |

## 6. Known issues, accepted as out of scope

These are behavior changes Phase 4 introduces that consumers of this fork will see. User-acknowledged on 2026-05-12 during brainstorming.

1. **No `$type` on the wire.** Consumers that relied on Newtonsoft's `TypeNameHandling.Auto` for embedded polymorphic fields must add `[JsonDerivedType(typeof(Derived), "discriminator")]` attributes to their base message types, OR use concrete fields. RawRabbit's *top-level* message type is unaffected — it travels via the `message_type` AMQP header, not via `$type` on the JSON body.
2. **No `$id`/`$ref` on the wire.** Reference identity is not preserved; same object referenced twice becomes two copies. Cyclic message graphs throw on serialize (`JsonException`).
3. **STJ's narrower `ObjectCreationHandling`.** Existing collection properties on a target object are not reused during deserialization; STJ creates a new collection each time.
4. **Camel-case naming.** STJ camelCase is mechanically the same as Newtonsoft `CamelCaseNamingStrategy` for ASCII property names. No known difference for the property names used by RawRabbit's own types; consumer types with non-ASCII property names may differ at the edges.
5. **Carries forward from prior phases:** `RawRabbit.Enrichers.HttpContext` still has no functional implementation on net10 (Phase 1 V1); `test/RawRabbit.IntegrationTests` still requires a live broker (Phase 6); the heterogeneous metadata acceptances from Phase 2 (`Authors` values, `<VersionPrefix>` on `Compatibility.Legacy`, `<GenerateAssembly*>` flags on MessagePack/Protobuf/ZeroFormatter) remain as documented.

## 7. Risks & responses

| # | Risk | Phase 4 response |
|---|---|---|
| 1 | A test silently relies on `$type` discriminator for a polymorphic payload | Tests don't reference `ISerializer` (verified: zero hits in `test/`). If a test surprises us by failing, isolate it; if it depends on `$type`, the test exposes a broader concern, document and revisit |
| 2 | `obj.GetType()` call on a null `obj` in STJ Serialize path | Existing `SerializeToString` early-returns on `null` (line 20–23 of current `JsonSerializer.cs`). New impl preserves this guard verbatim |
| 3 | `JsonSerializer.Deserialize(string, type, options)` throws on empty string | New `Deserialize(Type, string)` adds `string.IsNullOrEmpty(str)` guard returning null. Newtonsoft's `JsonTextReader(StringReader(""))` returned null/default; this preserves that |
| 4 | NU1903 warnings persist after package removal | Confirmed via `dotnet build src/RawRabbit/RawRabbit.csproj` that NU1903 currently fires 3× against Newtonsoft.Json 10.0.1 with link to GHSA-5crp-9r3c-p9vr. Removing the package removes the warnings |
| 5 | A consumer of the public `ISerializer` API breaks because they cast to the concrete `JsonSerializer` and read its private `_json` (Newtonsoft) member | `JsonSerializer` had no public Newtonsoft-typed member — only a private `_json` field. The public API (constructor signature changes from `Newtonsoft.Json.JsonSerializer` to `JsonSerializerOptions`; everything else identical) is the one observable change. Consumers who hand-wire a `Serialization.JsonSerializer` instance must update one constructor call. The default DI registration handles this for the framework path |
| 6 | STJ runtime-type Serialize overload misuse causes silent `{}` emission | §3 code calls `JsonSerializer.Serialize(obj, obj.GetType(), _options)` explicitly. CDR/CIR will check this line specifically |

**Two design rules these risks express:**

1. **Phase 4 does not change `ISerializer`'s shape.** The interface stays as it is. New implementation, same contract.
2. **Phase 4 does not add a new framework-level polymorphism mechanism.** STJ's `[JsonDerivedType]` is consumer-side; the framework stays out of message-type-discrimination business beyond the existing `message_type` header.

## 8. Verified assumptions

These were enumerated cold against the design and verified empirically before this spec was written. Each was either confirmed against repo state or against an external probe.

| ID | Assumption | Evidence |
|---|---|---|
| A1 | `Directory.Packages.props` contains exactly one `<PackageVersion Include="Newtonsoft.Json" Version="10.0.1" />` line; removing it doesn't orphan another mapping | `cat Directory.Packages.props` confirmed line 8; no other `Newtonsoft.Json` token in the file |
| A2 | `src/RawRabbit/RawRabbit.csproj` has `<PackageReference Include="Newtonsoft.Json" />` (CPM-style, no inline `Version`); removable as a single line | `cat src/RawRabbit/RawRabbit.csproj` confirmed line 19 |
| A3 | `src/RawRabbit/Serialization/JsonSerializer.cs` is exactly the 51-line file with single class `JsonSerializer : StringSerializerBase`, namespace `RawRabbit.Serialization`, public surface = ctor + `SerializeToString` + `Deserialize(Type, string)` | `Read` of file confirmed |
| A4 | `RawRabbitDependencyRegisterExtension.cs` has the 11-setting Newtonsoft `JsonSerializer` literal at lines 49–62 and `using Newtonsoft.Json;` + `using Newtonsoft.Json.Serialization;` at lines 2–3 | `Read` of file confirmed |
| A5 | `src/RawRabbit.Enrichers.Polly/RetryKey.cs:5` is `using Newtonsoft.Json.Linq;` and no `JToken`/`JObject`/`JArray`/other `Newtonsoft.Json.Linq.*` symbol is referenced anywhere in the file | `grep -n "JToken\|JObject\|JArray\|JsonConvert\|Newtonsoft" src/RawRabbit.Enrichers.Polly/RetryKey.cs` returned only line 5 (the using); no usage |
| A6 | `src/RawRabbit.Compatibility.Legacy/` has zero `.cs` files referencing `Newtonsoft` | `grep -rln "Newtonsoft" src/RawRabbit.Compatibility.Legacy --include="*.cs"` returned 0 hits |
| A7 | The only `.cs` files in `src/` referencing `Newtonsoft` are the 3 listed in §3 | `grep -rln "Newtonsoft" src --include="*.cs"` returned exactly: `RetryKey.cs`, `JsonSerializer.cs`, `RawRabbitDependencyRegisterExtension.cs` |
| A8 / A9 | No public RawRabbit API exposes a Newtonsoft type; no Newtonsoft attributes (`[JsonProperty]`, `[JsonIgnore]`, `[JsonConverter]`, `[JsonExtensionData]`, `[JsonObject]`, `[JsonArray]`, `[JsonConstructor]`) on any framework type | `grep -rEn "JsonProperty\b\|JsonIgnore\b\|JsonConverter\b\|JsonExtensionData\b\|JsonObject\b\|JsonArray\b\|JsonConstructor\b" src --include="*.cs"` returned 0 hits |
| A10 | No custom `JsonConverter` (Newtonsoft variant) is defined anywhere in `src/` | `grep -rEn "class\s+\w+\s*:\s*JsonConverter\|: Newtonsoft\.Json\.JsonConverter" src --include="*.cs"` returned 0 hits |
| A11 | No `.cs` file in `test/` references `Newtonsoft`, `ISerializer`, or `JsonSerializer` | `grep -rln "Newtonsoft\|ISerializer\|JsonSerializer" test --include="*.cs"` returned 0 hits — including `test/RawRabbit.IntegrationTests` |
| A12 | `System.Text.Json` is BCL on net10; types `JsonSerializer`, `JsonSerializerOptions`, `JsonNamingPolicy`, `JsonIgnoreCondition` are available without any `<PackageReference>` | Scratch probe `/tmp/stj-probe/Probe.csproj` (TargetFramework `net10.0`, no PackageReferences) compiled with these types referenced — `Build succeeded. 0 Warning(s) 0 Error(s)` |
| A13 | STJ exposes `JsonSerializer.Serialize(object value, Type inputType, JsonSerializerOptions options)` (runtime-type overload) on net10 | Scratch probe used this overload directly: `JsonSerializer.Serialize(obj, obj.GetType(), opts);` — compiled clean |
| A14 | STJ `JsonSerializer.Deserialize(string json, Type returnType, JsonSerializerOptions options)` exists on net10 | Scratch probe used this overload: `JsonSerializer.Deserialize(s1, typeof(object), opts);` — compiled clean |
| A15 | STJ default for unknown JSON properties on deserialize is "ignore" (matches Newtonsoft `MissingMemberHandling.Ignore`) | STJ documented behavior since net5; no setting required to match Newtonsoft `Ignore` |
| A16 | STJ default for trailing content after a JSON document is "throw" (matches Newtonsoft `CheckAdditionalContent = true`) | STJ documented behavior since net5; no setting required to match Newtonsoft `true` |
| A17 | `JsonNamingPolicy.CamelCase` and `JsonIgnoreCondition.WhenWritingNull` exist in net10's STJ | Scratch probe used both: `PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull` — compiled clean |
| A18 | Top-level message type during deserialization is supplied by the `message_type` AMQP header, not by `$type` on the JSON body | `Read` of `BodyDeserializationMiddleware.cs:78` confirmed `Serializer.Deserialize(messageType, bodyBytes)` where `messageType` comes from `MessageTypeFunc(context)` which defaults to `context.GetMessageType()` (pipe-context-from-header lookup) |
| A19 / A20 | No framework-internal type relies on `TypeNameHandling.Auto` for self-serialization; `ExceptionInformation` is a simple POCO that round-trips cleanly under STJ defaults | `Read` of `ExceptionInformation.cs` confirmed 4 string properties, no polymorphism, no attributes |
| A21 (refined) | Newtonsoft.Json 10.0.1 fires NU1903 (not NU1701) due to known high-severity vulnerability per GHSA-5crp-9r3c-p9vr; removing the package removes the warnings | `dotnet build src/RawRabbit/RawRabbit.csproj -c Release` showed 3× NU1903 against `Newtonsoft.Json 10.0.1` with the GHSA link. Refined from initial spec draft (which incorrectly anticipated NU1701) — strengthens the Phase 4 motivation |
| A22 | No other csproj in the repo has `<PackageReference Include="Newtonsoft.Json" />` waiting to fire NU1010 after the central `<PackageVersion>` is removed | `grep -rln "Newtonsoft" --include="*.csproj"` returned exactly `src/RawRabbit/RawRabbit.csproj` |
| A23 | No Newtonsoft serialization callbacks (`[OnDeserialized]`, `[OnSerializing]`, `[OnDeserializing]`, `[OnSerialized]`) on any framework type that would silently stop firing under STJ | `grep -rEn "OnDeserialized\b\|OnSerializing\b\|OnDeserializing\b\|OnSerialized\b" src --include="*.cs"` returned 0 hits |
| **(new finding)** | The Polly enricher's `using Newtonsoft.Json.Linq;` compiles today only because `Newtonsoft.Json` is reachable transitively via `ProjectReference → RawRabbit.csproj`. Removing the package from `RawRabbit.csproj` makes the namespace disappear for the Polly enricher in the same build → the dead `using` removal is **forced**, not optional | `cat src/RawRabbit.Enrichers.Polly/RawRabbit.Enrichers.Polly.csproj` confirmed `<ProjectReference Include="..\RawRabbit\RawRabbit.csproj" />` and no direct `Newtonsoft.Json` `<PackageReference>`. Forces atomicity of all 5 file changes (D7) |

## 9. Validation steps for the implementer

After all 5 changes are applied (single atomic commit per D7):

1. `dotnet --version` → confirm 10.0.107 (driven by `global.json`).
2. `dotnet restore` → 0 errors. NU1903 hits against `Newtonsoft.Json` are gone. Other warning shape stable.
3. `dotnet build -c Release` → 0 errors. Warning count drops by ~3 (the Newtonsoft NU1903 hits).
4. `dotnet test test/RawRabbit.Tests --no-build -c Release --blame-hang-timeout 15s` → 29 passed / 7 skipped / 0 failed.
5. `dotnet test test/RawRabbit.Enrichers.Polly.Tests --no-build -c Release` → 1 passed / 2 skipped / 0 failed.
6. `dotnet pack -c Release --no-build` → 0 errors. Pack-time NU5048 (deprecated `PackageIconUrl`) warnings expected per Phase 2 §2.3.
7. `find . -path "*/bin/Release/*.nupkg" -not -path "*/AspNet.Sample/*" -not -path "*/PerformanceTest/*" | wc -l` → `25`.
8. `grep -rn "Newtonsoft" src/ test/ sample/ Directory.Packages.props --include="*.cs" --include="*.csproj"` → 0 hits. Phase 4 acceptance criterion: zero `Newtonsoft` token in `src/`, `test/`, `sample/`, or `Directory.Packages.props`.

If any step fails, consult §7 risks for the response template.
