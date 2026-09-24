using System;
using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;

[assembly: MelonInfo(typeof(GregMod.MultiCable.MultiCableMod),
    GregMod.MultiCable.MyPluginInfo.PLUGIN_NAME,
    GregMod.MultiCable.MyPluginInfo.PLUGIN_VERSION,
    GregMod.MultiCable.MyPluginInfo.PLUGIN_AUTHOR)]
[assembly: MelonGame("Waseku", "Data Center")]

namespace GregMod.MultiCable
{
    /// <summary>
    /// gregMod.MultiCable — carry one cable, complete N parallel cables.
    ///
    /// The vanilla game connects exactly one cable per pull (1-to-1). This mod
    /// keeps that flow intact and adds, on top:
    ///  - live mirror ghosts while carrying (N cables visibly follow you), and
    ///  - automatic sibling creation on completion: the remaining N-1 cables
    ///    are replayed through vanilla's own methods onto free ports of the
    ///    same start/end devices — same route, redundant ports, ready for a
    ///    vanilla LACP group over long distances.
    ///
    /// Set the parallel count in the gregCore F1 Mod Config UI
    /// ("gregMod.MultiCable" -> "Parallel cables"), or in the F8 panel when
    /// gregCore is not installed.
    /// </summary>
    public sealed class MultiCableMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            try
            {
                MirrorGhostManager.RegisterIl2CppType();
                MultiCableConfig.Load();

                var harmony = new HarmonyLib.Harmony("com.gregmod.multicable");
                Patches.Apply(harmony);

                if (GregHost.HasCore)
                {
                    try
                    {
                        MultiCableConfig.RegisterF1Entries();
                        MultiCableCoreConfig.RegisterHub(
                            MyPluginInfo.PLUGIN_VERSION,
                            MirrorGhostManager.OpenPanel,
                            MirrorGhostManager.ClosePanel);
                    }
                    catch (Exception ex)
                    {
                        LoggerInstance.Warning($"[MultiCable] gregCore wiring failed: {ex.GetBaseException().Message}");
                    }
                }

                LoggerInstance.Msg(
                    $"[MultiCable] {MyPluginInfo.PLUGIN_VERSION} loaded. " +
                    $"Count={MultiCableConfig.CableCount}, Enabled={MultiCableConfig.Enabled}. " +
                    $"Press {MultiCableConfig.ToggleKey} for the panel" +
                    (GregHost.HasCore ? " (count also in F1 Mod Config)." : "."));
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"[MultiCable] Startup failed: {ex.GetBaseException().Message}");
            }
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            try { MirrorGhostManager.EnsureExists(); }
            catch { /* best-effort: manager respawns next scene load */ }
        }

        public override void OnUpdate()
        {
            try
            {
                var kb = Keyboard.current;
                if (kb == null) return;
                var key = kb[MultiCableConfig.ToggleKey];
                if (key != null && key.wasPressedThisFrame && !IsPauseMenuActive())
                    MirrorGhostManager.TogglePanel();
            }
            catch { /* input best-effort */ }
        }

        /// <summary>True while a game pause/settings canvas is on screen.</summary>
        internal static bool IsPauseMenuActive()
        {
            try
            {
                var all = Resources.FindObjectsOfTypeAll<Canvas>();
                if (all == null) return false;
                foreach (var c in all)
                {
                    if (c == null || !c.isActiveAndEnabled) continue;
                    var go = c.gameObject;
                    if (go == null) continue;
                    if (!go.scene.IsValid() || !go.scene.isLoaded) continue;
                    if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;

                    var n = go.name ?? "";
                    if (n.IndexOf("Pause", StringComparison.OrdinalIgnoreCase) >= 0
                        || n.IndexOf("EscapeMenu", StringComparison.OrdinalIgnoreCase) >= 0
                        || n.IndexOf("InGameMenu", StringComparison.OrdinalIgnoreCase) >= 0
                        || n.IndexOf("OptionsMenu", StringComparison.OrdinalIgnoreCase) >= 0
                        || n.IndexOf("SettingsMenu", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }
            catch { /* best-effort */ }
            return false;
        }
    }
}
