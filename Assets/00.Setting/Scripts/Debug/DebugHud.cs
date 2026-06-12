#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using VContainer;

namespace Framework
{
    public sealed class DebugHud : MonoBehaviour
    {
        private const int FPS_SAMPLE_FRAMES = 30;
        private const float HUD_WIDTH = 320f;
        private const float HUD_PAD = 8f;

        [Inject] private DebugManager _debug;
        [Inject] private UIManager _ui;
        [Inject] private AudioManager _audio;
        [Inject] private IPauseService _pause;
        [Inject] private IInputManager _input;

        private float _fps;
        private float _fpsAccum;
        private int _fpsFrames;
        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;

        private void Update()
        {
            var dt = Time.unscaledDeltaTime;
            if (dt <= 0f) return;
            _fpsAccum += 1f / dt;
            _fpsFrames++;
            if (_fpsFrames >= FPS_SAMPLE_FRAMES)
            {
                _fps = _fpsAccum / _fpsFrames;
                _fpsAccum = 0f;
                _fpsFrames = 0;
            }
        }

        private void OnGUI()
        {
            if (_debug == null || !_debug.HudVisible) return;

            EnsureStyles();

            var rect = new Rect(HUD_PAD, HUD_PAD, HUD_WIDTH, Screen.height - HUD_PAD * 2f);
            GUILayout.BeginArea(rect, _boxStyle);

            DrawStats();

            for (int i = 0; i < _debug.Panels.Count; i++)
            {
                GUILayout.Space(6f);
                GUILayout.Label($"-- {_debug.Panels[i].Title} --", _labelStyle);
                _debug.Panels[i].Draw();
            }

            GUILayout.EndArea();
        }

        private void DrawStats()
        {
            GUILayout.Label($"FPS: {_fps:0.0}", _labelStyle);
            GUILayout.Label($"Mono: {Profiler.GetMonoUsedSizeLong() / (1024f * 1024f):0.0} MB", _labelStyle);
            GUILayout.Label($"Total: {Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f):0.0} MB", _labelStyle);
            GUILayout.Label($"Scene: {SceneManager.GetActiveScene().name}", _labelStyle);

            if (_ui != null)
                GUILayout.Label($"Popups: {_ui.PopupCount} (Has={_ui.HasPopup})", _labelStyle);
            if (_audio != null)
                GUILayout.Label($"Vol M/B/S: {_audio.MasterVolume:0.00} / {_audio.BgmVolume:0.00} / {_audio.SfxVolume:0.00}", _labelStyle);
            if (_pause != null)
                GUILayout.Label($"Paused: {_pause.IsPaused} (cnt={_pause.PauseCount})", _labelStyle);
            if (_input != null)
                GUILayout.Label($"InputLocked: {_input.IsLocked}", _labelStyle);
        }

        private void EnsureStyles()
        {
            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft };
            }
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    wordWrap = true,
                    alignment = TextAnchor.UpperLeft
                };
                _labelStyle.normal.textColor = Color.white;
            }
        }
    }
}
#endif
