using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public interface IInteractable
{
    void Interact();
    void SecondInteraction();
    string GetInteractionID();
    bool CanBeInteractedAgain();
}
public class InteractableObject : MonoBehaviour, IInteractable
{
    [Header("Configuración básica")]
    [SerializeField] private string interactionID;
    [Tooltip("Evento que se disparará cuando el jugador interactúe con este objeto")]
    public UnityEvent onInteraction;
    [SerializeField] private UnityEvent onInteracted; //Evento por si volvemos a interactuar con el mismo objeto

    [Header("Comportamiento de interacción")]
    [Tooltip("Si está activado, este objeto solo podrá ser interactuado una vez")]
    [SerializeField] private bool singleUseInteraction = false;
    [Tooltip("Si está activado, el objeto se desactivará después de una interacción (solo aplica si singleUseInteraction = true)")]
    [SerializeField] private bool disableAfterInteraction = false;

    [Header("Detección de Rango")]
    [SerializeField] private float detectionRadius = 5f; // Radio del SphereCollider

    [Header("Configuración de Indicadores")]
    [SerializeField] private float edgeOffset = 50f; // Distancia desde el borde de la pantalla

    // Referencias a los indicadores
    private GameObject rangeIndicator;
    private GameObject interactIndicator;

    private bool playerOnRange = false;
    private bool isInitialized = false;

    private SphereCollider detectionCollider; // Collider para el rango de detección
    private PlayerInteraction playerInteraction;
    private Camera mainCamera;
    private RectTransform rangeIndicatorRect;
    private RectTransform interactIndicatorRect;
    private Canvas hudCanvas;
    private RectTransform canvasRectTransform;

    public bool objectInteracted = false;
    private float enterTime;
    private bool isExiting = false;

    private Vector3 lastScale;


    private void Start()
    {
        // Crear y configurar el SphereCollider
        detectionCollider = gameObject.AddComponent<SphereCollider>();
        detectionCollider.radius = detectionRadius;
        detectionCollider.isTrigger = true;
        
        float maxScaleFactor = Mathf.Max(
       Mathf.Abs(transform.lossyScale.x),
       Mathf.Max(Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.lossyScale.z))
   );

        detectionCollider.radius = detectionRadius / maxScaleFactor;
        detectionCollider.isTrigger = true;

        mainCamera = Camera.main;
        
        // Obtener el Canvas HUD a través del gestor
        hudCanvas = UIFeedbackManager.Instance.GetHUDCanvas();
        if (hudCanvas != null)
        {
            canvasRectTransform = hudCanvas.GetComponent<RectTransform>();
        }

        // Crear indicadores a través del gestor
        CreateFeedbackIndicators();

        // Buscar el UnifiedPlayerInteraction
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerInteraction = player.GetComponent<PlayerInteraction>();
        }

        isInitialized = true;
    }

    private void CreateFeedbackIndicators()
    {
        // Obtener el nombre único para este objeto
        string objectIdentifier = gameObject.name + "_" + GetInstanceID();

        // Crear indicadores a través del gestor
        rangeIndicator = UIFeedbackManager.Instance.CreateRangeIndicator(objectIdentifier);
        interactIndicator = UIFeedbackManager.Instance.CreateInteractIndicator(objectIdentifier);

        // Obtener los RectTransform
        if (rangeIndicator != null)
        {
            rangeIndicatorRect = rangeIndicator.GetComponent<RectTransform>();
            if (rangeIndicatorRect == null)
            {
                rangeIndicatorRect = rangeIndicator.GetComponentInChildren<RectTransform>();
            }
        }

        if (interactIndicator != null)
        {
            interactIndicatorRect = interactIndicator.GetComponent<RectTransform>();
            if (interactIndicatorRect == null)
            {
                interactIndicatorRect = interactIndicator.GetComponentInChildren<RectTransform>();
            }
        }

        // Comprobar que todo está bien
        if (rangeIndicator == null || interactIndicator == null ||
            rangeIndicatorRect == null || interactIndicatorRect == null)
        {
            Debug.LogError("No se pudieron crear o configurar los indicadores para " + gameObject.name);
            enabled = false;
            return;
        }

        Debug.Log("Indicadores de feedback creados para " + gameObject.name);
    }

    // Implementación de IInteractable
    public virtual void Interact()
    {
        Debug.Log($"Interactuando con objeto: {gameObject.name} (ID: {interactionID})");
        onInteraction.Invoke();

        // Marcar como interactuado
        objectInteracted = true;

        // Si es de un solo uso, destruir los indicadores de feedback
        if (singleUseInteraction)
        {
            // Destruir los indicadores visuales
            DestroyFeedbackIndicators();

            // Si está configurado para desactivarse después de la interacción
            if (disableAfterInteraction)
            {
                // Desactivar el objeto después de un breve retardo para permitir que las animaciones terminen
                //StartCoroutine(DisableAfterDelay(0.5f));
            }
        }
    }

    public virtual void SecondInteraction()
    {
        // Solo permitir segunda interacción si no es de un solo uso
        if (!singleUseInteraction)
        {
            Debug.Log($"Interactuando por segunda vez con objeto: {gameObject.name} (ID: {interactionID})");
            onInteracted.Invoke();
        }
    }

    // Implementación del método de la interfaz para verificar si puede volver a interactuarse
    public bool CanBeInteractedAgain()
    {
        return !singleUseInteraction || !objectInteracted;
    }

    public string GetInteractionID()
    {
        return interactionID;
    }

    // Método para destruir los indicadores de feedback
    private void DestroyFeedbackIndicators()
    {
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(false);
            Destroy(rangeIndicator);
            rangeIndicator = null;
        }

        if (interactIndicator != null)
        {
            interactIndicator.SetActive(false);
            Destroy(interactIndicator);
            interactIndicator = null;
        }

        // Desactivar el collider de detección para que el jugador no pueda volver a activar los indicadores
        if (detectionCollider != null)
        {
            detectionCollider.enabled = false;
        }
    }

    private void Update()
    {
        if (transform.lossyScale != lastScale)
        {
            float maxScaleFactor = Mathf.Max(
                Mathf.Abs(transform.lossyScale.x),
                Mathf.Max(Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.lossyScale.z))
            );

            detectionCollider.radius = detectionRadius / maxScaleFactor;
            lastScale = transform.lossyScale;
        }
        // Si es un objeto de un solo uso y ya fue interactuado, no hacer nada
        if (singleUseInteraction && objectInteracted)
        {
            return;
        }

        if (!isInitialized || !playerOnRange)
        {
            // Si no está inicializado o el jugador no está en rango, asegurarse que todo está desactivado
            if (rangeIndicator != null) rangeIndicator.SetActive(false);
            if (interactIndicator != null) interactIndicator.SetActive(false);
            return;
        }

        // Actualizar la posición de los indicadores en la pantalla
        UpdateIndicatorPosition();

        // Verificar si el jugador puede interactuar (pero solo después de cierto tiempo)
        if (Time.time - enterTime > 0.2f)
        {
            // Verificar si este objeto específico es el objetivo actual del raycast
            bool isTargetObject = IsTargetOfPlayerInteraction();

            // Modificación para que siempre muestre el indicador de rango cuando el jugador está en rango
            // pero el indicador de interacción solo cuando este objeto es el objetivo del raycast
            if (isTargetObject && playerInteraction != null && playerInteraction.canInteract)
            {
                // Este objeto es el objetivo y el jugador puede interactuar: mostrar indicador de interacción
                if (rangeIndicator != null) rangeIndicator.SetActive(false);
                if (interactIndicator != null) interactIndicator.SetActive(true);
            }
            else
            {
                // El jugador está en rango pero no está mirando este objeto o no puede interactuar: mostrar indicador de rango
                if (rangeIndicator != null) rangeIndicator.SetActive(true);
                if (interactIndicator != null) interactIndicator.SetActive(false);
            }
        }
    }

    // Método para verificar si este objeto es el objetivo actual del raycast del nuevo sistema
    private bool IsTargetOfPlayerInteraction()
    {
        if (playerInteraction == null)
            return false;

        // Lanzar un raycast desde la posición del jugador en la dirección que mira
        RaycastHit hit;
        if (Physics.Raycast(playerInteraction.transform.position,
                         playerInteraction.transform.forward,
                         out hit,
                         detectionRadius * 2,
                         1 << gameObject.layer))
        {
            // Comprobar si el raycast golpeó este objeto específico
            return hit.collider.gameObject == this.gameObject;
        }
        return false;
    }

    private void UpdateIndicatorPosition()
    {
        // Verificar que tengamos todas las referencias necesarias
        if (mainCamera == null || rangeIndicatorRect == null || interactIndicatorRect == null)
        {
            // Si no tenemos la cámara principal, intentar obtenerla
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null || rangeIndicatorRect == null || interactIndicatorRect == null)
            {
                Debug.LogWarning("Faltan referencias para actualizar la posición de los indicadores");
                return;
            }
        }

        // Convertir la posición del objeto a coordenadas de pantalla
        Vector3 screenPos = mainCamera.WorldToScreenPoint(transform.position);

        // Verificar si el objeto está frente a la cámara
        bool isInFrontOfCamera = screenPos.z > 0;

        if (isInFrontOfCamera)
        {
            // Obtener las dimensiones de la pantalla
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            Vector2 screenCenter = screenSize * 0.5f;

            // Comprobar si está dentro de la vista de la cámara
            bool isVisible = screenPos.x >= 0 && screenPos.x <= screenSize.x &&
                            screenPos.y >= 0 && screenPos.y <= screenSize.y;

            if (isVisible)
            {
                // El objeto es visible, posiciona el indicador directamente sobre él
                SetUIPosition(rangeIndicatorRect, screenPos);
                SetUIPosition(interactIndicatorRect, screenPos);

                // Restablecer la rotación cuando está visible
                rangeIndicatorRect.rotation = Quaternion.identity;
                interactIndicatorRect.rotation = Quaternion.identity;
            }
            else
            {
                // El objeto está fuera de la pantalla, posicionar en el borde
                Vector2 directionToObject = new Vector2(screenPos.x - screenCenter.x, screenPos.y - screenCenter.y).normalized;
                Vector2 edgePosition = screenCenter + directionToObject *
                    (Vector2.Distance(Vector2.zero, new Vector2(screenCenter.x - edgeOffset, screenCenter.y - edgeOffset)));

                // Asegurarse de que no se sale de los bordes de la pantalla
                edgePosition.x = Mathf.Clamp(edgePosition.x, edgeOffset, screenSize.x - edgeOffset);
                edgePosition.y = Mathf.Clamp(edgePosition.y, edgeOffset, screenSize.y - edgeOffset);

                SetUIPosition(rangeIndicatorRect, edgePosition);
                SetUIPosition(interactIndicatorRect, edgePosition);

                // Rotar el indicador para que apunte hacia el objeto
                float angle = Mathf.Atan2(directionToObject.y, directionToObject.x) * Mathf.Rad2Deg;
                rangeIndicatorRect.rotation = Quaternion.Euler(0, 0, angle - 90);
                interactIndicatorRect.rotation = Quaternion.Euler(0, 0, angle - 90);
            }
        }
        else
        {
            // Si el objeto está detrás de la cámara, colocar en el borde inferior
            Vector2 edgePosition = new Vector2(Screen.width * 0.5f, edgeOffset);
            SetUIPosition(rangeIndicatorRect, edgePosition);
            SetUIPosition(interactIndicatorRect, edgePosition);

            // Apuntar hacia abajo (objeto está detrás)
            rangeIndicatorRect.rotation = Quaternion.Euler(0, 0, 180);
            interactIndicatorRect.rotation = Quaternion.Euler(0, 0, 180);
        }
    }

    private void SetUIPosition(RectTransform rectTransform, Vector2 screenPosition)
    {
        if (rectTransform == null)
            return;

        if (hudCanvas != null)
        {
            // Determinar el tipo de renderizado del canvas
            if (hudCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // Para ScreenSpaceOverlay, las coordenadas de pantalla se pueden usar directamente
                rectTransform.position = new Vector3(screenPosition.x, screenPosition.y, 0);
            }
            else if (hudCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                // Para ScreenSpaceCamera, convertir de coordenadas de pantalla a viewport
                Vector2 viewportPosition = new Vector2(screenPosition.x / Screen.width, screenPosition.y / Screen.height);

                // Y luego a coordenadas de mundo
                Vector3 worldPos = hudCanvas.worldCamera != null ?
                    hudCanvas.worldCamera.ViewportToWorldPoint(new Vector3(viewportPosition.x, viewportPosition.y, hudCanvas.planeDistance)) :
                    Camera.main.ViewportToWorldPoint(new Vector3(viewportPosition.x, viewportPosition.y, 10));

                rectTransform.position = worldPos;
            }
            else // RenderMode.WorldSpace
            {
                // Para WorldSpace, usar un enfoque diferente
                if (canvasRectTransform != null)
                {
                    // Calcular la posición relativa al canvas
                    Vector2 canvasSize = canvasRectTransform.sizeDelta;
                    float canvasScale = canvasRectTransform.localScale.x;

                    // Convertir de coordenadas de pantalla a coordenadas normalizadas (0-1)
                    Vector2 normalizedPos = new Vector2(
                        screenPosition.x / Screen.width,
                        screenPosition.y / Screen.height
                    );

                    // Convertir a coordenadas locales del canvas
                    Vector2 localPos = new Vector2(
                        (normalizedPos.x - 0.5f) * canvasSize.x,
                        (normalizedPos.y - 0.5f) * canvasSize.y
                    );

                    rectTransform.localPosition = new Vector3(localPos.x, localPos.y, 0);
                }
            }
        }
        else
        {
            // Si no tenemos referencia al canvas, usar el método por defecto
            rectTransform.position = new Vector3(screenPosition.x, screenPosition.y, 0);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Si es un objeto de un solo uso y ya fue interactuado, ignorar
        if (singleUseInteraction && objectInteracted)
        {
            return;
        }

        enterTime = Time.time;

        // Verificamos si el objeto que entra en el trigger es el jugador
        if (other.CompareTag("Player"))
        {
            playerOnRange = true;
            playerInteraction = other.gameObject.GetComponent<PlayerInteraction>();
            Debug.Log("Jugador en rango de detección");

            // Desactivar explícitamente ambos indicadores
            if (rangeIndicator != null) rangeIndicator.SetActive(false);
            if (interactIndicator != null) interactIndicator.SetActive(false);

            // Usar una corrutina con más espera para asegurar que no haya parpadeos
            StartCoroutine(DelayedIndicatorActivation());
        }
    }

    private IEnumerator DelayedIndicatorActivation()
    {
        // Esperamos varios frames para asegurarnos que todo se ha actualizado correctamente
        yield return new WaitForSeconds(0.1f);

        // Solo si seguimos en rango y el objeto puede ser interactuado
        if (playerOnRange && (!singleUseInteraction || !objectInteracted))
        {
            // Verificar si este objeto es el objetivo del raycast
            bool isTargetObject = IsTargetOfPlayerInteraction();

            // Si es el objetivo y puede interactuar, mostrar indicador de interacción
            if (isTargetObject && playerInteraction != null && playerInteraction.canInteract)
            {
                if (rangeIndicator != null) rangeIndicator.SetActive(false);
                if (interactIndicator != null) interactIndicator.SetActive(true);
            }
            else
            {
                // De lo contrario, mostrar indicador de rango
                if (rangeIndicator != null) rangeIndicator.SetActive(true);
                if (interactIndicator != null) interactIndicator.SetActive(false);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Verificamos si el objeto que sale del trigger es el jugador
        if (other.CompareTag("Player"))
        {
            // Marcar que estamos en proceso de salida
            isExiting = true;

            // Desactivar INMEDIATAMENTE ambos indicadores
            if (interactIndicator != null)
            {
                interactIndicator.gameObject.SetActive(false);
            }

            if (rangeIndicator != null)
            {
                rangeIndicator.gameObject.SetActive(false);
            }

            playerOnRange = false;
            playerInteraction = null;

            Debug.Log("Jugador fuera de rango de detección");

            // Cancelar cualquier corrutina pendiente
            StopAllCoroutines();

            // Iniciar una corrutina de limpieza para asegurarnos que todo está desactivado
            StartCoroutine(CleanupAfterExit());
        }
    }

    private IEnumerator CleanupAfterExit()
    {
        // Esperar un par de frames para asegurarnos que todo está procesado
        yield return null;
        yield return null;

        // Desactivar los indicadores de nuevo, por si acaso
        if (interactIndicator != null) interactIndicator.gameObject.SetActive(false);
        if (rangeIndicator != null) rangeIndicator.gameObject.SetActive(false);

        // Esperar un poco más
        yield return new WaitForSeconds(0.1f);

        // Desactivar una vez más
        if (interactIndicator != null) interactIndicator.gameObject.SetActive(false);
        if (rangeIndicator != null) rangeIndicator.gameObject.SetActive(false);

        isExiting = false;
    }

    private void OnDisable()
    {
        if (rangeIndicator != null) rangeIndicator.SetActive(false);
        if (interactIndicator != null) interactIndicator.SetActive(false);
    }

    private void OnDestroy()
    {
        if (rangeIndicator != null) Destroy(rangeIndicator);
        if (interactIndicator != null) Destroy(interactIndicator);
    }

    // Método para dibujar Gizmos en el editor
    private void OnDrawGizmosSelected()
    {
        // Dibujar un círculo que represente el rango de detección
        Gizmos.color = Color.cyan; // Color del Gizmo
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}