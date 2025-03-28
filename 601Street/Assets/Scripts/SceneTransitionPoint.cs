using UnityEngine;

/// <summary>
/// Componente que define un punto de transición entre escenas.
/// Este script se coloca en objetos interactuables que deben cambiar de escena.
/// </summary>
[RequireComponent(typeof(InteractableObject))]
public class SceneTransitionPoint : MonoBehaviour
{
    [Header("Configuración de Transición")]
    [Tooltip("Nombre de la escena a la que se cambiará al interactuar")]
    [SerializeField] private string targetSceneName;

    [Tooltip("Nombre del punto de aparición en la escena destino")]
    [SerializeField] private string spawnPointName = "Player_InitialPosition";

    [Tooltip("¿Es este una puerta de salida a la escena principal?")]
    [SerializeField] private bool isExitToMainScene = false;

    [Header("Referencia (opcional)")]
    [Tooltip("Referencia al SceneChange (si está vacío, se buscará automáticamente)")]
    [SerializeField] private SceneChange sceneChangeManager;

    [Header("Debug")]
    [Tooltip("Mostrar logs detallados")]
    [SerializeField] private bool showDetailedLogs = false;

    private void Awake()
    {
        // Validar configuración
        if (string.IsNullOrEmpty(targetSceneName) && !isExitToMainScene)
        {
            Debug.LogError($"SceneTransitionPoint en {gameObject.name} no tiene una escena destino configurada.");
        }        
    }

    private void Start()
    {
        if (sceneChangeManager == null)
        {
            sceneChangeManager = FindFirstObjectByType<SceneChange>();
            if (sceneChangeManager == null)
            {
                Debug.LogError("No se encontró ningún SceneChange en la escena. Debe existir uno para cambiar de escena.");
            }
        }
    }

    // Método para añadir al evento OnInteraction del InteractableObject
    public void TransitionToScene()
    {
        if (sceneChangeManager == null)
        {
            Debug.LogError("No hay un SceneChange disponible para gestionar el cambio de escena");
            return;
        }

        Debug.Log($"Cambiando a escena: {targetSceneName}, punto de aparición: {spawnPointName}, es salida: {isExitToMainScene}");

        if (showDetailedLogs)
        {
            sceneChangeManager.LogSceneInfo();
        }

        // Guardar el punto de aparición para que esté disponible en la siguiente escena
        PlayerPrefs.SetString("LastSpawnPointName", spawnPointName);
        PlayerPrefs.Save();

        // Realizar el cambio de escena según la configuración
        if (isExitToMainScene)
        {
            if (showDetailedLogs)
            {
                Debug.Log($"Llamando a SalirAExterior con la escena principal configurada");
            }

            // Llamar al método simple que solo requiere un parámetro
            sceneChangeManager.SalirAExterior(targetSceneName);
        }
        else
        {
            if (showDetailedLogs)
            {
                Debug.Log($"Llamando a EntrarAInterior con escena '{targetSceneName}'");
            }

            // Llamar al método simple que solo requiere un parámetro
            sceneChangeManager.EntrarAInterior(targetSceneName);
        }
    }
}