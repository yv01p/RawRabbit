# Modernization Phase 4.5 Wave 1 — Quality Foundation + Scaffolding Implementation Plan

> **For agentic workers:** REQUIRED: Use `superpowers:subagent-driven-development` to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Source spec:** `docs/specs/2026-05-13-modernization-phase-4.5-design.md` (commit SHA: `9b5837d1c3ae39d5a0f17f82659ad727cd43a308`)

**Plan scope:** Wave 1 only. Waves 2–5 are independent per spec D5 / §6 and will be planned separately after Wave 1 ships. The user picked "Wave 1 only first" at plan-write time.

**Goal:** Land Wave 1's quality foundation (A25 anti-pattern cleanup of 7 existing test files; 2 JsonSerializer backfill tests; 2 Phase 4 cosmetics) and scaffolding (14 new test project skeletons added to `RawRabbit.sln`) such that spec §7 acceptance condition 1 holds at HEAD and the conventions are in place for Waves 2–5.

**Architecture:** One unit-test project per separate source assembly (matches existing `Polly.Tests` precedent). Wave 1 creates 14 new empty-source test projects (no `.cs` files yet — Waves 2–5 fill them in) and adds them to the solution. Wave 1 also cleans A25 anti-patterns from 7 existing test files (per D10, only NON-SKIPPED tests are touched; the 9 currently-skipped Phase 5/7-tagged tests retain their annotations verbatim) and adds 2 JsonSerializer backfill tests pinning Phase 4 string-passthrough + collection-replacement decisions.

**Tech stack (inherited from spec):** .NET 10 SDK 10.0.107 + C# 13 (default for net10); xUnit 2.9.3; xunit.runner.visualstudio 2.8.2; Microsoft.NET.Test.Sdk 18.5.1; Moq 4.20.72. CPM via `Directory.Packages.props`. No new package dependencies for Wave 1.

---

## File Structure

**Create (14 csproj files, 0 .cs files):**

- `test/RawRabbit.DependencyInjection.ServiceCollection.Tests/RawRabbit.DependencyInjection.ServiceCollection.Tests.csproj` — empty-source test project for ServiceCollection DI
- `test/RawRabbit.Operations.Get.Tests/RawRabbit.Operations.Get.Tests.csproj`
- `test/RawRabbit.Operations.MessageSequence.Tests/RawRabbit.Operations.MessageSequence.Tests.csproj`
- `test/RawRabbit.Operations.Publish.Tests/RawRabbit.Operations.Publish.Tests.csproj`
- `test/RawRabbit.Operations.Request.Tests/RawRabbit.Operations.Request.Tests.csproj`
- `test/RawRabbit.Operations.Respond.Tests/RawRabbit.Operations.Respond.Tests.csproj`
- `test/RawRabbit.Operations.StateMachine.Tests/RawRabbit.Operations.StateMachine.Tests.csproj`
- `test/RawRabbit.Operations.Subscribe.Tests/RawRabbit.Operations.Subscribe.Tests.csproj`
- `test/RawRabbit.Operations.Tools.Tests/RawRabbit.Operations.Tools.Tests.csproj`
- `test/RawRabbit.Enrichers.Attributes.Tests/RawRabbit.Enrichers.Attributes.Tests.csproj`
- `test/RawRabbit.Enrichers.GlobalExecutionId.Tests/RawRabbit.Enrichers.GlobalExecutionId.Tests.csproj`
- `test/RawRabbit.Enrichers.MessageContext.Tests/RawRabbit.Enrichers.MessageContext.Tests.csproj` — consolidates 3 source assemblies (MessageContext + .Respond + .Subscribe) per spec §2 row 12
- `test/RawRabbit.Enrichers.QueueSuffix.Tests/RawRabbit.Enrichers.QueueSuffix.Tests.csproj`
- `test/RawRabbit.Enrichers.RetryLater.Tests/RawRabbit.Enrichers.RetryLater.Tests.csproj`

**Modify:**

- `test/RawRabbit.Tests/Serialization/JsonSerializerTests.cs` — rename 1 test method; add 2 backfill tests + 1 internal POCO class
- `src/RawRabbit/Serialization/JsonSerializer.cs` — restore UTF-8 BOM (file is currently ASCII; siblings have BOM)
- `test/RawRabbit.Tests/Common/ConnectionStringParserTests.cs` — convert 45 AAA comments → blank lines, 2 non-generic `Assert.IsType` → generic, 2 try/catch → `Assert.Throws<T>`; consolidate similar tests via `[Theory]/[InlineData]` where natural (≥1 use)
- `test/RawRabbit.Tests/Common/NamingConventionsTests.cs` — convert 15 AAA comments → blank lines; consolidate 4 string-input tests via `[Theory]/[InlineData]`
- `test/RawRabbit.Tests/Channel/ChannelPoolTests.cs` — drop 2 NON-SKIPPED `Assert.True(true, …)` placeholders; convert 1 NON-SKIPPED try/catch → `Assert.ThrowsAsync<OperationCanceledException>`; preserve all 3 skip annotations verbatim
- `test/RawRabbit.Tests/Channel/DynamicChannelPoolTests.cs` — drop 1 `Assert.True(true, …)` placeholder; convert 9 AAA comments → blank lines
- `test/RawRabbit.Enrichers.Polly.Tests/Middleware/QueueDeclareMiddlewareTests.cs` — convert 2 AAA comments → blank lines
- `RawRabbit.sln` — append 14 new test project entries via `dotnet sln add`

**Verify-only (no edits, skip-preservation check per D10):**

- `test/RawRabbit.Tests/Channel/ChannelFactoryTests.cs` — all 4 tests skipped; confirm skip annotations + Phase 5/7 commentary unchanged after Wave 1
- `test/RawRabbit.Enrichers.Polly.Tests/Services/ChannelFactoryTests.cs` — all 2 tests skipped; same check

**Test:** No new test files in Wave 1 (the 2 backfill tests live in the existing `JsonSerializerTests.cs`).

---

## Inherited from spec

The following assumptions were verified by `thorough-brainstorming` at spec-write time (HEAD `b1c188c`) and are NOT re-verified here. Trusted as ground truth:

- A1 — All 19 active source-area paths exist (`test -d` confirmed)
- A2 — All 6 out-of-scope source paths exist
- A3 — Pre-Phase-4.5 test baseline: 39 passed + 7 skipped in `RawRabbit.Tests`; 1 passed + 2 skipped in `RawRabbit.Enrichers.Polly.Tests` (40 passed / 9 skipped / 0 failed total)
- A4 — Build clean: `dotnet build -c Release` returns 0 errors; ~143 pre-existing warnings
- A5 — 8 existing unit-test `.cs` files; cleanup target = 7 (excluding the already-clean `Serialization/JsonSerializerTests.cs`)
- A6 — 9 skipped tests at exact locations: 4 in `Channel/ChannelFactoryTests.cs` (lines 15, 46, 77, 103), 3 in `Channel/ChannelPoolTests.cs` (lines 106, 190, 245), 2 in `Polly.Tests/Services/ChannelFactoryTests.cs` (lines 16, 52); all skip messages contain "Phase 5/7 territory"
- A7 — A25 hit counts at the per-file totals reported in spec §4 table (NOTE: this plan corrects two undercounts in §4 — see Verified plan-level assumptions VP-1 and VP-2 below)
- A8 — Zero `[Theory]/[InlineData]` usage anywhere in `test/` — confirms NEW convention
- A9 — xUnit 2.9.3 + Moq 4.20.72 + Microsoft.NET.Test.Sdk 18.5.1 (per `Directory.Packages.props` lines 19–22)
- A14 — Mocking at `IChannel`/`IConnection` boundary works with Moq 4.20 on net10 for non-skipped paths (39 non-skipped tests pass)
- A15 — Both unit-test csprojs are SDK-style; auto-discover any `.cs` file under any subdirectory
- A16 — Existing tests use `new Mock<T>()` / `.Setup(...)` / `.Verify(...)` style
- A17 — Audit's "only Channel.* and Common.* (and now Serialization.*) have unit tests" claim holds at HEAD `b1c188c`
- A18 — `RawRabbit.Tests` ProjectReferences: RawRabbit + Autofac DI + Ninject DI (NOT ServiceCollection — pre-existing inconsistency, preserved by D8 + spec §13.2)
- D8 — One test project per separate source assembly; preserve existing `RawRabbit.Tests` inconsistency
- D9 — Wave 1 scaffolds 14 empty test projects up-front
- D10 — Skipped methods within cleanup-target files are NOT touched in Wave 1

---

## Verified plan-level assumptions

Newly introduced by this plan and verified empirically against HEAD `9b5837d` at plan-write time on 2026-05-13:

| # | Category | Assumption | Evidence |
|---|---|---|---|
| VP-1 | Cat 6 (consumer impact / spec correction) | Spec §4 column "try/catch" reports 0 for `ConnectionStringParserTests.cs` but 2 hand-rolled try/catch blocks exist in NON-SKIPPED tests | Read of `test/RawRabbit.Tests/Common/ConnectionStringParserTests.cs` lines 309–353: tests `Should_Throw_Format_Exception_When_ConnectionString_Has_Bad_Port` and `Should_Throw_Argument_Exception_When_ConnectionString_Has_Bad_Property` use the `try { Parse(…); } catch (Exception e) { exception = e; }` pattern. Plan converts both to `Assert.Throws<T>` per A25 anti-pattern #2. |
| VP-2 | Cat 6 (consumer impact / spec correction) | Spec §4 column "try/catch" reports 0 for `ChannelPoolTests.cs` but 1 hand-rolled try/catch exists in NON-SKIPPED test | Read of `test/RawRabbit.Tests/Channel/ChannelPoolTests.cs` lines 220–243: NON-SKIPPED test `Should_Be_Able_To_Cancel_With_Token` uses the try/catch pattern catching `OperationCanceledException`. Plan converts to `Assert.ThrowsAsync<OperationCanceledException>`. |
| VP-3 | Cat 6 (consumer impact / D10 application) | Spec §4 per-file totals include hits inside skipped tests; D10 forbids touching them. Wave 1 actual cleanup count is per-file totals MINUS skipped-test hits | Cross-reference of A6 (9 skipped tests at exact locations) with §4 totals + per-file reads. See "Per-file Wave 1 actual work" subsection in Task 5. |
| VP-4 | Cat 5 (code-in-plan validity) | Spec §5 line 177 cosmetic ("drop unused `using System.Collections.Generic;`") is invalidated by spec §5 line 172 backfill test which declares `List<int> Items { get; set; } = new() { 99 };` | Read of `test/RawRabbit.Tests/Serialization/JsonSerializerTests.cs:2` confirms the `using` exists; line 172 backfill test code requires it. User-confirmed resolution: drop the cosmetic; keep the using. Plan executes only 2 of the 3 originally-listed cosmetics. |
| VP-5 | Cat 2 (function signature) | `JsonSerializer.cs:23-26` has `if (obj is string str) return str;` early-return | Read of `src/RawRabbit/Serialization/JsonSerializer.cs:17-28`: lines 23–26 are exactly `if (obj is string str)\n{\n\treturn str;\n}` |
| VP-6 | Cat 2 (function signature) | `JsonSerializer.cs:32-35` has symmetric `if (type == typeof(string)) return str;` early-return | Read of `src/RawRabbit/Serialization/JsonSerializer.cs:30-41`: lines 32–35 are exactly `if (type == typeof(string))\n{\n\treturn str;\n}` |
| VP-7 | Cat 5 (code-in-plan validity) | The string early-return path produces UTF-8 bytes equal to `Encoding.UTF8.GetBytes(s)` | Read of `src/RawRabbit/Serialization/StringSerializerBase.cs:34-37`: `protected virtual byte[] ConvertToBytes(string serialzed) { return Encoding.UTF8.GetBytes(serialzed); }`. `JsonSerializer` does not override `ConvertToBytes`. So `serializer.Serialize("hello")` byte-equals `Encoding.UTF8.GetBytes("hello")` |
| VP-8 | Cat 2 (function signature) | `JsonSerializerTests.cs` contains the test method `Should_Deserialize_Pascal_Case_Json_Into_Pascal_Case_Properties` (rename target) | Read of `test/RawRabbit.Tests/Serialization/JsonSerializerTests.cs:71-83`: the `[Fact] public void Should_Deserialize_Pascal_Case_Json_Into_Pascal_Case_Properties()` method exists |
| VP-9 | Cat 1 (file paths) + Cat 2 (BOM presence) | `src/RawRabbit/Serialization/JsonSerializer.cs` is the BOM outlier in the Serialization directory | `file src/RawRabbit/Serialization/*.cs` returned: JsonSerializer.cs is "ASCII text"; ISerializer.cs and StringSerializerBase.cs are "Unicode text, UTF-8 (with BOM) text" |
| VP-10 | Cat 1 (file paths) | None of the 14 new test project paths exist | `ls -d test/RawRabbit.<each>.Tests` returned "No such file or directory" for all 14 |
| VP-11 | Cat 1 (file paths) | All 14 source assembly directories exist (ProjectReference targets) | `ls -d src/RawRabbit.<each>` returned all 16 expected paths (14 unique target assemblies + MessageContext.Respond + MessageContext.Subscribe consolidate into the 14th test project) |
| VP-12 | Cat 1 (file paths) | The 7 cleanup-target test files exist at exact paths claimed | `ls -la` confirmed all 7: ChannelFactoryTests.cs, ChannelPoolTests.cs, DynamicChannelPoolTests.cs, ConnectionStringParserTests.cs, NamingConventionsTests.cs, Polly Middleware/QueueDeclareMiddlewareTests.cs, Polly Services/ChannelFactoryTests.cs |
| VP-13 | Cat 5 (code-in-plan validity) | The csproj template (SDK-style + CPM `<PackageReference>` without versions) matches existing test project shape | Read of `test/RawRabbit.Tests/RawRabbit.Tests.csproj` and `test/RawRabbit.Enrichers.Polly.Tests/RawRabbit.Enrichers.Polly.Tests.csproj`: both are SDK-style; PackageReferences have no `Version` attribute (CPM-managed via `Directory.Packages.props`); both reference RawRabbit core + their source assembly under test |
| VP-14 | Cat 3 (build/test commands) | `dotnet build -c Release` is the build command; `dotnet test test/<project>/ --no-build -c Release` is the test command | Spec §11 A3 evidence used these exact invocations and produced the documented baseline; same shape used throughout Phase 4 |
| VP-15 | Cat 3 (build/test commands) | `dotnet sln add <csproj>` and `dotnet sln list` work with this VS2017-format `RawRabbit.sln` | Read of `RawRabbit.sln`: standard `Microsoft Visual Studio Solution File, Format Version 12.00`; standard project entries with GUIDs. `dotnet sln` has supported VS2017 format since dotnet-sdk-3.0+. |
| VP-16 | Cat 3 (build/test commands) | No active pre-commit hooks; no formatter (e.g., dotnet-format) auto-modifies files at commit time | `ls -la .git/hooks/` returned only `.sample` files (none active); `.pre-commit-config.yaml` does not exist; no `husky` / `dotnet-format` references in repo config |
| VP-17 | Cat 3 (build/test commands) | Commit message convention is lowercase imperative-mood ("Add X", "Replace X with Y", "Fix X") | `git log --oneline -20`: examples include "Add Phase 4.5 modernization spec (comprehensive test suite)", "Replace Newtonsoft.Json with System.Text.Json", "Fix MessageContextRepository to use AsyncLocal on net10". No `feat:`/`fix:` Conventional-Commits prefix in use |
| VP-18 | Cat 3 (codebase config) | `.editorconfig` requires `indent_style = tab` for `[*]` (`[*.csproj]` etc. use 2-space) | Read of `.editorconfig`: line 12 `indent_style = tab` under `[*]`; line 17 `indent_size = 2` under `[*.{csproj,…}]` |
| VP-19 | Cat 4 (task ordering) | All 7 tasks touch disjoint file sets; no hidden cross-task dependency | Cross-checked task `Modify`/`Create` lists: Task 1 → JsonSerializerTests.cs + JsonSerializer.cs; Task 2 → 14 new csprojs + RawRabbit.sln; Task 3 → ConnectionStringParserTests.cs; Task 4 → NamingConventionsTests.cs; Task 5 → 3 Channel/*.cs; Task 6 → 2 Polly test files; Task 7 → no edits. Only `RawRabbit.sln` is touched (only by Task 2) |
| VP-20 | Cat 6 (consumer impact) | New csprojs added to `RawRabbit.sln` don't trigger build of source assemblies that previously didn't get built | Read of `RawRabbit.sln`: all source projects (Operations.*, Enrichers.*, DependencyInjection.*) are already in the sln. New test ProjectReferences only consume existing buildables |
| VP-21 | Cat 6 (consumer impact) | Renaming `Should_Deserialize_Pascal_Case_Json_Into_Pascal_Case_Properties` doesn't break external callers | Test methods are xUnit reflection-discovered; no source-level callers exist for test method names. `grep -rn "Should_Deserialize_Pascal_Case_Json_Into_Pascal_Case_Properties"` should return only the method definition itself |

---

## Tasks

### Task 1: Phase 4 backfill tests + cosmetics

**Files:**
- Modify: `test/RawRabbit.Tests/Serialization/JsonSerializerTests.cs`
- Modify: `src/RawRabbit/Serialization/JsonSerializer.cs`

- [ ] **Step 1: Rename existing test method.** In `test/RawRabbit.Tests/Serialization/JsonSerializerTests.cs:72`, rename the method `Should_Deserialize_Pascal_Case_Json_Into_Pascal_Case_Properties` to `Should_Deserialize_Case_Insensitively_Despite_CamelCase_Policy`. The new name reflects what's actually being pinned: that `PropertyNameCaseInsensitive = true` lets pascal-case JSON deserialize into pascal-case POCO properties despite `PropertyNamingPolicy = CamelCase`. Body unchanged.

- [ ] **Step 2: Add backfill test 1 — string passthrough.** Append to `JsonSerializerTests.cs` (before the `CountOccurrences` helper, after the existing `Should_Have_ApplicationJson_ContentType` test):

  ```csharp
  [Fact]
  public void Should_Pass_Through_Raw_String_Without_Json_Encoding()
  {
  	var serializer = CreateSerializer();

  	var bytes = serializer.Serialize("hello");
  	var roundTripped = (string)serializer.Deserialize(typeof(string), bytes);

  	Assert.Equal(Encoding.UTF8.GetBytes("hello"), bytes);
  	Assert.Equal("hello", roundTripped);
  	Assert.NotEqual(Encoding.UTF8.GetBytes("\"hello\""), bytes);
  }
  ```

  This pins the early-returns at `JsonSerializer.cs:23-26` (`if (obj is string str) return str;`) and `:32-35` (`if (type == typeof(string)) return str;`). The third assertion makes the "no JSON encoding" property explicit: the bytes are NOT `"hello"` quoted-as-JSON.

- [ ] **Step 3: Add backfill test 2 — collection replacement.** Append to `JsonSerializerTests.cs` immediately after Step 2's test:

  ```csharp
  [Fact]
  public void Should_Replace_Existing_Collection_Property_During_Deserialization()
  {
  	var serializer = CreateSerializer();
  	var json = "{\"items\":[1,2,3]}";
  	var bytes = Encoding.UTF8.GetBytes(json);

  	var result = (WithCollection)serializer.Deserialize(typeof(WithCollection), bytes);

  	Assert.Equal(new[] { 1, 2, 3 }, result.Items);
  	Assert.Equal(3, result.Items.Count);
  }
  ```

  Add the corresponding `internal class` near the other test POCOs (alongside `SimplePoco`, `WithDefaults`, etc.):

  ```csharp
  internal class WithCollection
  {
  	public List<int> Items { get; set; } = new() { 99 };
  }
  ```

  This pins Phase 4 spec §6 item 4 (`ObjectCreationHandling` collection-replacement): `Items` defaults to `[99]`, but after deserializing `{"items":[1,2,3]}`, `Items` is `[1,2,3]` (replaced, not appended-to). `Count == 3` (not 4) is the falsifiable assertion.

  Note: the `using System.Collections.Generic;` already at `JsonSerializerTests.cs:2` IS NEEDED by `List<int>`. Per VP-4, the spec's "drop unused" cosmetic is invalidated; do NOT drop it.

- [ ] **Step 4: Restore UTF-8 BOM on `src/RawRabbit/Serialization/JsonSerializer.cs`.** The file is currently ASCII (per VP-9 evidence — `file` reports "ASCII text"). Sibling files `ISerializer.cs` and `StringSerializerBase.cs` are "Unicode text, UTF-8 (with BOM) text". Restore consistency:

  ```bash
  printf '\xef\xbb\xbf' | cat - src/RawRabbit/Serialization/JsonSerializer.cs > /tmp/JsonSerializer.cs.bom && mv /tmp/JsonSerializer.cs.bom src/RawRabbit/Serialization/JsonSerializer.cs
  file src/RawRabbit/Serialization/JsonSerializer.cs   # expect: "Unicode text, UTF-8 (with BOM) text"
  ```

  No source-code change; just byte-level prepend of the 3-byte BOM (0xEF 0xBB 0xBF).

- [ ] **Step 5: Build + run targeted tests.** Verify the changes compile and all serializer tests pass:

  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Tests --no-build -c Release --filter "FullyQualifiedName~JsonSerializerTests"
  ```

  Expected: 0 build errors; warning shape stable. Test count for `JsonSerializerTests`: 12 passed (was 10; +2 backfill), 0 skipped, 0 failed.

- [ ] **Step 6: Commit.**

  ```bash
  git add test/RawRabbit.Tests/Serialization/JsonSerializerTests.cs src/RawRabbit/Serialization/JsonSerializer.cs
  git commit -m "Add JsonSerializer string passthrough + collection replacement tests"
  ```

---

### Task 2: Scaffold 14 new test projects

**Files:**
- Create: 14 csproj files (one per project listed in File Structure section)
- Modify: `RawRabbit.sln` (append 14 project entries via `dotnet sln add`)

- [ ] **Step 1: Define csproj template.** All 14 new csprojs share this shape (only `<ProjectReference>` lines differ per project). Minimum CPM-style PackageReferences match the existing `RawRabbit.Enrichers.Polly.Tests` precedent (no `Version` attributes; versions resolve from `Directory.Packages.props`):

  ```xml
  <Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
      <IsPackable>false</IsPackable>
      <GenerateAssemblyConfigurationAttribute>false</GenerateAssemblyConfigurationAttribute>
      <GenerateAssemblyCompanyAttribute>false</GenerateAssemblyCompanyAttribute>
      <GenerateAssemblyProductAttribute>false</GenerateAssemblyProductAttribute>
    </PropertyGroup>

    <ItemGroup>
      <PackageReference Include="Microsoft.NET.Test.Sdk" />
      <PackageReference Include="xunit" />
      <PackageReference Include="xunit.runner.visualstudio" />
      <PackageReference Include="Moq" />
    </ItemGroup>

    <ItemGroup>
      <ProjectReference Include="..\..\src\RawRabbit\RawRabbit.csproj" />
      <ProjectReference Include="..\..\src\<SOURCE_ASSEMBLY>\<SOURCE_ASSEMBLY>.csproj" />
    </ItemGroup>

  </Project>
  ```

  Every test project gets RawRabbit core (consumed by virtually all source assemblies) + its specific source assembly. The MessageContext consolidated test project (item 12 below) gets 4 ProjectReferences instead of 2.

  Per VP-18, files use 2-space indent (`.editorconfig` `[*.{csproj,…}]` rule).

- [ ] **Step 2: Validate template via the first scaffold (canary).** Create the first csproj and confirm the toolchain accepts it before scaffolding the other 13:

  ```bash
  mkdir -p test/RawRabbit.DependencyInjection.ServiceCollection.Tests
  # Write the csproj with <SOURCE_ASSEMBLY> = RawRabbit.DependencyInjection.ServiceCollection
  dotnet sln add test/RawRabbit.DependencyInjection.ServiceCollection.Tests/RawRabbit.DependencyInjection.ServiceCollection.Tests.csproj
  dotnet build -c Release   # expect: 0 errors; the new project builds (empty assembly)
  ```

  If build fails, diagnose template before proceeding. If build passes, the template is validated for the next 13.

- [ ] **Step 3: Scaffold the remaining 13 test projects.** Apply the template to each, substituting `<SOURCE_ASSEMBLY>` per the table:

  | # | Test project directory | Source assembly substitution(s) |
  |---|---|---|
  | 2 | `test/RawRabbit.Operations.Get.Tests/` | `RawRabbit.Operations.Get` |
  | 3 | `test/RawRabbit.Operations.MessageSequence.Tests/` | `RawRabbit.Operations.MessageSequence` |
  | 4 | `test/RawRabbit.Operations.Publish.Tests/` | `RawRabbit.Operations.Publish` |
  | 5 | `test/RawRabbit.Operations.Request.Tests/` | `RawRabbit.Operations.Request` |
  | 6 | `test/RawRabbit.Operations.Respond.Tests/` | `RawRabbit.Operations.Respond` |
  | 7 | `test/RawRabbit.Operations.StateMachine.Tests/` | `RawRabbit.Operations.StateMachine` |
  | 8 | `test/RawRabbit.Operations.Subscribe.Tests/` | `RawRabbit.Operations.Subscribe` |
  | 9 | `test/RawRabbit.Operations.Tools.Tests/` | `RawRabbit.Operations.Tools` |
  | 10 | `test/RawRabbit.Enrichers.Attributes.Tests/` | `RawRabbit.Enrichers.Attributes` |
  | 11 | `test/RawRabbit.Enrichers.GlobalExecutionId.Tests/` | `RawRabbit.Enrichers.GlobalExecutionId` |
  | 12 | `test/RawRabbit.Enrichers.MessageContext.Tests/` | `RawRabbit.Enrichers.MessageContext` + `RawRabbit.Enrichers.MessageContext.Respond` + `RawRabbit.Enrichers.MessageContext.Subscribe` (3 refs in one csproj — spec §2 row 12 consolidation) |
  | 13 | `test/RawRabbit.Enrichers.QueueSuffix.Tests/` | `RawRabbit.Enrichers.QueueSuffix` |
  | 14 | `test/RawRabbit.Enrichers.RetryLater.Tests/` | `RawRabbit.Enrichers.RetryLater` |

  For each: create the directory, write the csproj with the substituted ProjectReferences, then `dotnet sln add` it.

- [ ] **Step 4: Verify all 14 are added to the solution.** Per spec R8 mitigation:

  ```bash
  BEFORE_COUNT=$(git show HEAD:RawRabbit.sln | grep -c '^Project(')   # baseline before this wave's sln edits
  AFTER_COUNT=$(grep -c '^Project(' RawRabbit.sln)
  echo "Before: $BEFORE_COUNT, After: $AFTER_COUNT, Delta: $((AFTER_COUNT - BEFORE_COUNT))"
  # Expect Delta = 14
  dotnet sln list | wc -l
  # Expect: 14 more entries than baseline
  ```

  Also: `git diff RawRabbit.sln` should show only project-entry additions (no other line changes, no GUID conflicts).

- [ ] **Step 5: Build clean.**

  ```bash
  dotnet build -c Release
  ```

  Expected: 0 errors. Warning shape stable from baseline (Phase 4 close: ~143 warnings, all pre-existing). New test projects produce empty assemblies; no source-file warnings.

- [ ] **Step 6: Commit.**

  ```bash
  git add test/RawRabbit.DependencyInjection.ServiceCollection.Tests test/RawRabbit.Operations.Get.Tests test/RawRabbit.Operations.MessageSequence.Tests test/RawRabbit.Operations.Publish.Tests test/RawRabbit.Operations.Request.Tests test/RawRabbit.Operations.Respond.Tests test/RawRabbit.Operations.StateMachine.Tests test/RawRabbit.Operations.Subscribe.Tests test/RawRabbit.Operations.Tools.Tests test/RawRabbit.Enrichers.Attributes.Tests test/RawRabbit.Enrichers.GlobalExecutionId.Tests test/RawRabbit.Enrichers.MessageContext.Tests test/RawRabbit.Enrichers.QueueSuffix.Tests test/RawRabbit.Enrichers.RetryLater.Tests RawRabbit.sln
  git commit -m "Scaffold 14 new test projects for Phase 4.5 Waves 2-5"
  ```

---

### Task 3: A25 cleanup — `Common/ConnectionStringParserTests.cs`

**Files:**
- Modify: `test/RawRabbit.Tests/Common/ConnectionStringParserTests.cs`

This is the largest cleanup file (357 lines, 14 tests, all NON-SKIPPED). It's also the strongest `[Theory]/[InlineData]` candidate (12 of 14 tests follow the same input-shape).

- [ ] **Step 1: Convert 2 try/catch → `Assert.Throws<T>`.** (Per VP-1 — spec §4 missed these.)

  - Test `Should_Throw_Format_Exception_When_ConnectionString_Has_Bad_Port` (lines 309–330):

    ```csharp
    [Fact]
    public void Should_Throw_Format_Exception_When_ConnectionString_Has_Bad_Port()
    {
    	const string connectionString = "username:password@host1,host2:port";

    	var exception = Assert.Throws<FormatException>(() => ConnectionStringParser.Parse(connectionString));

    	Assert.Equal("The supplied port 'port' in the connection string is not a number", exception.Message);
    }
    ```

  - Test `Should_Throw_Argument_Exception_When_ConnectionString_Has_Bad_Property` (lines 332–353):

    ```csharp
    [Fact]
    public void Should_Throw_Argument_Exception_When_ConnectionString_Has_Bad_Property()
    {
    	const string connectionString = "username:password@host1,host2?badproperty=true";

    	var exception = Assert.Throws<ArgumentException>(() => ConnectionStringParser.Parse(connectionString));

    	Assert.Equal("No configuration property named 'badproperty'", exception.Message);
    }
    ```

  Both conversions: drop `Exception exception = null;` + try/catch + `Assert.NotNull` + non-generic `Assert.IsType` + extract message check via `exception.Message`. The `Assert.Throws<T>` return value IS the typed exception.

  Net: -2 try/catch, -2 non-generic IsType (folded into the conversion), -2 `Assert.NotNull` (no longer needed), AAA comments removed (see Step 3).

- [ ] **Step 2: Consolidate 12 similar tests via `[Theory]/[InlineData]`.** Tests 1–12 share the input shape (single `connectionString` string → multiple `Assert.Equal` on parsed config fields). Group into `[Theory]` clusters. Suggested grouping (implementer may adjust if a finer grouping reads more naturally; minimum: ≥1 `[Theory]` use in this file per spec §7 condition 7):

  - **Cluster A — credentials/no-credentials × port-or-no × virtualhost-or-no** (single-host variants): tests 1, 2, 3, 5, 6, 7, 8, 9 → 1 `[Theory]` with 8 `[InlineData]` rows. Parameters: `(string connectionString, string expectedUsername, string expectedPassword, string expectedVirtualHost, string expectedHost, int expectedPort)`.
  - **Cluster B — multi-host with parameters**: tests 4, 10, 11, 12 → 1 `[Theory]` with 4 `[InlineData]` rows. Parameters include the 7 timing/recovery flag fields. Hosts asserted as `[expectedHost1, expectedHost2]`.

  Method body for cluster A:

  ```csharp
  [Theory]
  [InlineData("host", "guest", "guest", "/", "host", 5672)]
  [InlineData("host:1234", "guest", "guest", "/", "host", 1234)]
  [InlineData("host/virtualHost", "guest", "guest", "virtualHost", "host", 5672)]
  [InlineData("host:1234/virtualHost", "guest", "guest", "virtualHost", "host", 1234)]
  [InlineData("username:password@host1,host2", "username", "password", "/", "host1", 5672)]
  [InlineData("username:password@host1,host2/virtualHost", "username", "password", "virtualHost", "host1", 5672)]
  [InlineData("username:password@host1,host2:1234", "username", "password", "/", "host1", 1234)]
  [InlineData("username:password@host1,host2:1234/virtualHost", "username", "password", "virtualHost", "host1", 1234)]
  public void Should_Parse_Connection_String_With_Various_Hosts_And_Credentials(string connectionString, string username, string password, string virtualHost, string firstHost, int port)
  {
  	var config = ConnectionStringParser.Parse(connectionString);

  	Assert.Equal(username, config.Username);
  	Assert.Equal(password, config.Password);
  	Assert.Equal(virtualHost, config.VirtualHost);
  	Assert.Equal(firstHost, config.Hostnames[0]);
  	Assert.Equal(port, config.Port);
  }
  ```

  For cluster B, similar shape with the parameter-string fields included.

- [ ] **Step 3: Convert all 45 AAA `/* Setup */ /* Test */ /* Assert */` comments to blank-line separation.** Per spec §3 conventions row "AAA structure". Find/replace each `/* Setup */`, `/* Test */`, `/* Assert */` line with an empty line. Acceptance: `grep -c '/\* \(Setup\|Test\|Assert\) \*/' test/RawRabbit.Tests/Common/ConnectionStringParserTests.cs` returns 0.

- [ ] **Step 4: Build + targeted test run.**

  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Tests --no-build -c Release --filter "FullyQualifiedName~ConnectionStringParserTests"
  ```

  Expected: 0 build errors; ConnectionStringParserTests: 14 individual `[Fact]`/`[Theory]` test results pass — but post-Theory consolidation, the test method count drops while the per-row test result count stays at 14 (`[Theory]` rows each report as separate test results). Skipped count: 0 (unchanged — no skipped tests in this file).

- [ ] **Step 5: Commit.**

  ```bash
  git add test/RawRabbit.Tests/Common/ConnectionStringParserTests.cs
  git commit -m "Clean A25 anti-patterns from ConnectionStringParserTests; consolidate via Theory"
  ```

---

### Task 4: A25 cleanup — `Common/NamingConventionsTests.cs`

**Files:**
- Modify: `test/RawRabbit.Tests/Common/NamingConventionsTests.cs`

5 tests, all NON-SKIPPED. 4 of 5 take a single `string` input and assert one expected output — `[Theory]` candidate. The 5th takes a `string[]` array — keep as `[Fact]`.

- [ ] **Step 1: Consolidate 4 string-input tests via `[Theory]/[InlineData]`.** Tests 1, 2, 3, 4 (`Should_Be_Able_To_Get_Application_Name_From_Console_App_Or_Service`, `…_With_Vshost`, `…IIS_Hosted_App_With_ApplicationPool_Flag`, `…IIS_Hosted_App_With_Host_Flag`) all call `NamingConventions.GetApplicationName(string)` and assert one string. Consolidate:

  ```csharp
  [Theory]
  [InlineData(@"\""Services\\Micro.Services.MagicMaker\\bin\\Micro.Services.MagicMaker.exe\"" ", "micro_services_magicmaker")]
  [InlineData(@"\""Services\\Micro.Services.MagicMaker\\bin\\Micro.Services.MagicMaker.vshost.exe\"" ", "micro_services_magicmaker")]
  [InlineData(@"""c:\\windows\\system32\\inetsrv\\w3wp.exe -ap \""Application.Name\"" -v \""v4.0\"" -l \""webengine4.dll\"" -a \\\\.\\pipe\\iisipm6866bb0f-a36a-49b2-9ea8-d83ca69e873d -w \""\"" -m 0 -t 20 -ta 0""", "application_name")]
  [InlineData(@"""c:\\windows\\system32\\inetsrv\\w3wp.exe -ap \""Application.Name\"" -v \""v4.0\"" -l \""webengine4.dll\"" -a \\\\.\\pipe\\iisipm6866bb0f-a36a-49b2-9ea8-d83ca69e873d -h \""C:\\inetpub\\temp\\apppools\\voyager_dk\\voyager_dk.config\"" -w \""\"" -m 0 -t 20 -ta 0""", "application_name")]
  public void Should_Get_Application_Name_From_Single_Command_Line(string commandLine, string expectedName)
  {
  	var actual = NamingConventions.GetApplicationName(commandLine);

  	Assert.Equal(expectedName, actual);
  }
  ```

  Test 5 (`Should_Be_Able_To_Get_Appllication_Name_From_Dot_Net_Core_Hosted_Apps`) takes a `string[]`; keep as `[Fact]` (different overload, different shape).

- [ ] **Step 2: Convert 15 AAA comments to blank lines.** Same approach as Task 3 Step 3.

- [ ] **Step 3: Build + targeted test run.**

  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Tests --no-build -c Release --filter "FullyQualifiedName~NamingConventionsTests"
  ```

  Expected: 0 build errors; NamingConventionsTests: 5 test results pass (4 from `[Theory]` rows + 1 `[Fact]`); 0 skipped; 0 failed.

- [ ] **Step 4: Commit.**

  ```bash
  git add test/RawRabbit.Tests/Common/NamingConventionsTests.cs
  git commit -m "Clean AAA comments from NamingConventionsTests; consolidate via Theory"
  ```

---

### Task 5: A25 cleanup — 3 `Channel/` files

**Files:**
- Modify: `test/RawRabbit.Tests/Channel/ChannelPoolTests.cs`
- Modify: `test/RawRabbit.Tests/Channel/DynamicChannelPoolTests.cs`
- Verify-only (per D10): `test/RawRabbit.Tests/Channel/ChannelFactoryTests.cs` (all 4 tests skipped — no edits)

**Per-file Wave 1 actual work** (per VP-3, NON-SKIPPED only):

| File | Spec §4 totals (all hits) | Wave 1 actual work (NON-SKIPPED only) |
|------|------|---------|
| ChannelFactoryTests.cs | 2 Assert.True(true), 0 IsType, 0 AAA | **0 — all 4 tests skipped (verify-only)** |
| ChannelPoolTests.cs | 5 Assert.True(true), 0 IsType, 0 AAA | **2 Assert.True(true) + 1 try/catch (NON-SKIPPED tests #6 line 187 + #8 lines 234-243)** |
| DynamicChannelPoolTests.cs | 1 Assert.True(true), 0 IsType, 9 AAA | **1 Assert.True(true) + 9 AAA (no skipped tests)** |

The other 3 Assert.True(true) hits in ChannelPoolTests.cs (lines 127, 216, 267) are inside skipped tests — do NOT touch per D10.

- [ ] **Step 1: ChannelPoolTests.cs — convert NON-SKIPPED `Should_Be_Able_To_Cancel_With_Token` (lines 220–243).** Replace the try/catch + 2 `Assert.True(true/false)` with `Assert.ThrowsAsync`:

  ```csharp
  [Fact]
  public async Task Should_Be_Able_To_Cancel_With_Token()
  {
  	var closedChannel = new Mock<IModel> { Name = "Closed Channel" };
  	closedChannel.As<IRecoverable>();
  	closedChannel
  		.Setup(m => m.IsClosed)
  		.Returns(true);
  	var pool = new StaticChannelPool(new[] { closedChannel.Object });
  	var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

  	await Assert.ThrowsAsync<OperationCanceledException>(() => pool.GetAsync(cts.Token));
  }
  ```

  Drop the `/* Setup */`, `/* Test */`, `/* Assert */` AAA comments at the same time (visual blank-line separation only).

- [ ] **Step 2: ChannelPoolTests.cs — drop `Assert.True(true, …)` from `Should_Be_Able_To_Have_Multiple_Pending_Requests` (line 187).** The test calls `Task.WaitAll(taskArray)` and then asserts `Assert.True(true, "No exception thrown with multiple pending");`. Drop the `Assert.True` line entirely; the test name ("Should_Be_Able_To_Have_Multiple_Pending_Requests") + the absence of an exception during `Task.WaitAll` IS the assertion. Resulting body ends:

  ```csharp
  for (var i = 0; i < numberOfCalls; i++)
  {
  	taskArray[i] = pool.GetAsync();
  }

  Task.WaitAll(taskArray);
  ```

  No new assertion required — xUnit treats no-exception-thrown as pass.

- [ ] **Step 3: ChannelPoolTests.cs — preserve all 3 skip annotations verbatim.** Skipped tests at lines 106, 190, 245 retain their `[Fact(Skip = "Phase 5/7 territory: …")]` decorations and bodies UNCHANGED. Per D10 + spec §13.1.

- [ ] **Step 4: DynamicChannelPoolTests.cs — drop `Assert.True(true, …)` from `Should_Not_Throw_Exception_If_Trying_To_Remove_Channel_Not_In_Pool` (line 49).** Same pattern as Step 2: drop the `Assert.True` line; no replacement needed. Resulting body ends:

  ```csharp
  pool.Remove(channel.Object);
  ```

- [ ] **Step 5: DynamicChannelPoolTests.cs — convert all 9 AAA comments to blank lines.** 3 tests × 3 comments each = 9 AAA hits.

- [ ] **Step 6: ChannelFactoryTests.cs (Channel) — verify skip-preservation.** No edits. Confirm:

  ```bash
  grep -c '\[Fact(Skip = "Phase 5/7' test/RawRabbit.Tests/Channel/ChannelFactoryTests.cs
  # Expect: 4
  ```

  All 4 skip annotations at lines 15, 46, 77, 103 with the same Phase 5/7 commentary as before this wave.

- [ ] **Step 7: Build + targeted test run.**

  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Tests --no-build -c Release --filter "FullyQualifiedName~RawRabbit.Tests.Channel"
  ```

  Expected: 0 build errors; Channel namespace tests: 8 passed (was 8 — same non-skipped count: 5 in ChannelPoolTests + 3 in DynamicChannelPoolTests + 0 in ChannelFactoryTests), 7 skipped (preserved: 4 in ChannelFactoryTests + 3 in ChannelPoolTests), 0 failed.

- [ ] **Step 8: Commit.**

  ```bash
  git add test/RawRabbit.Tests/Channel/ChannelPoolTests.cs test/RawRabbit.Tests/Channel/DynamicChannelPoolTests.cs
  git commit -m "Clean A25 anti-patterns from Channel tests; preserve skip annotations"
  ```

---

### Task 6: A25 cleanup — 2 Polly test files

**Files:**
- Modify: `test/RawRabbit.Enrichers.Polly.Tests/Middleware/QueueDeclareMiddlewareTests.cs`
- Verify-only (per D10): `test/RawRabbit.Enrichers.Polly.Tests/Services/ChannelFactoryTests.cs` (all 2 tests skipped — no edits)

- [ ] **Step 1: QueueDeclareMiddlewareTests.cs — convert 2 AAA comments to blank lines.** The single test in this file has `/* Test */` at line 49 and `/* Assert */` at line 52. Replace both with blank lines. Note: there is no `/* Setup */` comment in this file (the setup section is unmarked).

  Other content preserved: the `Assert.True(policyCalled, "Should call policy")` at line 53 is `Assert.True(<bool-expression>, message)` — this is NOT the A25 anti-pattern #1 (which forbids `Assert.True(true, …)` specifically; literal-true). Keep as-is.

- [ ] **Step 2: Services/ChannelFactoryTests.cs — verify skip-preservation.** No edits. Confirm:

  ```bash
  grep -c '\[Fact(Skip = "Phase 5/7' test/RawRabbit.Enrichers.Polly.Tests/Services/ChannelFactoryTests.cs
  # Expect: 2
  ```

  Both skip annotations at lines 16, 52 with the same Phase 5/7 commentary as before this wave.

- [ ] **Step 3: Build + targeted test run.**

  ```bash
  dotnet build -c Release
  dotnet test test/RawRabbit.Enrichers.Polly.Tests --no-build -c Release
  ```

  Expected: 0 build errors; Polly tests: 1 passed (the QueueDeclareMiddleware test), 2 skipped (preserved: 2 in Services/ChannelFactoryTests), 0 failed.

- [ ] **Step 4: Commit.**

  ```bash
  git add test/RawRabbit.Enrichers.Polly.Tests/Middleware/QueueDeclareMiddlewareTests.cs
  git commit -m "Clean AAA comments from Polly QueueDeclareMiddlewareTests"
  ```

---

### Task 7: Wave 1 phase-acceptance verification (no commit)

**Files:** none — verification commands only.

This task confirms spec §7 acceptance condition 1 ("Wave 1 shipped (mandatory)") and the Wave 1-applicable subset of §10 validation steps. If any check fails, fix the issue and re-commit on the appropriate prior task; do NOT add a "fix Wave 1 acceptance" commit.

- [ ] **Step 1: Build clean (§10 step 1).**

  ```bash
  dotnet build -c Release
  ```

  Expected: 0 errors. Warning shape stable from Phase 4 close (~143 warnings, all pre-existing). Per spec §7 condition 4: no NEW warning categories introduced by Phase 4.5 code.

- [ ] **Step 2: All A25 anti-patterns removed from cleaned-up files (§7 condition 1, partial).**

  ```bash
  for f in \
    test/RawRabbit.Tests/Channel/ChannelPoolTests.cs \
    test/RawRabbit.Tests/Channel/DynamicChannelPoolTests.cs \
    test/RawRabbit.Tests/Common/ConnectionStringParserTests.cs \
    test/RawRabbit.Tests/Common/NamingConventionsTests.cs \
    test/RawRabbit.Enrichers.Polly.Tests/Middleware/QueueDeclareMiddlewareTests.cs; do
    echo "=== $f ==="
    # NON-SKIPPED Assert.True(true, ...) — should be 0 outside skipped tests; the skipped-test ones in ChannelPoolTests/ChannelFactoryTests stay
    grep -n 'Assert\.True(true' "$f" || echo "  (no Assert.True(true) hits)"
    # Non-generic Assert.IsType(typeof(...), ...) — should be 0
    grep -n 'Assert\.IsType(typeof' "$f" || echo "  (no non-generic IsType hits)"
    # AAA comments — should be 0 in cleanup-target files
    grep -nE '/\* (Setup|Arrange|Act|Test|Assert) \*/' "$f" || echo "  (no AAA comments)"
    # NON-SKIPPED hand-rolled try/catch around code-under-test — should be 0
    # (Some try/catch may legitimately exist in setup/teardown; manual review required)
  done
  ```

  Expected: NON-SKIPPED `Assert.True(true)` hits remain only in skipped tests (ChannelPoolTests lines 127, 216, 267 — 3 total; ChannelFactoryTests lines 38, 42, 69, 73 — 4 total per-line within skipped tests). Generic-IsType hits gone. AAA comments gone. NON-SKIPPED try/catch gone (note: Polly Services/ChannelFactoryTests.cs's skipped-test try/catch — if any — stays).

- [ ] **Step 3: 2 new JsonSerializer tests pass + 2 cosmetics applied (§7 condition 1, partial).**

  ```bash
  dotnet test test/RawRabbit.Tests --no-build -c Release --filter "FullyQualifiedName~JsonSerializerTests"
  # Expect: 12 passed (was 10), 0 skipped, 0 failed
  grep -c "Should_Pass_Through_Raw_String_Without_Json_Encoding" test/RawRabbit.Tests/Serialization/JsonSerializerTests.cs   # 1
  grep -c "Should_Replace_Existing_Collection_Property_During_Deserialization" test/RawRabbit.Tests/Serialization/JsonSerializerTests.cs   # 1
  grep -c "Should_Deserialize_Case_Insensitively_Despite_CamelCase_Policy" test/RawRabbit.Tests/Serialization/JsonSerializerTests.cs   # 1 (the rename target)
  grep -c "Should_Deserialize_Pascal_Case_Json_Into_Pascal_Case_Properties" test/RawRabbit.Tests/Serialization/JsonSerializerTests.cs   # 0 (renamed away)
  file src/RawRabbit/Serialization/JsonSerializer.cs   # expect: "Unicode text, UTF-8 (with BOM) text"
  ```

  Note: only 2 cosmetics, not 3 — the "drop unused using" cosmetic was retired per VP-4.

- [ ] **Step 4: 14 new test projects scaffolded and visible in `dotnet sln list` (§7 condition 1, partial; spec R8 mitigation).**

  ```bash
  dotnet sln list | grep -E "(ServiceCollection|Operations\..*\.Tests|Enrichers\..*\.Tests)" | wc -l
  # Expect: 16 (14 new + 2 pre-existing matching this regex: Enrichers.Polly.Tests + actually no Operations Tests pre-existed, so 14 new + Polly = 15... let me recompute)
  ```

  More-direct check for the 14 new csprojs:

  ```bash
  for proj in \
    RawRabbit.DependencyInjection.ServiceCollection.Tests \
    RawRabbit.Operations.Get.Tests RawRabbit.Operations.MessageSequence.Tests \
    RawRabbit.Operations.Publish.Tests RawRabbit.Operations.Request.Tests \
    RawRabbit.Operations.Respond.Tests RawRabbit.Operations.StateMachine.Tests \
    RawRabbit.Operations.Subscribe.Tests RawRabbit.Operations.Tools.Tests \
    RawRabbit.Enrichers.Attributes.Tests RawRabbit.Enrichers.GlobalExecutionId.Tests \
    RawRabbit.Enrichers.MessageContext.Tests RawRabbit.Enrichers.QueueSuffix.Tests \
    RawRabbit.Enrichers.RetryLater.Tests; do
    test -f "test/$proj/$proj.csproj" && echo "✓ $proj" || echo "✗ MISSING: $proj"
    grep -q "$proj" RawRabbit.sln && echo "  ✓ in sln" || echo "  ✗ NOT in sln"
  done
  ```

  Expected: all 14 marked ✓ ✓.

- [ ] **Step 5: Skipped tests preserved verbatim (§7 condition 6).**

  ```bash
  grep -rEn '\[Fact\(Skip\s*=\s*"Phase 5/7' test/RawRabbit.Tests test/RawRabbit.Enrichers.Polly.Tests
  # Expect: exactly 9 hits at:
  #   test/RawRabbit.Tests/Channel/ChannelFactoryTests.cs:15, :46, :77, :103
  #   test/RawRabbit.Tests/Channel/ChannelPoolTests.cs:106, :190, :245
  #   test/RawRabbit.Enrichers.Polly.Tests/Services/ChannelFactoryTests.cs:16, :52
  ```

  Each annotation message starts with `Phase 5/7 territory:` per Phase 4's documented convention.

- [ ] **Step 6: xUnit analyzer warnings on cleaned-up files (§7 condition 5).** Confirm xUnit2020 / xUnit2004 / xUnit2007 / xUnit1031 warnings are zero in the 5 cleaned-up files (the 2 verify-only files retain skipped-test patterns by design and are not in scope here):

  ```bash
  dotnet build -c Release 2>&1 | grep -E "xUnit(2020|2004|2007|1031)" | grep -E "(ChannelPoolTests|DynamicChannelPoolTests|ConnectionStringParserTests|NamingConventionsTests|QueueDeclareMiddlewareTests)\.cs"
  # Expect: empty output (no matching warnings in the 5 cleaned-up files)
  ```

  Pre-existing warnings elsewhere (e.g., in `IntegrationTests/`, in skipped tests, or in source code) are out of scope per spec §1 + §7 condition 4.

- [ ] **Step 7: Aggregate test pass count (§10 step 2 + §7 condition 2 partial — Wave 1 contribution only).**

  ```bash
  dotnet test test/RawRabbit.Tests --no-build -c Release
  # Expect: ≥41 passed (was 39 at Phase 4 close; +2 from JsonSerializer backfill; cleanup tasks may shift Theory row counts ±n but absolute non-skipped test methods unchanged); 7 skipped (unchanged); 0 failed
  dotnet test test/RawRabbit.Enrichers.Polly.Tests --no-build -c Release
  # Expect: 1 passed (unchanged); 2 skipped (unchanged); 0 failed
  ```

  Note: the 14 new test projects have no `.cs` source files, so `dotnet test` against them either reports 0 tests or "no tests found" — both are acceptable; Wave 1 doesn't add tests to them.

- [ ] **Step 8: `[Theory]/[InlineData]` convention exercised in ≥3 places across this wave's edits (§7 condition 7 — Wave 1 contribution).**

  ```bash
  grep -rEn '\[Theory\]' test/RawRabbit.Tests test/RawRabbit.Enrichers.Polly.Tests | wc -l
  # Expect: ≥2 from this wave (Task 3 cluster A + Task 4 string-input consolidation; Task 3 cluster B is optional). 
  # Phase-wide §7 condition 7 requires ≥3 across all of Phase 4.5; Waves 2-5 contribute the rest if Wave 1 only achieves 2. Acceptable for Wave 1 to land ≥2 with the third deferred.
  ```

  If only 2 land in Wave 1, document as "Wave 1 contributes 2 of the 3 phase-wide `[Theory]` uses; Waves 2-5 carry the third" in the wave-completion note (no separate commit).

- [ ] **Step 9: Wave 1 done.** When all Steps 1–8 hold, Wave 1 is complete. Aggregate state:
  - Tasks 1–6 each have a single commit on `2.0` (6 commits added in Wave 1)
  - 14 new csprojs in `RawRabbit.sln`; 2 new tests in `JsonSerializerTests.cs`; A25 anti-patterns gone from NON-SKIPPED tests in 5 files; 2 verify-only files unchanged
  - Test baseline: 41/9/0 in `RawRabbit.Tests`; 1/2/0 in `RawRabbit.Enrichers.Polly.Tests`; 42 passing aggregate (vs. 40 pre-Wave-1)
  - Branch is 6 commits ahead of `origin/2.0` (3 from Phase 4.5 spec/UDD inherited at session start + 6 from this wave's tasks 1–6 = 9 ahead total, modulo any user push between waves)
  - Decision point: plan Waves 2-5 (separately or combined), OR push to origin first

---

## Tasks NOT in this plan

Inherited verbatim from spec §12 (with Wave-1 scope clarification appended):

- **Per-area exact test method names** — derived during the implementation plan's verification pass (one per source class), not in this spec
- **`test/RawRabbit.IntegrationTests/*` cleanup** — Phase 6 (live broker required)
- **Un-skipping the 9 Phase 5/7-tagged tests** — Phase 5/7 (broker-layer modernization)
- **Performance benchmarks (`test/RawRabbit.PerformanceTest/`)** — separate concern
- **Tests for MessagePack / Protobuf / ZeroFormatter / HttpContext / Compatibility.Legacy** — Phase 7 (or Phase 1 V2 for HttpContext)
- **Solution file restructuring beyond `dotnet sln add`** — solution folder reorganization is not in scope; new projects join the existing `test` solution folder
- **CI configuration changes** — none required; existing pipeline (if any) auto-discovers new test projects via `dotnet test` at solution scope

A new spec → new plan cycle is required to add any of the above to a future phase.

**Wave-1-specific scope clarification:**

- **Phase 4.5 Waves 2–5** are out of scope for this plan. Waves 2–5 require their own plan(s), to be drafted after Wave 1 ships per the spec's "user picks wave-by-wave" model (D5).
- **Per-area test method enumeration for the 14 new test projects** is out of scope for Wave 1 — the new csprojs are scaffold-only (no `.cs` source files). Method enumeration happens in the Waves 2–5 plan(s).
- **The third Phase 4 cosmetic** ("drop unused `using System.Collections.Generic;`" per spec §5 line 177) is out of scope here — invalidated by the second backfill test which requires `List<int>`. Per VP-4.

## Known issues inherited from spec

Inherited verbatim from spec §13 (user-acknowledged on 2026-05-13 during the Phase 4.5 brainstorm):

1. **Pre-existing skipped test annotations remain in place.** The 9 currently-skipped tests stay skipped (D10); Phase 4.5 does not un-skip them.
2. **Pre-existing inconsistency in `RawRabbit.Tests` ProjectReferences preserved.** RawRabbit.Tests refs Autofac + Ninject DI but not ServiceCollection — Phase 4.5 does NOT fix this by either adding the missing ref or moving Autofac/Ninject to separate projects (D8). The new ServiceCollection.Tests is its own project; the inconsistency for Autofac/Ninject is intentional preservation, not gold-plating.
3. **`[Theory]` convention conversion is opportunistic, not exhaustive.** Wave 1 converts only where 3+ tests genuinely share input/expected shape (R4); other parameterized-style tests stay as `[Fact]` if conversion would obscure intent. Acceptance condition 7 requires ≥3 conversions, not all.
4. **Test-count estimates are ranges (217–307); only the lower bound is the acceptance gate.** Plan-write verification will re-tighten ranges per area; the upper bound is informational.
5. **Carries forward from prior phases:** `RawRabbit.Enrichers.HttpContext` still has no functional implementation on net10 (Phase 1 V1); `test/RawRabbit.IntegrationTests` still requires a live broker (Phase 6); MessagePack 1.7.3.4's NU1902 vulnerability warnings persist (Phase 7); the heterogeneous metadata acceptances from Phase 2 remain as documented.
