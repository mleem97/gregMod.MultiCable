using System;
using MelonLoader;
using UnityEngine.InputSystem;

namespace GregMod.MultiCable
{
    /// <summary>
    /// Configuration for gregMod.MultiCable.
    ///
    /// Two-layer model:
    ///  1. MelonPreferences (always available, editable in MelonPreferences.cfg).
    ///  2. gregCore ModConfigSystem / F1 config UI (when gregCore is installed).
    ///     F1 values take precedence at read time; the F8 panel shows them
    ///     read-only in that case. Without gregCore the F8 panel edits the
    ///     MelonPreferences values directly.
    ///
    /// Effective values are resolved on demand (every cable completion and every
    /// panel repaint) so F1 edits apply without a restart.
    /// </summary>
    internal static class MultiCableConfig
    {
        internal const string ModId = "gregMod.MultiCable";

        internal const int MinCables = 1;
        internal const int MaxCables = 4;

        private static MelonPreferences_Category _cat;
        private static MelonPreferences_Entry<bool> _enabled;
        private static MelonPreferences_Entry<int> _cableCount;
        private static MelonPreferences_Entry<bool> _mirrorGhosts;
        private static MelonPreferences_Entry<string> _mirrorColor;
        private static MelonPreferences_Entry<float> _ghostOffset;
        private static MelonPreferences_Entry<bool> _autoClone;
        private static MelonPreferences_Entry<float> _cloneDelaySec;
        private static MelonPreferences_Entry<string> _toggleKey;

        internal static Key ToggleKey = Key.F8;

        internal static void Load()
        {
            try
            {
                _cat = MelonPreferences.CreateCategory(ModId, "MultiCable");
                _enabled = _cat.CreateEntry("Enabled", true, "Enabled",
                    "Master switch. When off, the game behaves exactly like vanilla (1-to-1 cables).");
                _cableCount = _cat.CreateEntry("CableCount", 2, "Parallel cables",
                    "How many parallel cables one pull creates (1 = vanilla passthrough). Range 1-4. Also editable in the gregCore F1 config UI.");
                _mirrorGhosts = _cat.CreateEntry("MirrorGhosts", true, "Mirror ghosts",
                    "Show extra live preview ghosts following you while carrying a cable (true multi-carry visual).");
                _mirrorColor = _cat.CreateEntry("MirrorColor", "#35E0FF", "Mirror ghost color",
                    "HTML color of the extra preview ghosts (e.g. #35E0FF).");
                _ghostOffset = _cat.CreateEntry("GhostOffset", 0.07f, "Ghost lateral offset (m)",
                    "Sideways spacing between the mirrored preview ghosts, in metres. Cosmetic only.");
                _autoClone = _cat.CreateEntry("AutoClone", true, "Auto-create sibling cables",
                    "When the carried cable is completed, automatically create the remaining parallel cables on free ports of the same devices.");
                _cloneDelaySec = _cat.CreateEntry("CloneDelaySec", 1.0f, "Clone delay (s)",
                    "Delay after cable completion before sibling cables are created, so vanilla route evaluation can settle.");
                _toggleKey = _cat.CreateEntry("ToggleKey", "F8", "Panel hotkey",
                    "Input System key opening the MultiCable panel (e.g. F8, F7, Backquote).");
                _cat.SaveToFile(false);

                if (Enum.TryParse<Key>(_toggleKey.Value, true, out var k) && k != Key.None)
                    ToggleKey = k;
                else
                    MelonLogger.Warning($"[MultiCable] Unknown ToggleKey '{_toggleKey.Value}', defaulting to F8.");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[MultiCable] Config load failed, using built-in defaults: {ex.GetBaseException().Message}");
            }
        }

        // ── Effective values (F1 wins when gregCore is present) ──────────────

        internal static bool Enabled => GetBool("Enabled", _enabled, true);

        internal static int CableCount
        {
            get
            {
                int v = GetInt("CableCount", _cableCount, 2);
                if (v < MinCables) v = MinCables;
                if (v > MaxCables) v = MaxCables;
                return v;
            }
        }

        internal static bool MirrorGhosts => GetBool("MirrorGhosts", _mirrorGhosts, true);

        internal static bool AutoClone => GetBool("AutoClone", _autoClone, true);

        internal static string MirrorColorRaw => _mirrorColor != null ? _mirrorColor.Value : "#35E0FF";

        internal static float GhostOffset => _ghostOffset != null ? _ghostOffset.Value : 0.07f;

        internal static float CloneDelaySec
        {
            get
            {
                float v = _cloneDelaySec != null ? _cloneDelaySec.Value : 1.0f;
                if (v < 0f) v = 0f;
                if (v > 10f) v = 10f;
                return v;
            }
        }

        /// <summary>True while the F1 gregCore config UI owns the core settings.</summary>
        internal static bool F1OwnsCoreSettings => GregHost.HasCore;

        // ── Panel write path (prefs only; used when gregCore is absent) ──────

        internal static void SetEnabled(bool v)
        {
            if (_enabled == null) return;
            _enabled.Value = v;
            MelonPreferences.Save();
        }

        internal static void SetCableCount(int v)
        {
            if (_cableCount == null) return;
            if (v < MinCables) v = MinCables;
            if (v > MaxCables) v = MaxCables;
            _cableCount.Value = v;
            MelonPreferences.Save();
        }

        internal static void SetMirrorGhosts(bool v)
        {
            if (_mirrorGhosts == null) return;
            _mirrorGhosts.Value = v;
            MelonPreferences.Save();
        }

        internal static void SetAutoClone(bool v)
        {
            if (_autoClone == null) return;
            _autoClone.Value = v;
            MelonPreferences.Save();
        }

        // ── Backing-store helpers ─────────────────────────────────────────────

        private static bool GetBool(string key, MelonPreferences_Entry<bool> pref, bool fallback)
        {
            // F1 (gregCore ModConfigSystem) takes precedence when available.
            if (GregHost.HasCore)
            {
                try { return MultiCableCoreConfig.GetBoolValue(ModId, key, pref != null ? pref.Value : fallback); }
                catch { /* fall through to prefs */ }
            }
            try { return pref != null ? pref.Value : fallback; }
            catch { return fallback; }
        }

        private static int GetInt(string key, MelonPreferences_Entry<int> pref, int fallback)
        {
            if (GregHost.HasCore)
            {
                try { return MultiCableCoreConfig.GetIntValue(ModId, key, pref != null ? pref.Value : fallback); }
                catch { /* fall through to prefs */ }
            }
            try { return pref != null ? pref.Value : fallback; }
            catch { return fallback; }
        }

        // ── gregCore registration (called only when HasCore; own type for JIT split)
        internal static void RegisterF1Entries()
        {
            try { MultiCableCoreConfig.RegisterEntries(); }
            catch (Exception ex)
            {
                MelonLogger.Warning("[MultiCable] F1 config registration failed: " + ex.GetBaseException().Message);
            }
        }
    }
}
