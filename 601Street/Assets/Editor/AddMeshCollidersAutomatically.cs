using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AddMeshCollidersAutomatically : MonoBehaviour
{
    [MenuItem("Tools/Add Mesh Colliders To All Meshes")]
    static void AddMeshColliders()
    {
        MeshRenderer[] meshRenderers = FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        HashSet<GameObject> objectsWithMesh = new HashSet<GameObject>();
        foreach (MeshRenderer renderer in meshRenderers)
        {
            objectsWithMesh.Add(renderer.gameObject);
        }
        MeshFilter[] meshFilters = FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (MeshFilter filter in meshFilters)
        {
            objectsWithMesh.Add(filter.gameObject);
        }
        int addedCount = 0;
        int existingCount = 0;
        foreach (GameObject obj in objectsWithMesh)
        {
            // Comprobar si el objeto ya tiene cualquier tipo de collider
            if (obj.GetComponent<Collider>() != null)
            {
                existingCount++;
                continue;
            }
            MeshFilter filter = obj.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
            {
                continue;
            }
            MeshCollider collider = obj.AddComponent<MeshCollider>();
            collider.sharedMesh = filter.sharedMesh;
            addedCount++;
        }
        Debug.Log($"Added {addedCount} mesh colliders. {existingCount} objects already had colliders.");
    }
}