# Modernization Phase 4 Implementation Plan

> **For agentic workers:** REQUIRED: Use `superpowers:subagent-driven-development` to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Source spec:** `docs/specs/2026-05-12-modernization-phase-4-design.md` (commit SHA: `e61d51e`)

**Goal:** After this plan executes, on a clean clone with the .NET 10 SDK installed, `dotnet restore && dotnet build -c Release && dotnet test test/RawRabbit.Tests --no-build -c Release --blame-hang-timeout 15s && dotnet test test/RawRabbit.Enrichers.Polly.Tests --no-build -c Release && dotnet pack -c Release --no-build` succeeds end-to-end. Test counts go from the Phase 1/2/3 baseline of 30 passed / 9 skipped / 0 failed to **40 passed / 9 skipped / 0 failed** — the 10 added tests are new round-trip coverage for the STJ-backed `JsonSerializer`. `dotnet pack` produces 25 `.nupkg` files. `Newtonsoft.Json` no longer appears in `Directory.Packages.props`, in any `*.csproj`, or in any `*.cs` file under `src/`, `test/`, or `sample/`. The 3× `NU1903` warnings against `Newtonsoft.Json 10.0.1` (GHSA-5crp-9r3c-p9vr) disappear from the warning shape.

**Architecture:** Replace the Newtonsoft-backed `src/RawRabbit/Serialization/JsonSerializer.cs` with a System.Text.Json-backed implementation at the same path/namespace/base-class (`RawRabbit.Serialization.JsonSerializer : StringSerializerBase`); the constructor takes `JsonSerializerOptions` instead of `Newtonsoft.Json.JsonSerializer`. Replace the 11-setting Newtonsoft block in `RawRabbitDependencyRegisterExtension.cs` with a 4-property STJ options literal (`PropertyNamingPolicy = CamelCase`, `DefaultIgnoreCondition = WhenWritingNull`, `PropertyNameCaseInsensitive = true`, `WriteIndented = false`); rest are STJ defaults per spec §4 translation. Drop the `Newtonsoft.Json` `<PackageReference>` from `RawRabbit.csproj` and the `<PackageVersion>` from `Directory.Packages.props` — STJ ships in the BCL on net10. Drop the dead `using Newtonsoft.Json.Linq;` from Polly's `RetryKey.cs` (forced by transitive build resolution per spec §3 atomicity). Add 10 round-trip tests in a new `test/RawRabbit.Tests/Serialization/JsonSerializerTests.cs` to verify the new behavior.

**Tech stack (inherited from spec):** .NET 10 SDK 10.0.107 + C# 13 (default for net10); MSBuild Directory.Build.props + Directory.Packages.props from Phase 2; `Microsoft.Extensions.Logging.Abstractions` 10.0.0 + `Serilog.Extensions.Logging` 10.0.0 from Phase 3; `System.Text.Json` from net10 BCL (no PackageReference); `xunit` 2.9.3 (test stack from Phase 1). All other package versions stay at their Phase 1/2/3 values. `Newtonsoft.Json` is removed.

---

## File Structure

**Create (1):**
- `test/RawRabbit.Tests/Serialization/JsonSerializerTests.cs` — 10 `[Fact]` round-trip tests for the new STJ-backed `JsonSerializer` (Task 2)

**Modify (5; all in Task 1's atomic commit):**
- `Directory.Packages.props` — delete `Newtonsoft.Json` `<PackageVersion>` line
- `src/RawRabbit/RawRabbit.csproj` — delete `Newtonsoft.Json` `<PackageReference>` line
- `src/RawRabbit/Serialization/JsonSerializer.cs` — whole-file replacement with STJ implementation
- `src/RawRabbit/DependencyInjection/RawRabbitDependencyRegisterExtension.cs` — drop 2 Newtonsoft `using` directives, add 2 STJ ones, replace 11-setting block at lines 49–62 with 4-property `JsonSerializerOptions` literal
- `src/RawRabbit.Enrichers.Polly/RetryKey.cs` — delete dead `using Newtonsoft.Json.Linq;` (line 5)

**Test:** `test/RawRabbit.Tests/Serialization/JsonSerializerTests.cs` (the only added test file; same path as in Create above).

---

## Inherited from spec

The following assumptions were verified by `thorough-brainstorming` at spec-write time and are NOT re-verified here. Trusted as ground truth:

- **A1–A2:** `Directory.Packages.props` and `src/RawRabbit/RawRabbit.csproj` carry the `Newtonsoft.Json` central + project references in CPM-style (single removable lines).
- **A3:** `src/RawRabbit/Serialization/JsonSerializer.cs` is a 51-line single-class `JsonSerializer : StringSerializerBase` in namespace `RawRabbit.Serialization`.
- **A4:** `RawRabbitDependencyRegisterExtension.cs` has 11-setting Newtonsoft `JsonSerializer` literal at lines 49–62 and `using Newtonsoft.Json;` + `using Newtonsoft.Json.Serialization;` at lines 2–3.
- **A5:** `src/RawRabbit.Enrichers.Polly/RetryKey.cs:5` is `using Newtonsoft.Json.Linq;` and no `JToken`/`JObject`/`JArray` symbol is referenced anywhere in that file.
- **A6:** `src/RawRabbit.Compatibility.Legacy/` has zero `.cs` files referencing `Newtonsoft`.
- **A7:** The only `.cs` files in `src/` referencing `Newtonsoft` are the 3 in §3 of the spec.
- **A8/A9/A10:** No public RawRabbit API exposes a Newtonsoft type; no Newtonsoft attributes on framework types; no custom `JsonConverter` (Newtonsoft variant) defined.
- **A11:** No `.cs` file in `test/` references `Newtonsoft`, `ISerializer`, or `JsonSerializer`.
- **A12–A17:** STJ on net10 — `JsonSerializer`, `JsonSerializerOptions`, `JsonNamingPolicy.CamelCase`, `JsonIgnoreCondition.WhenWritingNull` are BCL; both runtime-type `Serialize(object, Type, JsonSerializerOptions)` and Type-param `Deserialize(string, Type, JsonSerializerOptions)` overloads exist; STJ defaults match Newtonsoft `MissingMemberHandling.Ignore` and `CheckAdditionalContent = true`.
- **A18:** Top-level message type is supplied by the `message_type` AMQP header, not `$type` on the body.
- **A19/A20:** No framework-internal type relies on `TypeNameHandling.Auto`; `ExceptionInformation` is a 4-string POCO.
- **A21:** `Newtonsoft.Json 10.0.1` fires `NU1903` (high-severity vulnerability per GHSA-5crp-9r3c-p9vr); package removal removes the warnings.
- **A22:** Only `RawRabbit.csproj` has a `Newtonsoft.Json` `<PackageReference>`; central removal won't orphan via NU1010.
- **A23:** No Newtonsoft serialization callbacks (`[OnDeserialized]`/etc.) on framework types.
- **(Polly transitive finding):** Polly enricher's `using Newtonsoft.Json.Linq;` survives today only via `ProjectReference → RawRabbit.csproj`; package removal forces atomic cleanup.
- **A24:** `test/RawRabbit.Tests/` follows one-subdirectory-per-source-area convention; new `Serialization/` subdir matches `Channel/`/`Common/` precedent.
- **A25:** Pre-existing tests have documented quality issues (`Assert.True(true)`, hand-rolled try/catch, race-prone timing, non-generic `IsType`, missing `[Theory]`, AAA block comments); new Phase 4 tests must NOT perpetuate them. (Comprehensive cleanup is deferred to Phase 4.5 per spec D8.)

---

## Verified plan-level assumptions

Newly introduced by this plan (paths, signatures, commands, ordering, code-in-plan validity, consumer impact) and verified at plan-write time against HEAD `e61d51e`:

| # | Category | Assumption | Evidence |
|---|---|---|---|
| P1 | File path | `Directory.Packages.props:8` is `<PackageVersion Include="Newtonsoft.Json" Version="10.0.1" />`; single removable line, no other `Newtonsoft.Json` token in the file | `grep -n "Newtonsoft" Directory.Packages.props` returned only line 8 |
| P2 | File path | `src/RawRabbit/RawRabbit.csproj:18` is `<PackageReference Include="Newtonsoft.Json" />` (CPM-style, no inline `Version`); single removable line | `grep -n "Newtonsoft" src/RawRabbit/RawRabbit.csproj` returned only line 18. (Spec A2 cited line 19 — off-by-one in the spec; the "single removable line in CPM-style" claim is intact and material to Task 1) |
| P3 | File path | `src/RawRabbit/Serialization/JsonSerializer.cs` exists at this path (whole-file replacement target) | `ls -la` confirmed 1046 bytes |
| P4 | File path / structure | `RawRabbitDependencyRegisterExtension.cs` has `using Newtonsoft.Json;` at line 2 and `using Newtonsoft.Json.Serialization;` at line 3; the 11-setting block runs lines 49–62 | `sed -n '1,5p;49,62p'` confirmed both ranges |
| P5 | File path | `src/RawRabbit.Enrichers.Polly/RetryKey.cs:5` is exactly `using Newtonsoft.Json.Linq;` (sole `Newtonsoft` token in the file) | `sed -n '5p'` confirmed |
| P6 | File path / structure | `test/RawRabbit.Tests/Serialization/` does not exist yet (Task 2 creates it as a new subdirectory matching the `Channel/`/`Common/` convention) | `ls test/RawRabbit.Tests/ \| grep -i serial` returned 0 hits |
| P7 | File path / structure | `test/RawRabbit.Tests/RawRabbit.Tests.csproj` is SDK-style (`<Project Sdk="Microsoft.NET.Sdk">`), so a new `.cs` file under any subdirectory is auto-picked-up by compilation/test discovery — no `<Compile Include>` edit required | `head -5` confirmed `<Project Sdk="Microsoft.NET.Sdk">` |
| P8 / P9 | Function signature | STJ `JsonSerializer.Serialize(object value, Type inputType, JsonSerializerOptions options)` and `JsonSerializer.Deserialize(string json, Type returnType, JsonSerializerOptions options)` exist on net10 | Inherited from spec A13/A14 (probe at `/tmp/stj-probe` compiled clean using both) |
| P10 | Function signature | `RawRabbit.Serialization.StringSerializerBase` is the abstract base class with abstract `string SerializeToString(object obj)` and `object Deserialize(Type type, string serialized)` — preserved by the new `JsonSerializer` impl | `cat src/RawRabbit/Serialization/StringSerializerBase.cs` confirmed both abstract members and the `byte[] Serialize(object obj)` / `object Deserialize(Type, byte[])` / `TType Deserialize<TType>(byte[])` concrete plumbing |
| P11 | Function signature | xunit 2.9.3 exposes the test-asserting APIs Task 2 uses: `Assert.Throws<T>(Action)`, `Assert.IsType<T>(object)`, `Assert.Equal<T>(T, T)`, `Assert.Null(object)`, `Assert.Empty(string)`, `Assert.Contains(string, string)` | Inherited from Phase 1 spec A11/A12 (xunit 2.9.3 verified on the wire); all listed APIs are stable since xunit 2.0 |
| P12 / P13 / P14 / P15 | Command | `dotnet build -c Release`, `dotnet test ... --no-build -c Release [--blame-hang-timeout 15s]`, `dotnet pack -c Release --no-build` work as written from the repo root | Phase 3 plan (`docs/plans/2026-05-12-modernization-phase-3-implementation-plan.md`) used these exact invocations and Phase 3 acceptance succeeded; SDK is bundled with these subcommands |
| P16 | Command | `dotnet --version` returns `10.0.107` (driven by `global.json`) | `dotnet --version` returned `10.0.107` |
| P17 | Task ordering | Task 2 must follow Task 1 — Task 2's tests instantiate `new JsonSerializer(JsonSerializerOptions)`, a constructor that only exists after Task 1's whole-file replacement of `JsonSerializer.cs` (the old `JsonSerializer(Newtonsoft.Json.JsonSerializer)` is gone). Adding Task 2's file before Task 1's source change would fail to compile | Logical analysis against the new file's constructor signature in spec §3 vs the current file's signature read at plan-write time |
| P18 | Task ordering | Within Task 1, the 5 file edits commit atomically with no inter-edit ordering — they're forced together by transitive build resolution per spec §3 atomicity / D7 | Spec §3 atomicity note + the (Polly transitive) finding |
| P19 | Code-in-plan | The new `JsonSerializer.cs` body in spec §3 compiles on net10/C# 13 with no `<LangVersion>` override | Inherited from Phase 3 P4 (no `<LangVersion>` set; default is C# 13) |
| P20 | Code-in-plan | `RawRabbit.Serialization.JsonSerializer` shadows `System.Text.Json.JsonSerializer` — the new file body must use the fully-qualified `System.Text.Json.JsonSerializer.Serialize(...)` to disambiguate (spec §3 illustrative code does this) | Spec §3 code block uses `System.Text.Json.JsonSerializer.Serialize(obj, obj.GetType(), _options);` and `System.Text.Json.JsonSerializer.Deserialize(str, type, _options);` — explicit qualification preserved |
| P21 | Code-in-plan | The DI registration block edit needs both `using System.Text.Json;` (for `JsonSerializerOptions`, `JsonNamingPolicy`) AND `using System.Text.Json.Serialization;` (for `JsonIgnoreCondition`) — the spec §3 illustrates the literal but does not enumerate the usings | The probe at `/tmp/stj-probe/Program.cs` used both `using System.Text.Json;` and `using System.Text.Json.Serialization;` to compile the same property assignments cleanly |
| P22 | Code-in-plan | `Newtonsoft.Json.Serialization`'s `DefaultContractResolver` and `CamelCaseNamingStrategy` are referenced inside the 49–62 block (line 54), confirming both `using Newtonsoft.Json…` lines (2 + 3) are removable cleanly after the block rewrite | `grep -nE "DefaultContractResolver\|CamelCaseNamingStrategy"` returned line 54 only |
| P23 / P25 | Consumer impact | The only caller of `new (Serialization.\|RawRabbit.Serialization.)?JsonSerializer\s*\(` across `src/`, `test/`, `sample/` is the DI registration line being rewritten (`RawRabbitDependencyRegisterExtension.cs:49`); no external consumer breaks when the constructor signature changes from `Newtonsoft.Json.JsonSerializer` to `JsonSerializerOptions` | `grep -rEn "new\s+(Serialization\.\|RawRabbit\.Serialization\.)?JsonSerializer\s*\("` returned exactly one hit, and that hit is the line Task 1 rewrites |
| P24 | Consumer impact | No `.cs` file in `src/` references `Newtonsoft.Json.Linq` other than `RetryKey.cs:5` — when Task 1 removes the `Newtonsoft.Json` package, the namespace transitively disappears for consumers, but no other consumer code breaks | `grep -rln "Newtonsoft\.Json\.Linq" src/ --include="*.cs"` returned only `RetryKey.cs` |
| P26 | Convention | Commit message format = short imperative single-line subject, no Conventional-Commits prefix | `git log --oneline -10` shows uniform style across all recent commits (Phase 3 + Phase 4 spec) |
| P27 | Baseline | Pre-Phase-4 test baseline: `RawRabbit.Tests` 29 passed / 7 skipped / 0 failed; `Polly.Tests` 1 passed / 2 skipped / 0 failed (totals 30/9/0) | `dotnet test` of both projects at HEAD `e61d51e` confirmed exactly these counts |

---

## Tasks

### Task 1: Replace Newtonsoft.Json with System.Text.Json (atomic)

This task is **atomic by build necessity**: removing `Newtonsoft.Json` from `RawRabbit.csproj` makes `Newtonsoft.Json.Linq` transitively disappear for the Polly enricher (verified P24); leaving the dead `using` in place breaks the build. Replacing `JsonSerializer.cs` without rewriting the DI registration breaks compilation (the constructor signature changes). Removing the central `<PackageVersion>` without removing the `<PackageReference>` fails NU1010. All 5 file edits commit together.

**Files:**
- Modify: `Directory.Packages.props` (delete `Newtonsoft.Json` `<PackageVersion>` line)
- Modify: `src/RawRabbit/RawRabbit.csproj` (delete `Newtonsoft.Json` `<PackageReference>` line)
- Modify: `src/RawRabbit/Serialization/JsonSerializer.cs` (whole-file replacement with STJ implementation)
- Modify: `src/RawRabbit/DependencyInjection/RawRabbitDependencyRegisterExtension.cs` (drop 2 Newtonsoft `using`s, add 2 STJ `using`s, replace 11-setting block at lines 49–62)
- Modify: `src/RawRabbit.Enrichers.Polly/RetryKey.cs` (delete dead `using Newtonsoft.Json.Linq;` at line 5)

- [ ] **Step 1: Remove the central `Newtonsoft.Json` `<PackageVersion>` from `Directory.Packages.props`.** Delete the single line:
  ```xml
  <PackageVersion Include="Newtonsoft.Json" Version="10.0.1" />
  ```
  Do NOT change any other entry. The `<!-- Library deps (Phases 4/5/7 will bump majors) -->` comment can be left unchanged — adjacent `RabbitMQ.Client`/`Polly`/etc. lines remain.

- [ ] **Step 2: Remove the `Newtonsoft.Json` `<PackageReference>` from `src/RawRabbit/RawRabbit.csproj`.** Delete the single line:
  ```xml
  <PackageReference Include="Newtonsoft.Json" />
  ```
  Preserve the surrounding `RabbitMQ.Client` and `Microsoft.Extensions.Logging.Abstractions` references and all `<PropertyGroup>` content untouched.

- [ ] **Step 3: Replace `src/RawRabbit/Serialization/JsonSerializer.cs` with the STJ implementation.** Full file content:
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
  Key correctness pin (from spec §3): `Serialize(obj, obj.GetType(), _options)` uses the runtime-type overload — the generic `Serialize<T>(value, options)` would use declared type `T` and emit `{}` for an `object` reference. The fully-qualified `System.Text.Json.JsonSerializer.Xxx(...)` is required because the file's own class is also named `JsonSerializer` (see P20).

- [ ] **Step 4: Edit `src/RawRabbit/DependencyInjection/RawRabbitDependencyRegisterExtension.cs`.**
  - At the top of the file, delete the 2 Newtonsoft `using` directives (lines 2 and 3):
    ```csharp
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    ```
    and ADD 2 STJ `using` directives in the same alphabetical position:
    ```csharp
    using System.Text.Json;
    using System.Text.Json.Serialization;
    ```
    (Both are required: `JsonSerializerOptions` and `JsonNamingPolicy` are in `System.Text.Json`; `JsonIgnoreCondition` is in `System.Text.Json.Serialization`. See P21.)
  - Replace the 11-setting block at lines 49–62 with this 4-property literal:
    ```csharp
    .AddSingleton<ISerializer>(resolver => new Serialization.JsonSerializer(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    }))
    ```
    Do NOT touch the surrounding `.AddSingleton<...>` chain.

- [ ] **Step 5: Edit `src/RawRabbit.Enrichers.Polly/RetryKey.cs`.** Delete line 5 entirely:
  ```csharp
  using Newtonsoft.Json.Linq;
  ```
  Do NOT touch any other line. The remaining 4 `using` directives stay; the rest of the file is unchanged.

- [ ] **Step 6: Verify the build is green**
  ```bash
  dotnet build -c Release
  ```
  Expect: 0 errors. The 3× `NU1903` warnings against `Newtonsoft.Json 10.0.1` (with link to GHSA-5crp-9r3c-p9vr) are GONE. Other warnings (NU1701/NU1902 from RabbitMQ.Client/Polly/etc., NU5048 etc.) stay at the Phase 3 baseline.

- [ ] **Step 7: Verify tests stay at the pre-Phase-4 baseline**
  ```bash
  dotnet test test/RawRabbit.Tests --no-build -c Release --blame-hang-timeout 15s
  dotnet test test/RawRabbit.Enrichers.Polly.Tests --no-build -c Release
  ```
  Expect: 29 passed / 7 skipped / 0 failed (RawRabbit.Tests); 1 passed / 2 skipped / 0 failed (Polly.Tests). Total 30/9/0 — unchanged from baseline because no test exercises the JSON path yet (Task 2 adds those).

- [ ] **Step 8: Commit (atomic — all 5 file changes together)**
  ```bash
  git add Directory.Packages.props \
          src/RawRabbit/RawRabbit.csproj \
          src/RawRabbit/Serialization/JsonSerializer.cs \
          src/RawRabbit/DependencyInjection/RawRabbitDependencyRegisterExtension.cs \
          src/RawRabbit.Enrichers.Polly/RetryKey.cs
  git commit -m "Replace Newtonsoft.Json with System.Text.Json"
  ```

### Task 2: Add JsonSerializer round-trip tests

**Files:**
- Create: `test/RawRabbit.Tests/Serialization/JsonSerializerTests.cs` (new file; Task 1 must precede this — see P17)

- [ ] **Step 1: Create the new test file.** Path: `test/RawRabbit.Tests/Serialization/JsonSerializerTests.cs`. The 10 `[Fact]` methods correspond 1:1 to spec §3.1's enumerated tests. File-local helper types (`SimplePoco`, `WithDefaults`, `BasePoco`/`DerivedPoco`, `WithRefs`, `WithCycle`) are declared at the bottom of the file, all `internal` to keep the test surface tight.

  Test method names use the existing repo `Should_Verb_Subject` convention (PascalCase with underscores). Test bodies use `Assert.Throws<T>(...)`, `Assert.IsType<T>(obj)` (generic form), `Assert.Equal`, `Assert.Null`, `Assert.Empty`, `Assert.Contains`, and `Assert.DoesNotContain` — and explicitly avoid the `Assert.True(true, "msg")` anti-pattern, hand-rolled try/catch around exceptions, the non-generic `Assert.IsType(typeof(X), obj)` form, race-prone `Wait(timespan)` timing, and the `/* Setup */ /* Test */ /* Assert */` block comments (per spec A25).

  All 10 tests instantiate a fresh `JsonSerializer` with the SAME `JsonSerializerOptions` literal Task 1 placed in the DI registration. A single helper `CreateSerializer()` returns the configured instance.

  Full file content:
  ```csharp
  using System;
  using System.Collections.Generic;
  using System.Text;
  using System.Text.Json;
  using System.Text.Json.Serialization;
  using Xunit;
  using RawRabbitJsonSerializer = RawRabbit.Serialization.JsonSerializer;

  namespace RawRabbit.Tests.Serialization
  {
      public class JsonSerializerTests
      {
          private static RawRabbitJsonSerializer CreateSerializer()
              => new RawRabbitJsonSerializer(new JsonSerializerOptions
              {
                  PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                  DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                  PropertyNameCaseInsensitive = true,
                  WriteIndented = false,
              });

          [Fact]
          public void Should_RoundTrip_Simple_Poco()
          {
              var serializer = CreateSerializer();
              var original = new SimplePoco { FirstName = "Ada", LastName = "Lovelace", Age = 36 };

              var bytes = serializer.Serialize(original);
              var roundTripped = (SimplePoco)serializer.Deserialize(typeof(SimplePoco), bytes);

              Assert.Equal(original.FirstName, roundTripped.FirstName);
              Assert.Equal(original.LastName, roundTripped.LastName);
              Assert.Equal(original.Age, roundTripped.Age);
          }

          [Fact]
          public void Should_Return_Empty_String_For_Null_Object()
          {
              var serializer = CreateSerializer();

              var bytes = serializer.Serialize(null);

              Assert.Empty(bytes);
          }

          [Fact]
          public void Should_Return_Null_For_Empty_String_Bytes()
          {
              var serializer = CreateSerializer();
              var emptyBytes = Encoding.UTF8.GetBytes(string.Empty);

              var result = serializer.Deserialize(typeof(SimplePoco), emptyBytes);

              Assert.Null(result);
          }

          [Fact]
          public void Should_Emit_CamelCase_Property_Names()
          {
              var serializer = CreateSerializer();
              var poco = new SimplePoco { FirstName = "Ada", LastName = "Lovelace", Age = 36 };

              var json = Encoding.UTF8.GetString(serializer.Serialize(poco));

              Assert.Contains("\"firstName\"", json);
              Assert.Contains("\"lastName\"", json);
              Assert.Contains("\"age\"", json);
              Assert.DoesNotContain("\"FirstName\"", json);
          }

          [Fact]
          public void Should_Deserialize_Pascal_Case_Json_Into_Pascal_Case_Properties()
          {
              var serializer = CreateSerializer();
              var pascalCaseJson = "{\"FirstName\":\"Ada\",\"LastName\":\"Lovelace\",\"Age\":36}";
              var bytes = Encoding.UTF8.GetBytes(pascalCaseJson);

              var result = (SimplePoco)serializer.Deserialize(typeof(SimplePoco), bytes);

              Assert.Equal("Ada", result.FirstName);
              Assert.Equal("Lovelace", result.LastName);
              Assert.Equal(36, result.Age);
          }

          [Fact]
          public void Should_Drop_Null_Properties_But_Keep_Default_Primitives()
          {
              var serializer = CreateSerializer();
              var poco = new WithDefaults { Name = null, Count = 0, Flag = false };

              var json = Encoding.UTF8.GetString(serializer.Serialize(poco));

              Assert.DoesNotContain("\"name\"", json);
              Assert.Contains("\"count\":0", json);
              Assert.Contains("\"flag\":false", json);
          }

          [Fact]
          public void Should_Use_Runtime_Type_When_Serializing_Object_Reference()
          {
              var serializer = CreateSerializer();
              object reference = new DerivedPoco { BaseProp = "base", DerivedProp = "derived" };

              var json = Encoding.UTF8.GetString(serializer.Serialize(reference));

              Assert.Contains("\"baseProp\":\"base\"", json);
              Assert.Contains("\"derivedProp\":\"derived\"", json);
          }

          [Fact]
          public void Should_Emit_Repeated_References_As_Two_Copies()
          {
              var serializer = CreateSerializer();
              var shared = new SimplePoco { FirstName = "Shared", LastName = "Instance", Age = 1 };
              var withRefs = new WithRefs { First = shared, Second = shared };

              var json = Encoding.UTF8.GetString(serializer.Serialize(withRefs));

              Assert.DoesNotContain("$id", json);
              Assert.DoesNotContain("$ref", json);
              // Both fields contain a full serialized copy
              var sharedFragment = "\"firstName\":\"Shared\"";
              Assert.Equal(2, CountOccurrences(json, sharedFragment));
          }

          [Fact]
          public void Should_Throw_On_Cyclic_Object_Graph()
          {
              var serializer = CreateSerializer();
              var a = new WithCycle { Name = "A" };
              var b = new WithCycle { Name = "B" };
              a.Other = b;
              b.Other = a;

              Assert.Throws<JsonException>(() => serializer.Serialize(a));
          }

          [Fact]
          public void Should_Have_ApplicationJson_ContentType()
          {
              var serializer = CreateSerializer();

              Assert.Equal("application/json", serializer.ContentType);
          }

          private static int CountOccurrences(string haystack, string needle)
          {
              var count = 0;
              var index = 0;
              while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) != -1)
              {
                  count++;
                  index += needle.Length;
              }
              return count;
          }
      }

      internal class SimplePoco
      {
          public string FirstName { get; set; }
          public string LastName { get; set; }
          public int Age { get; set; }
      }

      internal class WithDefaults
      {
          public string Name { get; set; }
          public int Count { get; set; }
          public bool Flag { get; set; }
      }

      internal class BasePoco
      {
          public string BaseProp { get; set; }
      }

      internal class DerivedPoco : BasePoco
      {
          public string DerivedProp { get; set; }
      }

      internal class WithRefs
      {
          public SimplePoco First { get; set; }
          public SimplePoco Second { get; set; }
      }

      internal class WithCycle
      {
          public string Name { get; set; }
          public WithCycle Other { get; set; }
      }
  }
  ```

- [ ] **Step 2: Build (must precede `--no-build` test invocation, since the new file needs compiling)**
  ```bash
  dotnet build -c Release
  ```
  Expect: 0 errors. Warning shape stable from Task 1.

- [ ] **Step 3: Verify the new tests pass and the total moves to 39 passing**
  ```bash
  dotnet test test/RawRabbit.Tests --no-build -c Release --blame-hang-timeout 15s
  ```
  Expect: **39 passed / 7 skipped / 0 failed** (the previous 29 + the 10 new `JsonSerializerTests`). Each of the 10 must pass individually — they're the verification gate for Task 1's STJ migration correctness (per spec §7 Risk #1). If any fail, the failure points at a specific behavior difference between the spec's claim and the implementation.

- [ ] **Step 4: Commit**
  ```bash
  git add test/RawRabbit.Tests/Serialization/JsonSerializerTests.cs
  git commit -m "Add JsonSerializer round-trip tests for STJ migration"
  ```

### Task 3: Verify acceptance per spec §9

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
  Expect: 0 errors. NU1903 hits against `Newtonsoft.Json` are gone (was 3× before Task 1). Other warning shape stable.

- [ ] **Step 3: Build**
  ```bash
  dotnet build -c Release
  ```
  Expect: 0 errors. Warning count drops by ~3 vs Phase 3 baseline (the Newtonsoft NU1903 hits removed).

- [ ] **Step 4: Tests at the new total**
  ```bash
  dotnet test test/RawRabbit.Tests --no-build -c Release --blame-hang-timeout 15s
  dotnet test test/RawRabbit.Enrichers.Polly.Tests --no-build -c Release
  ```
  Expect: 39 passed / 7 skipped / 0 failed (RawRabbit.Tests); 1 passed / 2 skipped / 0 failed (Polly.Tests). **Total 40/9/0.**

- [ ] **Step 5: Pack works**
  ```bash
  dotnet pack -c Release --no-build
  ```
  Expect: 0 errors. Pack-time NU5048 (deprecated `PackageIconUrl`) warnings expected per Phase 2 §2.3 — these are acceptable.
  ```bash
  find . -path "*/bin/Release/*.nupkg" -not -path "*/AspNet.Sample/*" -not -path "*/PerformanceTest/*" | wc -l
  ```
  Expect: `25`.

- [ ] **Step 6: Newtonsoft is gone everywhere it was supposed to be gone**
  ```bash
  grep -rn "Newtonsoft" src/ test/ sample/ Directory.Packages.props --include="*.cs" --include="*.csproj"
  ```
  Expect: 0 hits. Phase 4 acceptance criterion: zero `Newtonsoft` token in `src/`, `test/`, `sample/`, or `Directory.Packages.props`.

- [ ] **Step 7: If all six steps pass, Phase 4 is complete.** No commit for this task — pure verification. If any step fails, consult spec §7 (Risks & responses) for the recovery template.

---

## Tasks NOT in this plan

Inherited verbatim from spec §1 ("Out of scope for 'phase 4 acceptance'") and the additional Phase 4.5 deferral:

- `test/RawRabbit.IntegrationTests` — requires a live broker (Phase 6).
- `RawRabbit.Enrichers.HttpContext` no-op state on net10 (Phase 1 V1 known issue).
- `RawRabbit.Enrichers.Protobuf` and `RawRabbit.Enrichers.MessagePack` — alternative `ISerializer` implementations, untouched by this phase. Their fate is Phase 7.
- **Comprehensive test coverage and quality across the framework.** A test audit at Phase 4 spec-write time confirmed only 2 source areas (`Channel.*` and `Common.*` in `src/RawRabbit/`) have any unit-test coverage at all — a 5:1 source-to-test ratio. `Pipe/Middleware/*`, `DependencyInjection/*`, all 8 `Operations.*` projects, and 9 of 10 enrichers have zero tests. Existing tests also have several quality issues (`Assert.True(true)` anti-pattern, hand-rolled try/catch instead of `Assert.Throws<T>`, race-prone timing assertions, missing `[Theory]/[InlineData]` consolidation). The new Phase 4.5 will spec a comprehensive, high-quality test suite. Phase 4 itself adds only the serializer-specific round-trip tests required to verify its own change.

A new spec → new plan cycle is required to add any of these.

## Known issues inherited from spec

These are behavior changes Phase 4 introduces that consumers of this fork will see. User-acknowledged on 2026-05-12 during brainstorming.

1. **No `$type` on the wire.** Consumers that relied on Newtonsoft's `TypeNameHandling.Auto` for embedded polymorphic fields must add `[JsonDerivedType(typeof(Derived), "discriminator")]` attributes to their base message types, OR use concrete fields. RawRabbit's *top-level* message type is unaffected — it travels via the `message_type` AMQP header, not via `$type` on the JSON body.
2. **No `$id`/`$ref` on the wire.** Reference identity is not preserved; same object referenced twice becomes two copies. Cyclic message graphs throw on serialize (`JsonException`).
3. **Default-value primitives appear on the wire.** Newtonsoft's `DefaultValueHandling.Ignore` was omitting members whose value equaled the type default (`0` for `int`, `false` for `bool`, `""` for `string`). STJ `WhenWritingNull` keeps these — `{"name": "x", "count": 0}` instead of Newtonsoft's `{"name": "x"}`. To restore Newtonsoft's omission, switch the option to `WhenWritingDefault`; the spec picks `WhenWritingNull` to keep semantically-meaningful zero/false/empty values explicit on the wire.
4. **STJ's narrower `ObjectCreationHandling`.** Existing collection properties on a target object are not reused during deserialization; STJ creates a new collection each time.
5. **Camel-case naming.** STJ camelCase is mechanically the same as Newtonsoft `CamelCaseNamingStrategy` for ASCII property names. No known difference for the property names used by RawRabbit's own types; consumer types with non-ASCII property names may differ at the edges.
6. **Carries forward from prior phases:** `RawRabbit.Enrichers.HttpContext` still has no functional implementation on net10 (Phase 1 V1); `test/RawRabbit.IntegrationTests` still requires a live broker (Phase 6); the heterogeneous metadata acceptances from Phase 2 (`Authors` values, `<VersionPrefix>` on `Compatibility.Legacy`, `<GenerateAssembly*>` flags on MessagePack/Protobuf/ZeroFormatter) remain as documented.
