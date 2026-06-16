using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Framework
{
    public class ObjectPoolManager : IDisposable
    {
        private readonly IObjectResolver _resolver;
        private readonly ResourceCache<GameObject> _prefabs;
        private readonly Dictionary<string, Queue<GameObject>> _pools = new();
        private readonly Dictionary<GameObject, string> _instanceToKey = new();

        private Transform _poolRoot;
        private CancellationTokenSource _cts = new();

        public ObjectPoolManager(IObjectResolver resolver, ResourceManager resources)
        {
            _resolver = resolver;
            _prefabs = new ResourceCache<GameObject>(resources);
        }

        public void Initialize(Transform root)
        {
            _poolRoot = root;
        }

        /// <summary>
        /// 풀을 미리 채운다. 로드 중 <see cref="ReleaseAll"/>(씬 전환 등)가 호출되면
        /// <see cref="OperationCanceledException"/>을 던진다 — 호출자는 필요 시 catch.
        /// </summary>
        public async UniTask Preload(string key, int count)
        {
            // 로드 중 ReleaseAll(씬 전환 등) 발생 시 ResourceCache가 핸들을 즉시 해제하고 OCE를 던진다.
            var prefab = await _prefabs.Load(key, _cts.Token);

            var queue = GetOrCreateQueue(key);
            for (var i = 0; i < count; i++)
                queue.Enqueue(CreateInstance(key, prefab));
        }

        /// <summary>
        /// 풀에서 인스턴스를 꺼낸다(없으면 로드·생성). 로드 중 <see cref="ReleaseAll"/>(씬 전환 등)가
        /// 호출되면 <see cref="OperationCanceledException"/>을 던진다 — 호출자는 필요 시 catch.
        /// </summary>
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
            {
                // 로드 중 ReleaseAll(씬 전환 등) 발생 시 ResourceCache가 핸들을 즉시 해제하고 OCE를 던진다 — orphan 인스턴스 방지.
                var prefab = await _prefabs.Load(key, _cts.Token);
                go = CreateInstance(key, prefab);
            }

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
            // in-flight Spawn/Preload의 await 재개를 취소 (재사용 위해 새 토큰 발급)
            _cts.Cancel();
            _cts.Dispose();
            _cts = new CancellationTokenSource();

            _pools.Clear();

            foreach (var kvp in _instanceToKey)
            {
                var go = kvp.Key;
                if (go == null) continue;

                go.GetComponent<IPoolable>()?.OnDespawn();
                UnityEngine.Object.Destroy(go);
            }
            _instanceToKey.Clear();

            _prefabs.ReleaseAll();
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
    }
}
