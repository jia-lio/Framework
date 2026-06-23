#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    public sealed class DebugLogCapture : IDisposable
    {
        public const string CUSTOM_PREFIX = "[Custom]";

        private const int CAPACITY = 512;

        private readonly Queue<DebugLogEntry> _buffer = new Queue<DebugLogEntry>(CAPACITY);
        private readonly object _lock = new object();
        private bool _started;

        public void Start()
        {
            if (_started) return;
            Application.logMessageReceivedThreaded += Handle;
            _started = true;
        }

        public void Dispose()
        {
            if (!_started) return;
            Application.logMessageReceivedThreaded -= Handle;
            _started = false;
        }

        private void Handle(string condition, string stackTrace, LogType type)
        {
            var isCustom = condition != null && condition.StartsWith(CUSTOM_PREFIX, StringComparison.Ordinal);
            if (isCustom)
                condition = condition.Substring(CUSTOM_PREFIX.Length).TrimStart();

            var entry = new DebugLogEntry(type, condition, stackTrace, DateTime.UtcNow, isCustom);
            lock (_lock)
            {
                if (_buffer.Count >= CAPACITY) _buffer.Dequeue();
                _buffer.Enqueue(entry);
            }
        }

        public List<DebugLogEntry> Snapshot(LogTypeMask filter, int max)
        {
            if (max <= 0) return new List<DebugLogEntry>(0);
            var result = new List<DebugLogEntry>(Math.Min(max, CAPACITY));
            lock (_lock)
            {
                foreach (var e in _buffer)
                {
                    if (!filter.Contains(e.Type)) continue;
                    result.Add(e);
                }
            }
            if (result.Count > max) result.RemoveRange(0, result.Count - max);
            return result;
        }
    }
}
#endif
