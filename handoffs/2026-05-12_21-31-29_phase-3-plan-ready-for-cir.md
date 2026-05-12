---
date: 2026-05-12T21:31:29+00:00
git_commit: 56271bb052d2a78e49f34a37d8cd67b01cb9297c
branch: 2.0
repository: rawrabbit
topic: "RawRabbit modernization Phase 3 — spec/CDR/plan complete; ready for CIR"
tags: [handoff, session-transition, dotnet, net10, rabbitmq, liblog, mel, asynclocal, phase-3]
status: in_progress
last_updated: 2026-05-12
type: implementation_handoff
---

# Handoff: Phase 3 Plan Written — Ready for CIR

## 0. Executive Summary (TL;DR)

1. Drove RawRabbit modernization Phase 3 (LibLog → `Microsoft.Extensions.Logging.Abstractions` shim + AsyncLocal `#else→null` fix in 2 enricher repos) through `thorough-brainstorming` → `critical-design-review` (✅ Approve as-is) → `thorough-writing-plans`. Three commits this session: spec (`c60429a`), plan (`56271bb`), and 1 untracked CDR review file.
2. Stopped at: TWP just completed; HEAD is `56271bb` (plan commit); 11 plan-level assumptions verified clean; recommended downstream pipeline printed (CIR → UIP → SDD). No code touched yet.
3. Single most important next action: invoke `critical-implementation-review` against `docs/plans/2026-05-12-modernization-phase-3-implementation-plan.md` to produce CIR round 1; then SDD against the (possibly revised) plan.

## 1. Technical State

**Active Working Set** (files in high rotation right now):
- `docs/plans/2026-05-12-modernization-phase-3-implementation-plan.md:1` — Phase 3 plan, committed at `56271bb`. The artifact CIR will review next. 6 tasks; Task 2 atomic.
- `docs/specs/2026-05-12-modernization-phase-3-design.md:1` — Phase 3 spec, committed at `c60429a`. Source of truth for the plan; 24 verified assumptions.
- `docs/criticalreviews/2026-05-12-modernization-phase-3-design-critical-review-1.md:1` — **CDR round 1, untracked, written this session.** Verdict ✅ Approve as-is.
- `docs/criticalreviews/2026-05-12-modernization-phase-2-implementation-plan-critical-review-1.md:1` — pre-existing untracked from prior session (Phase 2 CIR; never committed; stale for Phase 3 purposes).
- `handoffs/2026-05-11_22-21-27_modernization-phase-1-ready-for-sdd.md:1` — pre-existing untracked from a prior session (stale).
- `src/RawRabbit/Logging/LibLog.cs:1` — 2391-line vendored façade; will be REPLACED with ~80-line shim by Task 2 of the plan.
- `src/RawRabbit.Enrichers.GlobalExecutionId/Dependencies/GlobalExecutionIdRepository.cs:24` — `#else return null;` block; Task 4 of the plan replaces with unconditional AsyncLocal usage.
- `src/RawRabbit.Enrichers.MessageContext/Dependencies/MessageContextRepository.cs:37` — same pattern; Task 5 of the plan.

**Current Errors / Blockers:**
None.

**Environment:**
- Uncommitted changes: 3 untracked files in `docs/criticalreviews/` and `handoffs/`:
  - `docs/criticalreviews/2026-05-12-modernization-phase-3-design-critical-review-1.md` (Phase 3 CDR round 1, just written this session — ✅ Approve as-is)
  - `docs/criticalreviews/2026-05-12-modernization-phase-2-implementation-plan-critical-review-1.md` (Phase 2 CIR, pre-existing from prior session)
  - `handoffs/2026-05-11_22-21-27_modernization-phase-1-ready-for-sdd.md` (stale Phase 1 handoff, pre-existing from prior session)
- Staged changes: none.
- ENV vars or config required: none for handoff/planning. .NET 10 SDK 10.0.107 installed and pinned via `global.json` (Phase 1 baseline).
- Any running processes / background jobs: none.

## 2. Progress Tracker

| Task | Status | Location | Notes |
|------|--------|----------|-------|
| Phase 3 brainstorming (thorough-brainstorming) | ✅ Complete | spec at `c60429a` | 24 assumptions verified; key findings: IVT count is 10 (not 9 per Phase 1 spec); `_logger.Name`/`Log()`/`LogProvider.IsLoggerAvailable` are LibLog-internal, not consumer-facing; MEL.Abstractions 10.0.0 explicit net10 target; SerilogLoggerFactory still ships in SLog.Ext.Logging 10.0.0 |
| Phase 3 CDR round 1 (critical-design-review) | ✅ Complete | review at `docs/criticalreviews/2026-05-12-modernization-phase-3-design-critical-review-1.md` (untracked) | ✅ Approve as-is; 24/24 assumptions reconfirmed; 0 §2 findings; 0 §3 forced decisions; security gate fired no triggers |
| Phase 3 implementation plan (thorough-writing-plans) | ✅ Complete | plan at `56271bb` | 11 plan-level assumptions verified; 6-task decomposition (5 implementation + 1 verification); Task 2 atomic by build necessity |
| Commit Phase 3 CDR file | ⏳ Pending | — | Trivial; user has not asked yet |
| Optionally commit handoffs | ⏳ Pending | — | This handoff + the 2 stale ones from prior sessions |
| Phase 3 CIR round 1 (critical-implementation-review) | ⏳ Pending | — | The recommended next action; user said "create handoff" before invoking CIR |
| Phase 3 SDD execution | ⏳ Pending | — | After CIR (and any plan revision via update-implementation-plan) |
| Phase 3 final code review | ⏳ Pending | — | Post-SDD step (per Phases 1/2 pattern) |
| Push Phase 3 commits to origin/2.0 | ⏳ Pending | — | After SDD lands |
| Phases 4–7 | ⏳ Pending | — | Per Phase 1 spec §1 decomposition |

## 3. Mental Model (Most Critical Section)

**Why Phase 3 looks the way it does — the static-shim pivot:**

User constrained the design at clarifying-question time: **"static factory shim, minimal call-site churn"** (vs DI-injected `ILogger<T>` ctor injection or hybrid). This forced the design shape:
- Replace LibLog.cs in place with a ~80-line shim file at the same path, same namespace (`RawRabbit.Logging`).
- The shim's `interface ILog : Microsoft.Extensions.Logging.ILogger {}` is a marker interface that lets the shim's extension methods (`Info/Debug/Warn/Error/Trace/Fatal/IsXxxEnabled/XxxException`) hang off `ILog` without colliding with MEL's own `LogInformation/LogDebug/...` extensions.
- `LogProvider.LoggerFactory` is a public static settable property (defaults to `NullLoggerFactory.Instance`). Mirrors LibLog's `SetCurrentLogProvider()` opt-in pattern.
- Result: ~30 consumer files compile UNCHANGED. `private readonly ILog _logger = LogProvider.For<T>();` and `_logger.Info("msg", arg);` still work verbatim.

**Why the AsyncLocal fix is bundled in (not deferred to Phase 5):**

User picked option (b) "Logging + AsyncLocal fix" at clarifying-question time. The 2 enricher repos (`GlobalExecutionIdRepository.cs`, `MessageContextRepository.cs`) had `#if NETSTANDARD1_5 / #elif NET451 / #else return null;` patterns — meaning `Get()` returned `null` on net10 (silently broken since Phase 1.5b conditional-fix work that left these unfinished). Deferring to Phase 5 would leave a known correctness hole through Phase 4. Both fixes are mechanical: drop dead conditionals; always use `AsyncLocal<T>`.

**Why CDR ✅ Approve as-is (no findings):**

I considered 5 §2 candidates during CDR:
1. **Visibility widening** of `ILog` (current LibLog: `internal` per `#else internal #endif` since `LIBLOG_PUBLIC` isn't defined) → `public` in shim. Wider visibility never breaks consumers; verified no public RawRabbit API today exposes `ILog` (`grep -rnE "public\s+\w+\s+(ILog\b|.*<ILog>)" src/` returned 0). Drop.
2. **`Func<string>` lazy-eval overloads** missing in shim (LibLog historically supported `_logger.Info(() => "...")`). Verified consumers don't use this pattern (`grep -rE '_logger\.\w+\s*\(\s*\(\)'` returned 0 hits in consumer files). Drop.
3. **`ErrorException` arg order**. Sole consumer call `src/RawRabbit/Common/ExclusiveLock.cs:75` is `_logger.ErrorException("Exception when performing exclusive executeasync", e);` — order `(message, exception)` matches shim's `(this ILog l, string m, Exception ex, params object[] a)` signature. Drop.
4. **2 unused IVT entries** (`Operations.Get`, `Operations.Tools` are listed in LibLog.cs IVT but don't actually use `LogProvider.For` / `ILog`). Preserving them in the shim is harmless verbosity. Drop.
5. **NuGet upper-bound conflict** between `Sinks.Console 6.1.1` and the auto-bumped Serilog 4.2+. Spec's risk #2 acknowledges generic breakage path; without specific upper-bound evidence, this is speculation. Drop. (TWP later EMPIRICALLY verified Sinks.Console 6.1.1 has `Serilog (>= 4.0.0)` with NO upper bound — see P11 in plan.)

**Why the plan adds Task 6 (verification) when spec §8 didn't:**

Spec §8 lists 5 implementation tasks and says "verification rolls into per-commit build/test; final acceptance after Task 5." Phase 1/2 plans both ended with a Task 6 ritualizing the §1/§4 acceptance check. I added Task 6 to mirror that convention — gives SDD a clear final-gate task with explicit checklist semantics. Slight expansion beyond spec but defensible (mirrors prior phase pattern; user-acknowledged-by-precedent).

**Codebase Gotchas Discovered This Session:**

- `src/RawRabbit/Logging/LibLog.cs:91-96` — `interface ILog` is wrapped in `#if LIBLOG_PUBLIC ... public ... #else internal ... #endif`. With neither LIBLOG_PUBLIC nor LIBLOG_PROVIDERS_ONLY defined in the csproj (only LIBLOG_PORTABLE), `ILog` is currently `internal`. The new shim makes it `public` — wider visibility, safe direction.
- `src/RawRabbit/Logging/LibLog.cs:560-565` — `static ILog For<T>()` is also under `#if LIBLOG_PUBLIC ... public ... #else internal #endif` → currently `internal` despite `LogProvider` itself being `public`. Same widening in shim.
- `src/RawRabbit/Logging/LibLog.cs:44-53` — IVT count is **10**, not 9 (Phase 1 spec §6 risk #8 was off by one). Sibling projects: Enrichers.RetryLater, Operations.{Get, MessageSequence, Publish, Request, Respond, StateMachine, Subscribe, Tools}, Enrichers.GlobalExecutionId. Shim must preserve all 10 verbatim.
- `src/RawRabbit/Logging/LibLog.cs:1046,1048` — the 2 hits each of `_logger.Name` and `_logger.Log(` that I initially worried about are INSIDE LibLog.cs (NLog adapter internal code), not consumer code. Same for `LogProvider.IsLoggerAvailable` (5 hits, all in LibLog.cs) and `LogLevel.X` (60+ hits, all in LibLog.cs).
- Serilog version in `sample/RawRabbit.ConsoleApp.Sample/RawRabbit.ConsoleApp.Sample.csproj` resolved to `Serilog 4.0.0` transitively via `Sinks.Console 6.1.1`. Adding `Serilog.Extensions.Logging 10.0.0` (which has floor `Serilog ≥ 4.2.0`) auto-resolves to max(4.0.0, 4.2.0) = 4.2.0+. Sinks.Console 6.1.1 has `Serilog (>= 4.0.0)` with NO upper bound (verified on nuget.org); no NU1xxx restore conflict expected.
- No `<LangVersion>` setting anywhere in the codebase. .NET 10 SDK 10.0.107 + no override = C# 13 default. Codebase already uses C# 13 (see commit `0a1f996 Fix net10/C#13 source compile errors`). The shim's primary constructor `LogWrapper(ILogger inner) : ILog` and target-typed `new()` syntax in the AsyncLocal fixes both compile.
- No `<Nullable>` setting anywhere. Project default = nullable disabled. The shim's `where TState : notnull` constraint is syntactically valid in C# 8+ regardless of nullable context — used to MATCH MEL ILogger's `BeginScope` signature, not to enforce anything.
- `src/RawRabbit.Compatibility.Legacy/RawRabbit.Compatibility.Legacy.csproj:14` carries `LIBLOG_PORTABLE` define but the project has ZERO LibLog usages in `.cs` files. The define has been a no-op since whenever; Task 2 cleans it up.

**Dead Ends — Do Not Repeat These:**

| Approach Tried | Why It Failed | Evidence |
|---------------|---------------|----------|
| (Considered then rejected at clarifying-question time) DI-injected `ILogger<T>` for ~30 middleware ctors | Bigger surface change; would touch ~30 ctors AND the IoC-resolver path; YAGNI for personal modernization fork on net10 only | User picked "static factory shim, minimal call-site churn" |
| (Considered then rejected) Add a `Name` extension method to the shim because consumers might use `_logger.Name` | The 2 `_logger.Name` hits I initially worried about are inside LibLog.cs internals (`LibLog.cs:1046,1048` NLog adapter), not consumer code | `grep -rn "_logger\.Name" src/ \| grep -v "Logging/LibLog.cs"` returned 0 |
| (Considered then rejected) Add a raw `Log(LogLevel, ...)` helper to the shim | Same — the 2 raw `.Log(` hits are inside LibLog.cs internals | Same evidence |
| (Considered then surfaced as CDR §2 candidate, then dropped) Worry that Sinks.Console 6.1.1 might cap Serilog version | Empirically falsified at TWP time: `Serilog (>= 4.0.0)` with NO upper bound per nuget.org | TWP P11 |
| (CDR-time consideration, dropped) `SerilogLoggerFactory` might be removed in newer Serilog.Extensions.Logging | Verified via github.com/serilog/serilog-extensions-logging that the class still ships in 10.0.0 as `public sealed class SerilogLoggerFactory : ILoggerFactory` with `(ILogger? logger = null, bool dispose = false, ...)` ctor | spec D2 / CDR §1 |

**Key Decisions Made:**

| Decision | Rationale | Alternative Rejected |
|----------|-----------|---------------------|
| Phase 3 scope: Logging + AsyncLocal fix bundled (option b) | The 2 enricher repos are mechanically broken on net10; deferring leaves a correctness hole through Phase 4. Bundling costs nothing extra | Option (a) Logging only — would leave the AsyncLocal bug for Phase 5 |
| Logger surface = static shim preserving `LogProvider.For<T>()` call-site shape | Minimum churn across ~30 consumer files; matches LibLog's existing static-provider architecture | DI-injected ILogger<T> (touches 30 ctors); hybrid (ctor with NullLogger default) |
| Replace LibLog.cs in place (same path/namespace) | Keeps `using RawRabbit.Logging;` unchanged in consumers; preserves 10 IVT declarations as standalone `[assembly: ...]` in the shim file; single-file replacement = single-commit atomic change | Move to a new file (would require namespace-or-path change in consumers) |
| MEL.Abstractions version: 10.0.0 (latest stable, released 11/11/2025) | Explicit net10 target; single Microsoft transitive dep (DI.Abstractions 10.0.0); no NU1701/NU1902/NU1903 expected | 9.0.11 (also exists; would work but slightly older) |
| Serilog adapter: `new SerilogLoggerFactory(Log.Logger)` | Direct, 1-line wiring; SerilogLoggerFactory still public in SLog.Ext.Logging 10.0.0 | `LoggerFactory.Create(b => b.AddSerilog(...))` (recommended-by-README pattern but requires `Microsoft.Extensions.Logging` runtime + IServiceCollection — needless complexity for sample wiring) |
| Plan adds Task 6 (verification) beyond spec §8's 5 implementation tasks | Mirrors Phase 1/2 plan convention (final-gate ritual with explicit checklist semantics for SDD); spec §1 acceptance criteria becomes actionable | Pure inheritance from spec §8 (5 tasks; verification implicit) |
| Stay on branch `2.0`, commit directly | Personal fork; 2.0 is the main branch; same pattern as Phase 1/2 | Worktree (extra steps); feature branch (extra merge) |

**Assumptions in Play:**

- **D3/P11 implication:** Adding `Serilog.Extensions.Logging 10.0.0` to the sample WILL auto-bump Serilog 4.0 → 4.2+. Behavior change is within Serilog 4.x major (minor bump) and Sinks.Console 6.1.1 accepts it. If anything breaks beyond pure compile (not expected since the sample isn't run in tests), see plan task 3 step 5.
- The 9 `[Fact(Skip="...")]` broker-layer tests stay skipped post-Phase-3 (Phase 3 doesn't touch any broker code). Phase 1 baseline is 30 passing / 9 skipped / 0 failed — Task 6 expects this exactly.
- SDD will respect Task 2's atomicity flag and commit the shim replacement + LIBLOG_PORTABLE define removals together. The plan note "atomic by build necessity" is the contract.
- IntegrationTests (containing GlobalExecutionIdTests / MessageSequenceTests) is OUT of Phase 3 acceptance (requires live broker per Phase 1 spec §2 / Phase 6). Phase 3's AsyncLocal fix benefits them when Phase 6 turns them on; no Phase 3 test count impact.
- The shim's `internal sealed class LogWrapper(ILogger inner) : ILog` primary constructor compiles on C# 13 (default for .NET 10 SDK; no LangVersion override). Already used in codebase per commit `0a1f996`.

## 4. Delta — Changes Made This Session

**On branch `2.0` (rawrabbit) — all committed and on local HEAD; not yet pushed to origin:**

```
56271bb Add Phase 3 modernization implementation plan       ← TWP output
c60429a Add Phase 3 modernization spec (LibLog -> MEL.Abstractions + AsyncLocal fix)  ← brainstorming output
```

**Uncommitted in working tree:**
- `docs/criticalreviews/2026-05-12-modernization-phase-3-design-critical-review-1.md` — Phase 3 CDR round 1 just written; ✅ Approve as-is
- `docs/criticalreviews/2026-05-12-modernization-phase-2-implementation-plan-critical-review-1.md` — Phase 2 CIR (pre-existing untracked from prior session; not Phase 3's concern)
- `handoffs/2026-05-11_22-21-27_modernization-phase-1-ready-for-sdd.md` — stale Phase 1 pre-SDD handoff (untracked from prior session)
- This handoff file (will be untracked after write)

## 5. Next Steps (Ordered — Do Not Skip Steps)

1. **Verify state** (run first to confirm environment):
   ```bash
   git -C /home/yv01p/rawrabbit log --oneline -4 && git -C /home/yv01p/rawrabbit status --short
   ```
   Expected output (top 4 commits):
   ```
   56271bb Add Phase 3 modernization implementation plan
   c60429a Add Phase 3 modernization spec (LibLog -> MEL.Abstractions + AsyncLocal fix)
   cc4bd44 Opt dropped-from-sln csprojs out of CPM to preserve their explicit-build behavior
   19b6545 Adopt Central Package Management (Directory.Packages.props) for in-sln csprojs
   ```
   Status should show 3 untracked files (the Phase 3 CDR + Phase 2 CIR + stale Phase 1 handoff) plus this handoff.

2. **Commit the Phase 3 CDR file** (trivial; review file is the CDR's terminal artifact):
   ```bash
   git -C /home/yv01p/rawrabbit add docs/criticalreviews/2026-05-12-modernization-phase-3-design-critical-review-1.md
   git -C /home/yv01p/rawrabbit commit -m "Add critical design review for Phase 3 spec (round 1)"
   ```

3. **Optionally commit handoffs** (low-stakes; documentation):
   ```bash
   git -C /home/yv01p/rawrabbit add handoffs/
   git -C /home/yv01p/rawrabbit commit -m "Add Phase 3 plan + CDR session handoff"
   ```

4. **Immediate action — invoke CIR round 1**: invoke `critical-implementation-review` against the plan.
   - Plan path: `docs/plans/2026-05-12-modernization-phase-3-implementation-plan.md`
   - Output: `docs/criticalreviews/2026-05-12-modernization-phase-3-implementation-plan-critical-review-1.md`
   - The plan's 11 P-assumptions are already verified; CIR cross-checks them and looks for plan-level literal-wrongness (different scope from CDR which focused on spec-level).
   - Phase 1/2 CIRs both came back ✅ Approve as-is — same pattern likely.

5. **If CIR has §2 findings**: invoke `update-implementation-plan` with the CIR review file path → revised plan (in-place edit + commit). If CIR is ✅ Approve as-is, skip this step.

6. **Then SDD**: invoke `superpowers:subagent-driven-development` against the (possibly revised) plan.
   - 6 tasks; SDD launches one subagent per task with two-stage review checkpoints (spec compliance + code quality) between
   - **Task 2 is atomic** — SDD subagent must replace LibLog.cs, modify RawRabbit.csproj (delete LIBLOG_PORTABLE + add MEL.Abstractions PackageReference), AND modify Compatibility.Legacy.csproj (delete orphan LIBLOG_PORTABLE) in the SAME COMMIT. Splitting breaks the build (deleting LIBLOG_PORTABLE while LibLog.cs still has `#if !LIBLOG_PORTABLE` blocks fails to compile; adding the shim while LibLog.cs still exists creates type clashes). Plan note "atomic by build necessity" is the contract.
   - Phase 1/2 used the same SDD pattern and worked smoothly — same expectation for Phase 3.

7. **Verification after SDD**: Task 6 runs the spec §1 acceptance sequence. Expected:
   ```bash
   cd /home/yv01p/rawrabbit
   dotnet --version                          # 10.0.107
   dotnet restore                             # 0 errors
   dotnet build -c Release                    # 0 errors, ~197 warnings (Phase 2 baseline)
   dotnet test test/RawRabbit.Tests --no-build -c Release --blame-hang-timeout 15s
   dotnet test test/RawRabbit.Enrichers.Polly.Tests --no-build -c Release
   dotnet pack -c Release --no-build          # 25 .nupkg files; NU5048 warnings expected
   find . -path "*/bin/Release/*.nupkg" -not -path "*/AspNet.Sample/*" -not -path "*/PerformanceTest/*" | wc -l   # 25
   grep -c "LIBLOG_PORTABLE" src/RawRabbit/RawRabbit.csproj src/RawRabbit.Compatibility.Legacy/RawRabbit.Compatibility.Legacy.csproj   # 0 each
   wc -l src/RawRabbit/Logging/LibLog.cs       # ~80 (was 2391)
   grep -c "#if NETSTANDARD1_5\|#elif NET451\|#else" src/RawRabbit.Enrichers.GlobalExecutionId/Dependencies/GlobalExecutionIdRepository.cs src/RawRabbit.Enrichers.MessageContext/Dependencies/MessageContextRepository.cs   # 0 each
   ```
   Expected test counts: 29 passed / 7 skipped / 0 failed (RawRabbit.Tests); 1 passed / 2 skipped / 0 failed (Polly.Tests). Total 30/9/0 — matches Phase 1/2 baseline.

8. **Watch for**:
   - **NU1xxx restore warnings beyond Phase 2 baseline (NU1701/NU1902/NU1903)** — MEL.Abstractions 10.0.0 should add only DI.Abstractions 10.0.0 transitively (Microsoft, explicit net10). If new NU codes fire, document and accept as long as no errors per spec §2.3 (Phase 2's "any pack-time NUxxxx acceptable" pattern).
   - **NU1010 at restore time during Task 1 commit** — wouldn't fire because Task 1 only adds to central props with no consumer; safe.
   - **CS errors at Task 2** — if the shim's `where TState : notnull` or primary constructor syntax fails, check `<LangVersion>` (none should be set; should default to C# 13). Also check `<Nullable>` (none should be set).
   - **Pack-time NU5xxx other than NU5048** — likely benign. Treat as acceptable per spec §2.3.
   - **Test count regressions** — any deviation from 30/9/0 indicates Phase 3 broke something Phase 1/2 had working. Investigate immediately.
   - **`grep` output for LIBLOG_PORTABLE / `#else` patterns showing non-zero** — means cleanup incomplete; reopen Task 2 / Tasks 4-5.
   - User has expressed preference for staying on `2.0` (the main branch) for these phases; same workspace pattern as Phases 1-2. Don't switch branches without asking.

## 6. Artifacts & References

- **Phase 3 spec**: `docs/specs/2026-05-12-modernization-phase-3-design.md` (commit `c60429a`)
- **Phase 3 plan** (the CIR target): `docs/plans/2026-05-12-modernization-phase-3-implementation-plan.md` (commit `56271bb`)
- **Phase 3 CDR round 1**: `docs/criticalreviews/2026-05-12-modernization-phase-3-design-critical-review-1.md` (untracked; commit pending — see §5 step 2)
- **Phase 1 spec** (for the 7-phase decomposition reference): `docs/specs/2026-05-11-modernization-phase-1-design.md` (commit `c88ac7a`)
- **Phase 2 spec** (CPM + Build.props baseline): `docs/specs/2026-05-12-modernization-phase-2-design.md` (commit `30e32ed`)
- **Phase 2 plan** (template for Phase 3's structure): `docs/plans/2026-05-12-modernization-phase-2-implementation-plan.md` (commit `7883dfc`)
- **Phase 1 handoffs**:
  - `handoffs/2026-05-11_22-21-27_modernization-phase-1-ready-for-sdd.md` (pre-Phase-1-SDD; never committed; stale)
  - `handoffs/2026-05-12_00-38-17_phase-1-complete-skill-fixes-shipped.md` (post-Phase-1-SDD + 3 skill fixes; committed)
- **Phase 2 handoffs**:
  - `handoffs/2026-05-12_20-02-48_phase-2-plan-cir-clean.md` (Phase 2 plan-CIR, the previous resume target; committed earlier this session via the f05c7c4 collateral)
- **Repo branches**: rawrabbit `2.0` → `https://github.com/yv01p/rawrabbit` (12 commits ahead of origin/2.0 — 10 from Phase 2, 2 from Phase 3; not yet pushed since Phase 3 work)
- **Wait — actually checked:** Phase 2 final push happened earlier this session (`git push origin 2.0` succeeded with `6b7614b..cc4bd44`). So origin/2.0 is at `cc4bd44`, and the 2 Phase 3 commits (`c60429a`, `56271bb`) are ahead by 2.
- **External refs consulted**:
  - https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions/10.0.0 — verified explicit net10 target, single Microsoft transitive dep
  - https://www.nuget.org/packages/Serilog.Extensions.Logging/ — verified Serilog ≥ 4.2.0 floor, AddSerilog() recommended pattern
  - https://github.com/serilog/serilog-extensions-logging — confirmed `SerilogLoggerFactory(ILogger? logger = null, bool dispose = false, ...)` still ships
  - https://www.nuget.org/packages/Serilog.Sinks.Console/6.1.1 — verified `Serilog (>= 4.0.0)` with NO upper bound (key TWP P11 finding)
- **Related issues**: none (greenfield modernization on personal fork)
