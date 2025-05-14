using UnityEngine;

public class DeActivateItem : MonoBehaviour
{
    public GameObject itemToDeactivate;

    public void DestroyItem()
    {
        if (itemToDeactivate != null)
        {
            itemToDeactivate.SetActive(false);
        }
    }
}

