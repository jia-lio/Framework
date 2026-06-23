#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;

namespace Framework
{
    public readonly struct DebugLogEntry
    {
        public readonly LogType Type;
        public readonly string Condition;
        public readonly string StackTrace;
        public readonly DateTime Time;
        public readonly bool IsCustom;

        public DebugLogEntry(LogType type, string condition, string stackTrace, DateTime time, bool isCustom)
        {
            Type = type;
            Condition = condition;
            StackTrace = stackTrace;
            Time = time;
            IsCustom = isCustom;
        }
    }
}
#endif
