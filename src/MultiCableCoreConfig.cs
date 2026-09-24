using System;
using MelonLoader;

namespace GregMod.MultiCable
{
    /// <summary>
    /// All direct gregCore references live in this class and this class only.
    /// It is called exclusively behind <see cref="GregHost.HasCore"/>, so the
    /// mod still loads when gregCore is absent (JIT split: referencing methods
    /// are never jitted without gregCore present).
    ///
    /// Covers:
    ///  - F1 config UI entries (DataCenterModLoader.ModConfigSystem),
    ///  - mod registry / HUD key hint / F1 hub panel opener.
    /// </summary>
    internal static class MultiCableCoreConfig
    {
        internal static void RegisterEntries()
        {
            DataCenterModLoader.ModConfigSystem.RegisterBool(
                MultiCableConfig.ModId, "Enabled", "Enabled", true,
                "Master switch. When off, the game behaves exactly like vanilla (1-to-1 cables).");
            DataCenterModLoader.ModConfigSystem.RegisterInt(
                MultiCableConfig.ModId, "CableCount", "Parallel cables", 2,
                MultiCableConfig.MinCables, MultiCableConfig.MaxCables,
                "How many parallel cables one pull creates (1 = vanilla passthrough).");
            DataCenterModLoader.ModConfigSystem.RegisterBool(
                MultiCableConfig.ModId, "MirrorGhosts", "Mirror ghosts", true,
                "Show extra live preview ghosts following you while carrying a cable.");
            DataCenterModLoader.ModConfigSystem.RegisterBool(
                MultiCableConfig.ModId, "AutoClone", "Auto-create sibling cables", true,
                "When the carried cable is completed, auto-create the remaining parallel cables.");
        }

        internal static bool GetBoolValue(string modId, string key, bool fallback)
        {
            try { return DataCenterModLoader.ModConfigSystem.GetBoolValue(modId, key, fallback); }
            catch { return fallback; }
        }

        internal static int GetIntValue(string modId, string key, int fallback)
        {
            try { return DataCenterModLoader.ModConfigSystem.GetIntValue(modId, key, fallback); }
            catch { return fallback; }
        }

        internal static void RegisterHub(string version, Action onOpen, Action onClose)
        {
            try
            {
                gregCore.Core.Mods.GregModRegistry.Register(
                    MultiCableConfig.ModId, "MultiCable", version,
                    new string[] { "multicable" });
                gregCore.UI.GregHudRegistry.Register(
                    "multicable", MultiCableConfig.ToggleKey.ToString(), "MultiCable");
                gregCore.UI.GregMenuRegistry.RegisterOpener("multicable", () =>
                {
                    try { onOpen?.Invoke(); } catch { /* best-effort */ }
                });
                gregCore.UI.GregMenuRegistry.RegisterCloser("multicable", () =>
                {
                    try { onClose?.Invoke(); } catch { /* best-effort */ }
                });
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[MultiCable] Hub registration failed: " + ex.GetBaseException().Message);
            }
        }
    }
}
