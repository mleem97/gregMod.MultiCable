using System;
using System.Collections.Generic;
using Il2Cpp;
using MelonLoader;

namespace GregMod.MultiCable
{
    /// <summary>
    /// Creates sibling cables for a completed pull by replaying the same
    /// vanilla entry points a physical port click would drive:
    ///
    ///   CablePositions.ReserveCableId()
    ///     -> CablePositions.AssignNewPosition(id, startLink, isStart, ...)
    ///     -> CablePositions.AssignNewPosition(id, endLink, ..., isEnd, ...)
    ///     -> CablePositions.GenerateFinalPath(id) if not complete
    ///   WaypointInitializationSystem.RequestRouteEvaluation()
    ///
    /// Each sibling lands on free ports of the SAME start/end devices, so the
    /// vanilla-generated path runs parallel to the original pull — the same
    /// cable route, redundant ports, LACP-ready discrete cables.
    ///
    /// Safety:
    ///  - IsCloning guards the RegisterCableConnection/CreateNewCable patches
    ///    against recursion (our siblings never enqueue grandchildren).
    ///  - Every step is individually try/caught; a failed sibling is discarded
    ///    via DiscardCable and reported, never left half-registered.
    ///  - Partial success (fewer free ports than wanted) is reported, not fatal.
    /// </summary>
    internal static class Cloner
    {
        internal static bool IsCloning;

        internal static void Execute(CloneJob job)
        {
            if (job == null || job.Origin == null) return;
            var rec = job.Origin;
            int siblingsWanted = Math.Max(0, job.WantedTotal - 1);
            if (siblingsWanted <= 0) return;

            var cp = CablePositions.instance;
            if (cp == null)
            {
                CarryState.LastResult = "Clone failed: CablePositions not ready.";
                MelonLogger.Warning("[MultiCable] Clone aborted: CablePositions.instance is null.");
                return;
            }

            if (rec.StartLink == null || rec.EndLink == null)
            {
                CarryState.LastResult = "Clone skipped: start/end port click was not observed (free-port search needs it).";
                MelonLogger.Warning("[MultiCable] Clone skipped: StartLink/EndLink unknown — click physical ports (not switch-config UI) for full tracking.");
                return;
            }

            var claimed = new HashSet<IntPtr>();
            var startSibs = SiblingFinder.FindFreeSiblings(rec.StartLink, siblingsWanted, claimed);
            foreach (var s in startSibs) claimed.Add(s.Pointer);
            var endSibs = SiblingFinder.FindFreeSiblings(rec.EndLink, siblingsWanted, claimed);
            foreach (var s in endSibs) claimed.Add(s.Pointer);

            int pairs = Math.Min(startSibs.Count, endSibs.Count);
            if (pairs == 0)
            {
                CarryState.LastResult =
                    $"Clone skipped: no free sibling ports (start: {SiblingFinder.Describe(rec.StartLink)}, end: {SiblingFinder.Describe(rec.EndLink)}).";
                MelonLogger.Warning("[MultiCable] Clone skipped: no free sibling port pair found.");
                return;
            }
            if (pairs < siblingsWanted)
                MelonLogger.Warning($"[MultiCable] Only {pairs}/{siblingsWanted} sibling pair(s) available — creating what fits.");

            int created = 0;
            var createdIds = new List<int>();
            IsCloning = true;
            try
            {
                for (int i = 0; i < pairs; i++)
                {
                    int newId = -1;
                    try
                    {
                        newId = cp.ReserveCableId();
                        var sl = startSibs[i];
                        var el = endSibs[i];

                        cp.AssignNewPosition(newId, sl.transform, true, false, rec.StartType, rec.StartServerID ?? "");
                        cp.AssignNewPosition(newId, el.transform, false, true, rec.EndType, rec.EndServerID ?? "");

                        if (!cp.IsCableComplete(newId))
                        {
                            try { cp.GenerateFinalPath(newId); }
                            catch (Exception ex)
                            {
                                MelonLogger.Warning($"[MultiCable] GenerateFinalPath({newId}) failed: {ex.Message}");
                            }
                        }

                        if (cp.IsCableComplete(newId))
                        {
                            created++;
                            createdIds.Add(newId);
                            MelonLogger.Msg($"[MultiCable] Sibling cable {newId} created " +
                                $"({SiblingFinder.Describe(sl)} <-> {SiblingFinder.Describe(el)}).");
                        }
                        else
                        {
                            try { cp.DiscardCable(newId); } catch { /* best-effort */ }
                            MelonLogger.Warning($"[MultiCable] Sibling cable {newId} incomplete after replay — discarded.");
                        }
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Warning($"[MultiCable] Sibling creation failed: {ex.GetBaseException().Message}");
                        if (newId >= 0)
                        {
                            try { cp.DiscardCable(newId); } catch { /* best-effort */ }
                        }
                    }
                }
            }
            finally
            {
                IsCloning = false;
            }

            try
            {
                var wis = WaypointInitializationSystem.Instance;
                if (wis != null) wis.RequestRouteEvaluation();
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[MultiCable] Route re-evaluation failed: {ex.Message}");
            }

            if (created > 0)
            {
                string note = pairs < siblingsWanted
                    ? $" (only {pairs}/{siblingsWanted} free port pairs)"
                    : "";
                CarryState.LastResult = $"Pull x{job.WantedTotal}: cable {rec.CableId} + {created} sibling(s) [{string.Join(",", createdIds)}]{note}. Group them via vanilla LACP if desired.";
                MelonLogger.Msg($"[MultiCable] Pull complete: origin {rec.CableId} + {created} sibling(s).");
            }
            else
            {
                CarryState.LastResult = "Clone failed: sibling replay did not complete — see console. Origin cable is unaffected.";
            }
        }
    }
}
