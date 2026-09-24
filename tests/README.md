# tests — gregMod.MultiCable

Tests, fixtures, and test documentation.

Back: [README.md](../README.md) · Docs: [docs/INDEX.md](../docs/INDEX.md).

## What can be tested without the game

- `dotnet build gregMod.MultiCable.csproj -c Release` must stay at
  **0 warnings, 0 errors** (interop-signature drift shows up here first).

## In-game checklist (see docs/COMPATIBILITY.md)

1. Mod loads; F8 panel opens; no console errors.
2. x2 pull switch→switch: mirrors follow; sibling lands on free ports.
3. x3/x4 pulls; partial free-port reporting; count=1 passthrough.
4. Discard mid-carry clears tracking; failures never touch the origin cable.
5. Save/load keeps siblings; co-op replicates; vanilla LACP grouping works.
6. With gregCore (F1 entries own the settings) and without (panel edits prefs).
