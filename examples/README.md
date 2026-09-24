# examples — gregMod.MultiCable

Examples and further material.

Back: [README.md](../README.md) · Docs: [docs/INDEX.md](../docs/INDEX.md).

## Redundant uplink recipe (the motivating use case)

Two switches across the hall, LACP-style backup over one walked route:

1. Set **Parallel cables = 2** (F1 Mod Config or F8 panel).
2. Click a free port on switch A, walk the tray/ladder route once to switch B,
   click a free port there.
3. The mod creates the second cable on the neighbouring free ports of A and B.
4. Group both cables in the vanilla LACP UI.

One walk, two cables, one shared path — instead of two identical walks.
