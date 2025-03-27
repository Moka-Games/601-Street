using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Gestiona la aplicación de una fuente consistente en todos los componentes TextMeshPro de todas las escenas.
/// Este script debe colocarse en un GameObject en la escena persistente.
/// </summary>
public class FontManager : MonoBehaviour
{
    [Header("Configuración de Fuente")]
    [Tooltip("Fuente a aplicar a todos los componentes de texto")]
    [SerializeField] private TMP_FontAsset globalFont;

    [Tooltip("Material de la fuente a aplicar (opcional)")]
    [SerializeField] private Material fontMaterial;

    [Header("Configuración Adicional")]
    [Tooltip("Aplicar también a TextMeshPro en el mundo 3D")]
    [SerializeField] private bool applyToTextMeshPro3D = true;

    [Tooltip("Aplicar a componentes de texto en la escena persistente")]
    [SerializeField] private bool applyToPersistentScene = true;

    [Tooltip("Escena persistente que siempre está cargada")]
    [SerializeField] private string persistentSceneName = "PersistentScene";

    [Header("Debug")]
    [Tooltip("Mostrar logs detallados de la aplicación de fuentes")]
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

        // Aplicamos las fuentes a la escena persistente siempre en Awake para asegurar la inicialización correcta
        ApplyFontToPersistentScene();

        // Suscribirse al evento de cambio de escena
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // En Start volvemos a aplicar a todas las escenas para asegurar que no falte ninguna
        // Esto captura componentes que podrían haberse inicializado después de Awake
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
        // Ignorar la escena persistente si está configurado para no aplicar
        if (!applyToPersistentScene && scene.name == persistentSceneName)
        {
            return;
        }

        // Aplicar la fuente a todos los componentes TMP en la escena cargada
        StartCoroutine(ApplyFontToSceneDelayed(scene));
    }

    // Corrutina para aplicar la fuente después de un breve retraso para asegurar que todo esté cargado
    private IEnumerator ApplyFontToSceneDelayed(Scene scene)
    {
        // Esperar un frame para asegurar que todos los objetos estén inicializados
        yield return null;

        // Esperar otro frame más para estar seguros
        yield return null;

        int count = ApplyFontToScene(scene);

        if (showDetailedLogs || count > 0)
        {
            Debug.Log($"FontManager: Se aplicó la fuente global a {count} componentes de texto en la escena '{scene.name}'");
        }
    }

    // Aplicar la fuente a todos los componentes TMP en una escena específica
    private int ApplyFontToScene(Scene scene)
    {
        int componentsAffected = 0;

        // Obtener todos los GameObjects raíz en la escena
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

            // Aplicar a todos los componentes TextMeshPro (3D) si está configurado
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

    // Método específico para aplicar la fuente a la escena persistente
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
                Debug.Log($"FontManager: Se aplicó la fuente global a {count} componentes de texto en la escena persistente");
            }
        }
        else
        {
            // Si la escena persistente no está cargada, buscar en la escena activa
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
            // Si no está en la escena persistente o está configurado para aplicar a la persistente
            bool inPersistentScene = textComponent.gameObject.scene.name == persistentSceneName;
            if (!inPersistentScene || applyToPersistentScene)
            {
                ApplyFontToComponent(textComponent);
                count++;
            }
        }

        // Aplicar a todos los componentes TextMeshPro (3D) en el juego, si está configurado
        if (applyToTextMeshPro3D)
        {
            TextMeshPro[] textMeshComponents = FindObjectsByType<TextMeshPro>(FindObjectsSortMode.None);
            foreach (TextMeshPro textMeshComponent in textMeshComponents)
            {
                // Si no está en la escena persistente o está configurado para aplicar a la persistente
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
            Debug.Log($"FontManager: Se aplicó la fuente global a {count} componentes de texto en todo el juego");
        }
    }

    // Aplicar la fuente a un componente TextMeshPro específico
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

            // Aplicar el material de la fuente si está configurado
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

    // Método público para forzar la aplicación de fuentes en todas las escenas cargadas inmediatamente
    public void ApplyFontToAllLoadedScenesImmediately()
    {
        int totalCount = 0;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);

            // Ignorar la escena persistente si está configurado para no aplicar
            if (!applyToPersistentScene && scene.name == persistentSceneName)
            {
                continue;
            }

            int count = ApplyFontToScene(scene);
            totalCount += count;

            if (showDetailedLogs)
            {
                Debug.Log($"FontManager: Se aplicó la fuente global a {count} componentes de texto en la escena '{scene.name}'");
            }
        }

        // Como respaldo adicional, aplicar a todos los componentes en el juego
        // Esto capturará cualquier componente que pueda haberse perdido por alguna razón
        ApplyFontToAllComponentsInGame();

        Debug.Log($"FontManager: Se aplicó la fuente global a un total de {totalCount} componentes de texto en todas las escenas cargadas");
    }

    // Método público para que otros scripts puedan solicitar una actualización de fuentes
    public void RefreshAllFonts()
    {
        ApplyFontToAllLoadedScenesImmediately();
    }
}