using UnityEngine;

public class Riku_Parque : MonoBehaviour
{
    public GameObject followActivator;

    public GameObject riku;

    private bool enableTrigger = false;
    private static Riku_Parque instance;
    public static Riku_Parque Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Object.FindFirstObjectByType<Riku_Parque>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("Riku_Parque");
                    instance = obj.AddComponent<Riku_Parque>();
                }
            }
            return instance;
        }
    }

    public GameObject puertaPortal;

    public void PuertaPortal()
    {
        puertaPortal.SetActive(true);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Follow_Riku()
    {
        followActivator.SetActive(true);
        enableTrigger = true;
        PuertaPortal();
    }

    public void DestroyRiku()
    {
        Destroy(riku);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && enableTrigger)
        {
            DestroyRiku();
        } 
    }
}
