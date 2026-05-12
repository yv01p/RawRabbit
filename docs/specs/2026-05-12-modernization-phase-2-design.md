# Modernization — Phase 2: Package metadata & Central Package Management

**Brief (verbatim):** Phase 2 of the 7-phase modernization decomposition introduced in Phase 1 spec §1. Scope per that spec row: "Package metadata & central package management" — `Directory.Packages.props` (CPM), `<PackageIcon>` migration, repo URL hygiene, `dotnet pack` validation. The Phase 1 plan additionally reserves for Phase 2: `<PackageIconUrl>`, `<PackageProjectUrl>`, `<Authors>`, `<VersionPrefix>` metadata; `Directory.Build.props`; `Directory.Packages.props`.

**Repo:** `/home/yv01p/rawrabbit`, branch `2.0`, base commit `6b7614b` (HEAD of Phase 1).

**Phase decomposition position:** This is Phase 2 of 7. Phases 1, 3, 4, 5, 6, 7 are scoped in `docs/specs/2026-05-11-modernization-phase-1-design.md` §1.

## 1. Goal & success criteria

After Phase 2 lands, on a clean clone with the .NET 10 SDK installed, this sequence succeeds:

```
dotnet restore
dotnet build -c Release
dotnet test test/RawRabbit.Tests --no-build -c Release
dotnet test test/RawRabbit.Enrichers.Polly.Tests --no-build -c Release
dotnet pack -c Release --no-build
```

with the same warning shape as Phase 1 plus expected pack-time NU5048/NU5125 warnings (related to the deferred `PackageIconUrl` / license metadata cleanup per §2.3) — no new errors. Test count unchanged from Phase 1's 30 passing / 9 skipped / 0 failed. `dotnet pack` produces 25 `.nupkg` files for the library projects under `src/`. Tests and samples produce no `.nupkg` (they are non-packable). No publish.

After Phase 2:
- Every package version lives in one file (`Directory.Packages.props`). A future package bump touches one line.
- `TargetFramework`, `PackageIconUrl`, and `PackageProjectUrl` live in one file (`Directory.Build.props`). Per-csproj duplication of those 3 properties is eliminated.
- Heterogeneous metadata (`Authors`, `VersionPrefix`, `GenerateAssembly*` flags) stays per-csproj verbatim. No semantic change.

## 2. Scope decisions

### 2.1 Publishing intent: no publish

Personal modernization fork. Nothing is being pushed to nuget.org or any internal feed. Cosmetic metadata fields (`PackageIconUrl`, `PackageProjectUrl`, `Authors`) are not "fixed" — only consolidated where uniform. `dotnet pack` validation is "verify packages assemble cleanly," not "fit for publication."

### 2.2 Verbatim consolidation only

User instruction: don't change values during the move. Verification (see §6) found that `Authors`, `VersionPrefix`, and `GenerateAssembly*` are NOT uniformly valued across the 25 src csprojs. Per the user's verbatim intent, those properties stay per-csproj. Only truly uniform properties move to `Directory.Build.props`.

### 2.3 Out of scope (deferred)

Carried forward from prior phases / explicitly deferred:

- AsyncLocal-based `Get()` implementations for `MessageContextRepository` and `GlobalExecutionIdRepository` (currently `#else return null;` no-ops from Phase 1.5b) — Phase 3 (logging) or Phase 5 (broker) territory
- Investigation/fix of the 9 skipped broker-layer tests (`ChannelFactoryTests`, `ChannelPoolTests`, Polly's `ChannelFactoryTests`) — Phase 5/7 per Phase 1 spec §6 Risk #5
- Bumping any package major version — Phases 4/5/7
- Fixing the broken `PackageIconUrl` value (`http://pardahlman.se/raw/icon.png`) — cosmetic, no-publish intent
- Updating `PackageProjectUrl` to point to the fork (`yv01p/rawrabbit`) — cosmetic, no-publish intent
- Normalizing the heterogeneous `Authors` values (5 distinct values plus 1 csproj — `Operations.Tools` — missing it entirely) — cosmetic, no-publish intent
- Adding `<VersionPrefix>` to `Compatibility.Legacy` (currently absent → defaults to 1.0.0) — cosmetic, no-publish intent
- Adding the 3 `GenerateAssembly*=false` flags to MessagePack/Protobuf/ZeroFormatter (currently absent → those projects emit those attributes) — cosmetic IL metadata
- Touching LibLog, RabbitMQ.Client API surface, or any `.cs` file
- CI on GitHub Actions — Phase 6

## 3. In-scope file changes

### 3.1 New files (2)

**`Directory.Build.props`** at repo root:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <PackageIconUrl>http://pardahlman.se/raw/icon.png</PackageIconUrl>
    <PackageProjectUrl>https://github.com/pardahlman/RawRabbit</PackageProjectUrl>
  </PropertyGroup>
</Project>
```

All 3 values are verbatim moves — confirmed identical across every csproj that has them today.

**`Directory.Packages.props`** at repo root:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <!-- Library deps (Phases 4/5/7 will bump majors) -->
    <PackageVersion Include="RabbitMQ.Client" Version="5.0.1" />
    <PackageVersion Include="Newtonsoft.Json" Version="10.0.1" />
    <PackageVersion Include="Polly" Version="5.3.1" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="1.0.2" />
    <PackageVersion Include="Autofac" Version="4.1.0" />
    <PackageVersion Include="Ninject" Version="3.3.4" />
    <PackageVersion Include="MessagePack" Version="1.7.3.4" />
    <PackageVersion Include="ZeroFormatter" Version="1.6.4" />
    <PackageVersion Include="Stateless" Version="3.0.0" />
    <PackageVersion Include="protobuf-net" Version="2.3.2" />
    <!-- Test stack (Phase 1 modernized) -->
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="18.5.1" />
    <PackageVersion Include="xunit" Version="2.9.3" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageVersion Include="Moq" Version="4.20.72" />
    <!-- ConsoleApp.Sample deps -->
    <PackageVersion Include="Microsoft.Extensions.Configuration.Json" Version="10.0.7" />
    <PackageVersion Include="Microsoft.Extensions.Configuration.Binder" Version="10.0.7" />
    <PackageVersion Include="Serilog.Sinks.Console" Version="6.1.1" />
  </ItemGroup>
</Project>
```

17 `<PackageVersion>` entries — the exhaustive set of unique packages referenced by in-sln csprojs as of HEAD `6b7614b`.

### 3.2 Per-csproj changes by category

| # | Category | Files | Changes |
|---|---|---|---|
| 4a | src libraries | 25 csprojs in `src/*` | Delete 3 lines: `<TargetFramework>`, `<PackageIconUrl>`, `<PackageProjectUrl>`. Strip `Version="..."` attribute from every `<PackageReference>`. Keep everything else verbatim. |
| 4b | in-sln test (modern) | `test/RawRabbit.Tests/RawRabbit.Tests.csproj`, `test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj` | Delete `<TargetFramework>`. Add `<IsPackable>false</IsPackable>`. Strip `Version` from `<PackageReference>` lines. Keep `Authors=par.dahlman`, the 3 `GenerateAssembly*` flags, `GenerateRuntimeConfigurationFiles`, `AssemblyName`, `PackageId`, `<None Update="xunit.runner.json">` etc. |
| 4c | in-sln test (Polly) | `test/RawRabbit.Enrichers.Polly.Tests/RawRabbit.Enrichers.Polly.Tests.csproj` | Delete `<TargetFramework>`. Strip `Version` from `<PackageReference>`. (Already has `<IsPackable>false</IsPackable>`.) |
| 4d | in-sln samples | `sample/RawRabbit.ConsoleApp.Sample/RawRabbit.ConsoleApp.Sample.csproj`, `sample/RawRabbit.Messages.Sample/RawRabbit.Messages.Sample.csproj` | Delete `<TargetFramework>`. Add `<IsPackable>false</IsPackable>`. Strip `Version` from `<PackageReference>` lines. |
| 4e | dropped from sln | `sample/RawRabbit.AspNet.Sample/RawRabbit.AspNet.Sample.csproj`, `test/RawRabbit.PerformanceTest/RawRabbit.PerformanceTest.csproj` | Add `<ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>` to opt out of CPM. Otherwise untouched (Phase 5/6 will rewrite them; no point dragging `Microsoft.AspNetCore.Mvc 2.0.0`, `BenchmarkDotNet 0.10.3`, etc. into the central file). |
| 4f | side-fix | `src/RawRabbit.Operations.Tools/RawRabbit.Operations.Tools.csproj` | Remove the duplicate `<PackageProjectUrl>` line (latent data-quality bug found during verification — file currently contains the same `<PackageProjectUrl>https://github.com/pardahlman/RawRabbit</PackageProjectUrl>` line twice; MSBuild last-wins so behavior is unchanged, just clean it up). |

Total file touch count: 33 csprojs (25 src + 4 test + 2 sample + 2 dropped) + 2 new Directory.* files = **35 files**.

### 3.3 Explicitly NOT touched

- `RawRabbit.sln` — already cleaned in Phase 1
- `global.json` — Phase 1 set the SDK pin
- `NuGet.Config` — Phase 1 cleaned it
- Any `.cs` source file
- `.build/*.ps1`, `.build/appveyor.yml` — Phase 6 territory
- `README.md`, `RELEASENOTES.md`, `docs/` (other than this spec)
- LibLog source — Phase 3 territory
- The 9 currently-skipped tests' `[Fact(Skip="...")]` attributes — Phase 5/7 territory

## 4. Acceptance per category

| Category | Acceptance |
|---|---|
| Build green | `dotnet restore && dotnet build -c Release` produces 0 errors, same warning shape as Phase 1 |
| Tests stay green | `dotnet test test/RawRabbit.Tests --no-build -c Release` reports 29 passed / 7 skipped / 0 failed; `dotnet test test/RawRabbit.Enrichers.Polly.Tests --no-build -c Release` reports 1 passed / 2 skipped / 0 failed |
| CPM works | `dotnet restore` produces 0 NU1010 errors (would fire if a `<PackageReference>` exists without a matching `<PackageVersion>`) |
| Pack works | `dotnet pack -c Release --no-build` produces exactly 25 `.nupkg` files (one per src library); 0 from tests/samples |
| Side-fix worked | `grep -c PackageProjectUrl src/RawRabbit.Operations.Tools/RawRabbit.Operations.Tools.csproj` returns 1 (was 2 before) |

## 5. Risks & responses

| # | Risk | Why it might fire | Response |
|---|---|---|---|
| 1 | A package referenced somewhere isn't in the central props → restore error NU1010 | Verification might have missed a transitively-pinned `<PackageReference>` somewhere obscure | The implementation plan must enumerate every `<PackageReference>` line across in-sln csprojs and ensure each appears in `Directory.Packages.props`. If NU1010 fires post-implementation, add the missing entry. |
| 2 | A `<PackageReference>` had a transitive-pin attribute (`PrivateAssets`, `IncludeAssets`, `GeneratePathProperty`, `Aliases`, `VersionOverride`) that interacts oddly with CPM | Verification confirmed (A4) that no in-sln PackageReference uses these attributes today. If a future commit between spec-write and implementation adds one, CPM still works but the attribute is preserved on the per-csproj line. | Re-run the A4 grep at plan-write time; if any non-Include/Version attribute appears, keep the per-csproj line (only Version moves to central props). |
| 3 | The 2 dropped-from-sln csprojs accidentally inherit `Directory.Build.props` in a way that conflicts | They're not in the sln so not built by `dotnet build`. They'd inherit the 3 properties (TargetFramework, PackageIconUrl, PackageProjectUrl) harmlessly if someone explicitly built them — except `<TargetFramework>net10.0</TargetFramework>` would override their `netcoreapp1.1` / `netcoreapp2.0` which would BREAK an explicit build. | Accepted. They are dead code per Phase 1 decision; Phase 5/6 will rewrite them. The `<ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>` opt-out handles CPM; the TFM clobber on explicit build is a non-issue (no one builds them). |
| 4 | `dotnet pack` errors on a problem we didn't have to worry about under just `dotnet build` | First time the repo runs `dotnet pack` post-Phase-1; Phase 1 only validated build/test | If pack errors: read the actual error; either fold the fix into Phase 2 (if metadata-related) or escalate to a spec-amendment. Pack-time warnings (NU5048 for deprecated `PackageIconUrl`, NU5125 for missing license metadata) are expected per §1 and §2.3; do NOT add cosmetic metadata to silence them. |
| 5 | NU1507 warning fires (multiple package sources + CPM) | CPM logs NU1507 if more than one source is configured. Phase 1's `NuGet.Config` has only one source (`nuget.org`) — should not fire. | Accept the warning if it does fire; it's a warning, not an error. |
| 6 | `Directory.Build.props` PackageIconUrl/PackageProjectUrl propagate to test/sample csprojs that didn't have them before | Both files inherit from the root Build.props automatically. Test/sample csprojs become `IsPackable=false` so the metadata never makes it into a `.nupkg`. PackageIconUrl/PackageProjectUrl are pack-only metadata, not assembly attributes — no compile-time impact. | Accepted. Harmless propagation. |

## 6. Verified assumptions

All assumptions verified at spec-write time against HEAD `6b7614b`:

| # | Category | Assumption | Evidence |
|---|---|---|---|
| A1 | File state | No pre-existing `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, or other `.props`/`.targets` file anywhere in the tree | `find /home/yv01p/rawrabbit -type f \( -name "Directory.*" -o -name "*.props" -o -name "*.targets" \) -not -path "*/bin/*" -not -path "*/obj/*" -not -path "*/.git/*"` returned no results |
| A2 | Code-in-spec | The 3 properties moving to Build.props (`TargetFramework=net10.0`, `PackageIconUrl=http://pardahlman.se/raw/icon.png`, `PackageProjectUrl=https://github.com/pardahlman/RawRabbit`) are uniformly valued across every csproj that has them today | Per-csproj grep over `src/*/*.csproj`: TargetFramework=`net10.0` everywhere (25/25); PackageIconUrl identical everywhere (25/25); PackageProjectUrl identical everywhere (25/25, plus a duplicate line in Operations.Tools — see A12) |
| A2-fail | Code-in-spec | `Authors`, `VersionPrefix`, `GenerateAssembly*` were ASSUMED uniform; they are NOT | Per-csproj grep revealed: 5 distinct `Authors` values (`pardahlman` × 9, `pardahlman;enrique-avalon` × 10, `par.dahlman` × 1 in DependencyInjection.Autofac, `par.dahlman;Joshua Barron` × 1 in DependencyInjection.Ninject, `LordMike` × 2 in MessagePack/ZeroFormatter); 1 csproj missing `Authors` entirely (`Operations.Tools`); 1 csproj missing `VersionPrefix` (`Compatibility.Legacy`); 3 csprojs missing the 3× `GenerateAssembly*` flags (`Enrichers.MessagePack`, `Enrichers.Protobuf`, `Enrichers.ZeroFormatter`). Per §2.2, these stay per-csproj verbatim. |
| A3 | Code-in-spec | The 17 packages listed in §3.1 `Directory.Packages.props` are exhaustive for in-sln csprojs (no missing, no extras) | `grep -hE 'PackageReference' $(find src test/RawRabbit.Tests test/RawRabbit.IntegrationTests test/RawRabbit.Enrichers.Polly.Tests sample/RawRabbit.ConsoleApp.Sample sample/RawRabbit.Messages.Sample -name "*.csproj") \| sort -u` enumerated exactly 17 distinct package IDs. The earlier 19-package working list incorrectly included `Serilog.Sinks.File` and `Serilog.Extensions.Logging`, which are referenced only by `sample/RawRabbit.AspNet.Sample/RawRabbit.AspNet.Sample.csproj` (out of sln) |
| A4 | Code-in-spec | No in-sln `<PackageReference>` uses `VersionOverride`, `PrivateAssets`, `IncludeAssets`, `ExcludeAssets`, `GeneratePathProperty`, `Aliases`, or `Version="$(...)"`. Only `Include="X" Version="literal"` shapes are present. | Same grep as A3; every line matches `<PackageReference Include="..." Version="..." />` shape exactly |
| A5 | Code-in-spec | Both modern test csprojs (`test/RawRabbit.Tests/RawRabbit.Tests.csproj`, `test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj`) follow the same shape: `Authors=par.dahlman` (dotted form), `AssemblyName`, `PackageId`, 3× `GenerateAssembly*=false`, no `IsPackable`, no IconUrl/ProjectUrl. IntegrationTests adds `GenerateRuntimeConfigurationFiles=true` and a `<None Update="xunit.runner.json">` block. | `Read` of both csproj files confirmed |
| A6 | Code-in-spec | `sample/RawRabbit.ConsoleApp.Sample/RawRabbit.ConsoleApp.Sample.csproj` has `OutputType=Exe`, no `Authors`/IconUrl/ProjectUrl/VersionPrefix, 3× `GenerateAssembly*=false`, 7 `<ProjectReference>`, 3 `<PackageReference>`. `sample/RawRabbit.Messages.Sample/RawRabbit.Messages.Sample.csproj` has only `TargetFramework`, `AssemblyName`, `PackageId`, 3× `GenerateAssembly*=false`, 1 `<ProjectReference>` (no PackageReferences). | `Read` of both files confirmed |
| A7 | Code-in-spec | Both `sample/RawRabbit.AspNet.Sample/RawRabbit.AspNet.Sample.csproj` and `test/RawRabbit.PerformanceTest/RawRabbit.PerformanceTest.csproj` exist on disk with inline `<PackageReference Version="...">` lines (12 and 4 lines respectively) | `Read` of both files confirmed; AspNet.Sample still on `netcoreapp2.0`, PerformanceTest still on `netcoreapp1.1` (untouched in Phase 1 per V3.1/V3.2 decisions) |
| A8 | External docs | .NET 10 SDK supports CPM (`<ManagePackageVersionsCentrally>`) | https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management — feature is GA since NuGet 6.2 / VS 2022 17.4; no upper version restriction |
| A9 | External docs | `<ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>` per-project is the documented opt-out | Same source, "Disabling Central Package Management" section |
| A10 | External docs | `Directory.Build.props` is imported BEFORE the csproj's PropertyGroup; properties set there are defaults overridable by the csproj | https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory — "Properties that are set in Directory.Build.props can be overridden elsewhere in the project file or in imported files, so you should think of the settings in Directory.Build.props as specifying the defaults for your projects." |
| A11 | Consumer impact | Nothing outside csprojs references `PackageIconUrl`, `PackageProjectUrl`, or `VersionPrefix` literally in a way that breaks when we move them | `grep -rl "PackageIconUrl\|PackageProjectUrl\|VersionPrefix"` returned only 4 docs files: 3 Phase 1 docs in `docs/specs/`, `docs/plans/`, `docs/reviews/` and 1 handoff. All historical references; no CI scripts, READMEs, or build scripts depend on these living in csprojs |
| A12 | Consumer impact | `src/RawRabbit.Operations.Tools/RawRabbit.Operations.Tools.csproj` contains a duplicate `<PackageProjectUrl>` line | Per-csproj grep showed `<PackageProjectUrl>` appearing twice in this file's PropertyGroup — latent data-quality bug. MSBuild last-wins so behavior is unchanged. Side-fix per §3.2 row 4f. |
| A13 | Phase 1 baseline | HEAD is `6b7614b` on branch `2.0`; build green; tests 30 passing / 9 skipped / 0 failed | `git log --oneline -1` returned `6b7614b Skip 9 broker-layer tests pending Phase 5/7 modernization`; Phase 1 handoff `handoffs/2026-05-12_00-38-17_phase-1-complete-skill-fixes-shipped.md` §1 records the test counts as the result of Task 8 verification |

## 7. Known issues, accepted as out of scope

In addition to Phase 1's accepted known issue (`RawRabbit.Enrichers.HttpContext` compiles to empty pass-through on net10), Phase 2 explicitly accepts:

- **Heterogeneous `Authors` values** stay heterogeneous (5 distinct values, plus `Operations.Tools` missing `Authors` entirely). Per the user's "verbatim" intent. A future cleanup phase could normalize if and when publishing intent appears.
- **`Compatibility.Legacy` keeps no `<VersionPrefix>`** (defaults to 1.0.0 vs the 2.0.0 the rest carry). Same rationale.
- **`Enrichers.MessagePack`, `Enrichers.Protobuf`, `Enrichers.ZeroFormatter` keep emitting `AssemblyConfiguration`/`Company`/`Product` attributes** (the 3 csprojs missing the suppression flags). Cosmetic IL metadata; same rationale.
- **`PackageIconUrl=http://pardahlman.se/raw/icon.png`** stays as a literal URL pointing to a personal domain. Per no-publish intent.
- **`PackageProjectUrl=https://github.com/pardahlman/RawRabbit`** stays pointing at upstream rather than the working fork (`yv01p/rawrabbit`). Per no-publish intent.

User-acknowledged on 2026-05-12 during brainstorming.
