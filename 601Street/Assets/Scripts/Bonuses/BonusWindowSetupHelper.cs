using UnityEngine;

/// <summary>
/// Helper para configurar automáticamente la ventana de bonuses
/// Añade este script temporalmente al Parent/Background para configurar posiciones
/// </summary>
public class BonusWindowSetupHelper : MonoBehaviour
{
    [Header("Configuración Automática")]
    [SerializeField] private bool autoConfigureOnStart = true;
    [SerializeField] private float closedRightPosition = 465f; // Right = 465 (cerrado)
    [SerializeField] private float openRightPosition = 260f;   // Right = 260 (abierto)

    private RectTransform rectTransform;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        if (autoConfigureOnStart && rectTransform != null)
        {
            ConfigurePositions();
        }
    }

    [ContextMenu("Configure Closed Position")]
    public void SetToClosedPosition()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            Vector2 currentOffsetMax = rectTransform.offsetMax;
            rectTransform.offsetMax = new Vector2(-closedRightPosition, currentOffsetMax.y);
            Debug.Log($"Panel configurado en posición cerrada: Right = {closedRightPosition}");
        }
    }

    [ContextMenu("Configure Open Position")]
    public void SetToOpenPosition()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            Vector2 currentOffsetMax = rectTransform.offsetMax;
            rectTransform.offsetMax = new Vector2(-openRightPosition, currentOffsetMax.y);
            Debug.Log($"Panel configurado en posición abierta: Right = {openRightPosition}");
        }
    }

    [ContextMenu("Configure Rect Mask")]
    public void ConfigureRectMask()
    {
        GameObject parent = transform.parent?.gameObject;
        if (parent != null)
        {
            UnityEngine.UI.RectMask2D mask = parent.GetComponent<UnityEngine.UI.RectMask2D>();
            if (mask == null)
            {
                mask = parent.AddComponent<UnityEngine.UI.RectMask2D>();
                Debug.Log($"Rect Mask 2D añadida a: {parent.name}");
            }
            else
            {
                Debug.Log($"Rect Mask 2D ya existe en: {parent.name}");
            }
        }
    }

    [ContextMenu("Full Auto Setup")]
    public void ConfigurePositions()
    {
        Debug.Log("=== CONFIGURACIÓN AUTOMÁTICA DE VENTANA DE BONUSES ===");

        // Configurar máscara en el padre
        ConfigureRectMask();

        // Configurar posición inicial (cerrada)
        SetToClosedPosition();

        // Mostrar información
        Debug.Log($"Posición cerrada configurada: Right = {closedRightPosition}");
        Debug.Log($"Posición abierta disponible: Right = {openRightPosition}");
        Debug.Log("Configuración completada. Puedes remover este script ahora.");

        Debug.Log("===================================================");
    }

    [ContextMenu("Test Animation")]
    public void TestAnimation()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("La animación de prueba solo funciona en Play Mode");
            return;
        }

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        Debug.Log("Probando animación...");

        float currentRight = -rectTransform.offsetMax.x;
        bool isOpen = Mathf.Approximately(currentRight, openRightPosition);

        if (isOpen)
        {
            // Cerrar - mover a Right = 465
            Vector2 currentOffsetMax = rectTransform.offsetMax;
            Vector2 targetOffsetMax = new Vector2(-closedRightPosition, currentOffsetMax.y);

            DG.Tweening.DOTween.To(
                () => rectTransform.offsetMax,
                (value) => rectTransform.offsetMax = value,
                targetOffsetMax,
                0.5f
            );
            Debug.Log("Cerrando ventana...");
        }
        else
        {
            // Abrir - mover a Right = 260
            Vector2 currentOffsetMax = rectTransform.offsetMax;
            Vector2 targetOffsetMax = new Vector2(-openRightPosition, currentOffsetMax.y);

            DG.Tweening.DOTween.To(
                () => rectTransform.offsetMax,
                (value) => rectTransform.offsetMax = value,
                targetOffsetMax,
                0.5f
            );
            Debug.Log("Abriendo ventana...");
        }
    }

    [ContextMenu("Debug Current State")]
    public void DebugCurrentState()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        Debug.Log("=== ESTADO ACTUAL DE LA VENTANA ===");
        Debug.Log($"GameObject: {gameObject.name}");
        Debug.Log($"Right actual: {-rectTransform.offsetMax.x}");
        Debug.Log($"Right cerrado objetivo: {closedRightPosition}");
        Debug.Log($"Right abierto objetivo: {openRightPosition}");

        float currentRight = -rectTransform.offsetMax.x;
        if (Mathf.Approximately(currentRight, closedRightPosition))
        {
            Debug.Log("Estado: CERRADO ✅");
        }
        else if (Mathf.Approximately(currentRight, openRightPosition))
        {
            Debug.Log("Estado: ABIERTO ✅");
        }
        else
        {
            Debug.Log($"Estado: POSICIÓN PERSONALIZADA (Right = {currentRight})");
        }

        // Verificar máscara en padre
        GameObject parent = transform.parent?.gameObject;
        if (parent != null)
        {
            bool hasMask = parent.GetComponent<UnityEngine.UI.RectMask2D>() != null;
            Debug.Log($"Padre ({parent.name}) tiene Rect Mask 2D: {(hasMask ? "✅" : "❌")}");
        }

        Debug.Log("================================");
    }
}