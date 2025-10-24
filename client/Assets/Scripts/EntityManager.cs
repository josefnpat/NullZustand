using System.Collections.Generic;
using UnityEngine;
using NullZustand;

public class EntityManager : MonoBehaviour
{
    public const long INVALID_ENTITY_ID = 0L;

    private ServerController _serverController;

    [SerializeField]
    private EntityTypePrefabsScriptableObjectScript _entityTypePrefabs;
    private Dictionary<long, Entity> _entities = new Dictionary<long, Entity>();
    private object _lock = new object();

    void Awake()
    {
        ServiceLocator.Register<EntityManager>(this);
    }

    void Start()
    {
        _serverController = ServiceLocator.Get<ServerController>();
        _serverController.OnSessionDisconnect += OnSessionDisconnect;
    }

    void Update()
    {
        UpdateAllEntityMovement();
    }

    public void CreateOrUpdateEntity(long entityId, EntityType type, Vector3 position, Quaternion rotation, float velocity, long timestampMs)
    {
        lock (_lock)
        {
            if (_entities.ContainsKey(entityId))
            {
                // Update existing entity
                Entity existingEntity = _entities[entityId];
                existingEntity.Type = type;
                existingEntity.Position = position;
                existingEntity.Rotation = rotation;
                existingEntity.Velocity = velocity;
                existingEntity.TimestampMs = timestampMs;
            }
            else
            {
                GameObject gameObject = CreateEntityGameObject(entityId, type, position, rotation);
                EntityController entityController = gameObject.GetComponent<EntityController>();
                if (entityController == null)
                {
                    Debug.LogError($"[EntityManager] No EntityController found for prefab type {type}");
                }
                _entities[entityId] = new Entity(type, position, rotation, velocity, timestampMs, gameObject, entityController);
            }
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
                UpdateEntityMovement(entityId);
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
            if (_entities.TryGetValue(entityId, out Entity entity))
            {
                // Destroy GameObject if it exists
                if (entity.GameObject != null)
                {
                    Destroy(entity.GameObject);
                }
                _entities.Remove(entityId);
                return true;
            }
            return false;
        }
    }

    public GameObject CreateEntityGameObject(long entityId, EntityType type, Vector3 position, Quaternion rotation)
    {
        lock (_lock)
        {

            if (_entities.TryGetValue(entityId, out Entity entity) && entity.GameObject != null)
            {
                Debug.LogWarning($"[EntityManager] GameObject for entity {entityId} already exists");
                return entity.GameObject;
            }

            GameObject prefab = GetPrefabForEntityType(type);
            if (prefab == null)
            {
                Debug.LogError($"[EntityManager] No prefab found for entity type {type}");
                return null;
            }

            GameObject entityGameObject = Instantiate(prefab, position, rotation);
            entityGameObject.name = $"{type}({entityId})";

            if (_entities.ContainsKey(entityId))
            {
                Debug.LogError($"[EntityManager] Entity for entity {entityId} already exists");
                return null;
            }

            Entity newEntity = new Entity();
            newEntity.GameObject = entityGameObject;
            _entities[entityId] = newEntity;

            return entityGameObject;
        }
    }

    public GameObject GetEntityGameObject(long entityId)
    {
        lock (_lock)
        {
            if (_entities.TryGetValue(entityId, out Entity entity))
            {
                return entity.GameObject;
            }
            return null;
        }
    }

    public void UpdateEntityMovement(long entityId)
    {
        lock (_lock)
        {
            if (_entities.TryGetValue(entityId, out Entity entity) &&
                entity.EntityController != null)
            {
                entity.EntityController.UpdateMovement(entity);
            }
        }
    }

    public void UpdateAllEntityMovement()
    {
        lock (_lock)
        {
            foreach (var kvp in _entities)
            {
                UpdateEntityMovement(kvp.Key);
            }
        }
    }

    private GameObject GetPrefabForEntityType(EntityType type)
    {
        if (_entityTypePrefabs?.entityTypePrefabs == null)
        {
            return null;
        }

        foreach (var entityTypePrefab in _entityTypePrefabs.entityTypePrefabs)
        {
            if (entityTypePrefab.entityType == type)
            {
                return entityTypePrefab.entityPrefab;
            }
        }
        return null;
    }

    public void ClearAllEntities()
    {
        lock (_lock)
        {
            // Create a copy of the keys to avoid modifying collection during enumeration
            var entityIds = new List<long>(_entities.Keys);

            // Remove each entity using the existing RemoveEntity method
            foreach (long entityId in entityIds)
            {
                RemoveEntity(entityId);
            }
        }
    }

    private void OnSessionDisconnect()
    {
        ClearAllEntities();
    }

    void OnDestroy()
    {
        _serverController.OnSessionDisconnect -= OnSessionDisconnect;
        ClearAllEntities();
    }
}
