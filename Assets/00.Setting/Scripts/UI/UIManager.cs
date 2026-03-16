using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer;
using VContainer.Unity;

namespace Framework
{
    public class UIManager
    {
        private readonly IObjectResolver _resolver;
        private readonly Dictionary<string, GameObject> _cache = new();
        private readonly Dictionary<string, AsyncOperationHandle<GameObject>> _handles = new();
        private readonly Stack<PopupView> _popupStack = new();
        private readonly HashSet<string> _loading = new();
        private readonly HashSet<string> _failed = new();

        private Transform _uiRoot;
        private bool _isClosing;
        private bool _isOpening;

        public int PopupCount => _popupStack.Count;
        public bool HasPopup => _popupStack.Count > 0;

        public UIManager(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        public void Initialize(Transform root)
        {
            _uiRoot = root;
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
            if (!_cache.TryGetValue(key, out var go))
            {
                if (_loading.Contains(key))
                {
                    await UniTask.WaitUntil(() => _cache.ContainsKey(key) || _failed.Contains(key));
                    if (_failed.Contains(key))
                        throw new InvalidOperationException($"[UIManager] Failed to load '{key}'.");
                    go = _cache[key];
                }
                else
                {
                    _failed.Remove(key);
                    _loading.Add(key);
                    try
                    {
                        var handle = Addressables.LoadAssetAsync<GameObject>(key);
                        var prefab = await handle;
                        go = UnityEngine.Object.Instantiate(prefab, _uiRoot);
                        go.name = key;
                        go.SetActive(false);
                        _resolver.InjectGameObject(go);
                        _cache[key] = go;
                        _handles[key] = handle;
                    }
                    catch
                    {
                        _failed.Add(key);
                        throw;
                    }
                    finally
                    {
                        _loading.Remove(key);
                    }
                }
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
                popup.CanvasGroup.interactable = false;
                popup.OnExit();
                await popup.OnHide();
                popup.gameObject.SetActive(false);

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
                popup.CanvasGroup.interactable = false;
                popup.OnExit();
                await popup.OnHide();
                popup.gameObject.SetActive(false);
            }
        }

        public void ReleaseAll()
        {
            _popupStack.Clear();
            _loading.Clear();
            _failed.Clear();
            _isOpening = false;
            _isClosing = false;

            foreach (var kvp in _cache)
                if (kvp.Value != null)
                    UnityEngine.Object.Destroy(kvp.Value);
            _cache.Clear();

            foreach (var kvp in _handles)
                Addressables.Release(kvp.Value);
            _handles.Clear();
        }
    }
}
