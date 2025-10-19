using System;
using System.Collections.Generic;

public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
    private static readonly object _lock = new object();

    public static void Register<T>(T service)
    {
        lock (_lock)
        {
            _services[typeof(T)] = service;
        }
    }

    public static T Get<T>()
    {
        lock (_lock)
        {
            if (_services.TryGetValue(typeof(T), out var service))
            {
                return (T)service;
            }
        }

        throw new Exception($"Service of type {typeof(T)} not found.");
    }

    public static bool TryGet<T>(out T service)
    {
        lock (_lock)
        {
            if (_services.TryGetValue(typeof(T), out var found))
            {
                service = (T)found;
                return true;
            }

            service = default;
            return false;
        }
    }

    public static void Clear()
    {
        lock (_lock)
        {
            _services.Clear();
        }
    }

    public static bool IsRegistered<T>()
    {
        lock (_lock)
        {
            return _services.ContainsKey(typeof(T));
        }
    }

    public static int GetServiceCount()
    {
        lock (_lock)
        {
            return _services.Count;
        }
    }
}