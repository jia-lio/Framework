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

            // ct는 의도적으로 내부 Load에 전달하지 않는다 — 로드를 끝까지 완료시켜 유효 entry(refcount=1)를
            // 만든 뒤 아래에서 handle.Dispose()로 0까지 내려 회수하기 위함. ct를 넘기면 로드가 entry 생성 전
            // OCE로 중단돼 refcount 회계가 깨진다(취소 시 retention 방지 보장 무효).
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
