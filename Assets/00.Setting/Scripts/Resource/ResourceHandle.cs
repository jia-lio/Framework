using System;

namespace Framework
{
    public sealed class ResourceHandle<T> : IDisposable where T : UnityEngine.Object
    {
        private ResourceManager _owner;
        private string _key;

        public T Asset { get; private set; }

        internal ResourceHandle(ResourceManager owner, string key, T asset)
        {
            _owner = owner;
            _key = key;
            Asset = asset;
        }

        public void Dispose()
        {
            if (_owner == null) return;
            _owner.ReleaseInternal(_key);
            _owner = null;
            _key = null;
            Asset = null;
        }
    }
}
