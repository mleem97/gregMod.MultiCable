using System;
using System.Collections.Generic;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes;
using MelonLoader;
using UnityEngine;

namespace GregMod.MultiCable
{
    /// <summary>
    /// Finds free sibling ports on the same device as a reference port.
    ///
    /// Matching rules (all must hold):
    ///  - same device: equal switchID for switch-bound ports, otherwise the
    ///    same parent reference (server / patch panel / internet / switch),
    ///  - same link type (Server / Switch / PatchPanel / ...),
    ///  - same SFP/fibre character (no copper-into-fibre surprises),
    ///  - free: cableIDsOnLink == 0 and not already claimed by this job.
    ///
    /// Candidates are returned nearest-first (adjacent redundant ports win),
    /// so long redundant runs stay parallel.
    /// </summary>
    internal static class SiblingFinder
    {
        internal static List<CableLink> FindFreeSiblings(CableLink origin, int needed,
            HashSet<IntPtr> claimed)
        {
            var result = new List<CableLink>();
            if (origin == null || needed <= 0) return result;

            CableLink[] all;
            try
            {
                var found = Resources.FindObjectsOfTypeAll<CableLink>();
                if (found == null || found.Count == 0) return result;
                all = new CableLink[found.Count];
                for (int i = 0; i < found.Count; i++) all[i] = found[i];
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[MultiCable] Port scan failed: {ex.Message}");
                return result;
            }

            string originSwitch = SafeSwitchID(origin);
            IntPtr originParent = ParentPointer(origin);
            Vector3 originPos = SafePosition(origin);

            var scored = new List<Tuple<float, CableLink>>();
            foreach (var link in all)
            {
                if (link == null) continue;
                if (SamePointer(link, origin)) continue;
                if (claimed != null && claimed.Contains(link.Pointer)) continue;
                if (!IsSameDevice(link, origin, originSwitch, originParent)) continue;
                if (!IsCompatible(link, origin)) continue;
                if (!IsFree(link)) continue;

                float d = Vector3.Distance(SafePosition(link), originPos);
                scored.Add(Tuple.Create(d, link));
            }

            scored.Sort((a, b) => a.Item1.CompareTo(b.Item1));
            for (int i = 0; i < scored.Count && result.Count < needed; i++)
                result.Add(scored[i].Item2);
            return result;
        }

        internal static string Describe(CableLink link)
        {
            if (link == null) return "unknown port";
            try
            {
                string sw = SafeSwitchID(link);
                string dev = !string.IsNullOrEmpty(sw) ? $"switch {sw}" : $"{link.typeOfLink} port";
                return $"{dev} @ {SafePosition(link)}";
            }
            catch { return "unknown port"; }
        }

        // ── Predicates ───────────────────────────────────────────────────────

        private static bool IsSameDevice(CableLink link, CableLink origin,
            string originSwitch, IntPtr originParent)
        {
            try
            {
                string sw = SafeSwitchID(link);
                // Switch-bound ports: compare the unique switch id.
                if (!string.IsNullOrEmpty(originSwitch) || !string.IsNullOrEmpty(sw))
                    return string.Equals(sw, originSwitch, StringComparison.Ordinal);
                // Non-switch ports (servers, patch panels, ...): compare parents.
                IntPtr p = ParentPointer(link);
                if (originParent == IntPtr.Zero || p == IntPtr.Zero) return false;
                return p == originParent;
            }
            catch { return false; }
        }

        private static bool IsCompatible(CableLink link, CableLink origin)
        {
            try
            {
                if (link.typeOfLink != origin.typeOfLink) return false;
                if (link.isSFPPort != origin.isSFPPort) return false;
                if (link.isFibrePort != origin.isFibrePort) return false;
                return true;
            }
            catch { return false; }
        }

        private static bool IsFree(CableLink link)
        {
            try { return link.cableIDsOnLink == 0; }
            catch { return false; }
        }

        // ── Safe accessors (Il2Cpp objects may be destroyed at any time) ────

        private static string SafeSwitchID(CableLink link)
        {
            try { return link.switchID ?? ""; }
            catch { return ""; }
        }

        private static Vector3 SafePosition(CableLink link)
        {
            try
            {
                var t = link.transform;
                if (t != null) return t.position;
            }
            catch { /* fall through */ }
            return Vector3.zero;
        }

        private static IntPtr ParentPointer(CableLink link)
        {
            try
            {
                if (link.parentSwitch != null) return link.parentSwitch.Pointer;
                if (link.parentServer != null) return link.parentServer.Pointer;
                if (link.parentPatchPanel != null) return link.parentPatchPanel.Pointer;
                if (link.parentInternet != null) return link.parentInternet.Pointer;
            }
            catch { /* fall through */ }
            return IntPtr.Zero;
        }

        private static bool SamePointer(CableLink a, CableLink b)
        {
            try
            {
                Il2CppObjectBase oa = a;
                Il2CppObjectBase ob = b;
                if (oa == null || ob == null) return false;
                return oa.Pointer == ob.Pointer;
            }
            catch { return false; }
        }
    }
}
