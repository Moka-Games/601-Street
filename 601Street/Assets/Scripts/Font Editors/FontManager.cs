using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Gestiona la aplicaci�n de una fuente consistente en todos los componentes TextMeshPro de todas las escenas.
/// Este script debe colocarse en un GameObject en la escena persistente.
/// </summary>
public class FontManager : MonoBehaviour
{
    [Header("Configuraci�n de Fuente")]
    [Tooltip("Fuente a aplicar a todos los componentes de texto")]
    [SerializeField] private TMP_FontAsset globalFont;

    [Tooltip("Material de la fuente a aplicar (opcional)")]
    [SerializeField] private Material fontMaterial;

    [Header("Configuraci�n Adicional")]
    [Tooltip("Aplicar tambi�n a TextMeshPro en el mundo 3D")]
    [SerializeField] private bool applyToTextMeshPro3D = true;

    [Tooltip("Aplicar a componentes de texto en la escena persistente")]
    [SerializeField] private bool applyToPersistentScene = true;

    [Tooltip("Escena persistente que siempre est� cargada")]
    [SerializeField] private string persistentSceneName = "PersistentScene";

    [Header("Debug")]
    [Tooltip("Mostrar logs detallados de la aplicaci�n de fuentes")]
    [SerializeField] private bool showDetailedLogs = false;

    // Singleton pattern
    private static FontManager instance;
    public static FontManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<FontManager>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        // Configurar singleton
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        // Verificar que tenemos una fuente configurada
        if (globalFont == null)
        {
            Debug.LogError("FontManager: No se ha asignado una fuente global. Por favor, asigna una fuente TMP_FontAsset en el inspector.");
            return;
        }

        // Aplicamos las fuentes a la escena persistente siempre en Awake para asegurar la inicializaci�n correcta
        ApplyFontToPersistentScene();

        // Suscribirse al evento de cambio de escena
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // En Start volvemos a aplicar a todas las escenas para asegurar que no falte ninguna
        // Esto captura componentes que podr�an haberse inicializado despu�s de Awake
        ApplyFontToAllLoadedScenesImmediately();
    }

    private void OnDestroy()
    {
        // Desuscribirse del evento cuando se destruye
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Evento que se dispara cuando se carga una nueva escena
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Ignorar la escena persistente si est� configurado para no aplicar
        if (!applyToPersistentScene && scene.name == persistentSceneName)
        {
            return;
        }

        // Aplicar la fuente a todos los componentes TMP en la escena cargada
        StartCoroutine(ApplyFontToSceneDelayed(scene));
    }

    // Corrutina para aplicar la fuente despu�s de un breve retraso para asegurar que todo est� cargado
    private IEnumerator ApplyFontToSceneDelayed(Scene scene)
    {
        // Esperar un frame para asegurar que todos los objetos est�n inicializados
        yield return null;

        // Esperar otro frame m�s para estar seguros
        yield return null;

        int count = ApplyFontToScene(scene);

        if (showDetailedLogs || count > 0)
        {
            Debug.Log($"FontManager: Se aplic� la fuente global a {count} componentes de texto en la escena '{scene.name}'");
        }
    }

    // Aplicar la fuente a todos los componentes TMP en una escena espec�fica
    private int ApplyFontToScene(Scene scene)
    {
        int componentsAffected = 0;

        // Obtener todos los GameObjects ra�z en la escena
        GameObject[] rootObjects = scene.GetRootGameObjects();

        foreach (GameObject rootObject in rootObjects)
        {
            // Aplicar a todos los componentes TextMeshProUGUI (UI)
            TextMeshProUGUI[] textComponents = rootObject.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI textComponent in textComponents)
            {
                ApplyFontToComponent(textComponent);
                componentsAffected++;
            }

            // Aplicar a todos los componentes TextMeshPro (3D) si est� configurado
            if (applyToTextMeshPro3D)
            {
                TextMeshPro[] textMeshComponents = rootObject.GetComponentsInChildren<TextMeshPro>(true);
                foreach (TextMeshPro textMeshComponent in textMeshComponents)
                {
                    ApplyFontToComponent(textMeshComponent);
                    componentsAffected++;
                }
            }
        }

        return componentsAffected;
    }

    // M�todo espec�fico para aplicar la fuente a la escena persistente
    public void ApplyFontToPersistentScene()
    {
        if (!applyToPersistentScene)
        {
            return;
        }

        Scene persistentScene = SceneManager.GetSceneByName(persistentSceneName);
        if (persistentScene.isLoaded)
        {
            int count = ApplyFontToScene(persistentScene);

            if (showDetailedLogs || count > 0)
            {
                Debug.Log($"FontManager: Se aplic� la fuente global a {count} componentes de texto en la escena persistente");
            }
        }
        else
        {
            // Si la escena persistente no est� cargada, buscar en la escena activa
            ApplyFontToAllComponentsInGame();
        }
    }

    // Aplicar la fuente a todos los componentes de texto en el juego, independientemente de la escena
    private void ApplyFontToAllComponentsInGame()
    {
        int count = 0;

        // Aplicar a todos los componentes TextMeshProUGUI (UI) en el juego
        TextMeshProUGUI[] textComponents = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        foreach (TextMeshProUGUI textComponent in textComponents)
        {
            // Si no est� en la escena persistente o est� configurado para aplicar a la persistente
            bool inPersistentScene = textComponent.gameObject.scene.name == persistentSceneName;
            if (!inPersistentScene || applyToPersistentScene)
            {
                ApplyFontToComponent(textComponent);
                count++;
            }
        }

        // Aplicar a todos los componentes TextMeshPro (3D) en el juego, si est� configurado
        if (applyToTextMeshPro3D)
        {
            TextMeshPro[] textMeshComponents = FindObjectsByType<TextMeshPro>(FindObjectsSortMode.None);
            foreach (TextMeshPro textMeshComponent in textMeshComponents)
            {
                // Si no est� en la escena persistente o est� configurado para aplicar a la persistente
                bool inPersistentScene = textMeshComponent.gameObject.scene.name == persistentSceneName;
                if (!inPersistentScene || applyToPersistentScene)
                {
                    ApplyFontToComponent(textMeshComponent);
                    count++;
                }
            }
        }

        if (showDetailedLogs || count > 0)
        {
            Debug.Log($"FontManager: Se aplic� la fuente global a {count} componentes de texto en todo el juego");
        }
    }

    // Aplicar la fuente a un componente TextMeshPro espec�fico
    private void ApplyFontToComponent<T>(T textComponent) where T : TMP_Text
    {
        if (textComponent != null && globalFont != null)
        {
            if (showDetailedLogs)
            {
                Debug.Log($"FontManager: Aplicando fuente a '{textComponent.name}' en GameObject '{textComponent.gameObject.name}' en escena '{textComponent.gameObject.scene.name}'");
            }

            // Guardar los valores actuales que queremos preservar
            float fontSize = textComponent.fontSize;
            FontStyles fontStyle = textComponent.fontStyle;
            Color textColor = textComponent.color;
            bool enableAutoSizing = textComponent.enableAutoSizing;
            float minFontSize = textComponent.fontSizeMin;
            float maxFontSize = textComponent.fontSizeMax;

            // Aplicar la nueva fuente
            textComponent.font = globalFont;

            // Aplicar el material de la fuente si est� configurado
            if (fontMaterial != null)
            {
                textComponent.fontMaterial = fontMaterial;
            }

            // Restaurar los valores que queremos preservar
            textComponent.fontSize = fontSize;
            textComponent.fontStyle = fontStyle;
            textComponent.color = textColor;
            textComponent.enableAutoSizing = enableAutoSizing;
            textComponent.fontSizeMin = minFontSize;
            textComponent.fontSizeMax = maxFontSize;
        }
    }

    // M�todo p�blico para forzar la aplicaci�n de fuentes en todas las escenas cargadas inmediatamente
    public void ApplyFontToAllLoadedScenesImmediately()
    {
        int totalCount = 0;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);

            // Ignorar la escena persistente si est� configurado para no aplicar
            if (!applyToPersistentScene && scene.name == persistentSceneName)
            {
                continue;
            }

            int count = ApplyFontToScene(scene);
            totalCount += count;

            if (showDetailedLogs)
            {
                Debug.Log($"FontManager: Se aplic� la fuente global a {count} componentes de texto en la escena '{scene.name}'");
            }
        }

        // Como respaldo adicional, aplicar a todos los componentes en el juego
        // Esto capturar� cualquier componente que pueda haberse perdido por alguna raz�n
        ApplyFontToAllComponentsInGame();

        Debug.Log($"FontManager: Se aplic� la fuente global a un total de {totalCount} componentes de texto en todas las escenas cargadas");
    }

    // M�todo p�blico para que otros scripts puedan solicitar una actualizaci�n de fuentes
    public void RefreshAllFonts()
    {
        ApplyFontToAllLoadedScenesImmediately();
    }
}