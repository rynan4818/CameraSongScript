# CameraSongScript - AI Coding Instructions

## Project Overview

Beat Saber mod (BSIPA plugin) that adds SongScript camera animation to the **Camera2** mod via reflection, with legacy **CameraPlus** support. The plugin reads CameraPlus-format `SongScript.json` files from song folders and drives Camera2's OverrideToken API to animate camera position, rotation, and FOV per-frame during gameplay.

## Architecture

```
Plugin.cs              Entry point (IPA lifecycle: Init → OnStart → OnExit)
├── CameraModDetector  Detects Camera2 vs CameraPlus at runtime
├── Helpers/
│   ├── Camera2ReflectionHelper   ALL Camera2 SDK calls via System.Reflection (no compile-time dep)
│   └── CameraPlusHarmonyHelper   CameraPlus Harmony-based integration
├── HarmonyPatches/
│   └── CameraSongScriptDetector        Patches CustomPreviewBeatmapLevel to detect scripts on song select
├── Installers/                   Zenject installers per scene (App/Menu/Player)
├── Models/
│   ├── MovementScriptJson        JSON deserialization POCOs (CameraPlus format)
│   └── CameraSongScriptData            Parses JSON → CameraSongScriptMovement list with validation
├── Configuration/
│   └── PluginConfig              BSIPA auto-persisted config (virtual properties pattern)
├── UI/
│   └── CameraSongScriptSettingsView    BSML GameplaySetup tab (.cs + .bsml pair)
└── CameraSongScriptController          Main runtime: IInitializable, ITickable, IDisposable
```

## Key Patterns

### Zenject DI with SiraUtil
- Scene-scoped installers: `Location.App`, `Location.Menu`, `Location.Player` in `Plugin.Init()`
- `CameraSongScriptController` binds with `BindInterfacesAndSelfTo<T>().AsSingle().NonLazy()` (Player scene only, Camera2 mode only)
- Use `[Inject]` for Beat Saber services; use `[Inject(Optional = true)]` for services that may not exist (e.g., `PauseController`)

### Reflection-Only Camera2 Integration
- `Camera2ReflectionHelper` wraps every Camera2 API call through `System.Reflection` — there is **no compile-time reference** to Camera2.dll. This allows the plugin to load even when Camera2 is not installed.
- Token-based camera control: acquire `OverrideToken` per camera → set position/rotation/FOV via reflection → close token on dispose.

### Harmony Patches
- Harmony ID: `"com.github.camerasongscript"` — patches applied in `OnApplicationStart()`, removed in `OnApplicationQuit()`
- `CameraSongScriptDetector` uses a Postfix on `CustomPreviewBeatmapLevel.GetCoverImageAsync` to scan song folders for valid JSON scripts

### BSIPA Configuration
- Config class (`CameraSongScriptConfig`) uses **virtual properties** — BSIPA generates a derived store class at runtime via `conf.Generated<CameraSongScriptConfig>()`
- Properties: `Enabled`, `UseAudioSync`, `TargetCameras` (comma-separated), `SelectedScriptFile`

### BSML UI
- Settings views consist of paired `.cs` / `.bsml` files in `UI/`; the `.bsml` is an embedded resource
- Registered as a GameplaySetup tab via `GameplaySetup.Instance.AddTab()`

## Code Conventions

- **Language**: C# 7.3 on .NET Framework 4.7.2
- **Comments**: Japanese (日本語) for inline comments and XML doc summaries
- **Private fields**: `_camelCase` with underscore prefix
- **Logging**: `Plugin.Log.Info/Warn/Error()` (static accessor to IPA Logger)
- **Locale-safe float parsing**: `CameraSongScriptData.ParseFloat()` handles both `.` and `,` decimal separators
- **Angle interpolation**: `Mathf.LerpAngle` for rotation (handles 360 wrapping); `FindShortestDelta` adjusts start angles
- **Easing**: Cubic ease-in-out in `CameraSongScriptController.Ease()` (4p^3 / 0.5((2p-2)^3+1))

## Build

- Solution: `CameraSongScript.slnx`
- Build: `msbuild CameraSongScript.sln` or via Visual Studio
- Requires Beat Saber managed DLLs at path defined by `$(BeatSaberDir)` (set via `Refs/` directory or `Directory.Build.props`)
- NuGet packages: `BeatSaberModdingTools.Tasks`, `BepInEx.AssemblyPublicizer.MSBuild`
- Output: `bin/Debug/CameraSongScript.dll` → copy to `Beat Saber\Plugins\`
- No automated test suite — manual testing in-game

## Reference Repositories

The workspace includes reference codebases (read-only context, not part of this project's build):
- `CameraPlus/` — Legacy camera mod with native SongScript; `MovementScriptJson` format originates here
- `CS_BeatSaber_Camera2/` — Camera2 mod source; the OverrideToken SDK that `Camera2ReflectionHelper` wraps
- `StagePositionViewer/` — Utility mod for player position visualization

## Data Flow Summary

1. **Song select** → Harmony postfix scans song folder for `*.json` matching CameraPlus MovementScript schema
2. **Gameplay start** → `CameraSongScriptController.Initialize()` loads JSON, parses to `CameraSongScriptMovement[]`, acquires Camera2 OverrideTokens
3. **Per-frame Tick** → reads `AudioTimeSyncController.songTime`, calculates lerp percentage with cubic easing, applies position/rotation/FOV to all tokens via reflection
4. **Dispose** → releases all tokens back to Camera2
