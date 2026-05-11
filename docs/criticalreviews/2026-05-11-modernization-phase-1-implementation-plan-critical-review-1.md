# Critical Implementation Review: 2026-05-11-modernization-phase-1-implementation-plan (Round 1)

**Plan:** `/home/yv01p/rawrabbit/docs/plans/2026-05-11-modernization-phase-1-implementation-plan.md`
**Verified plan-level assumptions section:** present

⚠️ 2 commits since plan-write time (SHA `c88ac7a`); cited file:line references re-checked under §1. Both commits are doc-only (`5387b19` Add critical design review; `9186a78` Add the plan itself); no codebase drift.

## 1. Verified-plan-assumptions cross-check

| # | Status |
|---|---|
| P1 — 25 csprojs in `src/*` | Still holds. `ls src/ \| wc -l` returns 25 unchanged. |
| P2 — sln Project entries with GUIDs `{313A89F7…}` (AspNet.Sample) and `{2788DED9…}` (PerformanceTest) | Still holds. `grep -nE "PerformanceTest\|AspNet.Sample" RawRabbit.sln` re-confirms entries at lines 25 and 67. |
| P3 — .NET 10 SDK 10.0.203 | Still holds. External evidence (nuget.org / dotnet.microsoft.com) re-checked at plan-write time minutes ago. |
| P4 — Serilog.Sinks.Console 6.1.1, net10-compatible | Still holds. Same external source. |
| P5 — Microsoft.Extensions.Configuration.Json 10.0.7 + Binder 10.0.7 | Still holds. |
| P6 — `Program.cs:25` is the only `.WriteTo.LiterateConsole()` callsite | Still holds. Single callsite confirmed. |
| P7 — `Program.cs` uses standard M.E.Configuration APIs (`ConfigurationBuilder().SetBasePath().AddJsonFile().Build().Get<T>()`) | Still holds. |
| P8 — No remaining csproj `<ProjectReference>`s INTO `RawRabbit.PerformanceTest` or `RawRabbit.AspNet.Sample` | Still holds. |
| P9 — No commit hooks, no `.husky/`, no `.pre-commit-config.*`, no `Makefile`, no `build.sh` | Still holds. |
| P10 — Direct `dotnet` CLI works without wrapper interception | Still holds. |
| P11 — All 25 `AssemblyInfo.cs` files contain only auto-generatable + dead-IVT/Guid/ComVisible attributes | Still holds. |
| P12 — Test code uses Moq APIs stable across 4.7→4.20 | Still holds. |
| P13 — xunit attribute usage is `[Fact]`-only | Still holds. |
| P14 — `xunit.runner.visualstudio 2.8.2` supports xunit v2 + net10 via fallback | Still holds. |
| P15 — SDK-style csproj template for Polly.Tests is well-formed | Still holds. |
| P16 — HttpContext stranded `<ItemGroup Condition="…== 'netstandard1.6'">` evaluates false on net10 (harmless dead clutter) | Still holds. |

All 16 verified plan-level assumptions reconfirmed.

## 2. Literal-wrongness findings

No literal-wrongness findings.

Adversarial sweep covered: code-block correctness in each task; static command typos; task-ordering hidden cross-deps; signature mismatches in code blocks; consumer-impact regressions of touched-function changes; runtime / dynamic concerns (race conditions in called primitives, error-path swallowing, integration edge cases at trust boundaries, transactional state in pooled connections); `dotnet restore`/`dotnet build`/`dotnet test` execution semantics under modernized csprojs; transitive dependency completeness in the rewritten Polly.Tests csproj template; NU1701 expectation correctness; conditional ItemGroup deletion completeness across all observed TFM conditions; verification command regex correctness.

The plan's static-correctness work (paths, signatures, commands, ordering) was already verified at plan-write time per CIR's input contract. The dynamic sweep found nothing additional that breaks the spec's outcome at execution time.

## 3. Forced decisions

No forced decisions found.

The plan's two prior forced decisions (F1 — Serilog `.cs` edit; F2 — xunit runner version pair) were resolved with user input at plan-write time and recorded under "Verified plan-level assumptions". Nothing new was forced by the implementation tasks themselves.

## 4. Recommendation

✅ **Approve as-is.** §1 reconfirmed all 16 plan-level assumptions. §2 and §3 are both empty. The plan is ready for execution via `subagent-driven-development`.
