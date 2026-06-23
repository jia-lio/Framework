#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;

namespace Framework
{
    [Flags]
    public enum LogTypeMask
    {
        None      = 0,
        Log       = 1 << 0,
        Warning   = 1 << 1,
        Error     = 1 << 2,
        Exception = 1 << 3,
        Assert    = 1 << 4,
        All       = ~0
    }

    public static class LogTypeMaskExtensions
    {
        public static bool Contains(this LogTypeMask mask, LogType type)
        {
            switch (type)
            {
                case LogType.Log:       return (mask & LogTypeMask.Log) != 0;
                case LogType.Warning:   return (mask & LogTypeMask.Warning) != 0;
                case LogType.Error:     return (mask & LogTypeMask.Error) != 0;
                case LogType.Exception: return (mask & LogTypeMask.Exception) != 0;
                case LogType.Assert:    return (mask & LogTypeMask.Assert) != 0;
                default:                return false;
            }
        }
    }
}
#endif
