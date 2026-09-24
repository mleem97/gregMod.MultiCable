# Source layout

All C# source lives in `src/`, current game/MelonLoader assemblies in `references/`
(symlinks into the local Data Center install — never commit DLLs), and project
documentation in `docs/`.

| File | Why it exists |
|---|---|
| `src/MultiCableMod.cs` | MelonMod entry point (startup, scenes, hotkey) |
| `src/MyPluginInfo.cs` | Plugin id/name/version constants |
| `src/GregHost.cs` | gregCore soft-dependency probe |
| `src/MultiCableConfig.cs` | Effective config (F1 wins, prefs fallback) |
| `src/MultiCableCoreConfig.cs` | Only file referencing gregCore (JIT split) |
| `src/CarryState.cs` | Tracked pull, connection record, clone queue |
| `src/Patches.cs` | Observational Harmony seams |
| `src/SiblingFinder.cs` | Free same-device port search |
| `src/Cloner.cs` | Delayed sibling creation via vanilla methods |
| `src/MirrorGhostManager.cs` | Mirror ghosts, clone pump, F8 panel |
