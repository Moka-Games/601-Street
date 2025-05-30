// 1. MEJORAR UIFeedbackManager.cs - Agregar registro de indicadores
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gestor de indicadores de UI para feedback visual de interacción
/// Versión mejorada con registro y limpieza automática de indicadores
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
    [SerializeField] private GameObject rangeIndicatorPrefab;
    [SerializeField] private GameObject interactIndicatorPrefab;

    [Header("Canvas de HUD")]
    [SerializeField] private Canvas hudCanvas;

    // NUEVO: Registro de indicadores para limpieza automática
    private Dictionary<int, List<GameObject>> objectIndicators = new Dictionary<int, List<GameObject>>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        if (hudCanvas == null)
        {
            FindHUDCanvas();
        }
    }

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
            Debug.LogWarning("UIFeedbackManager: No se encontró el Canvas HUD.");
        }
    }

    /// <summary>
    /// Crea un indicador de rango y lo registra para limpieza automática
    /// </summary>
    public GameObject CreateRangeIndicator(string name, int ownerInstanceID)
    {
        if (rangeIndicatorPrefab == null)
        {
            Debug.LogError("UIFeedbackManager: No se ha asignado un prefab para el indicador de rango");
            return null;
        }

        if (hudCanvas == null)
        {
            FindHUDCanvas();
            if (hudCanvas == null)
            {
                Debug.LogError("UIFeedbackManager: No se puede crear indicador sin un Canvas HUD");
                return null;
            }
        }

        GameObject indicator = Instantiate(rangeIndicatorPrefab, hudCanvas.transform);
        indicator.name = "RangeIndicator_" + name;
        indicator.SetActive(false);

        // NUEVO: Registrar el indicador
        RegisterIndicator(ownerInstanceID, indicator);

        return indicator;
    }

    /// <summary>
    /// Crea un indicador de interacción y lo registra para limpieza automática
    /// </summary>
    public GameObject CreateInteractIndicator(string name, int ownerInstanceID)
    {
        if (interactIndicatorPrefab == null)
        {
            Debug.LogError("UIFeedbackManager: No se ha asignado un prefab para el indicador de interacción");
            return null;
        }

        if (hudCanvas == null)
        {
            FindHUDCanvas();
            if (hudCanvas == null)
            {
                Debug.LogError("UIFeedbackManager: No se puede crear indicador sin un Canvas HUD");
                return null;
            }
        }

        GameObject indicator = Instantiate(interactIndicatorPrefab, hudCanvas.transform);
        indicator.name = "InteractIndicator_" + name;
        indicator.SetActive(false);

        ConfigureInteractIndicatorForRightwardsExpansion(indicator);

        // NUEVO: Registrar el indicador
        RegisterIndicator(ownerInstanceID, indicator);

        return indicator;
    }

    /// <summary>
    /// NUEVO: Registra un indicador asociado a un objeto específico
    /// </summary>
    private void RegisterIndicator(int ownerInstanceID, GameObject indicator)
    {
        if (!objectIndicators.ContainsKey(ownerInstanceID))
        {
            objectIndicators[ownerInstanceID] = new List<GameObject>();
        }

        objectIndicators[ownerInstanceID].Add(indicator);

        // Agregar un componente que detecte cuando el indicador es destruido
        var cleanupComponent = indicator.AddComponent<IndicatorCleanupHelper>();
        cleanupComponent.Initialize(this, ownerInstanceID, indicator);
    }

    /// <summary>
    /// NUEVO: Limpia todos los indicadores asociados a un objeto específico
    /// </summary>
    public void CleanupIndicatorsForObject(int ownerInstanceID)
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
            Debug.Log($"Limpiados indicadores para objeto con ID: {ownerInstanceID}");
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

    private void ConfigureInteractIndicatorForRightwardsExpansion(GameObject indicator)
    {
        Transform borderTransform = indicator.transform.Find("Interaction_Prompt_Border");
        TMPro.TMP_Text descriptionText = indicator.transform.Find("Interaction_Description")?.GetComponent<TMPro.TMP_Text>();

        if (borderTransform != null)
        {
            RectTransform borderRect = borderTransform as RectTransform;
            if (borderRect != null)
            {
                borderRect.pivot = new Vector2(0f, 0.5f);
                borderRect.anchorMin = new Vector2(0f, borderRect.anchorMin.y);
                borderRect.anchorMax = new Vector2(0f, borderRect.anchorMax.y);
                Vector2 anchoredPosition = borderRect.anchoredPosition;
                anchoredPosition.x = 0f;
                borderRect.anchoredPosition = anchoredPosition;
            }
        }

        if (descriptionText != null)
        {
            descriptionText.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            descriptionText.overflowMode = TMPro.TextOverflowModes.Overflow;

            RectTransform textRect = descriptionText.rectTransform;
            if (textRect != null)
            {
                textRect.pivot = new Vector2(0f, 0.5f);
                textRect.anchorMin = new Vector2(0f, textRect.anchorMin.y);
                textRect.anchorMax = new Vector2(0f, textRect.anchorMax.y);
                Vector2 textPosition = textRect.anchoredPosition;
                textPosition.x = 10f;
                textRect.anchoredPosition = textPosition;
            }

            descriptionText.text = "[PENDING UPDATE]";
            descriptionText.ForceMeshUpdate();
        }
    }

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