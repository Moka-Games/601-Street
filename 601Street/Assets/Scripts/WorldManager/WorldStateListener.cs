using UnityEngine;
using UnityEngine.Events;

public class WorldStateListener : MonoBehaviour
{
    [Header("Configuraci�n B�sica")]
    [SerializeField] private string objectID; // ID �nico para este objeto
    [SerializeField] private bool useSceneName = true; // Usar nombre de escena autom�ticamente
    [SerializeField] private string sceneName; // Nombre de escena manual si useSceneName=false
    [SerializeField] private bool defaultActiveState = true; // Estado por defecto del objeto

    [Header("Eventos")]
    [SerializeField] private UnityEvent onActivate; // Evento cuando el objeto se activa
    [SerializeField] private UnityEvent onDeactivate; // Evento cuando el objeto se desactiva

    [Header("Opciones Avanzadas")]
    [SerializeField] private bool listenOnlyWhenActive = false; // Escuchar eventos solo cuando est� activo
    [SerializeField] private bool useTransition = false; // Usar transici�n al cambiar estado
    [SerializeField] private float transitionDuration = 0.5f; // Duraci�n de la transici�n

    private string currentSceneName; // Nombre de la escena actual
    private bool isInitialized = false;
    private bool lastKnownState; // �ltimo estado conocido para evitar activaciones repetidas

    // Propiedad para acceder al ID
    public string ObjectID => objectID;

    private void Awake()
    {
        // Si no hay ID asignado, usar el nombre del objeto
        if (string.IsNullOrEmpty(objectID))
        {
            objectID = gameObject.name;
        }

        // Obtener el nombre de la escena
        currentSceneName = useSceneName ? gameObject.scene.name : sceneName;

        lastKnownState = gameObject.activeSelf;
    }

    private void OnEnable()
    {
        Debug.Log($"WorldStateListener OnEnable: {gameObject.name}, ID: {objectID}, Scene: {currentSceneName}");

        // Registrarse para escuchar cambios
        RegisterToWorldState();

        // Aplicar estado inmediatamente si no se est� inicializando desde desactivado
        if (!isInitialized)
        {
            ApplyState();
            isInitialized = true;
        }
    }

    private void OnDisable()
    {
        // Solo desregistrarse si es una desactivaci�n real, no un cambio de escena
        if (!WorldStateManager.Instance.GetObjectActive(currentSceneName, objectID, defaultActiveState))
        {
            // Nos desregistramos �nicamente si la aplicaci�n sigue ejecut�ndose
            if (Application.isPlaying)
            {
                UnregisterFromWorldState();
            }
        }
    }

    // Registrarse para escuchar cambios
    private void RegisterToWorldState()
    {
        if (WorldStateManager.Instance != null)
        {
            string stateKey = $"Object_{currentSceneName}_{objectID}";
            WorldStateManager.Instance.RegisterFlagListener(stateKey, OnWorldStateChanged);
        }
    }

    // Desregistrarse
    private void UnregisterFromWorldState()
    {
        if (WorldStateManager.Instance != null)
        {
            string stateKey = $"Object_{currentSceneName}_{objectID}";
            WorldStateManager.Instance.RemoveFlagListener(stateKey, OnWorldStateChanged);
        }
    }

    private void OnWorldStateChanged(bool active)
    {
        // Verificar si este gameObject todav�a existe o est� siendo destruido
        if (this == null || gameObject == null)
        {
            // Este objeto ya no existe, desregistrar este listener
            if (WorldStateManager.Instance != null)
            {
                string stateKey = $"Object_{currentSceneName}_{objectID}";
                WorldStateManager.Instance.RemoveFlagListener(stateKey, OnWorldStateChanged);
            }
            return;
        }

        // Si solo escuchamos cuando est� activo y el objeto est� inactivo, ignorar
        if (listenOnlyWhenActive && !gameObject.activeSelf)
            return;

        ApplyState();
    }

    public void ApplyState()
    {
        if (this == null || gameObject == null)
        {
            Debug.LogWarning($"ApplyState llamado en objeto destruido o nulo");
            return;
        }

        if (WorldStateManager.Instance == null)
        {
            Debug.LogWarning($"WorldStateManager no disponible para {gameObject.name}");
            return;
        }

        bool shouldBeActive = WorldStateManager.Instance.GetObjectActive(
            currentSceneName, objectID, defaultActiveState);

        // Evitar cambios redundantes
        if (shouldBeActive == lastKnownState && isInitialized)
        {
            Debug.Log($"Estado sin cambios para {gameObject.name} ({objectID}): {shouldBeActive}");
            return;
        }

        lastKnownState = shouldBeActive;

        Debug.Log($"Aplicando estado a {gameObject.name} ({objectID}) en {currentSceneName}: {shouldBeActive}");

        // Activar/desactivar con o sin transici�n
        if (useTransition)
        {
            StartCoroutine(TransitionState(shouldBeActive));
        }
        else
        {
            // Activar/desactivar directamente
            gameObject.SetActive(shouldBeActive);

            // Invocar evento correspondiente
            if (shouldBeActive)
                onActivate?.Invoke();
            else
                onDeactivate?.Invoke();
        }
    }
    // Transici�n suave
    private System.Collections.IEnumerator TransitionState(bool activate)
    {
        if (activate)
        {
            // Primero activar el objeto
            gameObject.SetActive(true);

            // Invocar evento de activaci�n
            onActivate?.Invoke();

            // Efecto de entrada (puedes personalizar esto)
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
                float elapsedTime = 0;

                while (elapsedTime < transitionDuration)
                {
                    canvasGroup.alpha = Mathf.Lerp(0, 1, elapsedTime / transitionDuration);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                canvasGroup.alpha = 1;
            }
        }
        else
        {
            // Invocar evento de desactivaci�n
            onDeactivate?.Invoke();

            // Efecto de salida
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1;
                float elapsedTime = 0;

                while (elapsedTime < transitionDuration)
                {
                    canvasGroup.alpha = Mathf.Lerp(1, 0, elapsedTime / transitionDuration);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
            }

            // Finalmente desactivar
            gameObject.SetActive(false);
        }
    }

    // M�todo p�blico para alternar el estado
    public void ToggleState()
    {
        if (WorldStateManager.Instance != null)
        {
            bool currentState = WorldStateManager.Instance.GetObjectActive(
                currentSceneName, objectID, defaultActiveState);

            WorldStateManager.Instance.SetObjectActive(
                currentSceneName, objectID, !currentState);
        }
    }
}