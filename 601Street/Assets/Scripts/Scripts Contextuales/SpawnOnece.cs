using UnityEngine;

public class SpawnOnece : MonoBehaviour
{
    private static bool hasSpawned = false;

    public void Start()
    {
        if (!hasSpawned)
        {
            hasSpawned = true;
        }
        else
        {
            IfSpawned();
        }
    }

    public void IfSpawned()
    {
        Destroy(gameObject);
    }
}
