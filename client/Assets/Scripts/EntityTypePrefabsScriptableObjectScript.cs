using System.Collections.Generic;
using NullZustand;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityTypePrefabsScriptableObjectScript", menuName = "Scriptable Objects/EntityTypePrefabsScriptableObjectScript")]
public class EntityTypePrefabsScriptableObjectScript : ScriptableObject
{
    public List<EntityTypePrefab> entityTypePrefabs;
}

[System.Serializable]
public class EntityTypePrefab
{
    public EntityType entityType;
    public GameObject entityPrefab;
}