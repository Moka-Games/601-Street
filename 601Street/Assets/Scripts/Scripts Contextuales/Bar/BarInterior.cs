using UnityEngine;

public class BarInterior : MonoBehaviour
{

    public GameObject botella;

    private void Start()
    {
        botella.SetActive(false);
    }
    public void ActivarBotella()
    {
        botella.SetActive(true);
    }
}
