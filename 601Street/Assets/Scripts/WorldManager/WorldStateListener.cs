using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class WorldStateListener : MonoBehaviour
{
    [Header("Configuración Básica")]
    [SerializeField] private string objectID;
    [SerializeField] private bool useSceneName = true;
    [SerializeField] private string sceneName;
    [SerializeField] private bool defaultActiveState = true;
    [SerializeField] private bool applyStateImmediately = true;

    [Header("Eventos")]
    [SerializeField] private UnityEvent onActivate;
    [SerializeField] private UnityEvent onDeactivate;

    [Header("Opciones Avanzadas")]
    [SerializeField] private bool listenOnlyWhenActive = false;
    [SerializeField] private bool useTransition = false;
    [SerializeField] private float transitionDuration = 0.5f;

    private string currentSceneName;
    private bool isInitialized = false;
    private bool lastKnownState;
    private string stateKey;
    private bool isDestroyed = false; // Nueva bandera para rastrear destrucción

    public string ObjectID => objectID;

    private void Awake()
    {
        if (string.IsNullOrEmpty(objectID))
        {
            objectID = gameObject.name;
        }

        currentSceneName = useSceneName ? gameObject.scene.name : sceneName;
        stateKey = $"Object_{currentSceneName}_{objectID}";
        lastKnownState = gameObject.activeSelf;
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(objectID))
        {
            objectID = gameObject.name;
            Debug.LogWarning($"WorldStateListener en {gameObject.name}: No se especificó ID, usando nombre del objeto");
        }

        currentSceneName = useSceneName ? gameObject.scene.name : sceneName;
        stateKey = $"Object_{currentSceneName}_{objectID}";

        RegisterToWorldState();

        if (applyStateImmediately && WorldStateManager.IsAvailable())
        {
            Debug.Log($"WorldStateListener: Aplicando estado inicial a {gameObject.name} (ID: {objectID}) en escena {currentSceneName}");
            ApplyState();
        }

        isInitialized = true;
    }

    private void OnEnable()
    {
        if (isInitialized && !isDestroyed)
        {
            RegisterToWorldState();
        }
    }

    private void OnDisable()
    {
        // Solo desregistrarse si no estamos siendo destruidos y el manager está disponible
        if (!isDestroyed && WorldStateManager.IsAvailable())
        {
            UnregisterFromWorldState();
        }
    }

    private void OnDestroy()
    {
        // Marcar como destruido para evitar accesos posteriores
        isDestroyed = true;

        // Intentar desregistrarse solo si el manager está disponible
        if (WorldStateManager.IsAvailable())
        {
            UnregisterFromWorldState();
        }
    }

    private void RegisterToWorldState()
    {
        // No registrarse si estamos destruidos o el manager no está disponible
        if (isDestroyed || !WorldStateManager.IsAvailable())
            return;

        WorldStateManager.Instance.RegisterFlagListener(stateKey, OnWorldStateChanged);
        Debug.Log($"WorldStateListener: {gameObject.name} registrado para escuchar cambios en {stateKey}");
    }

    private void UnregisterFromWorldState()
    {
        // Solo desregistrarse si el manager está disponible
        if (WorldStateManager.IsAvailable())
        {
            WorldStateManager.Instance.RemoveFlagListener(stateKey, OnWorldStateChanged);
        }
    }

    private void OnWorldStateChanged(bool active)
    {
        // Verificar si este gameObject todavía existe o está siendo destruido
        if (isDestroyed || this == null || gameObject == null)
        {
            // Este objeto ya no existe, desregistrar este listener
            if (WorldStateManager.IsAvailable())
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
        // No hacer nada si estamos destruidos o el objeto es nulo
        if (isDestroyed || this == null || gameObject == null)
        {
            Debug.LogWarning($"ApplyState llamado en objeto destruido o nulo");
            return;
        }

        // No hacer nada si el manager no está disponible
        if (!WorldStateManager.IsAvailable())
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
            try
            {
                if (shouldBeActive)
                    onActivate?.Invoke();
                else
                    onDeactivate?.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error al invocar eventos en {gameObject.name}: {e.Message}");
            }
        }
    }

    public void ToggleState()
    {
        if (isDestroyed || !WorldStateManager.IsAvailable())
            return;

        bool currentState = WorldStateManager.Instance.GetObjectActive(
            currentSceneName, objectID, defaultActiveState);

        WorldStateManager.Instance.SetObjectActive(
            currentSceneName, objectID, !currentState);

        if (applyStateImmediately)
        {
            ApplyState();
        }
    }

    private IEnumerator TransitionState(bool activate)
    {
        // Verificar que no estamos destruidos antes de empezar
        if (isDestroyed || this == null || gameObject == null)
            yield break;

        if (activate)
        {
            // Primero activar el objeto
            gameObject.SetActive(true);

            // Invocar evento de activación
            try
            {
                onActivate?.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error al invocar evento de activación: {e.Message}");
            }

            // Efecto de entrada
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
                float elapsedTime = 0;

                while (elapsedTime < transitionDuration && !isDestroyed && this != null && gameObject != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(0, 1, elapsedTime / transitionDuration);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                if (!isDestroyed && canvasGroup != null)
                {
                    canvasGroup.alpha = 1;
                }
            }
            else
            {
                // Opcional: Escala suave desde 0 a 1
                Transform targetTransform = transform;
                Vector3 originalScale = targetTransform.localScale;
                targetTransform.localScale = Vector3.zero;

                float elapsedTime = 0;
                while (elapsedTime < transitionDuration && !isDestroyed && this != null && gameObject != null)
                {
                    targetTransform.localScale = Vector3.Lerp(Vector3.zero, originalScale, elapsedTime / transitionDuration);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                if (!isDestroyed && targetTransform != null)
                {
                    targetTransform.localScale = originalScale;
                }
            }
        }
        else
        {
            // Invocar evento de desactivación
            try
            {
                onDeactivate?.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error al invocar evento de desactivación: {e.Message}");
            }

            // Efecto de salida
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1;
                float elapsedTime = 0;

                while (elapsedTime < transitionDuration && !isDestroyed && this != null && gameObject != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(1, 0, elapsedTime / transitionDuration);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
            }
            else
            {
                // Opcional: Escala suave desde tamaño actual a 0
                Transform targetTransform = transform;
                Vector3 originalScale = targetTransform.localScale;

                float elapsedTime = 0;
                while (elapsedTime < transitionDuration && !isDestroyed && this != null && gameObject != null)
                {
                    targetTransform.localScale = Vector3.Lerp(originalScale, Vector3.zero, elapsedTime / transitionDuration);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
            }

            // Finalmente desactivar el objeto (solo si no está destruido)
            if (!isDestroyed && this != null && gameObject != null)
            {
                gameObject.SetActive(false);
            }
        }
    }
}