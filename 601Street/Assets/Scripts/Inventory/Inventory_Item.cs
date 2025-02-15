using UnityEngine;

public class Inventory_Item : MonoBehaviour
{
    public ItemData itemData;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (itemData != null)
            {
                if (Inventory_Manager.Instance != null)
                {
                    Inventory_Manager.Instance.AddItem(itemData);
                    Destroy(gameObject);
                }
                else
                {
                    Debug.LogError("Inventory_Manager.Instance is null.");
                }
            }
            else
            {
                Debug.LogError("itemData is null on " + gameObject.name);
            }
        }
    }

}
