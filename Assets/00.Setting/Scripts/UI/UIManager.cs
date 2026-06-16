using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Framework
{
    public class UIManager : IDisposable
    {
        private readonly IObjectResolver _resolver;
        private readonly IEventBus _eventBus;
        private readonly ResourceCache<GameObject> _prefabs;
        private readonly Dictionary<string, GameObject> _instances = new();
        private readonly Stack<PopupView> _popupStack = new();

        private Transform _uiRoot;
        private IDisposable _cancelSub;
        private bool _isClosing;
        private bool _isOpening;

        public int PopupCount => _popupStack.Count;
        public bool HasPopup => _popupStack.Count > 0;

        public UIManager(IObjectResolver resolver, IEventBus eventBus, ResourceManager resources)
        {
            _resolver = resolver;
            _eventBus = eventBus;
            _prefabs = new ResourceCache<GameObject>(resources);
        }

        public void Initialize(Transform root)
        {
            _uiRoot = root;
            _cancelSub?.Dispose();
            _cancelSub = _eventBus.Subscribe<UICancelEvent>(_ => Back().Forget());
        }

        public async UniTask<T> Open<T>(string key, Action<T> setup = null) where T : PopupView
        {
            if (_isOpening)
            {
                Debug.LogWarning($"[UIManager] Open<{typeof(T).Name}>('{key}') rejected: another open is in progress.");
                return null;
            }
            _isOpening = true;

            try
            {
                return await OpenInternal<T>(key, setup);
            }
            finally
            {
                _isOpening = false;
            }
        }

        private async UniTask<T> OpenInternal<T>(string key, Action<T> setup) where T : PopupView
        {
            if (!_instances.TryGetValue(key, out var go))
            {
                var prefab = await _prefabs.Load(key);
                go = UnityEngine.Object.Instantiate(prefab, _uiRoot);
                go.name = key;
                go.SetActive(false);
                _resolver.InjectGameObject(go);
                _instances[key] = go;
            }

            var popup = go.GetComponent<T>();
            if (popup == null)
                throw new InvalidOperationException(
                    $"[UIManager] Prefab '{key}' does not contain component '{typeof(T).Name}'.");

            if (_popupStack.TryPeek(out var top) && top == popup)
                return popup;

            go.SetActive(true);

            if (_popupStack.TryPeek(out var current) && current != null)
                current.CanvasGroup.interactable = false;

            _popupStack.Push(popup);
            popup.CanvasGroup.interactable = true;

            setup?.Invoke(popup);

            await popup.OnShow();
            popup.OnEnter();

            return popup;
        }

        public async UniTask Back()
        {
            if (_popupStack.Count == 0 || _isClosing) return;
            _isClosing = true;

            try
            {
                var popup = _popupStack.Pop();
                await ClosePopup(popup);

                if (_popupStack.TryPeek(out var next) && next != null)
                    next.CanvasGroup.interactable = true;
            }
            finally
            {
                _isClosing = false;
            }
        }

        public async UniTask CloseAll()
        {
            while (_popupStack.Count > 0)
            {
                var popup = _popupStack.Pop();
                if (popup == null) continue;
                await ClosePopup(popup);
            }
        }

        private async UniTask ClosePopup(PopupView popup)
        {
            popup.CanvasGroup.interactable = false;
            popup.OnExit();
            await popup.OnHide();
            popup.gameObject.SetActive(false);

            _eventBus.Publish(new PopupClosedEvent(popup));
        }

        public void ReleaseAll()
        {
            _cancelSub?.Dispose();
            _cancelSub = null;
            _popupStack.Clear();
            _isOpening = false;
            _isClosing = false;

            foreach (var kvp in _instances)
                if (kvp.Value != null)
                    UnityEngine.Object.Destroy(kvp.Value);
            _instances.Clear();

            _prefabs.ReleaseAll();
        }

        public void Dispose() => ReleaseAll();
    }
}
