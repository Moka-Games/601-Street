using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Inventory_Item_Feedback : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject visualParent; // El objeto visual 3D en la escena

    [Header("Configuración")]
    public float edgeOffset = 50f; // Distancia desde el borde de la pantalla

    // Referencias que se encontrarán automáticamente
    private Canvas hudCanvas;
    private GameObject rangeIndicator; // Instancia del indicador de rango
    private GameObject interactIndicator; // Instancia del indicador de interacción

    private Inventory_Interactor inventory_Interactor;
    private bool playerOnRange = false;
    private RectTransform rangeIndicatorRect;
    private RectTransform interactIndicatorRect;
    private Camera mainCamera;
    private RectTransform canvasRectTransform;
    private bool isInitialized = false;

    private void Start()
    {
        // Buscar referencias automáticamente
        inventory_Interactor = FindAnyObjectByType<Inventory_Interactor>();
        visualParent = GameObject.Find("Inventory_Item_Feedback");
        mainCamera = Camera.main;

        // Buscar el HUD Canvas por nombre
        GameObject hudObj = GameObject.Find("HUD");
        if (hudObj != null)
        {
            hudCanvas = hudObj.GetComponent<Canvas>();
            if (hudCanvas == null)
            {
                hudCanvas = hudObj.GetComponentInChildren<Canvas>();
                if (hudCanvas == null)
                {
                    Debug.LogError("No se encontró componente Canvas en el objeto 'HUD'");
                    enabled = false; // Deshabilitar este script si no hay Canvas
                    return;
                }
            }

            canvasRectTransform = hudCanvas.GetComponent<RectTransform>();
            if (canvasRectTransform == null)
            {
                Debug.LogError("El Canvas no tiene un componente RectTransform");
                enabled = false;
                return;
            }
        }
        else
        {
            Debug.LogError("No se encontró objeto con nombre 'HUD'");
            enabled = false; 
            return;
        }

        GameObject nearItemFeedbackOriginal = GameObject.Find("Near_Item_Feedback");
        GameObject inputFeedbackOriginal = GameObject.Find("Input_Feedback");

        if (nearItemFeedbackOriginal == null)
        {
            Debug.LogError("No se encontró objeto con nombre 'Near_Item_Feedback'");
            enabled = false;
            return;
        }

        if (inputFeedbackOriginal == null)
        {
            Debug.LogError("No se encontró objeto con nombre 'Input_Feedback'");
            enabled = false;
            return;
        }

        // Guardar una referencia a los originales para poder desactivarlos si es necesario
        if (nearItemFeedbackOriginal.activeInHierarchy)
        {
            nearItemFeedbackOriginal.SetActive(false);
        }

        if (inputFeedbackOriginal.activeInHierarchy)
        {
            inputFeedbackOriginal.SetActive(false);
        }

        // Instanciar los indicadores como hijos del canvas
        rangeIndicator = Instantiate(nearItemFeedbackOriginal, hudCanvas.transform);
        interactIndicator = Instantiate(inputFeedbackOriginal, hudCanvas.transform);

        // Asegurarse de que tengan los nombres correctos para identificarlos fácilmente
        rangeIndicator.name = "RangeIndicator_" + gameObject.name;
        interactIndicator.name = "InteractIndicator_" + gameObject.name;

        // Obtener sus RectTransforms
        rangeIndicatorRect = rangeIndicator.GetComponent<RectTransform>();
        interactIndicatorRect = interactIndicator.GetComponent<RectTransform>();

        // Si los indicadores no tienen RectTransform, intentar encontrarlo en sus hijos
        if (rangeIndicatorRect == null)
        {
            rangeIndicatorRect = rangeIndicator.GetComponentInChildren<RectTransform>();
            if (rangeIndicatorRect == null)
            {
                Debug.LogError("No se pudo encontrar RectTransform para el indicador de rango");
                enabled = false;
                return;
            }
        }

        if (interactIndicatorRect == null)
        {
            interactIndicatorRect = interactIndicator.GetComponentInChildren<RectTransform>();
            if (interactIndicatorRect == null)
            {
                Debug.LogError("No se pudo encontrar RectTransform para el indicador de interacción");
                enabled = false;
                return;
            }
        }

        // Inicialmente ocultos (IMPORTANTE)
        rangeIndicator.SetActive(false);
        interactIndicator.SetActive(false);

        playerOnRange = false;

        // Verificación final
        if (inventory_Interactor == null)
        {
            Debug.LogError("No se pudo encontrar Inventory_Interactor en la escena");
            enabled = false;
            return;
        }

        isInitialized = true;
    }

    private void Update()
    {
        // Verificar si el objeto sigue existiendo (para manejar la destrucción)
        if (this == null || !gameObject.activeInHierarchy || !isInitialized)
        {
            CleanupIndicators();
            return;
        }

        // Comprobaciones de seguridad
        if (rangeIndicator == null || interactIndicator == null || !enabled)
        {
            CleanupIndicators();
            return;
        }

        if (playerOnRange)
        {
            // Actualizar la posición de los indicadores
            UpdateIndicatorPosition();

            if (inventory_Interactor != null && inventory_Interactor.canInteract)
            {
                rangeIndicator.SetActive(false);
                interactIndicator.SetActive(true);
            }
            else
            {
                rangeIndicator.SetActive(true);
                interactIndicator.SetActive(false);
            }
        }
        else
        {
            rangeIndicator.SetActive(false);
            interactIndicator.SetActive(false);
        }
    }

    private void UpdateIndicatorPosition()
    {
        // Verificar que tengamos todas las referencias necesarias
        if (mainCamera == null || rangeIndicatorRect == null || interactIndicatorRect == null)
        {
            Debug.LogWarning("Faltan referencias para actualizar la posición de los indicadores");
            return;
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

                // Opcional: Rotar el indicador para que apunte hacia el objeto
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
        // Método simplificado para posicionar elementos UI
        // Este método evita usar RectTransformUtility.ScreenPointToLocalPointInRectangle que estaba causando problemas

        if (hudCanvas == null || rectTransform == null)
            return;

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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerOnRange = true;
            Debug.Log("Player on range");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerOnRange = false;
            Debug.Log("Player out of range");
        }
    }

    private void CleanupIndicators()
    {
        // Eliminar los indicadores si existen
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(false);
            Destroy(rangeIndicator);
        }

        if (interactIndicator != null)
        {
            interactIndicator.SetActive(false);
            Destroy(interactIndicator);
        }
    }

    private void OnDisable()
    {
        // Limpiar cuando se deshabilite este script
        CleanupIndicators();
    }

    private void OnDestroy()
    {
        // Limpiar cuando se destruya este objeto
        CleanupIndicators();
    }
}