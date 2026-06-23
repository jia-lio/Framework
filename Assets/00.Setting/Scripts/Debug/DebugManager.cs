#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Framework
{
    public sealed class DebugManager : IInitializable, IDisposable
    {
        private const string HUD_VISIBLE_KEY = "DEBUG_HudVisible";
        private const string LOG_PANEL_VISIBLE_KEY = "DEBUG_LogPanelVisible";
        private const string LOG_FILTER_KEY = "DEBUG_LogFilter";

        private readonly DebugLogCapture _logCapture;
        private readonly List<IDebugPanel> _panels = new List<IDebugPanel>();
        private InputAction _hudToggleAction;
        private InputAction _logToggleAction;
        private bool _hudVisible;
        private bool _logPanelVisible;
        private LogTypeMask _logFilter = LogTypeMask.All;

        public DebugLogCapture LogCapture => _logCapture;
        public IReadOnlyList<IDebugPanel> Panels => _panels;

        public bool HudVisible
        {
            get => _hudVisible;
            private set
            {
                _hudVisible = value;
                PlayerPrefs.SetInt(HUD_VISIBLE_KEY, _hudVisible ? 1 : 0);
            }
        }

        public bool LogPanelVisible
        {
            get => _logPanelVisible;
            private set
            {
                _logPanelVisible = value;
                PlayerPrefs.SetInt(LOG_PANEL_VISIBLE_KEY, _logPanelVisible ? 1 : 0);
            }
        }

        public LogTypeMask LogFilter
        {
            get => _logFilter;
            set
            {
                _logFilter = value;
                PlayerPrefs.SetInt(LOG_FILTER_KEY, (int)_logFilter);
            }
        }

        public DebugManager(DebugLogCapture logCapture)
        {
            _logCapture = logCapture;
        }

        public void Initialize()
        {
            _hudVisible = PlayerPrefs.GetInt(HUD_VISIBLE_KEY, 0) == 1;
            _logPanelVisible = PlayerPrefs.GetInt(LOG_PANEL_VISIBLE_KEY, 0) == 1;
            _logFilter = (LogTypeMask)PlayerPrefs.GetInt(LOG_FILTER_KEY, (int)LogTypeMask.All);

            _logCapture.Start();

            _hudToggleAction = new InputAction("DebugHudToggle", InputActionType.Button, "<Keyboard>/f8");
            _hudToggleAction.performed += OnHudTogglePerformed;
            _hudToggleAction.Enable();

            _logToggleAction = new InputAction("DebugLogToggle", InputActionType.Button, "<Keyboard>/f9");
            _logToggleAction.performed += OnLogTogglePerformed;
            _logToggleAction.Enable();

            Debug.Log("[DebugManager] Initialized (F8 HUD, F9 LogPanel).");
        }

        public void Dispose()
        {
            if (_hudToggleAction != null)
            {
                _hudToggleAction.performed -= OnHudTogglePerformed;
                _hudToggleAction.Disable();
                _hudToggleAction.Dispose();
                _hudToggleAction = null;
            }
            if (_logToggleAction != null)
            {
                _logToggleAction.performed -= OnLogTogglePerformed;
                _logToggleAction.Disable();
                _logToggleAction.Dispose();
                _logToggleAction = null;
            }
            _logCapture.Dispose();
            _panels.Clear();
            PlayerPrefs.Save();
        }

        public void ToggleHud() => HudVisible = !_hudVisible;
        public void ToggleLogPanel() => LogPanelVisible = !_logPanelVisible;

        public IDisposable RegisterPanel(IDebugPanel panel)
        {
            if (panel == null) return NoopDisposable.Instance;
            if (_panels.Contains(panel))
            {
                Debug.LogWarning($"[DebugManager] Duplicate RegisterPanel('{panel.Title}') ignored.");
                return NoopDisposable.Instance;
            }
            _panels.Add(panel);
            return new PanelToken(this, panel);
        }

        private void OnHudTogglePerformed(InputAction.CallbackContext _) => ToggleHud();
        private void OnLogTogglePerformed(InputAction.CallbackContext _) => ToggleLogPanel();

        private sealed class PanelToken : IDisposable
        {
            private DebugManager _owner;
            private IDebugPanel _panel;

            public PanelToken(DebugManager owner, IDebugPanel panel)
            {
                _owner = owner;
                _panel = panel;
            }

            public void Dispose()
            {
                if (_owner != null && _panel != null)
                    _owner._panels.Remove(_panel);
                _owner = null;
                _panel = null;
            }
        }

        private sealed class NoopDisposable : IDisposable
        {
            public static readonly NoopDisposable Instance = new NoopDisposable();
            public void Dispose() { }
        }
    }
}
#endif
