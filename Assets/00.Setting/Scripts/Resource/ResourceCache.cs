using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Framework
{
    /// <summary>
    /// 한 소유자(UIManager/ObjectPoolManager 등)가 키별로 <see cref="ResourceHandle{T}"/>를
    /// 하나씩 보관하는 캐시. 동일 키 중복 로드를 단일 핸들로 합치고,
    /// <see cref="ReleaseAll"/>/<see cref="Dispose"/> 시 보유 핸들을 일괄 해제한다.
    /// ResourceManager의 ref counting과는 별개 계층 — 소유자 단위 lifecycle을 담당한다.
    /// 메인스레드 전용. ReleaseAll 후 재사용(재로드) 가능.
    /// </summary>
    public sealed class ResourceCache<T> : IDisposable where T : UnityEngine.Object
    {
        private readonly ResourceManager _resources;
        private readonly Dictionary<string, ResourceHandle<T>> _handles = new();

        public ResourceCache(ResourceManager resources)
        {
            _resources = resources;
        }

        public async UniTask<T> Load(string key, CancellationToken ct = default)
        {
            if (_handles.TryGetValue(key, out var existing))
                return existing.Asset;

            var handle = await _resources.Load<T>(key);

            // await 도중 owner의 ReleaseAll/취소가 발생했으면(이 시점 _handles에는 key 미등록 상태)
            // 방금 획득한 핸들을 즉시 해제해 refcount 잔존(메모리 retention)을 막고 OCE 전파.
            if (ct.IsCancellationRequested)
            {
                handle.Dispose();
                ct.ThrowIfCancellationRequested();
            }

            // await 도중 다른 호출이 같은 키를 먼저 등록했으면 중복 핸들 폐기.
            if (_handles.TryGetValue(key, out var raced))
            {
                handle.Dispose();
                return raced.Asset;
            }

            _handles[key] = handle;
            return handle.Asset;
        }

        public void ReleaseAll()
        {
            foreach (var kvp in _handles)
                kvp.Value.Dispose();
            _handles.Clear();
        }

        public void Dispose() => ReleaseAll();
    }
}
