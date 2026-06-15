using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Framework
{
    public class ObjectPoolManager : IDisposable
    {
        private readonly IObjectResolver _resolver;
        private readonly ResourceManager _resources;
        private readonly Dictionary<string, Queue<GameObject>> _pools = new();
        private readonly Dictionary<string, ResourceHandle<GameObject>> _prefabHandles = new();
        private readonly Dictionary<GameObject, string> _instanceToKey = new();

        private Transform _poolRoot;

        public ObjectPoolManager(IObjectResolver resolver, ResourceManager resources)
        {
            _resolver = resolver;
            _resources = resources;
        }

        public void Initialize(Transform root)
        {
            _poolRoot = root;
        }

        public async UniTask Preload(string key, int count)
        {
            var prefab = await LoadPrefab(key);

            var queue = GetOrCreateQueue(key);
            for (var i = 0; i < count; i++)
                queue.Enqueue(CreateInstance(key, prefab));
        }

        public async UniTask<T> Spawn<T>(string key, Transform parent = null) where T : Component
        {
            var queue = GetOrCreateQueue(key);

            GameObject go = null;
            while (queue.Count > 0)
            {
                var candidate = queue.Dequeue();
                if (candidate != null)
                {
                    go = candidate;
                    break;
                }
                if (!ReferenceEquals(candidate, null))
                    _instanceToKey.Remove(candidate);
            }

            if (go == null)
                go = CreateInstance(key, await LoadPrefab(key));

            var component = go.GetComponent<T>();
            if (component == null)
            {
                GetOrCreateQueue(key).Enqueue(go);
                throw new InvalidOperationException(
                    $"[ObjectPoolManager] Prefab '{key}' does not contain component '{typeof(T).Name}'.");
            }

            if (parent != null)
                go.transform.SetParent(parent);

            go.SetActive(true);
            go.GetComponent<IPoolable>()?.OnSpawn();

            return component;
        }

        public void Despawn(GameObject go)
        {
            if (go == null) return;

            if (!_instanceToKey.TryGetValue(go, out var key))
            {
                Debug.LogWarning($"[ObjectPoolManager] Despawn called on untracked GameObject '{go.name}'.");
                return;
            }

            go.GetComponent<IPoolable>()?.OnDespawn();
            go.SetActive(false);
            go.transform.SetParent(_poolRoot);

            GetOrCreateQueue(key).Enqueue(go);
        }

        public void ReleaseAll()
        {
            _pools.Clear();

            foreach (var kvp in _instanceToKey)
            {
                var go = kvp.Key;
                if (go == null) continue;

                go.GetComponent<IPoolable>()?.OnDespawn();
                UnityEngine.Object.Destroy(go);
            }
            _instanceToKey.Clear();

            foreach (var kvp in _prefabHandles)
                kvp.Value.Dispose();
            _prefabHandles.Clear();
        }

        public void Dispose() => ReleaseAll();

        private Queue<GameObject> GetOrCreateQueue(string key)
        {
            if (!_pools.TryGetValue(key, out var queue))
            {
                queue = new Queue<GameObject>();
                _pools[key] = queue;
            }
            return queue;
        }

        private GameObject CreateInstance(string key, GameObject prefab)
        {
            if (_poolRoot == null)
                throw new InvalidOperationException("[ObjectPoolManager] Initialize() must be called before use.");

            var go = UnityEngine.Object.Instantiate(prefab, _poolRoot);
            go.SetActive(false);
            _resolver.InjectGameObject(go);
            _instanceToKey[go] = key;
            return go;
        }

        private async UniTask<GameObject> LoadPrefab(string key)
        {
            if (_prefabHandles.TryGetValue(key, out var existing))
                return existing.Asset;

            var handle = await _resources.Load<GameObject>(key);
            if (_prefabHandles.TryGetValue(key, out var raced))
            {
                handle.Dispose();
                return raced.Asset;
            }
            _prefabHandles[key] = handle;
            return handle.Asset;
        }
    }
}
