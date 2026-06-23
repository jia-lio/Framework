using System;

namespace Framework
{
    /// <summary>
    /// Main-thread-only generic pub/sub. Duplicate handler registrations are rejected
    /// (warning logged). Publish snapshots the handler list so subscribe/unsubscribe
    /// during dispatch is safe. Handler exceptions are isolated per-handler.
    /// </summary>
    public interface IEventBus
    {
        IDisposable Subscribe<T>(Action<T> handler);
        void Unsubscribe<T>(Action<T> handler);
        void Publish<T>(T evt);
    }
}
