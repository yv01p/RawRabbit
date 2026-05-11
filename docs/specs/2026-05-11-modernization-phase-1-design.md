# Modernization — Phase 1: Build Green on net10.0

**Brief (verbatim):** "i want to modernize this repo" — for didactical purposes, full modernization (a 3.x in disguise), decomposed into 7 phases. This is Phase 1.

**Repo:** `/home/yv01p/rawrabbit`, branch `2.0`, base commit `3a349cb`.

## 1. Decomposition context

Full modernization is too big for a single spec. The agreed decomposition (recorded in conversation, not committed elsewhere) is:

| # | Phase | Lesson | Dependency |
|---|---|---|---|
| **1** | **Build green on net10.0** (this spec) | SDK-style csprojs, TFM consolidation, `Directory.Build.props` deferred, `global.json`, test SDK refresh, `NuGet.Config` cleanup, fix `<DefineConstants>` overwrite trap, drop hand-written AssemblyInfo | none |
| 2 | Package metadata & central package management | `Directory.Packages.props` (CPM), `<PackageIcon>` migration, repo URL hygiene, `dotnet pack` validation | Phase 1 |
| 3 | Logging modernization | LibLog → `Microsoft.Extensions.Logging.Abstractions` | Phase 1 |
| 4 | Serializer modernization | Newtonsoft 10 → 13, or → System.Text.Json | Phase 1 |
| 5 | RabbitMQ.Client 7.x async rewrite | `IChannel`/`IAsyncBasicConsumer`, removing `EventingBasicConsumer` | Phase 1, ideally Phase 3 |
| 6 | CI on GitHub Actions | Linux build matrix, `rabbitmq:3-management` service container, integration tests | Phase 1 |
| 7 | Dead enricher cleanup | ZeroFormatter, Ninject 3, MessagePack 1→2, Stateless 3→5, Polly 5→8 | Phases 1, 3, 5 |

Each later phase rests on Phase 1's "build green" foundation.

## 2. Goal & success criteria

After Phase 1 lands, on a clean clone with the .NET 10 SDK installed, this sequence succeeds:

```
dotnet restore
dotnet build -c Release
dotnet test test/RawRabbit.Tests
dotnet test test/RawRabbit.Enrichers.Polly.Tests
```

NU1701 warnings are expected and accepted (see §6 / decision V3); no compiler errors.

**Out-of-scope for "green":**
- `test/RawRabbit.IntegrationTests` — requires a live RabbitMQ broker (Phase 6).
- `test/RawRabbit.PerformanceTest` — requires a live broker and is benchmark code; will be rewritten anyway in Phase 5.
- `sample/RawRabbit.AspNet.Sample` — requires an AspNetCore 2.0 → modern rewrite (Phase 6 or later).
- `src/RawRabbit.Enrichers.HttpContext` — kept in solution but compiles to empty pass-through classes on net10 (see §5 decision V1).

## 3. In-scope file changes

Nothing in `*.cs` source files, with one exception: deleting (not editing) the 25 hand-written `Properties/AssemblyInfo.cs` files.

| Change | Files | Count |
|---|---|---|
| Replace `<TargetFrameworks>…</TargetFrameworks>` (or `<TargetFramework>old</TargetFramework>`) with `<TargetFramework>net10.0</TargetFramework>` | every `*.csproj` remaining in `RawRabbit.sln` after §4 sln edits except the legacy Polly.Tests (rewritten in full below) | 29 |
| Delete `<ItemGroup Condition=" '$(TargetFramework)' == 'net451' ">…</ItemGroup>` and `…== 'net46'…` blocks (System / Microsoft.CSharp legacy refs) | scattered across the same csprojs | 25 |
| Delete every `<PropertyGroup Condition="'$(Configuration)\|$(TargetFramework)\|$(Platform)'=='Debug\|netstandard1.5\|AnyCPU'">…</PropertyGroup>` (and the `netstandard1.6`/`netstandard2.0` siblings). They no-op once the only TFM is net10. | 20 csprojs | 20 |
| Rewrite the unconditional `<DefineConstants>LIBLOG_PORTABLE</DefineConstants>` to `<DefineConstants>$(DefineConstants);LIBLOG_PORTABLE</DefineConstants>` | `src/RawRabbit/RawRabbit.csproj`, `src/RawRabbit.Compatibility.Legacy/RawRabbit.Compatibility.Legacy.csproj` | 2 |
| Convert legacy non-SDK csproj to SDK-style with `<PackageReference>`s; delete companion `packages.config` | `test/RawRabbit.Enrichers.Polly.Tests/RawRabbit.Enrichers.Polly.Tests.csproj`, `test/RawRabbit.Enrichers.Polly.Tests/packages.config` | 1 csproj rewrite + 1 deletion |
| Bump test stack: `Microsoft.NET.Test.Sdk` → 18.5.1, `xunit` → 2.9.3, `xunit.runner.visualstudio` → 3.1.5, `Moq` → 4.20.72 | `test/RawRabbit.Tests/`, `test/RawRabbit.IntegrationTests/`, `test/RawRabbit.Enrichers.Polly.Tests/` | 3 |
| Replace `NuGet.Config` with single `https://api.nuget.org/v3/index.json` source | `NuGet.Config` | 1 |
| Add new `global.json` pinning `.NET 10` SDK with `rollForward: latestFeature` | `global.json` (new file at repo root) | 1 |
| Delete every `Properties/AssemblyInfo.cs` file (per V2 decision) | enumerated below | 25 |
| Update `RawRabbit.sln`: drop `RawRabbit.PerformanceTest`, drop `RawRabbit.AspNet.Sample` | `RawRabbit.sln` | 1 |
| Bump `Microsoft.Extensions.Configuration.*` and `Serilog.Sinks.Literate` → `Serilog.Sinks.Console` in `sample/RawRabbit.ConsoleApp.Sample/RawRabbit.ConsoleApp.Sample.csproj` (Serilog.Sinks.Literate is deprecated since 2017; replacement is mechanical) | `sample/RawRabbit.ConsoleApp.Sample/RawRabbit.ConsoleApp.Sample.csproj` | 1 |

### 25 AssemblyInfo.cs deletions (full list)

```
src/RawRabbit/Properties/AssemblyInfo.cs
src/RawRabbit.Compatibility.Legacy/Properties/AssemblyInfo.cs
src/RawRabbit.DependencyInjection.Autofac/Properties/AssemblyInfo.cs
src/RawRabbit.DependencyInjection.Ninject/Properties/AssemblyInfo.cs
src/RawRabbit.DependencyInjection.ServiceCollection/Properties/AssemblyInfo.cs
src/RawRabbit.Enrichers.Attributes/Properties/AssemblyInfo.cs
src/RawRabbit.Enrichers.GlobalExecutionId/Properties/AssemblyInfo.cs
src/RawRabbit.Enrichers.HttpContext/Properties/AssemblyInfo.cs
src/RawRabbit.Enrichers.MessageContext/Properties/AssemblyInfo.cs
src/RawRabbit.Enrichers.MessageContext.Respond/Properties/AssemblyInfo.cs
src/RawRabbit.Enrichers.MessageContext.Subscribe/Properties/AssemblyInfo.cs
src/RawRabbit.Enrichers.Polly/Properties/AssemblyInfo.cs
src/RawRabbit.Enrichers.QueueSuffix/Properties/AssemblyInfo.cs
src/RawRabbit.Operations.Get/Properties/AssemblyInfo.cs
src/RawRabbit.Operations.MessageSequence/Properties/AssemblyInfo.cs
src/RawRabbit.Operations.Publish/Properties/AssemblyInfo.cs
src/RawRabbit.Operations.Request/Properties/AssemblyInfo.cs
src/RawRabbit.Operations.Respond/Properties/AssemblyInfo.cs
src/RawRabbit.Operations.StateMachine/Properties/AssemblyInfo.cs
src/RawRabbit.Operations.Subscribe/Properties/AssemblyInfo.cs
sample/RawRabbit.ConsoleApp.Sample/Properties/AssemblyInfo.cs
sample/RawRabbit.Messages.Sample/Properties/AssemblyInfo.cs
test/RawRabbit.Enrichers.Polly.Tests/Properties/AssemblyInfo.cs
test/RawRabbit.IntegrationTests/Properties/AssemblyInfo.cs
test/RawRabbit.Tests/Properties/AssemblyInfo.cs
```

Each file contains only standard auto-generatable attributes (`AssemblyTitle`, `AssemblyDescription`, `AssemblyVersion`, `AssemblyFileVersion`, plus `ComVisible(false)` and `Guid(…)` which are irrelevant for non-COM net10 packages). The SDK regenerates `AssemblyTitle`/`AssemblyVersion`/etc. from csproj metadata.

### Sample of expected csproj diff (illustrative — `src/RawRabbit/RawRabbit.csproj`)

**Before:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <Description>A modern framework for communication over RabbitMq.</Description>
    <AssemblyTitle>RawRabbit</AssemblyTitle>
    <VersionPrefix>2.0.0</VersionPrefix>
    <Authors>pardahlman;enrique-avalon</Authors>
    <TargetFrameworks>netstandard1.5;net451</TargetFrameworks>
    <AssemblyName>RawRabbit</AssemblyName>
    <PackageId>RawRabbit</PackageId>
    ...
    <DefineConstants>LIBLOG_PORTABLE</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="RabbitMQ.Client" Version="5.0.1" />
    <PackageReference Include="Newtonsoft.Json" Version="10.0.1" />
  </ItemGroup>
  <ItemGroup Condition=" '$(TargetFramework)' == 'net451' ">
    <Reference Include="System" />
    <Reference Include="Microsoft.CSharp" />
  </ItemGroup>
</Project>
```

**After:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <Description>A modern framework for communication over RabbitMq.</Description>
    <AssemblyTitle>RawRabbit</AssemblyTitle>
    <VersionPrefix>2.0.0</VersionPrefix>
    <Authors>pardahlman;enrique-avalon</Authors>
    <TargetFramework>net10.0</TargetFramework>
    <AssemblyName>RawRabbit</AssemblyName>
    <PackageId>RawRabbit</PackageId>
    ...
    <DefineConstants>$(DefineConstants);LIBLOG_PORTABLE</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="RabbitMQ.Client" Version="5.0.1" />
    <PackageReference Include="Newtonsoft.Json" Version="10.0.1" />
  </ItemGroup>
</Project>
```

### `global.json`

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

The version line should be set to whatever 10.0.x patch the implementer's machine has installed (run `dotnet --list-sdks` to confirm). `rollForward: latestFeature` then accepts any 10.0.x or 10.1.x SDK newer than the pin.

### `NuGet.Config`

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
```

## 4. Decisions taken

| ID | Decision | Choice | Why |
|---|---|---|---|
| 3.1 | Sample projects in Phase 1 | (b) Modernize easy two (`ConsoleApp.Sample` to net10, `Messages.Sample` to net10), drop `AspNet.Sample` from sln | AspNetCore 2.0 → modern is its own substantial rewrite; out of scope for "build green" |
| 3.2 | PerformanceTest in Phase 1 | (b) Drop from sln, leave files on disk | Will be rewritten in Phase 5 anyway; modernizing now is wasted work |
| V1 | HttpContext enricher on net10 | (a) Keep, accept empty-class state | Compiles, does nothing. Tests don't exercise it. Restored properly in a later phase with `#if NET10_0_OR_GREATER` branch. Documented as known issue below. |
| V2 | 25 hand-written AssemblyInfo.cs | (a) Delete all 25 | Modern SDK convention: csproj is source of truth. Hand-written `ComVisible`/`Guid` attributes are irrelevant for non-COM net10 packages. |
| V3 | NU1701 warnings from old dep packages | (a) Accept | Warnings are the signal Phases 4/5/7 are designed to act on; suppressing them in Phase 1 hides the motivation |

## 5. Known issue, accepted as out of scope

**`RawRabbit.Enrichers.HttpContext` has no functional implementation on net10 after Phase 1.** All four `.cs` files in this enricher gate every functional code path behind `#if NETSTANDARD1_6` or `#if NET451`. Since neither define is set on net10, the project compiles to:
- `AspNetCoreHttpContextMiddleware` with no field, no constructor body, an empty `InvokeAsync` (only `return Next.InvokeAsync(...)`)
- `NetFxHttpContextMiddleware` with the same empty pass-through
- `PipeContextHttpExtensions.UseHttpContext` and `.GetHttpContext` — methods do not exist
- `HttpContextPlugin.UseHttpContext()` extension — registers a middleware with no http accessor injection

Consumers calling the plugin will get a no-op. No tests exercise this enricher (verified via `find test -name "*HttpContext*.cs"` returning nothing). The fix — adding a `#if NET10_0_OR_GREATER` branch wired to current `Microsoft.AspNetCore.Http` — is deferred to a later phase. Accepted by user during brainstorming on 2026-05-11.

## 6. Risks & responses

| # | Risk | Why it might fire | Phase 1 response |
|---|---|---|---|
| 1 | RabbitMQ.Client 5.0.1 doesn't restore cleanly on net10 | Package targets netstandard1.5 + net451 | Accept NU1701 warning. If runtime API missing: bump to 5.2.0 (last 5.x, source-compatible) — 1-line version bump. If 5.2.0 also fails: escalate to a Phase 1.5 RabbitMQ.Client compatibility decision; do not start Phase 5 under Phase 1 pressure. |
| 2 | Newtonsoft.Json 10.0.1 doesn't restore | Package targets netstandard1.0/1.3 | Same response shape. Bump to last 10.x patch only if necessary (Phase 4 owns the substantive bump). |
| 3 | LibLog source (`src/RawRabbit/Logging/LibLog.cs`) doesn't compile on net10 | LibLog uses reflection patterns that varied across .NET versions; LIBLOG_PORTABLE gates the safe subset | If it doesn't compile: stop Phase 1, escalate to Phase 3. Don't half-rip LibLog under Phase 1 pressure. |
| 4 | Old enricher dependency packages don't resolve at all (not just NU1701) on net10 | Some 2017-era packages may have hard incompatibilities | If a dependency physically refuses to resolve: drop that enricher project from `RawRabbit.sln` (Phase 7 will decide its fate). Don't bump versions in Phase 1. |
| 5 | A `*.cs` file in the legacy non-SDK Polly.Tests references types that have changed shape after Moq 4.7 → 4.20 | Moq 4.18+ changed protected-member mocking syntax | The Moq usage in `QueueDeclareMiddlewareTests.cs` and `ChannelFactoryTests.cs` (verified during brainstorming) uses only stable `Mock<T>`/`Setup`/`SetupSequence`/`Returns`/`Throws`/`It.IsAny<T>` patterns. If a test fails to compile despite this: skip it with `[Fact(Skip="Moq 4.20 incompatibility, fix in Phase 7")]`. Don't fix the test in Phase 1. |
| 6 | xunit 2.3 → 2.9 changes runner discovery shape | `xunit.runner.json` schema or convention drift | The existing `test/RawRabbit.IntegrationTests/xunit.runner.json` uses only `parallelizeAssembly`/`parallelizeTestCollections`/`maxParallelThreads`/`diagnosticMessages` — all stable since xunit 2.0. No change anticipated. |
| 7 | `Microsoft.NET.Test.Sdk` 18.5.1 lists net10.0 as "computed" rather than "explicit" | NuGet "computed" means compatible-via-fallback, not natively targeted | Compatible-via-fallback is fine for our use case (test discovery only). If discovery fails: pin to `Microsoft.NET.Test.Sdk` 17.x as a fallback. |
| 8 | After deleting AssemblyInfo.cs, some other file in the project carries `[assembly: …]` attributes that conflict with SDK-generated ones | Vendored files (e.g., `LibLog.cs`) carry `[assembly: InternalsVisibleTo(...)]` declarations | Verified during brainstorming: `src/RawRabbit/Logging/LibLog.cs` carries `[assembly: InternalsVisibleTo(...)]` for 9 sibling projects. These are NOT auto-generated by the SDK and won't conflict. No other AssemblyInfo-shaped attributes elsewhere. |

**Two design rules these risks express:**
1. **Phase 1 does not edit `*.cs` files.** Deletions of 25 AssemblyInfo.cs files are explicit and approved (V2). If a risk forces a non-deletion `*.cs` edit, the answer is to skip the project / skip the test / escalate to a later phase.
2. **Phase 1 does not bump major versions of runtime/data dependencies.** RabbitMQ.Client, Newtonsoft, Polly, MessagePack, Stateless, ZeroFormatter, Ninject, protobuf-net, Autofac, Microsoft.Extensions.DependencyInjection all stay on their current majors. Test stack (test SDK / xunit / Moq / xunit.runner.visualstudio) is the explicit exception.

## 7. Verified assumptions

These were enumerated cold against the design and verified empirically before this spec was written. Each was either confirmed against repo state or against authoritative external sources (NuGet Gallery).

| ID | Assumption | Evidence |
|---|---|---|
| A1 | .NET 10 SDK is GA-released | `Microsoft.NET.Test.Sdk` 18.5.1 (released 2026-04-28 per nuget.org) lists `net10.0` as a target — Microsoft does not publish test SDKs against unreleased TFMs |
| A2 | RabbitMQ.Client 5.0.1 nupkg consumable from net10 | nuget.org/RabbitMQ.Client/5.0.1: targets `netstandard1.5` + `net451`. net10 consumes netstandard1.5 via fallback (NU1701 expected) |
| A3 | RabbitMQ.Client 5.0.1 runtime APIs work on net10 | Dependency list (System.Collections.Concurrent, System.Net.Sockets, System.Net.Security, System.Reflection.Extensions etc.) — all alive on net10. No BinaryFormatter or removed APIs in dep tree |
| A4 | Newtonsoft.Json 10.0.1 + APIs we call | nuget.org: targets netstandard1.0/1.3 + netfx2.0/3.5/4.0/4.5. Code in `src/RawRabbit/Serialization/JsonSerializer.cs` uses only `Newtonsoft.Json.JsonSerializer`, `JsonTextReader`, `Serialize`/`Deserialize` — all present since 1.0 |
| A5 | LibLog source compiles on net10 with LIBLOG_PORTABLE | `src/RawRabbit/Logging/LibLog.cs` uses standard `System.Diagnostics.CodeAnalysis`, `System.Runtime.CompilerServices`, `System.Reflection` types under LIBLOG_PORTABLE define — all alive on net10 |
| A6 | HttpContext enricher source on net10 | All 4 `.cs` files have functional paths gated by `#if NETSTANDARD1_6` / `#if NET451` — neither set on net10 → compiles to empty pass-through (V1 decision: accept) |
| A7 | RawRabbit.PerformanceTest is in `RawRabbit.sln` | `grep` of RawRabbit.sln confirms project entry on line referencing `test\RawRabbit.PerformanceTest\RawRabbit.PerformanceTest.csproj` |
| A8 | All 3 sample projects are in `RawRabbit.sln` | Same grep confirms `RawRabbit.AspNet.Sample`, `RawRabbit.ConsoleApp.Sample`, `RawRabbit.Messages.Sample` entries |
| A9 | AspNet.Sample is the only sample with substantive AspNetCore deps | csproj inspection: AspNet.Sample pulls `Microsoft.AspNetCore.Mvc 2.0`, `Server.Kestrel 2.0`, etc. ConsoleApp.Sample pulls only `Microsoft.Extensions.Configuration.Binder/Json` + Serilog. Messages.Sample has zero external package refs |
| A10 | sln contains 32 csproj entries | grep enumerated all entries |
| A11 | Test stack target versions exist with net10 support | nuget.org: `Microsoft.NET.Test.Sdk` 18.5.1 (computed for net10), `xunit` 2.9.3 (Jan 2025), `xunit.runner.visualstudio` 3.1.5 (Sep 2025; explicit support for net8.0+, computed for net10), `Moq` 4.20.72 (Sep 2024; targets net6.0 → consumable on net10) |
| A12 | Moq 4.20+ source-compatibility with existing tests | `grep` of test/*.cs found only stable Moq APIs: `new Mock<T>()`, `.Setup`, `.SetupSequence`, `.Returns`, `.Throws`, `It.IsAny<T>`. No protected-member mocking, no `Mock.Of<T>` overload edge cases |
| A13 | Old enricher dependency packages resolve on net10 | All checked on nuget.org: Polly 5.3.1 (net45 + netstandard1.1), ZeroFormatter 1.6.4 (net45 + netstandard1.6), Ninject 3.3.4 (net45 + netstandard2.0), MessagePack 1.7.3.4 (net45 + netstandard1.6), protobuf-net 2.3.2 (net20 + netstandard1.3), Stateless 3.0.0 (net40 + netstandard1.0), Autofac 4.1.0 (net45 + netstandard1.1). All resolve via netstandard fallback (NU1701 expected) |
| A14 | Microsoft.Extensions.DependencyInjection 1.0.2 resolves on net10 | nuget.org: targets netstandard1.1 → consumable from net10 |
| A15 | Legacy Polly.Tests source files compatible with current Moq/Polly | Read confirms: stable Mock/Setup/Returns/Throws patterns; uses `Polly.Policy.Handle<T>().RetryAsync(...)` and `.WaitAndRetryAsync(...)` — APIs present in Polly 5.x |
| A16 | xunit.runner.json shape compatible with xunit 2.9 | Read of `test/RawRabbit.IntegrationTests/xunit.runner.json` shows only stable keys (`parallelizeAssembly`/`parallelizeTestCollections`/`maxParallelThreads`/`diagnosticMessages`) |
| A17 | No Release-config DefineConstants blocks we'd lose | `grep` of all csproj DefineConstants confirms only Debug-conditional blocks (plus the legacy non-SDK Polly.Tests Debug+Release blocks, which delete via SDK conversion) |
| A18 | 25 AssemblyInfo.cs files exist; all auto-generatable content | `find` enumerated all 25; sample read of Polly.Tests AssemblyInfo confirms only standard attributes |
| A19 | Serilog.Sinks.Literate is deprecated | nuget.org explicitly: "This package has been deprecated as it is legacy and is no longer maintained… migrate to Serilog.Sinks.Console" — last update Jul 2017 |
| A20 | Messages.Sample is trivial | csproj has only `<ProjectReference Include="...RawRabbit.Enrichers.Attributes...">`; source is 6 POCO message types |
| A21 | No Directory.Build.props or Directory.Packages.props exists | `ls` confirms absence at repo root |

## 8. Validation steps for the implementer

After all changes are applied:

1. `dotnet --version` → confirm 10.0.x is selected (driven by `global.json`)
2. `dotnet restore` → completes; NU1701 warnings expected on the old dep packages
3. `dotnet build -c Release` → completes with 0 errors, NU1701 warnings only
4. `dotnet test test/RawRabbit.Tests` → all tests pass or skip
5. `dotnet test test/RawRabbit.Enrichers.Polly.Tests` → all tests pass or skip
6. `dotnet build sample/RawRabbit.ConsoleApp.Sample` → builds (sample isn't run)
7. `dotnet build sample/RawRabbit.Messages.Sample` → builds

If any step fails, consult §6 risks for the response template.
