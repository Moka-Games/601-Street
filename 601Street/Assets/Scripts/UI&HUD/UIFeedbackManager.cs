using UnityEngine;

/// <summary>
/// Gestor de indicadores de UI para feedback visual de interacción
/// </summary>
public class UIFeedbackManager : MonoBehaviour
{
    private static UIFeedbackManager _instance;

    public static UIFeedbackManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Buscar instancia en la escena
                _instance = FindAnyObjectByType<UIFeedbackManager>();

                // Si no existe, crear un objeto con este componente
                if (_instance == null)
                {
                    GameObject managerObject = new GameObject("UIFeedbackManager");
                    _instance = managerObject.AddComponent<UIFeedbackManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Prefabs de Indicadores UI")]
    [Tooltip("Prefab para indicar que un objeto está en rango")]
    [SerializeField] private GameObject rangeIndicatorPrefab;

    [Tooltip("Prefab para indicar que se puede interactuar con un objeto")]
    [SerializeField] private GameObject interactIndicatorPrefab;

    [Header("Canvas de HUD")]
    [Tooltip("Canvas donde se instanciarán los indicadores")]
    [SerializeField] private Canvas hudCanvas;

    private void Awake()
    {
        // Singleton pattern
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        // Intentar encontrar el canvas si no está asignado
        if (hudCanvas == null)
        {
            FindHUDCanvas();
        }
    }

    // Método para buscar el canvas de HUD
    private void FindHUDCanvas()
    {
        GameObject hudObj = GameObject.Find("HUD");
        if (hudObj != null)
        {
            hudCanvas = hudObj.GetComponent<Canvas>();
            if (hudCanvas == null)
            {
                hudCanvas = hudObj.GetComponentInChildren<Canvas>();
            }
        }

        if (hudCanvas == null)
        {
            Debug.LogWarning("UIFeedbackManager: No se encontró el Canvas HUD. Algunos indicadores podrían no mostrarse correctamente.");
        }
    }

    /// <summary>
    /// Crea un indicador de rango como hijo del canvas HUD
    /// </summary>
    /// <param name="name">Nombre para el nuevo indicador</param>
    /// <returns>Instancia del indicador creado</returns>
    public GameObject CreateRangeIndicator(string name)
    {
        if (rangeIndicatorPrefab == null)
        {
            Debug.LogError("UIFeedbackManager: No se ha asignado un prefab para el indicador de rango");
            return null;
        }

        // Buscar el canvas HUD si no lo tenemos aún
        if (hudCanvas == null)
        {
            FindHUDCanvas();

            if (hudCanvas == null)
            {
                Debug.LogError("UIFeedbackManager: No se puede crear indicador sin un Canvas HUD");
                return null;
            }
        }

        // Instanciar el indicador como hijo del canvas
        GameObject indicator = Instantiate(rangeIndicatorPrefab, hudCanvas.transform);
        indicator.name = "RangeIndicator_" + name;

        // Inicialmente desactivado
        indicator.SetActive(false);

        return indicator;
    }

    /// <summary>
    /// Crea un indicador de interacción como hijo del canvas HUD
    /// </summary>
    /// <param name="name">Nombre para el nuevo indicador</param>
    /// <returns>Instancia del indicador creado</returns>
    public GameObject CreateInteractIndicator(string name)
    {
        if (interactIndicatorPrefab == null)
        {
            Debug.LogError("UIFeedbackManager: No se ha asignado un prefab para el indicador de interacción");
            return null;
        }

        // Buscar el canvas HUD si no lo tenemos aún
        if (hudCanvas == null)
        {
            FindHUDCanvas();

            if (hudCanvas == null)
            {
                Debug.LogError("UIFeedbackManager: No se puede crear indicador sin un Canvas HUD");
                return null;
            }
        }

        // Instanciar el indicador como hijo del canvas
        GameObject indicator = Instantiate(interactIndicatorPrefab, hudCanvas.transform);
        indicator.name = "InteractIndicator_" + name;

        // Inicialmente desactivado
        indicator.SetActive(false);

        return indicator;
    }

    // Getter para el Canvas HUD
    public Canvas GetHUDCanvas()
    {
        if (hudCanvas == null)
        {
            FindHUDCanvas();
        }

        return hudCanvas;
    }
}