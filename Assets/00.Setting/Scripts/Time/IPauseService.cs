using System;

namespace Framework
{
    public interface IPauseService
    {
        bool IsPaused { get; }
        int PauseCount { get; }
        IDisposable AcquirePause();
    }
}
