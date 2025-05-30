using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gestor de indicadores de UI para feedback visual de interacción
/// Versión mejorada con registro, limpieza automática y verificación de componentes
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
                _instance = FindAnyObjectByType<UIFeedbackManager>();
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

    // NUEVO: Registro de indicadores para limpieza automática Y verificación de componentes
    private Dictionary<int, List<GameObject>> objectIndicators = new Dictionary<int, List<GameObject>>();
    private Dictionary<int, MonoBehaviour> objectComponents = new Dictionary<int, MonoBehaviour>();

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

    private void Update()
    {
        // NUEVO: Verificar estados de componentes periódicamente
        VerifyComponentStates();
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
    /// Crea un indicador de rango y lo registra para limpieza automática
    /// </summary>
    /// <param name="name">Nombre para el nuevo indicador</param>
    /// <param name="ownerInstanceID">ID de instancia del objeto propietario</param>
    /// <param name="ownerComponent">Componente propietario para verificación de estado</param>
    /// <returns>Instancia del indicador creado</returns>
    public GameObject CreateRangeIndicator(string name, int ownerInstanceID, MonoBehaviour ownerComponent = null)
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

        // NUEVO: Registrar el indicador y el componente
        RegisterIndicator(ownerInstanceID, indicator, ownerComponent);

        return indicator;
    }

    /// <summary>
    /// Crea un indicador de interacción y lo registra para limpieza automática
    /// </summary>
    /// <param name="name">Nombre para el nuevo indicador</param>
    /// <param name="ownerInstanceID">ID de instancia del objeto propietario</param>
    /// <param name="ownerComponent">Componente propietario para verificación de estado</param>
    /// <returns>Instancia del indicador creado</returns>
    public GameObject CreateInteractIndicator(string name, int ownerInstanceID, MonoBehaviour ownerComponent = null)
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

        // Configurar el texto y el borde para que crezcan hacia la derecha
        ConfigureInteractIndicatorForRightwardsExpansion(indicator);

        // NUEVO: Registrar el indicador y el componente
        RegisterIndicator(ownerInstanceID, indicator, ownerComponent);

        return indicator;
    }

    /// <summary>
    /// NUEVO: Registra un indicador asociado a un objeto y componente específico
    /// </summary>
    private void RegisterIndicator(int ownerInstanceID, GameObject indicator, MonoBehaviour ownerComponent = null)
    {
        if (!objectIndicators.ContainsKey(ownerInstanceID))
        {
            objectIndicators[ownerInstanceID] = new List<GameObject>();
        }

        objectIndicators[ownerInstanceID].Add(indicator);

        // NUEVO: Registrar el componente propietario para verificación de estado
        if (ownerComponent != null)
        {
            objectComponents[ownerInstanceID] = ownerComponent;
        }

        // Agregar un componente que detecte cuando el indicador es destruido
        var cleanupComponent = indicator.AddComponent<IndicatorCleanupHelper>();
        cleanupComponent.Initialize(this, ownerInstanceID, indicator);
    }

    /// <summary>
    /// NUEVO: Verifica el estado de los componentes y desactiva indicadores si es necesario
    /// </summary>
    public void VerifyComponentStates()
    {
        foreach (var kvp in objectComponents)
        {
            int ownerID = kvp.Key;
            MonoBehaviour component = kvp.Value;

            // Verificar si el componente existe y está habilitado
            bool shouldShowIndicators = component != null &&
                                      component.gameObject != null &&
                                      component.gameObject.activeInHierarchy &&
                                      component.enabled;

            // Actualizar estado de los indicadores basado en el estado del componente
            if (objectIndicators.ContainsKey(ownerID))
            {
                foreach (var indicator in objectIndicators[ownerID])
                {
                    if (indicator != null)
                    {
                        // Solo aplicar la verificación si el indicador estaba activo
                        if (!shouldShowIndicators && indicator.activeInHierarchy)
                        {
                            indicator.SetActive(false);
                        }
                        // Nota: No reactivamos automáticamente aquí, eso lo maneja la lógica del componente
                    }
                }
            }
        }
    }

    /// <summary>
    /// NUEVO: Verifica el estado de un componente específico
    /// </summary>
    public bool IsComponentValid(int ownerInstanceID)
    {
        if (objectComponents.ContainsKey(ownerInstanceID))
        {
            MonoBehaviour component = objectComponents[ownerInstanceID];
            return component != null &&
                   component.gameObject != null &&
                   component.gameObject.activeInHierarchy &&
                   component.enabled;
        }
        return false;
    }

    /// <summary>
    /// NUEVO: Destruye todos los indicadores asociados a un objeto específico (solo cuando se destruye el objeto)
    /// </summary>
    public void DestroyIndicatorsForObject(int ownerInstanceID)
    {
        if (objectIndicators.ContainsKey(ownerInstanceID))
        {
            foreach (var indicator in objectIndicators[ownerInstanceID])
            {
                if (indicator != null)
                {
                    indicator.SetActive(false);
                    Destroy(indicator);
                }
            }
            objectIndicators.Remove(ownerInstanceID);
            objectComponents.Remove(ownerInstanceID); // NUEVO: Limpiar también el registro de componentes
            Debug.Log($"Indicadores destruidos para objeto con ID: {ownerInstanceID}");
        }
    }

    /// <summary>
    /// NUEVO: Oculta/muestra todos los indicadores asociados a un objeto específico (para activar/desactivar)
    /// </summary>
    public void SetIndicatorsActiveForObject(int ownerInstanceID, bool active)
    {
        if (objectIndicators.ContainsKey(ownerInstanceID))
        {
            foreach (var indicator in objectIndicators[ownerInstanceID])
            {
                if (indicator != null)
                {
                    indicator.SetActive(active && indicator.name.Contains("ShouldBeActive"));
                }
            }
            Debug.Log($"Indicadores {(active ? "activados" : "desactivados")} para objeto con ID: {ownerInstanceID}");
        }
    }

    /// <summary>
    /// NUEVO: Oculta todos los indicadores de un objeto (sin destruirlos)
    /// </summary>
    public void HideIndicatorsForObject(int ownerInstanceID)
    {
        if (objectIndicators.ContainsKey(ownerInstanceID))
        {
            foreach (var indicator in objectIndicators[ownerInstanceID])
            {
                if (indicator != null)
                {
                    indicator.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// NUEVO: Limpia indicadores huérfanos (cuyo objeto padre ya no existe)
    /// </summary>
    public void CleanupOrphanedIndicators()
    {
        var keysToRemove = new List<int>();

        foreach (var kvp in objectIndicators)
        {
            var indicatorsToRemove = new List<GameObject>();

            foreach (var indicator in kvp.Value)
            {
                if (indicator == null)
                {
                    indicatorsToRemove.Add(indicator);
                }
            }

            // Remover indicadores nulos
            foreach (var indicator in indicatorsToRemove)
            {
                kvp.Value.Remove(indicator);
            }

            // Si no quedan indicadores, marcar para remover la entrada
            if (kvp.Value.Count == 0)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        // Remover entradas vacías
        foreach (var key in keysToRemove)
        {
            objectIndicators.Remove(key);
        }
    }

    /// <summary>
    /// NUEVO: Permite desregistrar un indicador específico
    /// </summary>
    public void UnregisterIndicator(int ownerInstanceID, GameObject indicator)
    {
        if (objectIndicators.ContainsKey(ownerInstanceID))
        {
            objectIndicators[ownerInstanceID].Remove(indicator);

            if (objectIndicators[ownerInstanceID].Count == 0)
            {
                objectIndicators.Remove(ownerInstanceID);
            }
        }
    }

    /// <summary>
    /// MÉTODO OBSOLETO: Usar DestroyIndicatorsForObject en su lugar
    /// </summary>
    [System.Obsolete("Usar DestroyIndicatorsForObject para destrucción o HideIndicatorsForObject para ocultar")]
    public void CleanupIndicatorsForObject(int ownerInstanceID)
    {
        DestroyIndicatorsForObject(ownerInstanceID);
    }

    /// <summary>
    /// Configura el indicador de interacción para que se expanda hacia la derecha
    /// </summary>
    private void ConfigureInteractIndicatorForRightwardsExpansion(GameObject indicator)
    {
        // Buscar componentes necesarios
        Transform borderTransform = indicator.transform.Find("Interaction_Prompt_Border");
        TMPro.TMP_Text descriptionText = indicator.transform.Find("Interaction_Description")?.GetComponent<TMPro.TMP_Text>();

        // Configurar el borde
        if (borderTransform != null)
        {
            RectTransform borderRect = borderTransform as RectTransform;
            if (borderRect != null)
            {
                // Configurar pivot a la izquierda para que crezca hacia la derecha
                borderRect.pivot = new Vector2(0f, 0.5f);

                // Establecer anclajes a la izquierda
                borderRect.anchorMin = new Vector2(0f, borderRect.anchorMin.y);
                borderRect.anchorMax = new Vector2(0f, borderRect.anchorMax.y);

                // Posición fija desde la izquierda
                Vector2 anchoredPosition = borderRect.anchoredPosition;
                anchoredPosition.x = 0f;
                borderRect.anchoredPosition = anchoredPosition;
            }
        }

        // Configurar el texto
        if (descriptionText != null)
        {
            // Configurar el texto para que no haga wrapping
            descriptionText.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            descriptionText.overflowMode = TMPro.TextOverflowModes.Overflow;

            // Configurar pivot y anclajes
            RectTransform textRect = descriptionText.rectTransform;
            if (textRect != null)
            {
                // Pivot a la izquierda
                textRect.pivot = new Vector2(0f, 0.5f);

                // Anclajes a la izquierda
                textRect.anchorMin = new Vector2(0f, textRect.anchorMin.y);
                textRect.anchorMax = new Vector2(0f, textRect.anchorMax.y);

                // Posición con un pequeño margen izquierdo
                Vector2 textPosition = textRect.anchoredPosition;
                textPosition.x = 10f; // Margen desde el borde
                textRect.anchoredPosition = textPosition;
            }

            // No establecemos un texto inicial aquí, lo dejamos vacío o con un placeholder
            // para que sea establecido por el componente del objeto interactuable
            descriptionText.text = "[PENDING UPDATE]";

            // Forzar actualización del texto para que se calcule el tamaño correcto
            descriptionText.ForceMeshUpdate();
        }
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

    // NUEVO: Limpieza general al destruir el manager
    private void OnDestroy()
    {
        foreach (var kvp in objectIndicators)
        {
            foreach (var indicator in kvp.Value)
            {
                if (indicator != null)
                {
                    Destroy(indicator);
                }
            }
        }
        objectIndicators.Clear();
        objectComponents.Clear(); // NUEVO: Limpiar también componentes
    }
}

/// <summary>
/// NUEVO: Componente auxiliar para detectar cuando un indicador es destruido
/// </summary>
public class IndicatorCleanupHelper : MonoBehaviour
{
    private UIFeedbackManager manager;
    private int ownerInstanceID;
    private GameObject indicator;

    public void Initialize(UIFeedbackManager mgr, int ownerID, GameObject ind)
    {
        manager = mgr;
        ownerInstanceID = ownerID;
        indicator = ind;
    }

    private void OnDestroy()
    {
        if (manager != null)
        {
            manager.UnregisterIndicator(ownerInstanceID, indicator);
        }
    }
}