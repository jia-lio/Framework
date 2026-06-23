#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Framework
{
    public sealed class DebugLogPanel : MonoBehaviour
    {
        private const int LOG_DISPLAY_MAX = 100;
        private const int TAB_ALL = 0;
        private const int TAB_CUSTOM = 1;
        private const int WINDOW_ID = 0x0DEB0106;
        private const float DEFAULT_W = 520f;
        private const float DEFAULT_H = 360f;
        private const float LOG_VIEW_HEIGHT = 240f;
        private const string POS_X_KEY = "DEBUG_LogPanelX";
        private const string POS_Y_KEY = "DEBUG_LogPanelY";

        private static readonly string[] TAB_LABELS = { "All", "Custom" };

        [Inject] private DebugManager _debug;

        private Rect _window;
        private Vector2 _scroll;
        private GUIStyle _labelStyle;
        private GUIStyle _entryStyle;
        private bool _initialized;
        private int _tab = TAB_ALL;
        private string _search = string.Empty;
        private bool _collapse;
        private readonly List<(DebugLogEntry Entry, int Count)> _collapsed = new List<(DebugLogEntry, int)>(LOG_DISPLAY_MAX);

        private void OnGUI()
        {
            if (_debug == null || !_debug.LogPanelVisible) return;

            EnsureInit();
            EnsureStyles();

            _window = GUILayout.Window(WINDOW_ID, _window, DrawWindow, "Debug Logs (F9)");
        }

        private void OnDisable()
        {
            if (!_initialized) return;
            PlayerPrefs.SetFloat(POS_X_KEY, _window.x);
            PlayerPrefs.SetFloat(POS_Y_KEY, _window.y);
        }

        private void EnsureInit()
        {
            if (_initialized) return;
            var x = PlayerPrefs.GetFloat(POS_X_KEY, Screen.width - DEFAULT_W - 16f);
            var y = PlayerPrefs.GetFloat(POS_Y_KEY, 16f);
            _window = new Rect(x, y, DEFAULT_W, DEFAULT_H);
            _initialized = true;
        }

        private void DrawWindow(int id)
        {
            DrawTabs();
            DrawFilters();
            DrawSearch();
            GUILayout.Space(4f);
            DrawLogs();
            GUI.DragWindow(new Rect(0, 0, _window.width, 20f));
        }

        private void DrawTabs()
        {
            GUILayout.BeginHorizontal();
            _tab = GUILayout.Toolbar(_tab, TAB_LABELS, GUILayout.Width(160f));
            GUILayout.FlexibleSpace();
            _collapse = GUILayout.Toggle(_collapse, "Collapse", GUILayout.Width(80f));
            GUILayout.EndHorizontal();
            GUILayout.Space(4f);
        }

        private void DrawSearch()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Search:", _labelStyle, GUILayout.Width(56f));
            _search = GUILayout.TextField(_search);
            if (GUILayout.Button("X", GUILayout.Width(24f)))
            {
                _search = string.Empty;
                GUI.FocusControl(null);
            }
            GUILayout.EndHorizontal();
        }

        private void DrawFilters()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Filter:", _labelStyle, GUILayout.Width(48f));
            DrawFilterToggle("Log",  LogTypeMask.Log);
            DrawFilterToggle("Warn", LogTypeMask.Warning);
            DrawFilterToggle("Err",  LogTypeMask.Error);
            DrawFilterToggle("Exc",  LogTypeMask.Exception);
            DrawFilterToggle("Asrt", LogTypeMask.Assert);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("All", GUILayout.Width(40f))) _debug.LogFilter = LogTypeMask.All;
            if (GUILayout.Button("None", GUILayout.Width(50f))) _debug.LogFilter = LogTypeMask.None;
            GUILayout.EndHorizontal();
        }

        private void DrawFilterToggle(string label, LogTypeMask flag)
        {
            var current = (_debug.LogFilter & flag) != 0;
            var next = GUILayout.Toggle(current, label, GUILayout.Width(56f));
            if (next == current) return;
            _debug.LogFilter = next
                ? (_debug.LogFilter | flag)
                : (_debug.LogFilter & ~flag);
        }

        private void DrawLogs()
        {
            var entries = _debug.LogCapture.Snapshot(_debug.LogFilter, int.MaxValue);

            if (_tab == TAB_CUSTOM)
                entries.RemoveAll(e => !e.IsCustom);

            if (!string.IsNullOrEmpty(_search))
                entries.RemoveAll(e => e.Condition == null
                    || e.Condition.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0);

            Collapse(entries);

            if (_collapsed.Count > LOG_DISPLAY_MAX)
                _collapsed.RemoveRange(0, _collapsed.Count - LOG_DISPLAY_MAX);

            GUILayout.Label($"Entries: {_collapsed.Count} (max {LOG_DISPLAY_MAX})", _labelStyle);
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(LOG_VIEW_HEIGHT));
            for (int i = 0; i < _collapsed.Count; i++)
            {
                var (e, count) = _collapsed[i];
                _entryStyle.normal.textColor = ColorFor(e.Type);
                var suffix = count > 1 ? $" (x{count})" : string.Empty;
                GUILayout.Label($"[{e.Time:HH:mm:ss}][{e.Type}] {e.Condition}{suffix}", _entryStyle);
            }
            GUILayout.EndScrollView();
        }

        private void Collapse(List<DebugLogEntry> entries)
        {
            _collapsed.Clear();
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (_collapse && _collapsed.Count > 0)
                {
                    var last = _collapsed[_collapsed.Count - 1];
                    if (last.Entry.Type == e.Type && last.Entry.Condition == e.Condition)
                    {
                        _collapsed[_collapsed.Count - 1] = (e, last.Count + 1);
                        continue;
                    }
                }
                _collapsed.Add((e, 1));
            }
        }

        private static Color ColorFor(LogType type)
        {
            switch (type)
            {
                case LogType.Warning:   return new Color(1f, 0.85f, 0.3f);
                case LogType.Error:     return new Color(1f, 0.4f, 0.4f);
                case LogType.Exception: return new Color(1f, 0.3f, 0.6f);
                case LogType.Assert:    return new Color(1f, 0.5f, 0.5f);
                default:                return Color.white;
            }
        }

        private void EnsureStyles()
        {
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleLeft
                };
                _labelStyle.normal.textColor = Color.white;
            }
            if (_entryStyle == null)
            {
                _entryStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    wordWrap = true,
                    alignment = TextAnchor.UpperLeft
                };
            }
        }
    }
}
#endif
