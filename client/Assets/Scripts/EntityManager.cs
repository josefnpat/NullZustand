using System.Collections.Generic;
using UnityEngine;
using NullZustand;

public class EntityManager : MonoBehaviour
{
    public const long INVALID_ENTITY_ID = 0L;

    private Dictionary<long, Entity> _entities = new Dictionary<long, Entity>();
    private object _lock = new object();

    void Awake()
    {
        ServiceLocator.Register<EntityManager>(this);
    }

    public void CreateEntity(long entityId)
    {
        lock (_lock)
        {
            if (!_entities.ContainsKey(entityId))
            {
                _entities[entityId] = new Entity();
            }
        }
    }

    public void CreateEntity(long entityId, EntityType type, Vector3 position, Quaternion rotation, float velocity, long timestampMs)
    {
        lock (_lock)
        {
            _entities[entityId] = new Entity(type, position, rotation, velocity, timestampMs);
        }
    }

    public Entity GetEntity(long entityId)
    {
        lock (_lock)
        {
            _entities.TryGetValue(entityId, out Entity entity);
            return entity;
        }
    }

    public bool UpdateEntity(long entityId, Vector3 position, Quaternion rotation, float velocity, long timestampMs)
    {
        lock (_lock)
        {
            if (_entities.TryGetValue(entityId, out Entity entity))
            {
                entity.Position = position;
                entity.Rotation = rotation;
                entity.Velocity = velocity;
                entity.TimestampMs = timestampMs;
                return true;
            }
            return false;
        }
    }

    public bool HasEntity(long entityId)
    {
        lock (_lock)
        {
            return _entities.ContainsKey(entityId);
        }
    }

    public bool RemoveEntity(long entityId)
    {
        lock (_lock)
        {
            return _entities.Remove(entityId);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _entities.Clear();
        }
    }
}
