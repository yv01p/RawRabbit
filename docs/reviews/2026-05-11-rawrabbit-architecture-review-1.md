# Architecture Review: rawrabbit (Round 1)

**Repo:** /home/yv01p/rawrabbit
**Brief (verbatim):** "modernization"

## 1. Literal-wrongness findings

1. **All projects target frameworks that are out of mainstream support.**
   - Evidence: `src/RawRabbit/RawRabbit.csproj:7` (`<TargetFrameworks>netstandard1.5;net451</TargetFrameworks>`); 22 csprojs share `netstandard1.5;net451`; `src/RawRabbit.Enrichers.HttpContext/RawRabbit.Enrichers.HttpContext.csproj`, `…/MessagePack/…csproj`, `…/ZeroFormatter/…csproj` use `netstandard1.6`; `src/RawRabbit.DependencyInjection.Ninject/RawRabbit.DependencyInjection.Ninject.csproj:7` (`netstandard2.0;net451`); `test/RawRabbit.Tests/RawRabbit.Tests.csproj:6` and `test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj:6` (`net46`); `sample/RawRabbit.ConsoleApp.Sample/RawRabbit.ConsoleApp.Sample.csproj:4` (`netcoreapp1.0`); `sample/RawRabbit.AspNet.Sample/RawRabbit.AspNet.Sample.csproj:4` (`netcoreapp2.0`); `test/RawRabbit.PerformanceTest/RawRabbit.PerformanceTest.csproj:4` (`netcoreapp1.1`).
   - .NET Framework 4.5.1 has been EOL since January 2016; `netcoreapp1.0`/`1.1`/`2.0` are EOL; `netstandard1.x` is functionally deprecated. Reference assemblies for these TFMs are no longer shipped by current `dotnet` SDKs without a populated NuGetFallbackFolder, and `dotnet pack` emits NU1701/NU1605 warnings or fails.
   - Fix: pick a single modern target story (e.g., `netstandard2.0;net8.0` for libraries, `net8.0` for tests/samples) and update every TFM in tree.

2. **`test/RawRabbit.Enrichers.Polly.Tests/RawRabbit.Enrichers.Polly.Tests.csproj` is non-SDK-style legacy MSBuild + `packages.config` + HintPath references.**
   - Evidence: `test/RawRabbit.Enrichers.Polly.Tests/RawRabbit.Enrichers.Polly.Tests.csproj:2` (`<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">` — no `Sdk=` attribute), `:13` (`<TargetFrameworkVersion>v4.6</TargetFrameworkVersion>`), `:43-67` (HintPath references to `..\..\packages\xunit.core.2.3.0\…`, `..\..\packages\Castle.Core.4.2.0\…`, `..\..\packages\RabbitMQ.Client.5.0.1\…`); accompanying `test/RawRabbit.Enrichers.Polly.Tests/packages.config`.
   - The `packages\` folder does not exist in the repo and is not produced by `dotnet restore` against an SDK-style solution. `dotnet build` on a fresh clone fails this project at the `EnsureNuGetPackageBuildImports` `<Error>` target (lines 93-99). Sibling test projects (`test/RawRabbit.Tests/RawRabbit.Tests.csproj`) are SDK-style and demonstrate the working pattern.
   - Fix: convert to `<Project Sdk="Microsoft.NET.Sdk">` with `<PackageReference>`s mirroring the sibling test projects; delete `packages.config`.

3. **`NuGet.Config` declares the dead xunit MyGet feed and the v2 nuget.org endpoint.**
   - Evidence: `NuGet.Config:5-6` (`<add key="xunit" value="https://www.myget.org/F/xunit/api/v3/index.json" />` and `<add key="nuget.org" value="https://www.nuget.org/api/v2/" />`).
   - The xunit MyGet feed has been retired; xunit 2.x ships only on nuget.org. The v2 nuget.org endpoint is a slow legacy path and is already redundant with line 4 (`api.nuget.org` → `https://api.nuget.org/v3/index.json`). On a clean `dotnet restore` the dead feed adds latency or surfaces NU1301 warnings.
   - Fix: keep only `<add key="nuget.org" value="https://api.nuget.org/v3/index.json" />`.

4. **Test SDK and runner pinned to a January 2017 preview.**
   - Evidence: `test/RawRabbit.Tests/RawRabbit.Tests.csproj:18-21` (`Microsoft.NET.Test.Sdk` Version `15.0.0-preview-20170106-08`, `xunit` 2.3.0, `xunit.runner.visualstudio` 2.3.0, `Moq` 4.7.137); identical block in `test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj:35-38`.
   - Modern `dotnet test` and the Visual Studio Test Platform require `Microsoft.NET.Test.Sdk` ≥ 17.x for reliable test discovery on .NET 6+. Combined with the dead xunit MyGet feed (finding 3) and the EOL TFMs (finding 1), the test phase will not run end-to-end on a contemporary CI runner.
   - Fix: bump `Microsoft.NET.Test.Sdk` to ≥ 17.10, `xunit` ≥ 2.9, `xunit.runner.visualstudio` ≥ 2.8, `Moq` ≥ 4.20.

5. **CI is Windows-only AppVeyor on the Visual Studio 2015 image, driven by PowerShell scripts that install RabbitMQ via Chocolatey.**
   - Evidence: `.build/appveyor.yml:3` (`image: Visual Studio 2015`), `.build/Build.ps1`, `.build/Install-Environment.ps1`, `.build/Install-RestartRabbitMq.ps1`, `.build/Install-MgmtPlugin.ps1`, `.build/Install-AddUser.ps1`, `.build/Util-RabbitMqPath.ps1` (all PowerShell, all assume Windows + Chocolatey + the Erlang/RabbitMQ Windows installer paths via `Get-RabbitMQPath`); `.github/` directory contains only `CONTRIBUTING.md` and `PULL_REQUEST_TEMPLATE.md` — no `workflows/` directory.
   - The `Visual Studio 2015` AppVeyor image is end-of-life. There is no Linux/macOS build matrix and no docker-compose for the integration broker.
   - Fix: replace `.build/*.ps1` + `appveyor.yml` with a `.github/workflows/ci.yml` running on `ubuntu-latest`, using a `rabbitmq:3-management` service container for `dotnet test` of `RawRabbit.IntegrationTests`.

6. **`<DefineConstants>…</DefineConstants>` in 18 csprojs overwrites instead of appending to `$(DefineConstants)`.**
   - Evidence: 18 csprojs include a `<PropertyGroup Condition="'$(Configuration)|$(TargetFramework)|$(Platform)'=='Debug|netstandard1.5|AnyCPU'">` block that hardcodes `<DefineConstants>TRACE;DEBUG;NETSTANDARD1_5;LIBLOG_PORTABLE</DefineConstants>` — examples: `src/RawRabbit.Operations.Publish/RawRabbit.Operations.Publish.csproj:21`, `src/RawRabbit.DependencyInjection.Autofac/RawRabbit.DependencyInjection.Autofac.csproj:23`, `src/RawRabbit.Operations.Subscribe/RawRabbit.Operations.Subscribe.csproj:21`. The main library has the unconditional `<DefineConstants>LIBLOG_PORTABLE</DefineConstants>` at `src/RawRabbit/RawRabbit.csproj:17`.
   - When the modernization pass adds new TFMs (e.g., `net8.0`, `netstandard2.0`), the SDK-injected per-TFM constants (`NET8_0`, `NETSTANDARD2_0`) will be silently absent in Debug builds, breaking any future `#if NET8_0` / `#if NETSTANDARD2_0` paths added during the migration.
   - Fix: switch every override to `<DefineConstants>$(DefineConstants);LIBLOG_PORTABLE</DefineConstants>`.

7. **`RabbitMQ.Client 5.0.1` is two major versions behind and incompatible with the modern AMQP client API.**
   - Evidence: `src/RawRabbit/RawRabbit.csproj:23` (`<PackageReference Include="RabbitMQ.Client" Version="5.0.1" />`); consumer construction at `src/RawRabbit/Consumer/ConsumerFactory.cs:65` (`return new EventingBasicConsumer(channel);`); 10 source files reference `IBasicConsumer`/`EventingBasicConsumer`/`IModel` (e.g., `src/RawRabbit/Subscription/Subscription.cs`, `src/RawRabbit/Pipe/Middleware/SubscriptionMiddleware.cs`, `src/RawRabbit/Pipe/Middleware/ConsumerConsumeMiddleware.cs`).
   - 5.0.1 (May 2017) receives no security fixes. 6.x renamed connection/channel APIs; 7.x removed `IModel`, `IBasicConsumer`, and `EventingBasicConsumer` in favour of `IChannel`, `BasicConsumeAsync`, and `IAsyncBasicConsumer`. A modernization pass that bumps the broker SDK is the same diff as rewriting the consumer abstraction; leaving 5.0.1 leaves the library on a deprecated client.
   - Fix is structural and forces a directional choice — see §2 finding 1.

8. **`Newtonsoft.Json 10.0.1` predates known-CVE patches and the modern `System.Text.Json` baseline.**
   - Evidence: `src/RawRabbit/RawRabbit.csproj:24` (`<PackageReference Include="Newtonsoft.Json" Version="10.0.1" />`); usages at `src/RawRabbit/Serialization/JsonSerializer.cs`, `src/RawRabbit.Enrichers.Polly/RetryKey.cs`, `src/RawRabbit/DependencyInjection/RawRabbitDependencyRegisterExtension.cs`.
   - 10.0.1 (Jul 2017) is below the security-rebaselined floor (≥13.0.3). Modern .NET ships `System.Text.Json` and consumers expect it. A modernization pass cannot leave the serializer floor here.
   - Fix is directional — see §2 finding 2.

9. **Package metadata uses deprecated `<PackageIconUrl>` and never bundles the icon.**
   - Evidence: 25 csprojs declare `<PackageIconUrl>http://pardahlman.se/raw/icon.png</PackageIconUrl>` (e.g., `src/RawRabbit/RawRabbit.csproj:13`); `icon.png` exists at `/home/yv01p/rawrabbit/icon.png` but is not packed via `<PackageIcon>` + `<None Include="…/icon.png" Pack="true" PackagePath="\" />`.
   - `PackageIconUrl` has been deprecated since NuGet 5.3 (June 2019) and emits NU5048 at pack time; nuget.org publishes warnings on every push. The `http://` scheme also fails secure-feed policies.
   - Fix: across every packed csproj, replace `<PackageIconUrl>…</PackageIconUrl>` with `<PackageIcon>icon.png</PackageIcon>` and add `<None Include="..\..\icon.png" Pack="true" PackagePath="\" />`. Centralizing these in a `Directory.Build.props` is the natural seat — see §2 finding 5.

10. **No `global.json` pinning the .NET SDK.**
    - Evidence: no `global.json` at the repo root (verified via `ls global.json` → no such file).
    - Combined with the EOL TFM mix (finding 1), build outcomes diverge across SDK 6 / 7 / 8 / 9 depending on what each developer or CI agent has installed.
    - Fix: add `global.json` pinning to whatever SDK version the modernization pass settles on (likely .NET 8 or 9).

11. **Bundled LibLog source file is gated by an abandoned shim.**
    - Evidence: `src/RawRabbit/Logging/LibLog.cs` (single-file vendored copy of LibLog); `LIBLOG_PORTABLE` define in `src/RawRabbit/RawRabbit.csproj:17` and 17 sibling csprojs; `LogProvider.For<>()` call sites throughout (e.g., `src/RawRabbit/Consumer/ConsumerFactory.cs:18`).
    - LibLog (damianh's source-included logging shim) was archived years ago. The modern .NET ecosystem standardised on `Microsoft.Extensions.Logging.Abstractions`. A modernization pass that ships the bundled abandoned shim has not modernised logging.
    - Fix is directional — see §2 finding 4.

## 2. Forced decisions

1. **Public API stability vs RabbitMQ.Client async-only consumer model.**
   - Why forced: the existing consumer abstraction (`src/RawRabbit/Consumer/ConsumerFactory.cs`, `src/RawRabbit/Subscription/Subscription.cs`, `src/RawRabbit/Pipe/Middleware/ConsumerConsumeMiddleware.cs`) maps mechanically to `IModel` + `EventingBasicConsumer` types that RabbitMQ.Client 7.x removed. The brief's modernization outcome cannot include the modern broker SDK without resolving this.
   - Options:
     - (a) Preserve 2.x public API by wrapping `IChannel`/`IAsyncBasicConsumer` behind a sync façade (carries deadlock risk; not really "modern" under the surface).
     - (b) Ship a 3.x with breaking changes — every consuming app rewrites against new `IBusClient`/middleware contracts.
     - (c) Freeze 2.x as legacy and start a parallel `RawRabbit.V3` line targeting RabbitMQ.Client 7.x natively; consumers migrate when ready.

2. **Default serializer story.**
   - Why forced: `Newtonsoft.Json 10.0.1` (`src/RawRabbit/RawRabbit.csproj:24`) cannot stay; *something* must change. The choice has user-visible serialization-semantics consequences (polymorphism, type handling, default casing).
   - Options:
     - (a) Bump Newtonsoft to ≥13.0.3 — smallest blast radius, consumers continue using Newtonsoft.
     - (b) Replace the core serializer with `System.Text.Json` and demote Newtonsoft to an optional enricher — modern default; breaks consumers that depended on Newtonsoft type-handling defaults.
     - (c) Strip the serializer from the core entirely and force every consumer to wire one via a `Serialization.*` enricher — most modern, biggest migration burden.

3. **Dead/orphaned enricher SDKs: ZeroFormatter, Ninject, MessagePack 1.x, Stateless 3.x, Polly 5.x, protobuf-net 2.x, Autofac 4.x.**
   - Why forced: ZeroFormatter (`src/RawRabbit.Enrichers.ZeroFormatter/RawRabbit.Enrichers.ZeroFormatter.csproj:14`, `1.6.4`) is unmaintained with no modern replacement of the same shape; Ninject `3.3.4` (`src/RawRabbit.DependencyInjection.Ninject/RawRabbit.DependencyInjection.Ninject.csproj:32`) is .NET-Framework-era; MessagePack 1→2 is a breaking-API jump; Polly 5→8 is a major rewrite; protobuf-net 2→3 likewise. A modernization pass forces a per-enricher decision.
   - Options:
     - (a) Drop the abandoned ones (ZeroFormatter, Ninject) and rewrite the live ones onto current majors.
     - (b) Keep all enrichers but pin them to a frozen TFM in a build matrix, decoupling them from the core's modernization timeline.
     - (c) Republish only the core + a minimal enricher set; transfer maintenance of the others to community packages.

4. **Logging abstraction: bundled LibLog vs `Microsoft.Extensions.Logging.Abstractions`.**
   - Why forced: every modern .NET library targets `ILogger<T>`; LibLog exists exactly to avoid that dependency in an era when `Microsoft.Extensions.Logging` did not. Modernizing logging is incompatible with keeping LibLog.
   - Options:
     - (a) Replace LibLog with `Microsoft.Extensions.Logging.Abstractions` — clean, but every `LogProvider.For<>()` call site (across the core and most enricher packages) changes.
     - (b) Keep LibLog and add an optional `Microsoft.Extensions.Logging` enricher — back-compat for existing consumers, two systems indefinitely.
     - (c) Drop logging from the core and require consumers to inject their own — purest, biggest break.

5. **Centralised package management vs per-project metadata.**
   - Why forced: 27 csprojs in `src/` carry duplicated metadata blocks (`<PackageIconUrl>`, `<PackageProjectUrl>`, `<VersionPrefix>`, `<Authors>`, `<GenerateAssembly*Attribute>`); the TFM divergence (`netstandard1.5` in 22 csprojs vs `netstandard1.6` in 3 vs `netstandard2.0` in 1) is the visible symptom of decentralised drift. Any modernization pass that doesn't centralize will leave the same trap that produced the drift.
   - Options:
     - (a) Introduce both `Directory.Build.props` (shared metadata, TFM, package icon) and `Directory.Packages.props` (Central Package Management for versions).
     - (b) Introduce only `Directory.Build.props` — fixes metadata drift but leaves package versions per-project.
     - (c) Leave each csproj independent — current state; the drift trap remains.

## 3. Recommendation

🛑 **Surface forced decisions to user.** §1 and §2 are both non-empty. The literal-wrongness items can be ground through mechanically, but five directional choices (broker SDK API contract, serializer default, dead-enricher policy, logging abstraction, central package management) shape the entire downstream effort and need to be picked before any of the §1 fixes land. Without those choices, the modernization pass either thrashes or quietly forecloses on options the user might have wanted to keep open.
