using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Framework
{
    /// <summary>
    /// Addressable 에셋 핸들 lifecycle 일원화. ref counting + single-flight.
    /// 메인스레드 전용.
    /// </summary>
    public sealed class ResourceManager : IDisposable
    {
        private sealed class Entry
        {
            public AsyncOperationHandle Handle;
            public int RefCount;
        }

        private readonly Dictionary<string, Entry> _entries = new();
        private bool _disposed;

        public int LoadedCount => _entries.Count;

        public async UniTask<ResourceHandle<T>> Load<T>(string key) where T : UnityEngine.Object
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ResourceManager));

            if (_entries.TryGetValue(key, out var entry))
            {
                entry.RefCount++;
                try
                {
                    if (!entry.Handle.IsValid())
                        throw new InvalidOperationException($"[ResourceManager] '{key}' was released during load.");
                    if (!entry.Handle.IsDone)
                        await entry.Handle.ToUniTask();
                    if (!entry.Handle.IsValid() || entry.Handle.Status != AsyncOperationStatus.Succeeded)
                        throw new InvalidOperationException($"[ResourceManager] Load('{key}') failed.");
                    if (entry.Handle.Result is not T typed)
                        throw new InvalidOperationException($"[ResourceManager] '{key}' is not {typeof(T).Name}.");
                    return new ResourceHandle<T>(this, key, typed);
                }
                catch
                {
                    ReleaseInternal(key);
                    throw;
                }
            }

            var handle = Addressables.LoadAssetAsync<T>(key);
            _entries[key] = new Entry { Handle = handle, RefCount = 1 };
            try
            {
                var asset = await handle;
                if (asset == null)
                    throw new InvalidOperationException($"[ResourceManager] Load('{key}') returned null.");
                return new ResourceHandle<T>(this, key, asset);
            }
            catch
            {
                ReleaseInternal(key);
                throw;
            }
        }

        internal void ReleaseInternal(string key)
        {
            if (_disposed) return;
            if (!_entries.TryGetValue(key, out var entry)) return;
            entry.RefCount--;
            if (entry.RefCount > 0) return;
            _entries.Remove(key);
            if (entry.Handle.IsValid()) Addressables.Release(entry.Handle);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_entries.Count > 0)
                Debug.LogWarning($"[ResourceManager] Dispose with {_entries.Count} live entries (leak?).");
            foreach (var kvp in _entries)
            {
                // 앱 종료 시 Addressables teardown 순서에 따라 Release가 실패할 수 있음
                try
                {
                    if (kvp.Value.Handle.IsValid()) Addressables.Release(kvp.Value.Handle);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ResourceManager] Release on dispose failed for '{kvp.Key}': {e.Message}");
                }
            }
            _entries.Clear();
        }
    }
}
