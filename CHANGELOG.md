# Changelog — gregMod.MultiCable

Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/). Version: see [`VERSION`](VERSION).

## [Unreleased]

## [0.1.0] — 2026-09-24

### Added

- Initial release: true multi-carry for cable pulls.
- Configurable parallel count 1–4 (default 2) via gregCore F1 Mod Config UI
  (`gregMod.MultiCable` -> `Parallel cables`) with MelonPreferences fallback.
- Live mirror ghosts following the carried cable on the same path.
- Automatic sibling creation on completion onto free same-device ports,
  replayed through vanilla methods (`ReserveCableId` / `AssignNewPosition` /
  `GenerateFinalPath` + route re-evaluation).
- Standalone F8 panel (status, count, toggles, stop-tracking), gregCore HUD +
  F1 hub opener registration.
- Observational Harmony seams only (`CableLink.InteractOnClick`,
  `CablePositions.CreateNewCable` / `DiscardCable`,
  `NetworkMap.RegisterCableConnection`); no vanilla behaviour suppressed.
- Recursion guard, per-sibling error isolation with `DiscardCable` rollback,
  partial-success reporting.
- Docs: `docs/USAGE.md`, `docs/ARCHITECTURE.md`, `docs/COMPATIBILITY.md`.
