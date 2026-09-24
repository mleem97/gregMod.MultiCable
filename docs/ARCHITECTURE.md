# Architecture — gregMod.MultiCable

> Carry one cable, complete N parallel cables. Vanilla flow is never suppressed.

## Components

```text
port click ──► Patches (observe) ──► CarryState ──► MirrorGhostManager ──► Cloner
   │                  │                    │                │                   │
   │ vanila cable     │ track start/       │ tracked id,    │ mirror ghosts +   │ replay vanilla
   │ flows untouched  │ complete/discard   │ records, jobs  │ delayed job pump  │ entry points
```

| File | Responsibility |
|---|---|
| `src/MultiCableMod.cs` | MelonMod entry: config load, Harmony apply, gregCore wiring, F8 hotkey, manager bootstrap per scene |
| `src/MultiCableConfig.cs` | Two-layer config: MelonPreferences always; gregCore F1 (`ModConfigSystem`) wins when present. Effective values resolved on demand |
| `src/MultiCableCoreConfig.cs` | **Only** file with direct gregCore references (JIT split behind `GregHost.HasCore`): F1 entries, hub/HUD/panel-opener registration |
| `src/GregHost.cs` | Soft-dependency probe (`Type.GetType`, no hard load) |
| `src/CarryState.cs` | Tracked cable id, start/end click candidates, authoritative `ConnectionRecord`, cached `NetworkMap` instance, delayed `CloneJob` queue, panel status |
| `src/Patches.cs` | 4 observational Harmony seams (see below); recursion guard via `Cloner.IsCloning` |
| `src/SiblingFinder.cs` | Free-port search on the same device (switch-id / parent equality, type + SFP/fibre match, `cableIDsOnLink == 0`), nearest-first |
| `src/Cloner.cs` | Delayed sibling creation via `ReserveCableId` → 2× `AssignNewPosition` → `GenerateFinalPath` if needed → `RequestRouteEvaluation`; per-sibling rollback with `DiscardCable` |
| `src/MirrorGhostManager.cs` | Il2Cpp `MonoBehaviour`: per-frame mirror ghost rendering from the live vanilla waypoint list; clone-queue pump; F8 `BeginArea` panel |
| `src/MyPluginInfo.cs` | Plugin id / name / version constants |

## Harmony seams (all observational)

| Target | Hook | Purpose |
|---|---|---|
| `CableLink.InteractOnClick` | prefix (record only) | Capture clicked port; role (start/end) inferred from carry state |
| `CablePositions.CreateNewCable` | postfix | Detect pull start → `CarryState.Begin(__result)` (skipped for own siblings and when count ≤ 1) |
| `NetworkMap.RegisterCableConnection` | postfix | Authoritative completion record (all endpoint/port args + waypoint snapshot); enqueue `CloneJob`; cache `NetworkMap` instance |
| `CablePositions.DiscardCable` | postfix | Cancel tracking when the carried cable is discarded |

No prefix returns `false` anywhere. The mod adds exactly one behaviour:
delayed sibling creation through public vanilla methods.

## Data flows

1. **Carry:** click → `CreateNewCable` id → `Begin(id)` → manager copies
   `CablePositions.GetCablePositions(id)` (fallback `GetRawCablePositions`)
   each frame into `count-1` `LineRenderer`s with a lateral offset.
2. **Complete:** `RegisterCableConnection` args → `ConnectionRecord` (+ links
   from click candidates) → `CloneJob{ExecuteAt = now + CloneDelaySec}`.
3. **Clone:** pump dequeues → `SiblingFinder` pairs free ports → per sibling:
   `ReserveCableId`, `AssignNewPosition(start)`, `AssignNewPosition(end)`,
   `GenerateFinalPath` if `!IsCableComplete`, else `DiscardCable` + warn →
   single `RequestRouteEvaluation` at the end.
4. **Config:** F1 `ModConfigSystem` (when gregCore present) else
   `MelonPreferences`; panel reads effective values, edits prefs only when
   F1 does not own them.

## Failure handling

- Seam missing at startup → warning, degraded tracking, vanilla unaffected.
- Sibling step throws → that sibling is discarded, origin cable untouched.
- No free port pair → job reports and stops (partial pairs still created).
- Own sibling completions re-enter the `RegisterCableConnection` postfix but
  return early under `IsCloning` — no grandchild jobs, no infinite loop.

Record changes here + [`CHANGELOG.md`](../CHANGELOG.md) (Unreleased).
