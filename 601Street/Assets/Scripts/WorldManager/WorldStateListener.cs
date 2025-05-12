using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class WorldStateListener : MonoBehaviour
{
    [Header("Configuración Básica")]
    [SerializeField] private string objectID; // ID único para este objeto
    [SerializeField] private bool useSceneName = true; // Usar nombre de escena automáticamente
    [SerializeField] private string sceneName; // Nombre de escena manual si useSceneName=false
    [SerializeField] private bool defaultActiveState = true; // Estado por defecto del objeto
    [SerializeField] private bool applyStateImmediately = true; // Nuevo: aplicar cambios inmediatamente

    [Header("Eventos")]
    [SerializeField] private UnityEvent onActivate; // Evento cuando el objeto se activa
    [SerializeField] private UnityEvent onDeactivate; // Evento cuando el objeto se desactiva

    [Header("Opciones Avanzadas")]
    [SerializeField] private bool listenOnlyWhenActive = false; // Escuchar eventos solo cuando está activo
    [SerializeField] private bool useTransition = false; // Usar transición al cambiar estado
    [SerializeField] private float transitionDuration = 0.5f; // Duración de la transición

    private string currentSceneName; // Nombre de la escena actual
    private bool isInitialized = false;
    private bool lastKnownState; // Último estado conocido para evitar activaciones repetidas
    private string stateKey; // Clave para almacenar el estado

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

        // Generar la clave de estado consistentemente
        stateKey = $"Object_{currentSceneName}_{objectID}";

        lastKnownState = gameObject.activeSelf;
    }

    private void Start()
    {
        // Asegurarnos de que estamos registrados y aplicamos el estado inicial
        RegisterToWorldState();

        if (applyStateImmediately)
        {
            ApplyState();
        }

        isInitialized = true;
    }

    private void OnEnable()
    {
        // Si ya estamos inicializados, registrarnos para escuchar cambios
        if (isInitialized)
        {
            RegisterToWorldState();
        }
    }

    private void OnDisable()
    {
        // Solo desregistrarse si es una desactivación real, no un cambio de escena
        if (WorldStateManager.Instance != null && Application.isPlaying)
        {
            UnregisterFromWorldState();
        }
    }

    // Registrarse para escuchar cambios
    private void RegisterToWorldState()
    {
        if (WorldStateManager.Instance != null)
        {
            WorldStateManager.Instance.RegisterFlagListener(stateKey, OnWorldStateChanged);
            Debug.Log($"WorldStateListener: {gameObject.name} registrado para escuchar cambios en {stateKey}");
        }
        else
        {
            Debug.LogWarning($"WorldStateManager no disponible para {gameObject.name}");
        }
    }

    // Desregistrarse
    private void UnregisterFromWorldState()
    {
        if (WorldStateManager.Instance != null)
        {
            WorldStateManager.Instance.RemoveFlagListener(stateKey, OnWorldStateChanged);
        }
    }

    private void OnWorldStateChanged(bool active)
    {
        // Verificar si este gameObject todavía existe o está siendo destruido
        if (this == null || gameObject == null)
        {
            // Este objeto ya no existe, desregistrar este listener
            if (WorldStateManager.Instance != null)
            {
                WorldStateManager.Instance.RemoveFlagListener(stateKey, OnWorldStateChanged);
            }
            return;
        }

        // Si solo escuchamos cuando está activo y el objeto está inactivo, ignorar
        if (listenOnlyWhenActive && !gameObject.activeSelf)
            return;

        // Aplicar el estado inmediatamente
        if (applyStateImmediately)
        {
            ApplyState();
        }
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
            return;
        }

        lastKnownState = shouldBeActive;

        Debug.Log($"Aplicando estado a {gameObject.name} ({objectID}) en {currentSceneName}: {shouldBeActive}");

        // Activar/desactivar con o sin transición
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

    // Transición suave - método existente

    // Método público para alternar el estado
    public void ToggleState()
    {
        if (WorldStateManager.Instance != null)
        {
            bool currentState = WorldStateManager.Instance.GetObjectActive(
                currentSceneName, objectID, defaultActiveState);

            WorldStateManager.Instance.SetObjectActive(
                currentSceneName, objectID, !currentState);

            // Aplicar inmediatamente si es necesario
            if (applyStateImmediately)
            {
                ApplyState();
            }
        }
    }

    // Transición suave entre estados
    private IEnumerator TransitionState(bool activate)
    {
        if (activate)
        {
            // Primero activar el objeto
            gameObject.SetActive(true);

            // Invocar evento de activación
            onActivate?.Invoke();

            // Efecto de entrada
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
            // Si no hay CanvasGroup, podemos usar otros efectos opcionales
            else
            {
                // Opcional: Escala suave desde 0 a 1
                Transform targetTransform = transform;
                Vector3 originalScale = targetTransform.localScale;
                targetTransform.localScale = Vector3.zero;

                float elapsedTime = 0;
                while (elapsedTime < transitionDuration)
                {
                    targetTransform.localScale = Vector3.Lerp(Vector3.zero, originalScale, elapsedTime / transitionDuration);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                targetTransform.localScale = originalScale;
            }
        }
        else
        {
            // Invocar evento de desactivación
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
            // Si no hay CanvasGroup, intentar otros efectos
            else
            {
                // Opcional: Escala suave desde tamaño actual a 0
                Transform targetTransform = transform;
                Vector3 originalScale = targetTransform.localScale;

                float elapsedTime = 0;
                while (elapsedTime < transitionDuration)
                {
                    targetTransform.localScale = Vector3.Lerp(originalScale, Vector3.zero, elapsedTime / transitionDuration);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
            }

            // Finalmente desactivar el objeto
            gameObject.SetActive(false);
        }
    }
}