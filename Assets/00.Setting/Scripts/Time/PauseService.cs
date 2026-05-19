using System;
using UnityEngine;

namespace Framework
{
    public sealed class PauseService : IPauseService, IDisposable
    {
        private readonly IEventBus _eventBus;
        private int _pauseCount;
        private bool _disposed;

        public bool IsPaused => _pauseCount > 0;
        public int PauseCount => _pauseCount;

        public PauseService(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public IDisposable AcquirePause()
        {
            if (_disposed)
            {
                Debug.LogWarning("[PauseService] AcquirePause called after Dispose. Returning no-op token.");
                return NoopToken.Instance;
            }

            _pauseCount++;
            if (_pauseCount == 1)
            {
                Time.timeScale = 0f;
                _eventBus.Publish(new GamePausedEvent());
            }
            return new PauseToken(this);
        }

        private void ReleasePause()
        {
            if (_disposed) return;
            if (_pauseCount == 0) return;

            _pauseCount--;
            if (_pauseCount == 0)
            {
                Time.timeScale = 1f;
                _eventBus.Publish(new GameResumedEvent());
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _pauseCount = 0;
            Time.timeScale = 1f;
        }

        private sealed class PauseToken : IDisposable
        {
            private PauseService _owner;

            public PauseToken(PauseService owner) => _owner = owner;

            public void Dispose()
            {
                if (_owner == null) return;
                var owner = _owner;
                _owner = null;
                owner.ReleasePause();
            }
        }

        private sealed class NoopToken : IDisposable
        {
            public static readonly NoopToken Instance = new NoopToken();
            public void Dispose() { }
        }
    }
}
