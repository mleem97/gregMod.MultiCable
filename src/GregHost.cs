using System;

namespace GregMod.MultiCable
{
    // Soft-dependency probe (pure type-name lookup): any method that touches
    // gregCore types must only run when HasCore is true, otherwise the runtime
    // throws a JIT TypeLoad without the gregCore DLL present.
    internal static class GregHost
    {
        private const string ProbeType = "gregCore.UI.GregNotificationManager, gregCore";
        private static bool? _hasCore;

        public static bool HasCore
        {
            get
            {
                if (_hasCore == null)
                {
                    try { _hasCore = Type.GetType(ProbeType) != null; }
                    catch { _hasCore = false; }
                }
                return _hasCore.Value;
            }
        }
    }
}
