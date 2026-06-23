using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Framework
{
    public class InputManager : IInputManager, IInitializable, IDisposable
    {
        private const string UI_MAP = "UI";
        private const string PLAYER_MAP = "Player";
        private const string CANCEL_ACTION = "Cancel";

        private readonly InputActionAsset _asset;
        private readonly IEventBus _eventBus;

        private InputActionMap _uiMap;
        private InputActionMap _playerMap;
        private InputAction _cancel;

        private InputActionMap _activeMap;
        private int _lockCount;
        private bool _initialized;

        public bool IsLocked => _lockCount > 0;

        public InputManager(InputActionAsset asset, IEventBus eventBus)
        {
            _asset = asset;
            _eventBus = eventBus;
        }

        public void Initialize()
        {
            if (_initialized) return;
            if (_asset == null)
            {
                Debug.LogError("[InputManager] InputActionAsset is null. Assign on RootCanvasSetting.InputActions.");
                return;
            }
            _initialized = true;

            _uiMap = _asset.FindActionMap(UI_MAP, throwIfNotFound: true);
            _playerMap = _asset.FindActionMap(PLAYER_MAP, throwIfNotFound: true);
            _cancel = _uiMap.FindAction(CANCEL_ACTION, throwIfNotFound: true);
            _cancel.performed += OnCancelPerformed;

            SetActive(_uiMap);
        }

        public void SwitchToUI() => SetActive(_uiMap);
        public void SwitchToGameplay() => SetActive(_playerMap);

        public IDisposable AcquireLock()
        {
            _lockCount++;
            if (_lockCount == 1) _activeMap?.Disable();
            return new LockToken(this);
        }

        private void ReleaseLock()
        {
            if (_lockCount == 0) return;
            _lockCount--;
            if (_lockCount == 0) _activeMap?.Enable();
        }

        private void SetActive(InputActionMap next)
        {
            if (_activeMap == next) return;

            if (_lockCount == 0)
            {
                _activeMap?.Disable();
                next?.Enable();
            }
            _activeMap = next;
        }

        private void OnCancelPerformed(InputAction.CallbackContext ctx)
            => _eventBus.Publish(new UICancelEvent());

        public void Dispose()
        {
            if (_cancel != null) _cancel.performed -= OnCancelPerformed;

            _uiMap?.Disable();
            _playerMap?.Disable();

            _cancel = null;
            _uiMap = null;
            _playerMap = null;
            _activeMap = null;
            _lockCount = 0;
            _initialized = false;
        }

        private sealed class LockToken : IDisposable
        {
            private InputManager _owner;

            public LockToken(InputManager owner) => _owner = owner;

            public void Dispose()
            {
                if (_owner == null) return;
                var owner = _owner;
                _owner = null;
                owner.ReleaseLock();
            }
        }
    }
}
