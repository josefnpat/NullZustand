using System;
using System.Collections.Concurrent;

namespace NullZustand
{
    public class EntityManager
    {
        private readonly ConcurrentDictionary<long, Entity> _entities;
        private long _nextEntityId;

        public EntityManager()
        {
            _entities = new ConcurrentDictionary<long, Entity>();
            _nextEntityId = 1;
        }

        public long CreateEntity()
        {
            long entityId = _nextEntityId++;
            var entity = new Entity();
            _entities.TryAdd(entityId, entity);
            return entityId;
        }

        public long CreateEntity(Vec3 position, Quat rotation, float velocity, long timestampMs)
        {
            long entityId = _nextEntityId++;
            var entity = new Entity(position, rotation, velocity, timestampMs);
            _entities.TryAdd(entityId, entity);
            return entityId;
        }

        public Entity GetEntity(long entityId)
        {
            _entities.TryGetValue(entityId, out Entity entity);
            return entity;
        }

        public bool UpdateEntity(long entityId, Vec3 position, Quat rotation, float velocity, long timestampMs)
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

        public bool RemoveEntity(long entityId)
        {
            return _entities.TryRemove(entityId, out _);
        }

        public bool HasEntity(long entityId)
        {
            return _entities.ContainsKey(entityId);
        }
    }
}
