# Usage — gregMod.MultiCable

## Goal

Create **N parallel cables** (default: 2) between two devices with **one**
walked route — e.g. redundant switch-to-switch uplinks over a long distance
that you later group with the vanilla LACP UI.

## Setup

1. Install the DLL (see [QUICKSTART.md](../QUICKSTART.md)).
2. Set the parallel count:
   - **With gregCore:** press **F1** → Mod Config → `gregMod.MultiCable` →
     **Parallel cables** (`1–4`). `1` means vanilla passthrough.
   - **Without gregCore:** press **F8** and move the slider in the panel.
3. Recommended: leave **Mirror ghosts** ON and **Auto-clone** ON.

## Pull workflow (example: 2 cables, switch A → switch B)

1. Click a **free port** on switch A (physical port, as usual).
   The panel status switches to `Carrying cable <id>` and one extra ghost
   follows your vanilla cable on the same path.
2. Walk to switch B and manage the cable exactly like vanilla.
3. Click a **free port** on switch B.
   - The vanilla cable completes normally (route evaluation runs).
   - ~1 second later the mod creates **sibling cable #2** on the nearest
     free ports of switch A and switch B and re-runs route evaluation.
4. Check the panel `Last:` line, e.g.
   `Pull x2: cable 41 + 1 sibling(s) [42]. Group them via vanilla LACP…`.
5. (Optional) Select both cables in the vanilla LACP UI to form the
   redundant group.

## Notes

- Siblings need **free ports on both devices**. Matching rules: same device
  (switch id / same parent), same link type, same SFP/fibre character,
  unoccupied. If fewer pairs are free, the mod creates what fits and says so.
- Each sibling consumes spool length like a normal cable. If the spool runs
  out, vanilla gates apply and the sibling is discarded with a warning —
  the original cable is never affected.
- Siblings are **real vanilla cables**: they save/load, replicate in co-op
  through the same code paths as clicked cables, carry traffic, and can join
  LACP groups.
- Click **physical ports**. The switch-config UI port buttons
  (`NetworkSwitchConfiguration.ClickPort`) are a different flow and are not
  tracked as endpoints.
- **Stop tracking (keeps cable)** only clears the mod's observation state; it
  never discards your live cable.

## Settings reference

| Setting | F1 (gregCore) | F8 panel (no gregCore) | Default | Meaning |
|---|---|---|---|---|
| Enabled | yes | toggle | ON | Master switch; OFF = vanilla behaviour |
| Parallel cables | yes (1–4) | slider | 2 | Cables per pull |
| Mirror ghosts | yes | toggle | ON | Extra live preview lines while carrying |
| Auto-clone | yes | toggle | ON | Create siblings on completion |
| Mirror ghost color | prefs only | — | `#35E0FF` | HTML color of mirror lines |
| Ghost lateral offset | prefs only | — | `0.07 m` | Spacing between mirror lines (cosmetic) |
| Clone delay | prefs only | — | `1.0 s` | Settle time before sibling creation |
| Panel hotkey | prefs only | — | `F8` | Opens/closes the panel |

`prefs only` = `MelonPreferences.cfg`, category `gregMod.MultiCable`.
When gregCore is installed, the four core settings are owned by F1 and the
panel shows them read-only.
