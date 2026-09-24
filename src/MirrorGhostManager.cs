using System;
using Il2Cpp;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using UnityEngine;

namespace GregMod.MultiCable
{
    /// <summary>
    /// In-game runtime: mirror ghosts, delayed clone pump, F8 panel.
    ///
    /// Mirror ghosts (the "cables follow you" visual): while a vanilla cable
    /// is being carried, the mod copies the live waypoint list every frame and
    /// renders CableCount-1 extra preview lines with a lateral offset. They
    /// share the vanilla path exactly — one pull, N visible cables.
    ///
    /// Clone pump: executes queued <see cref="CloneJob"/>s once their
    /// ExecuteAt time passes, so vanilla route evaluation can settle first.
    /// </summary>
    public class MirrorGhostManager : MonoBehaviour
    {
        private const int PanelWindowId = 9201;

        private static bool _panelVisible;
        private static Rect _panelRect = new Rect(20, 20, 430, 120);

        private GameObject[] _ghosts = new GameObject[0];
        private LineRenderer[] _lines = new LineRenderer[0];
        private Vector3 _offsetDir = Vector3.right;

        internal static void EnsureExists()
        {
            try
            {
                if (FindObjectOfType<MirrorGhostManager>() == null)
                {
                    var go = new GameObject("MultiCableManager");
                    go.AddComponent<MirrorGhostManager>();
                    DontDestroyOnLoad(go);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[MultiCable] Manager spawn failed: {ex.Message}");
            }
        }

        internal static void TogglePanel() => _panelVisible = !_panelVisible;
        internal static void OpenPanel() => _panelVisible = true;
        internal static void ClosePanel() => _panelVisible = false;
        internal static bool IsPanelVisible => _panelVisible;

        public MirrorGhostManager(IntPtr ptr) : base(ptr) { }

        public void Update()
        {
            try { PumpCloneQueue(); } catch (Exception ex)
            {
                MelonLogger.Warning($"[MultiCable] Clone pump failed: {ex.Message}");
            }
            try { UpdateMirrorGhosts(); } catch (Exception ex)
            {
                MelonLogger.Warning($"[MultiCable] Mirror update failed: {ex.Message}");
            }
        }

        public void OnGUI()
        {
            if (!_panelVisible) return;
            try
            {
                // NOTE: GUILayout.Window needs an Il2Cpp-crossing delegate
                // (GUI.WindowFunction) which is unreliable under IL2CPP
                // interop, so the panel is a fixed BeginArea instead.
                GUILayout.BeginArea(_panelRect, "MultiCable — parallel cable pulls", GUI.skin.window);
                DrawPanel();
                GUILayout.EndArea();
            }
            catch (Exception ex)
            {
                try { GUILayout.EndArea(); } catch { /* best-effort */ }
                MelonLogger.Warning($"[MultiCable] Panel failed: {ex.Message}");
                _panelVisible = false;
            }
        }

        public void OnDestroy()
        {
            ClearGhosts();
        }

        // ── Clone pump ───────────────────────────────────────────────────────

        private static void PumpCloneQueue()
        {
            if (CarryState.PendingJobs.Count == 0) return;
            if (Cloner.IsCloning) return;
            var job = CarryState.PendingJobs.Peek();
            if (job == null)
            {
                CarryState.PendingJobs.Dequeue();
                return;
            }
            if (Time.time < job.ExecuteAt) return;
            CarryState.PendingJobs.Dequeue();
            Cloner.Execute(job);
        }

        // ── Mirror ghosts ────────────────────────────────────────────────────

        private void UpdateMirrorGhosts()
        {
            bool want = MultiCableConfig.Enabled
                && MultiCableConfig.MirrorGhosts
                && CarryState.IsTracking
                && MultiCableConfig.CableCount > 1;

            if (!want)
            {
                if (_ghosts.Length > 0) ClearGhosts();
                return;
            }

            Vector3[] pts = ReadLivePath(CarryState.TrackedCableId);
            if (pts == null || pts.Length < 2)
            {
                if (_ghosts.Length > 0) ClearGhosts();
                return;
            }

            int mirrors = MultiCableConfig.CableCount - 1;
            EnsureGhostCapacity(mirrors);
            UpdateOffsetDirection(pts);

            Color col = ParseColor(MultiCableConfig.MirrorColorRaw, new Color(0.2f, 0.88f, 1f, 1f));
            float width = ResolveWidth();
            float step = MultiCableConfig.GhostOffset;

            for (int g = 0; g < _lines.Length; g++)
            {
                var lr = _lines[g];
                if (lr == null) continue;
                Vector3 off = _offsetDir * (step * (g + 1)) + Vector3.up * (0.01f * (g + 1));
                lr.positionCount = pts.Length;
                for (int i = 0; i < pts.Length; i++)
                    lr.SetPosition(i, pts[i] + off);
                lr.startWidth = width;
                lr.endWidth = width;
                lr.startColor = col;
                lr.endColor = col;
                try { if (lr.material != null) lr.material.color = col; } catch { /* best-effort */ }
            }
        }

        private static Vector3[] ReadLivePath(int cableId)
        {
            try
            {
                var cp = CablePositions.instance;
                if (cp == null) return null;
                var raw = cp.GetCablePositions(cableId);
                if (raw == null || raw.Count == 0)
                    raw = cp.GetRawCablePositions(cableId);
                if (raw == null || raw.Count < 2) return null;
                var arr = new Vector3[raw.Count];
                for (int i = 0; i < raw.Count; i++) arr[i] = raw[i];
                return arr;
            }
            catch
            {
                return null;
            }
        }

        private void EnsureGhostCapacity(int mirrors)
        {
            if (_lines.Length == mirrors && _ghosts.Length == mirrors) return;
            ClearGhosts();
            _ghosts = new GameObject[mirrors];
            _lines = new LineRenderer[mirrors];
            for (int i = 0; i < mirrors; i++)
            {
                var go = new GameObject($"MultiCable_Mirror_{i}");
                DontDestroyOnLoad(go);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                try
                {
                    var mat = new Material(Shader.Find("Sprites/Default"));
                    lr.material = mat;
                }
                catch { /* renderer still draws with default material */ }
                _ghosts[i] = go;
                _lines[i] = lr;
            }
        }

        private void ClearGhosts()
        {
            for (int i = 0; i < _ghosts.Length; i++)
            {
                try { if (_ghosts[i] != null) Destroy(_ghosts[i]); } catch { /* best-effort */ }
            }
            _ghosts = new GameObject[0];
            _lines = new LineRenderer[0];
        }

        private void UpdateOffsetDirection(Vector3[] pts)
        {
            try
            {
                Vector3 a = pts[0];
                Vector3 b = pts[pts.Length - 1];
                Vector3 d = b - a;
                d.y = 0f;
                if (d.sqrMagnitude < 0.0001f) return;
                d.Normalize();
                _offsetDir = new Vector3(-d.z, 0f, d.x);
            }
            catch { /* keep previous */ }
        }

        private static float ResolveWidth()
        {
            try
            {
                var cp = CablePositions.instance;
                if (cp != null && cp.cableWidth > 0f) return cp.cableWidth * 0.9f;
            }
            catch { /* fall through */ }
            return 0.045f;
        }

        private static Color ParseColor(string raw, Color fallback)
        {
            try
            {
                string hex = (raw ?? "").Trim();
                if (!hex.StartsWith("#")) hex = "#" + hex;
                if (ColorUtility.TryParseHtmlString(hex, out Color c)) return c;
            }
            catch { /* fall through */ }
            return fallback;
        }

        // ── F8 panel ─────────────────────────────────────────────────────────

        private static void DrawPanel()
        {
            GUILayout.BeginVertical();

            bool f1 = MultiCableConfig.F1OwnsCoreSettings;
            GUILayout.Label(f1
                ? "Settings source: F1 gregCore config (panel is read-only)."
                : "Settings source: local preferences (no gregCore).",
                GUILayout.MaxWidth(400));

            bool enabled = MultiCableConfig.Enabled;
            int count = MultiCableConfig.CableCount;
            bool mirrors = MultiCableConfig.MirrorGhosts;
            bool auto = MultiCableConfig.AutoClone;

            if (f1)
            {
                GUILayout.Label($"Enabled: {(enabled ? "ON" : "OFF")}");
                GUILayout.Label($"Parallel cables: {count}");
                GUILayout.Label($"Mirror ghosts: {(mirrors ? "ON" : "OFF")}");
                GUILayout.Label($"Auto-clone: {(auto ? "ON" : "OFF")}");
                GUILayout.Label("Change these in F1 -> Mod Config -> gregMod.MultiCable.");
            }
            else
            {
                bool newEnabled = GUILayout.Toggle(enabled, "Enabled (master switch)");
                if (newEnabled != enabled) MultiCableConfig.SetEnabled(newEnabled);

                GUILayout.Label($"Parallel cables: {count}  (1 = vanilla)");
                float slider = GUILayout.HorizontalSlider(count, MultiCableConfig.MinCables, MultiCableConfig.MaxCables);
                int newCount = (int)Math.Round(slider);
                if (newCount != count) MultiCableConfig.SetCableCount(newCount);

                bool newMirrors = GUILayout.Toggle(mirrors, "Mirror ghosts while carrying");
                if (newMirrors != mirrors) MultiCableConfig.SetMirrorGhosts(newMirrors);

                bool newAuto = GUILayout.Toggle(auto, "Auto-create sibling cables on completion");
                if (newAuto != auto) MultiCableConfig.SetAutoClone(newAuto);
            }

            GUILayout.Space(6);
            GUILayout.Label("Status:", GUILayout.MaxWidth(400));
            string status = CarryState.IsTracking
                ? $"Carrying cable {CarryState.TrackedCableId} — {count - 1} mirror(s) following you."
                : "Idle.";
            if (CarryState.PendingJobs.Count > 0)
                status += $" {CarryState.PendingJobs.Count} clone job(s) queued.";
            GUILayout.Label(status, GUILayout.MaxWidth(400));
            GUILayout.Label($"Last: {CarryState.LastResult}", GUILayout.MaxWidth(400));

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Stop tracking (keeps cable)"))
            {
                CarryState.StopTracking("Tracking stopped by user — vanilla cable untouched.");
                MelonLogger.Msg("[MultiCable] Tracking stopped by user.");
            }
            if (GUILayout.Button("Close"))
                _panelVisible = false;
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.Label("Workflow: set count -> click start port -> walk & manage the cable -> " +
                "click end port. Siblings land on free ports of the same devices; " +
                "group them via vanilla LACP for redundancy.", GUILayout.MaxWidth(400));

            GUILayout.EndVertical();
        }

        internal static void RegisterIl2CppType()
        {
            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<MirrorGhostManager>();
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[MultiCable] Il2Cpp type registration failed: {ex.Message}");
            }
        }
    }
}
