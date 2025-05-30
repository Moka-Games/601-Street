using UnityEngine;

public class Riku_Parque : MonoBehaviour
{
    private bool enableTrigger = false;
    private static Riku_Parque instance;

    [SerializeField] private GameObject Riku;

    private GameStateController gameStateController;

    [SerializeField] private string OnFail_State;
    [SerializeField] private string OnSuccess_State;
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

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        gameStateController = FindFirstObjectByType<GameStateController>();
        if (gameStateController == null)
        {
            Debug.LogError("No se encontr� el GameStateController en la escena.");
        }
    }
    public void RikuSucces()
    {
        gameStateController.ChangeGameState(OnSuccess_State);
        enableTrigger = true;
    }

    public void RikuFail()
    {
        gameStateController.ChangeGameState(OnFail_State);
    }

    public void DestroyRiku()
    {
        Destroy(Riku);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player") && enableTrigger)
        {
            DestroyRiku();
        }
    }
}
