# Compatibility — gregMod.MultiCable

## Baseline (built and signature-checked against)

- Game: **Data Center** by **Waseku** (`app.info`: `Waseku / Data Center`).
- Interop assemblies dated **2026-09-19** (`MelonLoader/Il2CppAssemblies`,
  `GameAssembly.dll` of the local Steam install).
- Loader: **MelonLoader 0.7.x**, mod target **net6.0-x64**.
- `gregCore` (optional): soft dependency only — mod loads and works without it.
- Platforms: Windows x64 / Linux x64 (same assemblies as sibling Greg mods).

## Reverse-engineering evidence (method signatures from interop dummies)

Cable geometry — `Il2Cpp.CablePositions` (`instance` singleton):

- `Int32 CreateNewCable()`, `Int32 ReserveCableId()`
- `AssignNewPosition(Int32 cableId, Transform linkTransform, Boolean isStartPoint,
  Boolean isEndPoint, CableLink.TypeOfLink typeOfLink, String serverID)`
- `GenerateFinalPath(Int32)`, `DiscardCable(Int32)`, `Boolean IsCableComplete(Int32)`
- `List<Vector3> GetCablePositions(Int32)` / `GetRawCablePositions(Int32)`
- Properties: `activeCableId`, `cableWidth`, `startSwitchID/endSwitchID`,
  `startServerID/endServerID`, `currentCableLength`, `totalCableLengthLaid`

Network logic — `Il2Cpp.WaypointInitializationSystem` (`Instance` singleton):

- `cables: Dictionary<Int32, CableInfo{CableID, StartPoint, EndPoint, Waypoints…}>`
- `CreateCableWithSpawners(Int32, List<Vector3>)`, `RequestRouteEvaluation()`

Connection record — `Il2Cpp.NetworkMap` (instance cached from patch):

- `RegisterCableConnection(Int32 cableId, Vector3 startPos, Vector3 endPos,
  TypeOfLink startType, TypeOfLink endType, String startSwitchID,
  String endSwitchID, Int32 startCustomerID, Int32 endCustomerID,
  String startServerID, String endServerID)`
- `RemoveCableConnection(Int32, Boolean)`, LACP: `CreateLACPGroup(String, String,
  List<Int32>, Int32)` (not used by v1 — grouping stays manual, see limits)

Ports — `Il2Cpp.CableLink : Interact`:

- `InteractOnClick()` (virtual), `switchID`, `typeOfLink`
  (`None/Server/Switch/Base/LB/PatchPanel`), `cableIDsOnLink` (occupancy),
  `isSFPPort/isFibrePort`, parents (`parentSwitch/parentServer/
  parentPatchPanel/parentInternet`), `transform`, `CableEndpoint{Type, Position,
  CustomerID, SwitchID, ServerID}`

Co-op — `Il2Cpp.CoopWorldSync` (static): `RequestCableCreate(Int32,
List<Vector3>, CablePositions)`, `SetCablePortOccupancy(CableSaveData)`,
`FindCablePortByPosition(Vector3)`. Spool — `Il2Cpp.CableSpinner`:
`IsCableLenghtEnough()` (sic), `LowerAmountOfCable(Single)`.

> The interop dummies contain **no IL** — beyond signatures, vanilla call order
> is inferred from runtime seams, not asserted. The mod therefore replays only
> public entry points and verifies each step (`IsCableComplete`, try/catch +
> `DiscardCable` rollback).

## Known limits (v1)

1. **In-game verification pending** — build passes (`0 warnings, 0 errors`);
   load/carry/clone behaviour must still be confirmed in a live game
   (see `tests/README.md` checklist).
2. **Physical ports only.** Switch-config UI clicks
   (`NetworkSwitchConfiguration.ClickPort(Int32)`) are not tracked as
   endpoints; the clone then skips with an explanatory message.
3. **No automatic LACP grouping.** Siblings are discrete vanilla cables;
   grouping stays a manual vanilla step (auto-group is roadmap, not v1 —
   `CreateLACPGroup` id allocation is unverified).
4. **Spool consumption** follows whatever the replayed vanilla path does;
   unusually long pulls may need a second spool trip — reported, never fatal.
5. **Patch panels / SFP / fibre** are matched type-to-type; mixed-media
   redundancy across different port characters is out of scope.
6. Game updates that rename the four seam methods degrade the mod to
   **vanilla passthrough with a console warning** (fail-safe by design).

## Test status

- [x] `dotnet build -c Release` — clean (0 warnings, 0 errors).
- [ ] Mod loads in game, F8 panel opens, no console errors.
- [ ] x2 pull switch→switch: mirrors follow, sibling lands on free ports.
- [ ] x3/x4 pulls; partial free-port reporting; count=1 passthrough.
- [ ] Discard mid-carry clears tracking; origin cable never touched on failure.
- [ ] Save/load keeps siblings; co-op replicates; LACP grouping works.
- [ ] With and without gregCore (F1 entries vs local panel editing).
