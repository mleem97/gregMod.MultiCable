using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace GregMod.MultiCable
{
    /// <summary>
    /// Runtime state of the currently tracked cable pull.
    ///
    /// Vanilla flow (1-to-1) observed through Harmony seams:
    ///   port click (CableLink.InteractOnClick)
    ///     -> CablePositions.CreateNewCable() allocates the active cable id
    ///     -> player walks, vanilla ghost follows, waypoints accumulate
    ///     -> second port click completes the cable
    ///     -> NetworkMap.RegisterCableConnection(...) records endpoints/ports
    ///     -> CoopWorldSync.RequestCableCreate(...) replicates + route re-eval
    ///
    /// MultiCable never suppresses or replaces these steps. It tracks the
    /// vanilla cable, mirrors its ghost (visual true multi-carry), and after
    /// completion replays the vanilla entry points for sibling cables on free
    /// ports of the same devices (see Cloner).
    /// </summary>
    internal static class CarryState
    {
        internal const int NoCable = -1;

        /// <summary>Vanilla cable id currently being carried, or -1 when idle.</summary>
        internal static int TrackedCableId = NoCable;

        /// <summary>Port clicked to start the tracked pull (best-effort, may be null).</summary>
        internal static CableLink StartLinkCandidate;

        /// <summary>Port clicked while carrying (best-effort end candidate).</summary>
        internal static CableLink EndLinkCandidate;

        /// <summary>Authoritative connection record of the completed pull.</summary>
        internal static ConnectionRecord LastConnection;

        /// <summary>Cached NetworkMap instance (captured from patch, used by Cloner).</summary>
        internal static NetworkMap NetworkMapInstance;

        /// <summary>Pending sibling-creation jobs (executed delayed by the manager).</summary>
        internal static readonly Queue<CloneJob> PendingJobs = new Queue<CloneJob>();

        /// <summary>Last human-readable result shown in the panel.</summary>
        internal static string LastResult = "Idle — carry a cable to test.";

        internal static bool IsTracking => TrackedCableId != NoCable;

        internal static void Begin(int cableId)
        {
            TrackedCableId = cableId;
            EndLinkCandidate = null;
            LastConnection = null;
        }

        /// <summary>Stop tracking without touching the vanilla cable.</summary>
        internal static void StopTracking(string reason)
        {
            TrackedCableId = NoCable;
            StartLinkCandidate = null;
            EndLinkCandidate = null;
            if (!string.IsNullOrEmpty(reason))
                LastResult = reason;
        }

        internal static void Enqueue(CloneJob job)
        {
            PendingJobs.Enqueue(job);
        }
    }

    /// <summary>Authoritative endpoint/port record of one completed vanilla cable.</summary>
    internal sealed class ConnectionRecord
    {
        internal int CableId;
        internal Vector3 StartPos;
        internal Vector3 EndPos;
        internal CableLink.TypeOfLink StartType;
        internal CableLink.TypeOfLink EndType;
        internal string StartSwitchID;
        internal string EndSwitchID;
        internal int StartCustomerID;
        internal int EndCustomerID;
        internal string StartServerID;
        internal string EndServerID;
        internal CableLink StartLink; // may be null if click was not observed
        internal CableLink EndLink;   // may be null if click was not observed
        internal Vector3[] Waypoints; // snapshot at completion
    }

    /// <summary>Deferred sibling-creation request executed by the manager.</summary>
    internal sealed class CloneJob
    {
        internal ConnectionRecord Origin;
        internal int WantedTotal;   // e.g. 2
        internal float ExecuteAt;   // UnityEngine.Time.time threshold
    }
}
