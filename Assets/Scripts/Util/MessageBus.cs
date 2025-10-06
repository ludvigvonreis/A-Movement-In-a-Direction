using System;
using System.Collections.Generic;
using System.Linq;

public interface IMessage { }

public class MessageBus
{
    private readonly Dictionary<Type, List<Delegate>> handlers = new();

    public void Subscribe<T>(Action<T> handler) where T : IMessage
    {
        if (!handlers.TryGetValue(typeof(T), out var list))
            handlers[typeof(T)] = list = new List<Delegate>();

        list.Add(handler);
    }

    public void Unsubscribe<T>(Action<T> handler) where T : IMessage
    {
        if (handlers.TryGetValue(typeof(T), out var list))
            list.Remove(handler);
    }

    public void Publish<T>(T message) where T : IMessage
    {
        if (handlers.TryGetValue(typeof(T), out var list))
        {
            foreach (var h in list.Cast<Action<T>>())
				h(message);
        }
    }
}