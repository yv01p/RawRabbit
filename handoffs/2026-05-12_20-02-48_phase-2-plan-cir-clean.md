---
date: 2026-05-12T20:02:48+00:00
git_commit: 7883dfc910aa8d8f6bf86ddc8b78b04cb5cac8a7
branch: 2.0
repository: rawrabbit
topic: "RawRabbit modernization Phase 2 — spec/CDR/plan/CIR pipeline complete; ready for SDD"
tags: [handoff, session-transition, dotnet, net10, rabbitmq, msbuild, cpm, central-package-management, phase-2]
status: in_progress
last_updated: 2026-05-12
type: implementation_handoff
---

# Handoff: Phase 2 Plan + CIR Clean — Ready for SDD

## 0. Executive Summary (TL;DR)

1. Drove RawRabbit modernization Phase 2 through the full superpowers pipeline this session: `thorough-brainstorming` → `critical-design-review` → spec edit → `thorough-writing-plans` → `critical-implementation-review`. Five commits produced the spec, CDR, two spec edits responding to CDR/plan-write findings, and the implementation plan.
2. Stopped at: CIR round 1 verdict ✅ **Approve as-is** (10 verified plan-level assumptions reconfirmed; 0 literal-wrongness; 0 forced decisions). HEAD is `7883dfc` (plan commit). The CIR review file is **untracked** (not committed yet) — see §1.
3. Single most important next action: commit the untracked CIR file, then invoke `superpowers:subagent-driven-development` against `docs/plans/2026-05-12-modernization-phase-2-implementation-plan.md` to execute the 6-task plan.

## 1. Technical State

**Active Working Set** (files in high rotation right now):
- `docs/plans/2026-05-12-modernization-phase-2-implementation-plan.md:1` — Phase 2 plan, committed at `7883dfc`. The artifact SDD will execute next.
- `docs/specs/2026-05-12-modernization-phase-2-design.md:1` — Phase 2 spec, last edited at `30e32ed` (corrected §4 acceptance count). Source of truth for the plan.
- `docs/criticalreviews/2026-05-12-modernization-phase-2-design-critical-review-1.md:1` — CDR round 1, committed at `5056d44`. Found 1 literal-wrongness (success-criteria warning shape vs `dotnet pack`); fixed by `3fc6f7b`.
- `docs/criticalreviews/2026-05-12-modernization-phase-2-implementation-plan-critical-review-1.md:1` — **CIR round 1, untracked, just written.** Verdict ✅ Approve as-is.
- `handoffs/2026-05-11_22-21-27_modernization-phase-1-ready-for-sdd.md:1` — pre-Phase-1-SDD handoff from prior session (untracked, never committed; stale).

**Current Errors / Blockers:**
None.

**Environment:**
- Uncommitted changes: 2 untracked files in `handoffs/` and `docs/criticalreviews/`:
  - `docs/criticalreviews/2026-05-12-modernization-phase-2-implementation-plan-critical-review-1.md` (CIR round 1, just written this session)
  - `handoffs/2026-05-11_22-21-27_modernization-phase-1-ready-for-sdd.md` (stale Phase 1 handoff, pre-existing from a prior session)
- Staged changes: none.
- ENV vars or config required: none for handoff/planning. .NET 10 SDK 10.0.107 is installed and pinned via `global.json` (Phase 1 baseline).
- Any running processes / background jobs: none.

## 2. Progress Tracker

| Task | Status | Location | Notes |
|------|--------|----------|-------|
| Phase 2 brainstorming (thorough-brainstorming) | ✅ Complete | spec at `2689e86` | 13 assumptions verified; A2-fail surfaced heterogeneity that shifted Build.props from 8 properties down to 3 |
| Phase 2 CDR round 1 (critical-design-review) | ✅ Complete | review at `5056d44` | 1 literal-wrongness: success criteria forbade new warnings but introduced dotnet pack (NU5048) |
| Spec edit addressing CDR finding | ✅ Complete | commit `3fc6f7b` | §1 acknowledges expected NU5048/NU5125; Risk #4 tightened to errors-only |
| Spec edit correcting §4 acceptance count | ✅ Complete | commit `30e32ed` | Plan-write surfaced that side-fix grep would return 0, not 1 (Task 2 deletes BOTH PackageProjectUrl copies in Operations.Tools) |
| Phase 2 implementation plan (thorough-writing-plans) | ✅ Complete | plan at `7883dfc` | 10 plan-level assumptions verified; 6-task decomposition; CPM transition correctly identified as atomic |
| Phase 2 CIR round 1 (critical-implementation-review) | ✅ Complete | review at `docs/criticalreviews/2026-05-12-modernization-phase-2-implementation-plan-critical-review-1.md` (untracked) | ✅ Approve as-is — all 10 P-assumptions reconfirmed, 0 §2 findings, 0 §3 forced decisions |
| Commit CIR file | ⏳ Pending | — | User has not asked yet; trivial step |
| Optionally commit pending handoffs | ⏳ Pending | — | This handoff + the stale Phase 1 handoff |
| Phase 2 SDD execution (subagent-driven-development) | ⏳ Pending | — | The next action; ready to start |
| Phase 2 final code review | ⏳ Pending | — | Post-SDD step (per Phase 1's pattern) |
| Push Phase 2 commits to origin/2.0 | ⏳ Pending | — | After SDD lands |
| Phases 3–7 | ⏳ Pending | — | Per Phase 1 spec §1 decomposition |

## 3. Mental Model (Most Critical Section)

**Why the design has the shape it has — the verbatim-consolidation pivot:**

The original brainstorming draft proposed putting 8 properties (`TargetFramework`, `VersionPrefix`, `Authors`, `PackageIconUrl`, `PackageProjectUrl`, 3× `GenerateAssembly*`) into `Directory.Build.props`. User instruction was "verbatim move — don't touch values." Empirical verification (assumption A2-fail) revealed Authors/VersionPrefix/GenerateAssembly* are NOT uniformly valued across the 25 src csprojs:
- 5 distinct Authors values (`pardahlman` ×9, `pardahlman;enrique-avalon` ×10, `par.dahlman` ×1 in DI.Autofac, `par.dahlman;Joshua Barron` ×1 in DI.Ninject, `LordMike` ×2 in MessagePack/ZeroFormatter)
- 1 csproj missing Authors entirely (`Operations.Tools`)
- 1 csproj missing VersionPrefix (`Compatibility.Legacy`)
- 3 csprojs missing the 3× `GenerateAssembly*` flags (`Enrichers.MessagePack`, `Enrichers.Protobuf`, `Enrichers.ZeroFormatter`)

Per "verbatim" intent, only the truly uniform 3 properties moved to Build.props. The heterogeneous ones stay per-csproj as known-issue out-of-scope items (spec §7). This is the **option α** the user picked when surfaced with α/β/γ during brainstorming.

**Why the spec was edited twice after CDR/plan-write:**

CDR finding (commit `3fc6f7b`): §1 success criteria said "NU1701/NU1902/NU1903 only — no new errors" but introduced `dotnet pack` which Phase 1 never validated. NU5048 (deprecated `PackageIconUrl`) fires per src library × 25. Strict reading of "warning shape" was inconsistent with Risk #4's pack-warnings posture. Edit: §1 now reads "...plus expected pack-time NU5048/NU5125 warnings (related to the deferred PackageIconUrl/license metadata cleanup per §2.3) — no new errors." Risk #4 tightened to errors-only. (Note: NU5125 may not actually fire since no csproj has the deprecated `PackageLicenseUrl`; the actual second warning code might be different. The CDR introduced this inaccuracy; CIR did not re-litigate since spec correctness is CDR's beat, not CIR's.)

Plan-write finding (commit `30e32ed`): The spec §4 side-fix acceptance check originally said `grep -c PackageProjectUrl src/RawRabbit.Operations.Tools/RawRabbit.Operations.Tools.csproj` returns `1` after Phase 2. But Task 2's bulk deletion of `<PackageProjectUrl>` from src csprojs naturally removes BOTH copies in Operations.Tools (the Build.props consolidation subsumes the standalone side-fix). Plan would leave count at `0`, not `1`. User authorized inline spec edit to `returns 0`. The standalone side-fix task (spec §3.2 row 4f) is documented as subsumed in plan Task 2; no separate task needed.

**Why the plan is structured as 6 tasks (not more, not fewer):**

| Task | One commit (build green) | Why this granularity |
|------|--------------------------|----------------------|
| 1: Create Build.props | ✓ | Build green: csprojs still inline-declare same values, csproj wins per A10 |
| 2: Delete 3 lines from 25 src csprojs | ✓ | Build green: csprojs now inherit from Build.props |
| 3: Modify 5 in-sln test/sample csprojs (delete TFM, add IsPackable=false) | ✓ | Build green; semantically distinct from Task 2 (different csproj sets, different operations — IsPackable=false is a NEW behavior) |
| 4: Create Packages.props + strip Version in 13 csprojs | ✓ | **Atomic by necessity** — enabling CPM with any inline Version triggers NU1008. Both halves must commit together. |
| 5: Opt 2 dropped-from-sln csprojs out of CPM | ✓ | Independent of others; could go before/after/parallel. Plan puts it after Task 4 for noise-free linear ordering. |
| 6: Verify (build, test, pack) | (no commit) | Pure validation per spec §4 acceptance |

The decomposition mirrors Phase 1's granularity (8 tasks for similar surface area). Splitting Tasks 1+2 vs combining is a judgment call — kept separate for cleaner git blame and easier review per task.

**Codebase Gotchas Discovered This Session:**

- `src/RawRabbit.Operations.Tools/RawRabbit.Operations.Tools.csproj` — has a **duplicate** `<PackageProjectUrl>https://github.com/pardahlman/RawRabbit</PackageProjectUrl>` line in the same `<PropertyGroup>`. MSBuild last-wins so behavior was unaffected, but it's a latent data-quality bug. Naturally addressed by Task 2's bulk deletion (both lines go).
- `src/RawRabbit/RawRabbit.csproj:17` and `src/RawRabbit.Compatibility.Legacy/RawRabbit.Compatibility.Legacy.csproj:17` — both have `<DefineConstants>$(DefineConstants);LIBLOG_PORTABLE</DefineConstants>` on line 17, distinct from the 3 lines being deleted (lines 8/12/13). Task 2 must preserve these per Phase 1 V2 / plan task 4d. Plan §Task 2 Step 1 calls this out explicitly.
- 5 distinct `Authors` values across src csprojs — see "verbatim-consolidation pivot" above. Spec §2.3 / §7 catalogs all of them as known-issue out-of-scope. A future cleanup phase could normalize.
- All 25 src csprojs have `<Description>` (no NU5104 risk on pack). No csproj has `<PackageLicense>`/`<License>`/`<PackageReadme>`/`<PackageReleaseNotes>`. No csproj uses CPM-sensitive PackageReference attributes (`PrivateAssets`, `IncludeAssets`, etc. — only `Include="X" Version="Y"` shape).
- The 9 `[Fact(Skip="...")]` tests added in Phase 1.5c are untouched by Phase 2 (Phase 5/7 territory).
- `.NET 10 SDK 10.0.107` is what `dotnet --version` returns; `global.json` pins it with `rollForward: latestFeature`.

**Dead Ends — Do Not Repeat These:**

| Approach Tried | Why It Failed | Evidence |
|---------------|---------------|----------|
| Original brainstorming draft put 8 properties in Build.props with assumed-uniform values | Empirical verification (A2-fail) showed Authors/VersionPrefix/GenerateAssembly* are heterogeneous; "verbatim" intent forbids picking a single default | Spec §6 A2-fail; option α picked over β/γ |
| Original spec §4 side-fix acceptance: `grep -c PackageProjectUrl ... returns 1` | Plan would leave count at 0 (Task 2 deletes BOTH copies as part of bulk consolidation, not just the duplicate) | Spec correction `30e32ed`; plan Task 2's "subsumes spec §3.2 row 4f" note |
| Original spec §1 success criteria: "NU1701/NU1902/NU1903 only — no new errors" | Phase 2 introduces `dotnet pack` (Phase 1 didn't validate it); NU5048 fires per src library × 25 = 25 new warnings | CDR finding 1; spec edit `3fc6f7b` |
| (CDR-introduced inaccuracy — DO NOT spend cycles fixing in CIR) Spec/plan reference NU5125 alongside NU5048 as expected pack warnings | NU5125 fires only when `<PackageLicenseUrl>` is set; no csproj has it. Actual second warning code may differ. Editorial inaccuracy, not literal-wrongness. CIR explicitly didn't surface this since spec correctness is CDR's beat. | This handoff §3 "CDR introduced this inaccuracy" |

**Key Decisions Made:**

| Decision | Rationale | Alternative Rejected |
|----------|-----------|---------------------|
| Phase 2 scope: strictly metadata + CPM (option from brainstorming) | Smallest design that delivers spec §1 row 2; the 2 deferred Phase 1.5 questions stay out (AsyncLocal Get() to Phase 3/5; 9 broker tests to Phase 5/7) | Expanded scope (option B/C) — would muddy the phase concern |
| No publish | Personal modernization fork; cosmetic metadata not "fixed", only consolidated | "Maybe publish later" — would force fixing PackageIconUrl/ProjectUrl/Authors values |
| Verbatim consolidation only (option α) | User explicit intent; values vary across csprojs | Conservative with GenerateAssembly* centralized (β); maximum with picked defaults (γ) |
| Side-fix the duplicate `<PackageProjectUrl>` line in Operations.Tools as part of Phase 2 | Phase 2 already touches every csproj; cost is zero | Leave for future scrub |
| Stay on branch `2.0`, commit directly | Personal fork; 2.0 is also the main branch; same pattern as Phase 1 | Worktree (extra steps); feature branch (extra merge) |
| Plan as 6 tasks (vs Phase 1's 8) | YAGNI — combine adjacent similar work, keep distinct work in separate tasks; CPM atomicity drives Task 4 to be one chunky commit | Splitting Tasks 1+2 (kept separate); combining Task 5 with others (kept independent) |
| CIR didn't re-litigate spec's NU5125 inaccuracy | CIR v2 explicitly out-of-scope for spec-level concerns; that's CDR's beat | Surface as §2 finding (would violate "DON'T re-litigate the design") |

**Assumptions in Play:**

- `dotnet pack -c Release --no-build` will succeed on all 25 in-sln src libraries — this is the first time pack runs in the modernization. Pack-time warnings expected (NU5048 at minimum); no errors expected. If errors fire, follow spec Risk #4 (read the actual error; fold in if metadata-related; escalate to spec amendment if structural).
- The exact NU5xxx warning codes that fire may differ from what the spec/plan text says (e.g., NU5125 may not fire because no `PackageLicenseUrl` is set). The asked-for outcome (success, 0 errors) is met regardless. See "CDR-introduced inaccuracy" above.
- The 9 `[Fact(Skip="...")]` tests stay skipped post-Phase-2 (Phase 2 doesn't touch any `.cs` source). Phase 1 baseline is 30 passing / 9 skipped / 0 failed — Task 6 expects this exactly.
- SDD will respect Task 4's atomicity flag and commit Directory.Packages.props + version-stripping together. The plan note "atomic by necessity" is the contract.
- The dropped-from-sln csprojs (AspNet.Sample, PerformanceTest) are still dead code in Phase 2; Phase 5/6 will rewrite them.

## 4. Delta — Changes Made This Session

**On branch `2.0` (rawrabbit) — all committed and on local HEAD; not yet pushed to origin:**

```
7883dfc Add Phase 2 modernization implementation plan        ← TWP output
30e32ed Correct Phase 2 spec §4 side-fix acceptance count    ← plan-write surfaced
3fc6f7b Update Phase 2 spec to address CDR round 1 finding 1 ← CDR §2 fix
5056d44 Add critical design review for Phase 2 spec (round 1) ← CDR output
2689e86 Add Phase 2 modernization spec (package metadata + CPM) ← brainstorming output
```

**Uncommitted in working tree:**
- `docs/criticalreviews/2026-05-12-modernization-phase-2-implementation-plan-critical-review-1.md` — CIR round 1 just written; ✅ Approve as-is
- `handoffs/2026-05-11_22-21-27_modernization-phase-1-ready-for-sdd.md` — stale Phase 1 pre-SDD handoff (untracked from prior session, not Phase 2's concern)
- This handoff file (will be untracked after write)

## 5. Next Steps (Ordered — Do Not Skip Steps)

1. **Verify state** (run first to confirm environment):
   ```bash
   git -C /home/yv01p/rawrabbit log --oneline -6 && git -C /home/yv01p/rawrabbit status --short
   ```
   Expected output (top 6 commits):
   ```
   7883dfc Add Phase 2 modernization implementation plan
   30e32ed Correct Phase 2 spec §4 side-fix acceptance count
   3fc6f7b Update Phase 2 spec to address CDR round 1 finding 1
   5056d44 Add critical design review for Phase 2 spec (round 1)
   2689e86 Add Phase 2 modernization spec (package metadata + CPM)
   6b7614b Skip 9 broker-layer tests pending Phase 5/7 modernization
   ```
   Status should show 2 untracked files (the CIR + stale handoff) plus any new handoff written from this session.

2. **Commit the CIR file** (trivial; review file is the CIR's terminal artifact):
   ```bash
   git -C /home/yv01p/rawrabbit add docs/criticalreviews/2026-05-12-modernization-phase-2-implementation-plan-critical-review-1.md
   git -C /home/yv01p/rawrabbit commit -m "Add critical implementation review for Phase 2 plan (round 1)"
   ```

3. **Optionally commit handoffs** (low-stakes; documentation):
   ```bash
   git -C /home/yv01p/rawrabbit add handoffs/
   git -C /home/yv01p/rawrabbit commit -m "Add Phase 2 plan + CIR session handoff"
   ```

4. **Immediate action — kick off SDD**: invoke `superpowers:subagent-driven-development` against the plan.
   - Plan path: `docs/plans/2026-05-12-modernization-phase-2-implementation-plan.md`
   - 6 tasks; SDD launches one subagent per task with verification checkpoints between
   - **Task 4 is atomic** — SDD subagent must create Directory.Packages.props AND strip Version attributes from 13 in-sln csprojs in the same commit (creating Packages.props alone would trigger NU1008 across the codebase). The plan's "atomic by necessity" callout is the contract; trust it.
   - Phase 1 used the same SDD pattern and worked smoothly — same expectation for Phase 2.

5. **Verification**: after SDD completes, verify acceptance per spec §4 (Task 6 of the plan):
   ```bash
   cd /home/yv01p/rawrabbit
   dotnet --version                          # 10.0.107
   dotnet restore                             # 0 errors
   dotnet build -c Release                    # 0 errors
   dotnet test test/RawRabbit.Tests --no-build -c Release --blame-hang-timeout 15s
   dotnet test test/RawRabbit.Enrichers.Polly.Tests --no-build -c Release
   dotnet pack -c Release --no-build          # 25 .nupkg files; NU5048 warnings expected
   find . -path "*/bin/Release/*.nupkg" -not -path "*/AspNet.Sample/*" -not -path "*/PerformanceTest/*" | wc -l   # 25
   grep -c PackageProjectUrl src/RawRabbit.Operations.Tools/RawRabbit.Operations.Tools.csproj   # 0
   ```
   Expected test counts: 29 passed / 7 skipped / 0 failed (RawRabbit.Tests); 1 passed / 2 skipped / 0 failed (Polly.Tests). Total 30/9/0 — Phase 1 baseline.

6. **Watch for**:
   - **NU5xxx warnings during pack other than NU5048** — likely benign (e.g., NU5026 for missing Authors in `Operations.Tools`, NU5128 dependency mismatch). Acceptable as long as no errors. The spec/plan's "NU5048/NU5125" enumeration is approximate; treat any pack-time warnings as acceptable per spec §2.3 (cosmetic metadata deferred).
   - **NU1008** at restore time during Task 4 execution — would mean an in-sln csproj still has inline `Version=` under CPM. Fix by completing Task 4's version-strip step.
   - **NU1010** at restore time — would mean a `<PackageReference>` lacks a matching `<PackageVersion>` in central props. Fix by adding the missing entry (spec Risk #1 anticipates this).
   - Test count regressions — any deviation from 30/9/0 indicates Phase 2 broke something Phase 1 had working. Investigate immediately.
   - User has expressed preference for staying on `2.0` (the main branch) for these phases; same workspace pattern as Phase 1. Don't switch branches without asking.

## 6. Artifacts & References

- **Phase 2 spec**: `docs/specs/2026-05-12-modernization-phase-2-design.md` (last edit at commit `30e32ed`)
- **Phase 2 plan** (the SDD target): `docs/plans/2026-05-12-modernization-phase-2-implementation-plan.md` (commit `7883dfc`)
- **Phase 2 CDR round 1**: `docs/criticalreviews/2026-05-12-modernization-phase-2-design-critical-review-1.md` (commit `5056d44`)
- **Phase 2 CIR round 1**: `docs/criticalreviews/2026-05-12-modernization-phase-2-implementation-plan-critical-review-1.md` (untracked; commit pending — see §5 step 2)
- **Phase 1 spec** (for the 7-phase decomposition reference): `docs/specs/2026-05-11-modernization-phase-1-design.md` (commit `c88ac7a`)
- **Phase 1 plan** (template for Phase 2's structure): `docs/plans/2026-05-11-modernization-phase-1-implementation-plan.md` (commit `9186a78`)
- **Phase 1 handoffs**:
  - `handoffs/2026-05-11_22-21-27_modernization-phase-1-ready-for-sdd.md` (pre-Phase-1-SDD; never committed; stale)
  - `handoffs/2026-05-12_00-38-17_phase-1-complete-skill-fixes-shipped.md` (post-Phase-1-SDD + 3 skill fixes; committed)
- **Repo branches**: rawrabbit `2.0` → `https://github.com/yv01p/rawrabbit` (5 Phase 2 commits ahead of origin/2.0; not pushed)
- **NuGet docs consulted this session**:
  - https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management — CPM enable, opt-out, VersionOverride mechanics
  - https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory — Directory.Build.props import order, override semantics
  - https://learn.microsoft.com/en-us/nuget/reference/errors-and-warnings/nu1010 — NU1010 confirmed as the missing-PackageVersion error under CPM
- **Verification commands** known-good (run from `/home/yv01p/rawrabbit`):
  - `dotnet --version` → `10.0.107`
  - `find src test sample -name "*.csproj" -not -path "*/bin/*" -not -path "*/obj/*" | wc -l` → `32` (25 src + 4 test + 3 sample)
  - `grep -hE 'PackageReference Include' <in-sln csprojs> | sort -u | wc -l` → `17` (the central PackageVersion list)
- **Related issues**: none (greenfield modernization on personal fork)
