using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    public sealed class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new();
        private static readonly IDisposable _noop = new NoopDisposable();

        public IDisposable Subscribe<T>(Action<T> handler)
        {
            if (handler == null)
            {
                Debug.LogWarning("[EventBus] Subscribe rejected: handler is null.");
                return _noop;
            }

            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>(4);
                _handlers[type] = list;
            }

            if (list.Contains(handler))
            {
                Debug.LogWarning($"[EventBus] Duplicate Subscribe<{type.Name}> ignored.");
                return _noop;
            }

            list.Add(handler);
            return new Subscription(this, type, handler);
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null) return;
            if (!_handlers.TryGetValue(typeof(T), out var list)) return;
            list.Remove(handler);
        }

        public void Publish<T>(T evt)
        {
            if (!_handlers.TryGetValue(typeof(T), out var list) || list.Count == 0)
                return;

            var count = list.Count;
            var snapshot = new Action<T>[count];
            for (int i = 0; i < count; i++)
                snapshot[i] = (Action<T>)list[i];

            for (int i = 0; i < count; i++)
            {
                try { snapshot[i].Invoke(evt); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        private void RemoveHandler(Type type, Delegate handler)
        {
            if (!_handlers.TryGetValue(type, out var list)) return;
            list.Remove(handler);
        }

        private sealed class Subscription : IDisposable
        {
            private EventBus _bus;
            private Delegate _handler;
            private readonly Type _type;
            private bool _disposed;

            public Subscription(EventBus bus, Type type, Delegate handler)
            {
                _bus = bus;
                _type = type;
                _handler = handler;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                if (_bus != null && _handler != null)
                    _bus.RemoveHandler(_type, _handler);
                _bus = null;
                _handler = null;
            }
        }

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}
