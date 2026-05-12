# Critical Design Review: 2026-05-12-modernization-phase-2-design (Round 1)

**Spec:** `/home/yv01p/rawrabbit/docs/specs/2026-05-12-modernization-phase-2-design.md`
**Verified Assumptions section:** present

## 1. Verified-assumptions cross-check

Fresh read against cited evidence at HEAD `6b7614b`:

| # | Assumption | Status |
|---|---|---|
| A1 | No pre-existing `Directory.*` MSBuild files | **Reconfirmed.** `find` over the tree returns no results. |
| A2 | The 3 properties moving to Build.props are uniformly valued | **Reconfirmed.** Per-csproj grep shows `TargetFramework=net10.0`, `PackageIconUrl=http://pardahlman.se/raw/icon.png`, `PackageProjectUrl=https://github.com/pardahlman/RawRabbit` are identical across the 25 src csprojs that have them. |
| A2-fail | `Authors`, `VersionPrefix`, `GenerateAssembly*` are NOT uniform | **Reconfirmed.** Per-csproj grep at spec-write time documented the heterogeneity exactly as listed. Spec adopts option α (these stay per-csproj). |
| A3 | The 17-package `Directory.Packages.props` list is exhaustive for in-sln csprojs | **Reconfirmed.** Independent re-grep returned exactly 17 distinct package IDs across `src/`, `test/RawRabbit.Tests`, `test/RawRabbit.IntegrationTests`, `test/RawRabbit.Enrichers.Polly.Tests`, and the 2 in-sln samples. |
| A4 | No in-sln `<PackageReference>` uses CPM-sensitive attributes | **Reconfirmed.** Independent re-grep shows every in-sln line matches `<PackageReference Include="..." Version="..." />` shape exactly; no `PrivateAssets`/`IncludeAssets`/etc. |
| A5 | Both modern test csprojs share the documented shape | **Reconfirmed.** `Read` of both files matches description. |
| A6 | Sample csprojs match the documented shape | **Reconfirmed.** `Read` of both files matches description. |
| A7 | Dropped-from-sln csprojs exist on disk with inline versions | **Reconfirmed.** Both files present at expected paths. |
| A8 | .NET 10 SDK supports CPM | **Reconfirmed.** MS docs confirm CPM is GA since NuGet 6.2; no upper-version restriction. |
| A9 | Per-project CPM opt-out via `ManagePackageVersionsCentrally=false` | **Reconfirmed.** MS docs "Disabling Central Package Management" section verbatim. |
| A10 | Directory.Build.props is imported BEFORE the csproj's PropertyGroup | **Reconfirmed.** MS docs: "settings in Directory.Build.props as specifying the defaults for your projects" — csproj overrides Build.props. |
| A11 | Nothing outside csprojs references the metadata literally in a way that breaks | **Reconfirmed.** Re-grep: only 4 docs/handoffs mention the metadata names; no CI scripts, READMEs, or build scripts. |
| A12 | `RawRabbit.Operations.Tools.csproj` has duplicate `<PackageProjectUrl>` | **Reconfirmed.** Per-csproj grep shows the line appears twice in the PropertyGroup. |
| A13 | Phase 1 baseline holds | **Reconfirmed.** `git log --oneline -1` returns `6b7614b`; handoff records 30/9/0. |

All 13 verified assumptions reconfirmed.

## 2. Literal-wrongness findings

### Finding 1: `dotnet pack` will emit NU5048 (and likely NU5125/NU5128) warnings, contradicting the §1 success-criteria phrase "NU1701/NU1902/NU1903 only — no new errors"

**Description.** §1 success criteria states "with the same warning shape as Phase 1 (NU1701/NU1902/NU1903 only — no new errors)." The criteria sequence introduces `dotnet pack -c Release --no-build` as the final step. Phase 1 never ran `dotnet pack` (its Task 8 validated only `dotnet restore`, `dotnet build`, `dotnet test`), so any pack-time warning is by definition new.

After Phase 2:
- All 25 src csprojs inherit `<PackageIconUrl>http://pardahlman.se/raw/icon.png</PackageIconUrl>` from `Directory.Build.props`. NuGet's pack target emits **NU5048** ("The 'PackageIconUrl' element is deprecated. Consider using the 'PackageIcon' element instead.") once per project being packed → 25 NU5048 warnings.
- No src csproj sets `<PackageLicense>`, `<PackageLicenseExpression>`, `<PackageLicenseFile>`, or the deprecated `PackageLicenseUrl` (verified by grep — no hits across `src/*/*.csproj`). Pack will emit **NU5128** ("Some target frameworks declared in the dependencies group of the .nuspec and the lib/ref folder do not have exact matches in the other location...") and/or **NU5125** ("The 'licenseUrl' element will be deprecated.") variants depending on NuGet version. At minimum NU5125 fires across all 25 packages.

Under the literal reading of "NU1701/NU1902/NU1903 only," the success criterion is unmet. The spec's own Risk #4 partially anticipates this ("If pack fails... do NOT add cosmetic metadata to silence pack warnings — those are spec §2.3 deferred items"), creating an internal inconsistency: §1 forbids new warnings; Risk #4 anticipates them but only addresses pack failures (errors), not warnings.

**Evidence:**
- `docs/specs/2026-05-12-modernization-phase-2-design.md:21` — success criteria phrasing
- `docs/specs/2026-05-12-modernization-phase-2-design.md:14-19` — pack added to validation sequence
- `docs/specs/2026-05-12-modernization-phase-2-design.md:146` — Risk #4 anticipates pack issues but only addresses errors
- `docs/plans/2026-05-11-modernization-phase-1-implementation-plan.md:485-490` — Phase 1's Task 8 explicitly stops at `dotnet test`; never invokes `dotnet pack`
- Empirical grep across `src/*/*.csproj` for `<PackageLicense|<License>|<PackageReadme|<PackageReleaseNotes` — no hits
- NuGet docs: NU5048 and NU5125 are emitted whenever `PackageIconUrl`/`licenseUrl` are present and pack runs

**Proposed fix.** The user must pick one of three resolutions; the spec must explicitly say which:

1. **Update §1 success criteria language** to acknowledge expected pack-time warnings — e.g., change "NU1701/NU1902/NU1903 only — no new errors" to "NU1701/NU1902/NU1903 plus NU5048/NU5125 (cosmetic-metadata-deferred-to-publish-phase) — no new errors." This is the YAGNI choice and aligns with §2.3's deferral of `PackageIconUrl` migration. Risk #4 should drop the "warnings" framing (it really only handles errors).

2. **Add a `<NoWarn>NU5048;NU5125</NoWarn>` line to `Directory.Build.props`** to suppress these warnings at the source. Keeps §1 literal but adds noise that hides real future warnings of the same code.

3. **Migrate `PackageIconUrl` → `PackageIcon` and add a license expression now** as part of Phase 2. Contradicts the §2.3 deferral the user explicitly chose; expands scope.

Recommendation: option 1. It honors the user's "no publish, verbatim, defer cosmetic" stance and removes the internal inconsistency without changing the deliverable.

## 3. Forced decisions

No forced decisions found. (The NU5048/NU5125 resolution is folded into §2 Finding 1 as proposed-fix options rather than a standalone forced decision because the spec text is literally inconsistent rather than silent.)

## 5. Recommendation

⚠️ **Approve with literal-wrongness fixes**

§2 has one finding (success-criteria language inconsistency around `dotnet pack` warnings). Resolution is editorial, not architectural — pick one of the three options listed in Finding 1 and update the spec text accordingly. After that edit, the spec is ready for `thorough-writing-plans`. §3 is empty.
