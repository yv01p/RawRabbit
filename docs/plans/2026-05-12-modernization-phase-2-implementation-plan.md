# Modernization Phase 2 Implementation Plan

> **For agentic workers:** REQUIRED: Use `superpowers:subagent-driven-development` to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Source spec:** `docs/specs/2026-05-12-modernization-phase-2-design.md` (commit SHA: `30e32edb` — includes the CDR round 1 finding fix and the §4 acceptance correction surfaced during plan-write)

**Goal:** After this plan executes, on a clean clone with the .NET 10 SDK installed, `dotnet restore && dotnet build -c Release && dotnet test test/RawRabbit.Tests --no-build -c Release && dotnet test test/RawRabbit.Enrichers.Polly.Tests --no-build -c Release && dotnet pack -c Release --no-build` succeeds end-to-end. Tests stay 30 passing / 9 skipped / 0 failed (Phase 1 baseline). `dotnet pack` produces 25 `.nupkg` files for src libraries; 0 from tests/samples. Warning shape: same as Phase 1 plus expected pack-time NU5048 / NU5125 (per spec §1).

**Architecture:** Two new MSBuild files at repo root (`Directory.Build.props` for the 3 truly-uniform metadata properties; `Directory.Packages.props` enabling Central Package Management with all 17 in-sln package versions). 25 src csprojs lose 3 redundant property lines each (consolidated into Build.props). 5 in-sln test/sample csprojs lose `<TargetFramework>` and 4 of them gain `<IsPackable>false</IsPackable>`. 13 in-sln csprojs lose inline `Version="..."` attributes from `<PackageReference>` lines. 2 dropped-from-sln csprojs (AspNet.Sample, PerformanceTest) gain `<ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>` to preserve their explicit-build behavior. The duplicate `<PackageProjectUrl>` line in `Operations.Tools.csproj` is removed as a side effect of the bulk deletion (no standalone task needed; spec §3.2 row 4f and §4 acceptance are fully addressed by Task 2).

**Tech stack (inherited from spec):** .NET 10 SDK 10.0.107 (pinned via `global.json` from Phase 1); MSBuild's `Directory.Build.props` / `Directory.Packages.props` convention; NuGet Central Package Management (GA since NuGet 6.2 / VS 2022 17.4). All package versions stay at their Phase 1 values (Phases 4/5/7 will bump majors).

---

## File Structure

**Create (2):**
- `Directory.Build.props` — repo root; carries `<TargetFramework>net10.0</TargetFramework>`, `<PackageIconUrl>`, `<PackageProjectUrl>` as defaults inheritable by every csproj under the tree.
- `Directory.Packages.props` — repo root; enables CPM (`<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`) and lists 17 `<PackageVersion>` entries.

**Modify (33):**
- 25 csprojs in `src/*` (Task 2): delete 3 redundant property lines; for the 9 of those with `<PackageReference>` lines, also strip inline `Version="..."` (Task 4)
- 3 in-sln test csprojs in `test/*` (Task 3): delete `<TargetFramework>`; add `<IsPackable>false</IsPackable>` to 2 of them (Polly Tests already has it); strip `Version="..."` from `<PackageReference>` lines in all 3 (Task 4)
- 2 in-sln sample csprojs in `sample/*` (Task 3): delete `<TargetFramework>`; add `<IsPackable>false</IsPackable>` to both; strip `Version="..."` from PackageReferences in ConsoleApp.Sample (Task 4 — Messages.Sample has none)
- 2 dropped-from-sln csprojs (Task 5): add `<ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>`. Otherwise untouched (`netcoreapp1.1` / `netcoreapp2.0` TFMs in their own csprojs override Build.props per A10).

**Test:** No new test files. Acceptance is "Phase 1 test counts unchanged" (29/7/0 + 1/2/0 = 30/9/0).

---

## Inherited from spec

The following assumptions were verified by `thorough-brainstorming` at spec-write time and are NOT re-verified here. Trusted as ground truth:

- **A1:** No pre-existing `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, or other `.props`/`.targets` file anywhere in the tree (verified via tree-wide `find`)
- **A2:** The 3 properties moving to Build.props (`TargetFramework=net10.0`, `PackageIconUrl=http://pardahlman.se/raw/icon.png`, `PackageProjectUrl=https://github.com/pardahlman/RawRabbit`) are uniformly valued across every src csproj that has them today (25/25)
- **A2-fail:** `Authors`, `VersionPrefix`, `GenerateAssembly*` are NOT uniform across src csprojs (5 distinct Authors values; 1 csproj missing Authors; 1 missing VersionPrefix; 3 missing GenerateAssembly*). Per spec §2.2, these stay per-csproj verbatim.
- **A3:** The 17 packages listed in Directory.Packages.props are exhaustive for in-sln csprojs
- **A4:** No in-sln `<PackageReference>` uses CPM-sensitive attributes (`VersionOverride`, `PrivateAssets`, etc.); only `Include="X" Version="literal"` shapes are present
- **A5:** Both modern test csprojs (`RawRabbit.Tests`, `RawRabbit.IntegrationTests`) follow the same shape with `Authors=par.dahlman`, 3× `GenerateAssembly*=false`, no `IsPackable`, no IconUrl/ProjectUrl
- **A6:** Sample csprojs (ConsoleApp.Sample, Messages.Sample) have minimal metadata — TargetFramework + AssemblyName + PackageId + 3× GenerateAssembly* + ProjectReferences; no Authors/IconUrl/ProjectUrl/VersionPrefix
- **A7:** Both dropped-from-sln csprojs (AspNet.Sample on `netcoreapp2.0`, PerformanceTest on `netcoreapp1.1`) exist on disk with inline `<PackageReference Version="...">` lines (12 and 4 respectively)
- **A8:** .NET 10 SDK supports CPM (GA since NuGet 6.2)
- **A9:** `<ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>` per-project is the documented opt-out
- **A10:** `Directory.Build.props` is imported BEFORE the csproj's PropertyGroup; csproj values override Build.props defaults
- **A11:** Nothing outside csprojs (CI scripts, READMEs, build scripts) references the metadata names in a way that breaks when consolidated
- **A12:** `src/RawRabbit.Operations.Tools/RawRabbit.Operations.Tools.csproj` contains a duplicate `<PackageProjectUrl>` line (latent data-quality bug; addressed naturally by Task 2's bulk deletion)
- **A13:** Phase 1 baseline holds at HEAD `6b7614b`: build green, tests 30 passing / 9 skipped / 0 failed

---

## Verified plan-level assumptions

Newly introduced by this plan (paths, commands, ordering, code-in-plan validity, consumer impact) and verified at plan-write time against HEAD `30e32edb`:

| # | Category | Assumption | Evidence |
|---|---|---|---|
| P1 | File path | All 32 csproj paths still exist at the expected locations at HEAD `30e32edb` (spec verified at `6b7614b`; spec/CDR commits since don't touch csprojs) | `find src test sample -name "*.csproj" -not -path "*/bin/*" -not -path "*/obj/*"` returned exactly the 32 expected paths (25 src + 4 test + 3 sample) |
| P2 | File path | `Directory.Build.props` and `Directory.Packages.props` still don't exist at HEAD `30e32edb` (Task 1 / Task 4 will create them) | `find . -maxdepth 2 -name "Directory.*"` returned no results |
| P3 | Command | `dotnet --version` returns `10.0.107`; `global.json` pins this version with `rollForward: latestFeature` | `dotnet --version` returned `10.0.107`; `cat global.json` confirmed pin |
| P4 | Task ordering | Each task's commit leaves the build green: Task 1 (Build.props create only — csprojs still inline-declare same values, no conflict per A10); Task 2 (delete 3 lines from 25 src csprojs — they now inherit from Build.props); Task 3 (same for 5 test/sample csprojs); Task 4 (atomic CPM creation + version stripping — the only viable order); Task 5 (opt-out for 2 dropped-from-sln csprojs — they're not in sln so build/restore unaffected). Task 4 must come after Tasks 2+3 (so all in-sln csprojs are normalized before CPM goes live). | Logical analysis against the 4-line MSBuild import order documented in spec A10 + the CPM rules in spec A8/A9 |
| P5 | Code-in-plan | The XML in Build.props (Task 1) and Packages.props (Task 4) is well-formed MSBuild | Manual element-by-element check: `<Project>` root (no Sdk attribute needed for props), `<PropertyGroup>`, `<ItemGroup>`, `<TargetFramework>`/`<PackageIconUrl>`/`<PackageProjectUrl>`/`<ManagePackageVersionsCentrally>`/`<PackageVersion>` are all standard MSBuild elements per the docs cited in spec A8/A10 |
| P6 | Consumer impact | Task 2's deletion of `<TargetFramework>`/`<PackageIconUrl>`/`<PackageProjectUrl>` from `src/RawRabbit/RawRabbit.csproj` and `src/RawRabbit.Compatibility.Legacy/RawRabbit.Compatibility.Legacy.csproj` does NOT accidentally clobber their `<DefineConstants>$(DefineConstants);LIBLOG_PORTABLE</DefineConstants>` lines | `grep -nE "DefineConstants\|TargetFramework\|PackageIconUrl\|PackageProjectUrl"` shows DefineConstants on line 17 in both files; the 3 properties to delete are on lines 8/12/13 — distinct |
| P7 | Consumer impact | No `<ProjectReference>` in any in-sln csproj uses a `Version=` attribute (which would be unusual); Task 4's "strip Version" rule only needs to target `<PackageReference>` | `grep -hE "ProjectReference" \| grep -i "version"` returned no results |
| P8 | Consumer impact | Non-PropertyGroup blocks in test/sample csprojs (`<None Update="rawrabbit.json">` in ConsoleApp.Sample, `<None Update="xunit.runner.json">` in IntegrationTests, `<Service Include=...>` in PerformanceTest) live in separate `<ItemGroup>` blocks and are NOT affected by Tasks 3/5 PropertyGroup edits | `grep -nE "<None Update\|<Service Include"` confirmed: ConsoleApp.Sample line 14, IntegrationTests line 48, PerformanceTest line 26 — all in ItemGroups, separate from PropertyGroup |
| P9 | File path | Exactly 13 in-sln csprojs have ≥1 `<PackageReference>` (drives Task 4's Modify list precisely): 9 src csprojs (RawRabbit:2 PRs; DI.Autofac:1; DI.Ninject:1; DI.ServiceCollection:1; Enrichers.MessagePack:1; Enrichers.Polly:1; Enrichers.Protobuf:1; Enrichers.ZeroFormatter:1; Operations.StateMachine:1) + 3 test csprojs (RawRabbit.Tests:4; IntegrationTests:4; Enrichers.Polly.Tests:5) + 1 sample csproj (ConsoleApp.Sample:3) | `for f in <in-sln csprojs>; do grep -c "<PackageReference" $f; done` returned the per-file PR counts above; total 13 csprojs need version stripping |
| P10 | Command | Phase 1's commit message convention is short imperative single-line subject, no Conventional-Commits prefix (e.g., "Modernize src/ csprojs to net10.0", "Delete legacy AssemblyInfo.cs files") | `git log --oneline -15` shows consistent style across both Phase 1 and Phase 2 spec/CDR commits |

---

## Tasks

### Task 1: Create Directory.Build.props

**Files:**
- Create: `Directory.Build.props`

- [ ] **Step 1: Create `Directory.Build.props` at repo root** with this exact content:
  ```xml
  <Project>
    <PropertyGroup>
      <TargetFramework>net10.0</TargetFramework>
      <PackageIconUrl>http://pardahlman.se/raw/icon.png</PackageIconUrl>
      <PackageProjectUrl>https://github.com/pardahlman/RawRabbit</PackageProjectUrl>
    </PropertyGroup>
  </Project>
  ```
  Note: file is added but no csproj is modified yet. Build stays green because every existing csproj still inline-declares these 3 properties (with the same values), and per spec A10 the csproj's value wins over Build.props.

- [ ] **Step 2: Verify build still green**
  ```bash
  dotnet build -c Release
  ```
  Expect: 0 errors, same warning shape as Phase 1.

- [ ] **Step 3: Commit**
  ```bash
  git add Directory.Build.props
  git commit -m "Add Directory.Build.props with shared TFM and package URL metadata"
  ```

### Task 2: Remove 3 redundant property lines from 25 src csprojs

**Files (Modify, 25):**
- `src/RawRabbit/RawRabbit.csproj`
- `src/RawRabbit.Compatibility.Legacy/RawRabbit.Compatibility.Legacy.csproj`
- `src/RawRabbit.DependencyInjection.Autofac/RawRabbit.DependencyInjection.Autofac.csproj`
- `src/RawRabbit.DependencyInjection.Ninject/RawRabbit.DependencyInjection.Ninject.csproj`
- `src/RawRabbit.DependencyInjection.ServiceCollection/RawRabbit.DependencyInjection.ServiceCollection.csproj`
- `src/RawRabbit.Enrichers.Attributes/RawRabbit.Enrichers.Attributes.csproj`
- `src/RawRabbit.Enrichers.GlobalExecutionId/RawRabbit.Enrichers.GlobalExecutionId.csproj`
- `src/RawRabbit.Enrichers.HttpContext/RawRabbit.Enrichers.HttpContext.csproj`
- `src/RawRabbit.Enrichers.MessageContext.Respond/RawRabbit.Enrichers.MessageContext.Respond.csproj`
- `src/RawRabbit.Enrichers.MessageContext.Subscribe/RawRabbit.Enrichers.MessageContext.Subscribe.csproj`
- `src/RawRabbit.Enrichers.MessageContext/RawRabbit.Enrichers.MessageContext.csproj`
- `src/RawRabbit.Enrichers.MessagePack/RawRabbit.Enrichers.MessagePack.csproj`
- `src/RawRabbit.Enrichers.Polly/RawRabbit.Enrichers.Polly.csproj`
- `src/RawRabbit.Enrichers.Protobuf/RawRabbit.Enrichers.Protobuf.csproj`
- `src/RawRabbit.Enrichers.QueueSuffix/RawRabbit.Enrichers.QueueSuffix.csproj`
- `src/RawRabbit.Enrichers.RetryLater/RawRabbit.Enrichers.RetryLater.csproj`
- `src/RawRabbit.Enrichers.ZeroFormatter/RawRabbit.Enrichers.ZeroFormatter.csproj`
- `src/RawRabbit.Operations.Get/RawRabbit.Operations.Get.csproj`
- `src/RawRabbit.Operations.MessageSequence/RawRabbit.Operations.MessageSequence.csproj`
- `src/RawRabbit.Operations.Publish/RawRabbit.Operations.Publish.csproj`
- `src/RawRabbit.Operations.Request/RawRabbit.Operations.Request.csproj`
- `src/RawRabbit.Operations.Respond/RawRabbit.Operations.Respond.csproj`
- `src/RawRabbit.Operations.StateMachine/RawRabbit.Operations.StateMachine.csproj`
- `src/RawRabbit.Operations.Subscribe/RawRabbit.Operations.Subscribe.csproj`
- `src/RawRabbit.Operations.Tools/RawRabbit.Operations.Tools.csproj` *(also has duplicate `<PackageProjectUrl>` line per spec A12 — both lines get deleted; subsumes spec §3.2 row 4f side-fix)*

- [ ] **Step 1: For each of the 25 csprojs above, delete these 3 lines from the `<PropertyGroup>`:**
  - `<TargetFramework>net10.0</TargetFramework>`
  - `<PackageIconUrl>http://pardahlman.se/raw/icon.png</PackageIconUrl>`
  - `<PackageProjectUrl>https://github.com/pardahlman/RawRabbit</PackageProjectUrl>`

  For `Operations.Tools.csproj` specifically: delete BOTH copies of the `<PackageProjectUrl>` line.

  Do NOT touch any other lines in any csproj. Specifically preserve:
  - `<DefineConstants>$(DefineConstants);LIBLOG_PORTABLE</DefineConstants>` in `RawRabbit.csproj` (line 17) and `Compatibility.Legacy.csproj` (line 17)
  - All `<Description>`, `<AssemblyTitle>`, `<VersionPrefix>`, `<Authors>`, `<AssemblyName>`, `<PackageId>`, `<PackageTags>`, `<GenerateAssembly*>` lines (per spec §2.2: heterogeneous metadata stays per-csproj)
  - All `<ProjectReference>` and `<PackageReference>` lines (Task 4 will strip the `Version=` attribute from PackageReferences)

- [ ] **Step 2: Verify the right number of lines were deleted**
  ```bash
  grep -c "<TargetFramework>" src/*/*.csproj
  grep -c "<PackageIconUrl>" src/*/*.csproj
  grep -c "<PackageProjectUrl>" src/*/*.csproj
  ```
  Expect: all three counts return 0 across `src/*` (was 25/25/26 — the 26 PackageProjectUrl count includes the Operations.Tools duplicate).

- [ ] **Step 3: Verify DefineConstants preserved**
  ```bash
  grep -l "DefineConstants" src/*/*.csproj
  ```
  Expect: 2 lines (`src/RawRabbit/RawRabbit.csproj`, `src/RawRabbit.Compatibility.Legacy/RawRabbit.Compatibility.Legacy.csproj`).

- [ ] **Step 4: Verify build still green**
  ```bash
  dotnet build -c Release
  ```
  Expect: 0 errors. Csprojs now inherit the 3 properties from Build.props.

- [ ] **Step 5: Commit**
  ```bash
  git add src/
  git commit -m "Move TFM, PackageIconUrl, PackageProjectUrl to Directory.Build.props for src csprojs"
  ```

### Task 3: Modify 5 in-sln test/sample csprojs (delete TFM, add IsPackable=false)

**Files (Modify, 5):**
- `test/RawRabbit.Tests/RawRabbit.Tests.csproj` — delete `<TargetFramework>`; add `<IsPackable>false</IsPackable>`
- `test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj` — delete `<TargetFramework>`; add `<IsPackable>false</IsPackable>`
- `test/RawRabbit.Enrichers.Polly.Tests/RawRabbit.Enrichers.Polly.Tests.csproj` — delete `<TargetFramework>`; (already has `<IsPackable>false</IsPackable>`, do NOT add a duplicate)
- `sample/RawRabbit.ConsoleApp.Sample/RawRabbit.ConsoleApp.Sample.csproj` — delete `<TargetFramework>`; add `<IsPackable>false</IsPackable>`
- `sample/RawRabbit.Messages.Sample/RawRabbit.Messages.Sample.csproj` — delete `<TargetFramework>`; add `<IsPackable>false</IsPackable>`

- [ ] **Step 1: For each of the 5 csprojs above, delete the line `<TargetFramework>net10.0</TargetFramework>` from the `<PropertyGroup>`.** TFM will inherit from Build.props.

- [ ] **Step 2: For 4 of the 5 csprojs (all except Polly.Tests), add `<IsPackable>false</IsPackable>` inside the `<PropertyGroup>`.** Place it immediately after the line where `<TargetFramework>` was (so the property order in the file stays sensible). Polly.Tests already has this on line 5 — do not modify or duplicate.

- [ ] **Step 3: Do NOT touch other lines.** Preserve `<Authors>par.dahlman</Authors>`, `<AssemblyName>`, `<PackageId>`, `<GenerateRuntimeConfigurationFiles>` (IntegrationTests only), `<OutputType>` (ConsoleApp.Sample only), all `<GenerateAssembly*>` flags, all `<ItemGroup>` blocks (including `<None Update="rawrabbit.json">` in ConsoleApp.Sample line 14, `<None Update="xunit.runner.json">` in IntegrationTests line 48, all `<ProjectReference>` and `<PackageReference>` blocks).

- [ ] **Step 4: Verify**
  ```bash
  grep -c "<TargetFramework>" test/RawRabbit.Tests/RawRabbit.Tests.csproj test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj test/RawRabbit.Enrichers.Polly.Tests/RawRabbit.Enrichers.Polly.Tests.csproj sample/RawRabbit.ConsoleApp.Sample/RawRabbit.ConsoleApp.Sample.csproj sample/RawRabbit.Messages.Sample/RawRabbit.Messages.Sample.csproj
  ```
  Expect: all 5 return 0.
  ```bash
  grep -c "<IsPackable>false</IsPackable>" test/RawRabbit.Tests/RawRabbit.Tests.csproj test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj test/RawRabbit.Enrichers.Polly.Tests/RawRabbit.Enrichers.Polly.Tests.csproj sample/RawRabbit.ConsoleApp.Sample/RawRabbit.ConsoleApp.Sample.csproj sample/RawRabbit.Messages.Sample/RawRabbit.Messages.Sample.csproj
  ```
  Expect: all 5 return 1.

- [ ] **Step 5: Verify build + tests still green**
  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Tests --no-build -c Release
  dotnet test test/RawRabbit.Enrichers.Polly.Tests --no-build -c Release
  ```
  Expect: 0 errors; tests 29 passed / 7 skipped / 0 failed (RawRabbit.Tests) and 1 passed / 2 skipped / 0 failed (Polly.Tests).

- [ ] **Step 6: Commit**
  ```bash
  git add test/ sample/
  git commit -m "Move TFM to Directory.Build.props for in-sln test and sample csprojs; mark non-packable"
  ```

### Task 4: Create Directory.Packages.props + strip inline Version from in-sln PackageReferences (atomic)

This task is atomic by necessity: enabling CPM via `Directory.Packages.props` while any csproj still has inline `<PackageReference Version="...">` triggers NU1008. Both halves must commit together.

**Files (Create, 1):**
- `Directory.Packages.props`

**Files (Modify, 13 — every in-sln csproj with ≥1 PackageReference):**
- `src/RawRabbit/RawRabbit.csproj` (2 PRs)
- `src/RawRabbit.DependencyInjection.Autofac/RawRabbit.DependencyInjection.Autofac.csproj` (1 PR)
- `src/RawRabbit.DependencyInjection.Ninject/RawRabbit.DependencyInjection.Ninject.csproj` (1 PR)
- `src/RawRabbit.DependencyInjection.ServiceCollection/RawRabbit.DependencyInjection.ServiceCollection.csproj` (1 PR)
- `src/RawRabbit.Enrichers.MessagePack/RawRabbit.Enrichers.MessagePack.csproj` (1 PR)
- `src/RawRabbit.Enrichers.Polly/RawRabbit.Enrichers.Polly.csproj` (1 PR)
- `src/RawRabbit.Enrichers.Protobuf/RawRabbit.Enrichers.Protobuf.csproj` (1 PR)
- `src/RawRabbit.Enrichers.ZeroFormatter/RawRabbit.Enrichers.ZeroFormatter.csproj` (1 PR)
- `src/RawRabbit.Operations.StateMachine/RawRabbit.Operations.StateMachine.csproj` (1 PR)
- `test/RawRabbit.Tests/RawRabbit.Tests.csproj` (4 PRs)
- `test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj` (4 PRs)
- `test/RawRabbit.Enrichers.Polly.Tests/RawRabbit.Enrichers.Polly.Tests.csproj` (5 PRs)
- `sample/RawRabbit.ConsoleApp.Sample/RawRabbit.ConsoleApp.Sample.csproj` (3 PRs)

- [ ] **Step 1: Create `Directory.Packages.props` at repo root** with this exact content:
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

- [ ] **Step 2: For each of the 13 csprojs above, strip the `Version="..."` attribute from every `<PackageReference Include="..." Version="..." />` line.** Each line transforms from:
  ```xml
  <PackageReference Include="X" Version="Y" />
  ```
  to:
  ```xml
  <PackageReference Include="X" />
  ```
  Do NOT touch `<ProjectReference>` lines (they don't have Version attributes per P7).

- [ ] **Step 3: Verify CPM resolution works**
  ```bash
  dotnet restore
  ```
  Expect: 0 errors (in particular, no NU1008 — would fire if any in-sln csproj still had an inline Version under CPM; no NU1010 — would fire if any PackageReference lacked a matching PackageVersion in central props). Same NU1701/NU1902/NU1903 warnings as Phase 1.

- [ ] **Step 4: Verify build + tests still green**
  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Tests --no-build -c Release
  dotnet test test/RawRabbit.Enrichers.Polly.Tests --no-build -c Release
  ```
  Expect: 0 errors; tests 29/7/0 + 1/2/0.

- [ ] **Step 5: Commit**
  ```bash
  git add Directory.Packages.props src/ test/ sample/RawRabbit.ConsoleApp.Sample/RawRabbit.ConsoleApp.Sample.csproj
  git commit -m "Adopt Central Package Management (Directory.Packages.props) for in-sln csprojs"
  ```

### Task 5: Opt 2 dropped-from-sln csprojs out of CPM

**Files (Modify, 2):**
- `sample/RawRabbit.AspNet.Sample/RawRabbit.AspNet.Sample.csproj`
- `test/RawRabbit.PerformanceTest/RawRabbit.PerformanceTest.csproj`

- [ ] **Step 1: For each csproj above, add `<ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>` to the `<PropertyGroup>`.** Place it as the first or last property in the existing PropertyGroup — either is fine. This opts the csproj out of CPM so its 12 / 4 inline `<PackageReference Version="...">` lines remain valid (they would otherwise trigger NU1008 if anyone explicitly built the csproj).

- [ ] **Step 2: Do NOT touch other lines.** Preserve `<TargetFramework>netcoreapp2.0</TargetFramework>` (AspNet.Sample) / `<TargetFramework>netcoreapp1.1</TargetFramework>` (PerformanceTest) — these are explicit values that override Build.props per A10. Preserve all `<PackageReference>` lines (with their inline Versions intact). Preserve the `<Service Include="...">` block in PerformanceTest (line 26).

- [ ] **Step 3: Verify sln build still works** (these csprojs are not in the sln, so this confirms no cross-contamination)
  ```bash
  dotnet build -c Release
  ```
  Expect: 0 errors. (We don't try to build the dropped-from-sln csprojs explicitly — Phase 1 V3.1/V3.2 decided they're out of scope until Phase 5/6 rewrites them.)

- [ ] **Step 4: Commit**
  ```bash
  git add sample/RawRabbit.AspNet.Sample/RawRabbit.AspNet.Sample.csproj test/RawRabbit.PerformanceTest/RawRabbit.PerformanceTest.csproj
  git commit -m "Opt dropped-from-sln csprojs out of CPM to preserve their explicit-build behavior"
  ```

### Task 6: Verify acceptance per spec §4

**Files:** none modified; verification only.

- [ ] **Step 1: Confirm SDK selection**
  ```bash
  dotnet --version
  ```
  Expect: `10.0.107` (driven by `global.json`).

- [ ] **Step 2: Restore**
  ```bash
  dotnet restore
  ```
  Expect: 0 errors. Warnings: NU1701 + NU1902 + NU1903 (Phase 1 baseline) only.

- [ ] **Step 3: Build**
  ```bash
  dotnet build -c Release
  ```
  Expect: 0 errors. Same warning shape as Phase 1.

- [ ] **Step 4: Tests stay green**
  ```bash
  dotnet test test/RawRabbit.Tests --no-build -c Release --blame-hang-timeout 15s
  dotnet test test/RawRabbit.Enrichers.Polly.Tests --no-build -c Release
  ```
  Expect: 29 passed / 7 skipped / 0 failed (RawRabbit.Tests); 1 passed / 2 skipped / 0 failed (Polly.Tests). Total 30/9/0 — matches Phase 1 baseline.

- [ ] **Step 5: Pack works**
  ```bash
  dotnet pack -c Release --no-build
  ```
  Expect: 0 errors. Pack-time NU5048 (deprecated PackageIconUrl) and NU5125 (missing license metadata) warnings expected per spec §1 / §2.3 — these are acceptable. Produces `.nupkg` files for the 25 src libraries; 0 from tests/samples (verified via the `<IsPackable>false</IsPackable>` flags).

  ```bash
  find . -path "*/bin/Release/*.nupkg" -not -path "*/AspNet.Sample/*" -not -path "*/PerformanceTest/*" | wc -l
  ```
  Expect: `25`.

- [ ] **Step 6: Side-fix verification (per spec §4 corrected acceptance)**
  ```bash
  grep -c PackageProjectUrl src/RawRabbit.Operations.Tools/RawRabbit.Operations.Tools.csproj
  ```
  Expect: `0` (was 2 before; both lines deleted by Task 2 — naturally subsumes the duplicate-fix).

- [ ] **Step 7: If all six steps pass, Phase 2 is complete.** No commit for this task — pure verification. If any step fails, consult spec §5 (Risks & responses) for the recovery template.

---

## Tasks NOT in this plan

Inherited verbatim from spec §2.3 ("Out of scope (deferred)"):

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

A new spec → new plan cycle is required to add any of these.

## Known issues inherited from spec

In addition to Phase 1's accepted known issue (`RawRabbit.Enrichers.HttpContext` compiles to empty pass-through on net10), Phase 2 explicitly accepts:

- **Heterogeneous `Authors` values** stay heterogeneous (5 distinct values, plus `Operations.Tools` missing `Authors` entirely). Per the user's "verbatim" intent. A future cleanup phase could normalize if and when publishing intent appears.
- **`Compatibility.Legacy` keeps no `<VersionPrefix>`** (defaults to 1.0.0 vs the 2.0.0 the rest carry). Same rationale.
- **`Enrichers.MessagePack`, `Enrichers.Protobuf`, `Enrichers.ZeroFormatter` keep emitting `AssemblyConfiguration`/`Company`/`Product` attributes** (the 3 csprojs missing the suppression flags). Cosmetic IL metadata; same rationale.
- **`PackageIconUrl=http://pardahlman.se/raw/icon.png`** stays as a literal URL pointing to a personal domain. Per no-publish intent.
- **`PackageProjectUrl=https://github.com/pardahlman/RawRabbit`** stays pointing at upstream rather than the working fork (`yv01p/rawrabbit`). Per no-publish intent.

User-acknowledged on 2026-05-12 during brainstorming.
