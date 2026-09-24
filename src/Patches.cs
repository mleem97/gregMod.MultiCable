using System;
using System.Reflection;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace GregMod.MultiCable
{
    /// <summary>
    /// Harmony seams. Every patch is strictly observational except the additive
    /// sibling creation (Cloner), which runs delayed and never during the
    /// original call. No prefix ever suppresses vanilla behaviour
    /// (all prefixes return void / always call through).
    ///
    /// Recursion guard: <see cref="Cloner.IsCloning"/> makes the
    /// RegisterCableConnection postfix ignore cables created by the mod itself.
    /// </summary>
    internal static class Patches
    {
        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            int applied = 0;

            applied += TryPatch(harmony, typeof(CableLink), "InteractOnClick",
                prefix: typeof(PatchPortClick).GetMethod(nameof(PatchPortClick.Prefix),
                    BindingFlags.Static | BindingFlags.NonPublic));

            applied += TryPatch(harmony, typeof(CablePositions), "CreateNewCable",
                postfix: typeof(PatchCableCreated).GetMethod(nameof(PatchCableCreated.Postfix),
                    BindingFlags.Static | BindingFlags.NonPublic));

            applied += TryPatch(harmony, typeof(NetworkMap), "RegisterCableConnection",
                postfix: typeof(PatchCableConnected).GetMethod(nameof(PatchCableConnected.Postfix),
                    BindingFlags.Static | BindingFlags.NonPublic));

            applied += TryPatch(harmony, typeof(CablePositions), "DiscardCable",
                postfix: typeof(PatchCableDiscarded).GetMethod(nameof(PatchCableDiscarded.Postfix),
                    BindingFlags.Static | BindingFlags.NonPublic));

            MelonLogger.Msg($"[MultiCable] Harmony seams applied: {applied}/4.");
        }

        private static int TryPatch(HarmonyLib.Harmony harmony, Type target, string method,
            MethodInfo prefix = null, MethodInfo postfix = null)
        {
            try
            {
                var m = target.GetMethod(method,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (m == null)
                {
                    MelonLogger.Warning($"[MultiCable] Seam missing: {target.Name}.{method} — tracking degraded.");
                    return 0;
                }
                harmony.Patch(m,
                    prefix != null ? new HarmonyLib.HarmonyMethod(prefix) : null,
                    postfix != null ? new HarmonyLib.HarmonyMethod(postfix) : null);
                MelonLogger.Msg($"[MultiCable] Seam hooked: {target.Name}.{method}.");
                return 1;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[MultiCable] Failed to hook {target.Name}.{method}: {ex.GetBaseException().Message}");
                return 0;
            }
        }
    }

    /// <summary>Records every physical port click (start/end role inferred by carry state).</summary>
    internal static class PatchPortClick
    {
        internal static void Prefix(CableLink __instance)
        {
            try
            {
                if (__instance == null) return;
                if (!MultiCableConfig.Enabled) return;

                if (CarryState.IsTracking)
                    CarryState.EndLinkCandidate = __instance;
                else
                    CarryState.StartLinkCandidate = __instance;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[MultiCable] Port-click seam failed: {ex.Message}");
            }
        }
    }

    /// <summary>Detects the start of a vanilla cable pull.</summary>
    internal static class PatchCableCreated
    {
        internal static void Postfix(int __result)
        {
            try
            {
                if (__result < 0) return;
                if (!MultiCableConfig.Enabled) return;
                if (Cloner.IsCloning) return; // our own sibling ids are not tracked
                int want = MultiCableConfig.CableCount;
                if (want <= 1) return; // vanilla passthrough

                CarryState.Begin(__result);
                CarryState.LastResult = $"Carrying cable {__result} — mirroring {want - 1} extra ghost(s).";
                MelonLogger.Msg($"[MultiCable] Tracking carry of cable {__result} (x{want} mode).");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[MultiCable] Cable-created seam failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Captures the authoritative connection record when vanilla completes a
    /// cable and enqueues sibling creation (delayed, see MirrorGhostManager).
    /// </summary>
    internal static class PatchCableConnected
    {
        internal static void Postfix(NetworkMap __instance,
            int cableId, Vector3 startPos, Vector3 endPos,
            CableLink.TypeOfLink startType, CableLink.TypeOfLink endType,
            string startSwitchID, string endSwitchID,
            int startCustomerID, int endCustomerID,
            string startServerID, string endServerID)
        {
            try
            {
                if (__instance != null)
                    CarryState.NetworkMapInstance = __instance;
                if (Cloner.IsCloning) return; // ignore our own siblings
                if (!MultiCableConfig.Enabled) return;
                if (!CarryState.IsTracking || cableId != CarryState.TrackedCableId) return;

                int want = MultiCableConfig.CableCount;

                var rec = new ConnectionRecord
                {
                    CableId = cableId,
                    StartPos = startPos,
                    EndPos = endPos,
                    StartType = startType,
                    EndType = endType,
                    StartSwitchID = startSwitchID,
                    EndSwitchID = endSwitchID,
                    StartCustomerID = startCustomerID,
                    EndCustomerID = endCustomerID,
                    StartServerID = startServerID,
                    EndServerID = endServerID,
                    StartLink = CarryState.StartLinkCandidate,
                    EndLink = CarryState.EndLinkCandidate,
                    Waypoints = SnapshotWaypoints(cableId),
                };
                CarryState.LastConnection = rec;
                CarryState.StopTracking(null);

                if (want <= 1 || !MultiCableConfig.AutoClone)
                {
                    CarryState.LastResult = $"Cable {cableId} completed (x{want} mode, auto-clone off).";
                    return;
                }

                CarryState.Enqueue(new CloneJob
                {
                    Origin = rec,
                    WantedTotal = want,
                    ExecuteAt = Time.time + MultiCableConfig.CloneDelaySec,
                });
                CarryState.LastResult = $"Cable {cableId} completed — {want - 1} sibling(s) queued.";
                MelonLogger.Msg($"[MultiCable] Cable {cableId} completed, queued {want - 1} sibling(s).");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[MultiCable] Cable-connected seam failed: {ex.Message}");
            }
        }

        private static Vector3[] SnapshotWaypoints(int cableId)
        {
            try
            {
                var cp = CablePositions.instance;
                if (cp == null) return null;
                var pts = cp.GetCablePositions(cableId);
                if (pts == null || pts.Count == 0)
                    pts = cp.GetRawCablePositions(cableId);
                if (pts == null || pts.Count == 0) return null;
                var arr = new Vector3[pts.Count];
                for (int i = 0; i < pts.Count; i++) arr[i] = pts[i];
                return arr;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>Cancels tracking when vanilla discards the carried cable.</summary>
    internal static class PatchCableDiscarded
    {
        internal static void Postfix(int cableId)
        {
            try
            {
                if (CarryState.IsTracking && cableId == CarryState.TrackedCableId)
                {
                    CarryState.StopTracking($"Carry of cable {cableId} discarded — tracking cleared.");
                    MelonLogger.Msg($"[MultiCable] Tracked cable {cableId} discarded, tracking cleared.");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[MultiCable] Cable-discarded seam failed: {ex.Message}");
            }
        }
    }
}
