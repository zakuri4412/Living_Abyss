using System;
using System.Collections.Generic;

namespace LivingAbyss.Systems
{
    // Simple typed pub/sub — no direct coupling between systems (GDD §14.2)
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _handlers = new();

        public static void Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                _handlers[type] = list;
            }
            if (!list.Contains(handler))
                list.Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (_handlers.TryGetValue(typeof(T), out var list))
                list.Remove(handler);
        }

        public static void Publish<T>(T evt)
        {
            if (!_handlers.TryGetValue(typeof(T), out var list)) return;
            // Iterate backwards: allows safe removal during dispatch
            for (int i = list.Count - 1; i >= 0; i--)
                ((Action<T>)list[i]).Invoke(evt);
        }

        public static void Clear() => _handlers.Clear();
    }
}
