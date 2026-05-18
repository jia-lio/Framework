using System;

namespace Framework
{
    public interface IInputManager
    {
        bool IsLocked { get; }

        void SwitchToUI();
        void SwitchToGameplay();

        IDisposable AcquireLock();
    }
}
