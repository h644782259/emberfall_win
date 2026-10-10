# Portable source validation

Requires Python 3.9+, Bash, and the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
The first reference download also requires curl.

```sh
bash Tests/Run-CloudValidation.sh
bash Tests/Run-CloudValidation.sh --download-references
# An SDK installed outside PATH:
bash Tests/Run-CloudValidation.sh --dotnet /path/to/dotnet --compile
# Compile Android conditional runtime code (no SDK, Gradle, APK or device involved):
bash Tests/Run-CloudValidation.sh --compile --compile-android
# Exact installed Unity 6.6 APIs, including the project's Editor tools:
bash Tests/Run-CloudValidation.sh --unity-editor /path/to/Editor/Unity
```

The first command runs the registered standalone suites against actual production source,
in separate temporary projects. Saves and
generated build files are isolated and removed afterward. Logs and a JSON report
remain under the ignored fixed `Tests/TestResults/Cloud-Latest` directory.

`--download-references` also compiles every `Assets/Scripts/**/*.cs` file with C# 9
against the same pinned `UnityEngine.Modules` 2021.3.33 package used by
`Tests/Run-CompileCheck.ps1`. The package SHA-512 is checked before extraction.
`--compile` reuses reference DLLs already present in `Tools/ReferenceAssemblies`.
Generated projects restore without external package feeds.

When present, `UpgradeProgressionTests.cs`, `BossAttackPolicyTests.cs`,
`PlayerUpgradeTests.cs`, `EncounterPlanTests.cs`, `RunChoicesTests.cs`, and
`ApplicationPauseStateTests.cs`, `CombatBalanceTests.cs`, `CompanionRulesTests.cs`, and
`RebalanceProgressionTests.cs` are run separately against their production logic.
The progression suites cover permanent slot reinforcement, highest-rank legacy
migration, automatic equip/preview equality, failed-write rollback, and repeated
swap/sale/save/load stability. UI and real Unity JsonUtility acceptance remain
separate engine checks.
The JSON report records source SHA-256 hashes and refuses an overall pass if
source files changed during the run; rerun after concurrent edits finish.
`--unity-editor` additionally compiles Windows, iOS and Android runtime branches, Editor
tools, and the separate visual-validation player source against the installed
Unity 6000.6 managed assemblies, with the relevant Unity 6.6 defines and .NET
Standard 2.1 references. It does not launch the Editor or activate a license.

These checks use .NET 8 and the standalone tests' existing Unity shims. They do
not validate Unity's real JsonUtility, Editor-driven compilation/import, physics,
rendering, shaders, GUI, audio, or Windows/iOS/Android builds. Run the repository's real
Unity validation and platform builds separately before release.


0.4.0 adds isolated save/delete/load/exit, mobile layout and gesture, progression reminders, town economy, trial/room/large-boss rules, destructible navigation, recap and procedural/filled-volume geometry checks. The report lists each executed check and rejects source changes during a run. Optional compilation configurations cover legacy API references, an explicit Android branch, and installed Unity runtime/editor branches. Python source contracts remain separate from these managed tests. Fixed `Cloud-Latest` logs replace only the previous generated latest report; historical reports are preserved.

`--compile-android` adds a separately named `android-runtime-compile` check with `UNITY_ANDROID`. It verifies conditional C# code only. It cannot validate a manifest, player import, Android SDK/NDK/JDK compatibility, signing, APK installation, Back gestures or device lifecycle. Use the Android preflight/build/device instructions for those distinct stages.

Focused reward regression: pass `--only legendary-equipment-pity`. This executes the current single-item chest pool, 4% legendary probability, persistent pity, and failed-save/reload checks. `--only` accepts repeated options or comma-separated names; a requested check that did not execute fails the report.

When changing gameplay or reward rules, update the corresponding `Assets/Editor/*Validation.cs` build checks and `Tests/` assertions in the same change. Runtime compilation alone does not execute either set of assertions.
