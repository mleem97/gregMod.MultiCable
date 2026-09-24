# AGENTS.md — Notes for AI agents (gregMod.MultiCable)

Repo: [https://github.com/mleem97/gregMod.MultiCable](https://github.com/mleem97/gregMod.MultiCable) · License: Apache-2.0 · Version: see `VERSION`.

## Duties

1. **Read first:** `README.md`, `docs/INDEX.md`, `CONTRIBUTING.md` — only then make changes.
2. **Do not commit secrets** (keys, tokens, `.env`). Use keys only via environment variables.
3. **Preserve history:** no `push --force`, no history rewrite without instruction.
4. **Verify changes:** before reporting completion, build/test whatever the repo supports (`QUICKSTART.md`).
5. **Keep docs in sync:** for new features, update `README.md` + `docs/` + `CHANGELOG.md` (Unreleased).
6. **Conventions:** Conventional Commits (`feat:`, `fix:`, `docs:`, `chore:` …), one logical change per commit.
7. **When in doubt:** stop and ask instead of guessing — especially for deletes, migrations, CI.

## Mod-specific rules

- **Never suppress vanilla cable behaviour.** All Harmony patches are
  observational; the only additive step is `Cloner.Execute`, which runs delayed
  (clone pump in `MirrorGhostManager.Update`) and guarded by
  `Cloner.IsCloning` against recursion.
- **gregCore is a soft dependency.** Direct gregCore references live only in
  `src/MultiCableCoreConfig.cs`, called exclusively behind `GregHost.HasCore`
  (JIT split). The mod must load and work without gregCore (F8 panel +
  MelonPreferences).
- **0Harmony.dll ships v1 (`Harmony`) and v2 (`HarmonyLib`) APIs.** Always use
  fully qualified `HarmonyLib.Harmony` / `HarmonyLib.HarmonyMethod`.
- **No Il2Cpp-crossing delegates.** The F8 panel uses `GUILayout.BeginArea`,
  not `GUILayout.Window` (its `GUI.WindowFunction` delegate is unreliable
  under IL2CPP interop).
- **Reverse-engineering evidence** for game internals belongs in
  `docs/COMPATIBILITY.md` (types, methods, assembly dates). The
  `Assembly-CSharp` interop dummies contain no IL — call graphs beyond
  signatures must be confirmed at runtime, never asserted from statics.

## Layout

See [README.md](README.md) → Repository Layout. Central entry points: `docs/INDEX.md`, `scripts/`, `tests/`.
Source: `src/` (`MultiCableMod`, `MultiCableConfig`, `MultiCableCoreConfig`,
`CarryState`, `Patches`, `SiblingFinder`, `Cloner`, `MirrorGhostManager`).
