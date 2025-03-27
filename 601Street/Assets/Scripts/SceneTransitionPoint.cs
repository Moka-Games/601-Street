using UnityEngine;

/// <summary>
/// Componente que define un punto de transici�n entre escenas.
/// Este script se coloca en objetos interactuables que deben cambiar de escena.
/// </summary>
[RequireComponent(typeof(InteractableObject))]
public class SceneTransitionPoint : MonoBehaviour
{
    [Header("Configuraci�n de Transici�n")]
    [Tooltip("Nombre de la escena a la que se cambiar� al interactuar")]
    [SerializeField] private string targetSceneName;

    [Tooltip("Nombre del punto de aparici�n en la escena destino")]
    [SerializeField] private string spawnPointName = "Player_InitialPosition";

    [Tooltip("�Es este una puerta de salida a la escena principal?")]
    [SerializeField] private bool isExitToMainScene = false;

    [Header("Referencia (opcional)")]
    [Tooltip("Referencia al SceneChange (si est� vac�o, se buscar� autom�ticamente)")]
    [SerializeField] private SceneChange sceneChangeManager;

    private void Awake()
    {
        // Validar configuraci�n
        if (string.IsNullOrEmpty(targetSceneName) && !isExitToMainScene)
        {
            Debug.LogError($"SceneTransitionPoint en {gameObject.name} no tiene una escena destino configurada.");
        }        
    }

    private void Start()
    {
        if (sceneChangeManager == null)
        {
            sceneChangeManager = FindAnyObjectByType<SceneChange>();
            if (sceneChangeManager == null)
            {
                Debug.LogError("No se encontr� ning�n SceneChange en la escena. Debe existir uno para cambiar de escena.");
            }
        }
    }
    // M�todo para a�adir al evento OnInteraction del InteractableObject
    public void TransitionToScene()
    {
        if (sceneChangeManager == null)
        {
            Debug.LogError("No hay un SceneChange disponible para gestionar el cambio de escena");
            return;
        }

        Debug.Log($"Cambiando a escena: {targetSceneName}, punto de aparici�n: {spawnPointName}, es salida: {isExitToMainScene}");

        // Guardar el punto de aparici�n para que est� disponible en la siguiente escena
        PlayerPrefs.SetString("LastSpawnPointName", spawnPointName);
        PlayerPrefs.Save();

        // Realizar el cambio de escena seg�n la configuraci�n
        if (isExitToMainScene)
        {
            // Llamar al m�todo simple que solo requiere un par�metro
            sceneChangeManager.SalirAExterior(targetSceneName);
        }
        else
        {
            // Llamar al m�todo simple que solo requiere un par�metro
            sceneChangeManager.EntrarAInterior(targetSceneName);
        }
    }
}