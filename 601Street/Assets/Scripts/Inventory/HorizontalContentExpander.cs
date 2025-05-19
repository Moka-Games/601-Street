using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Componente que expande din�micamente el ancho del RectTransform hacia la derecha (X positivo)
/// con opciones flexibles de alineamiento y padding
/// </summary>
public class HorizontalContentExpander : MonoBehaviour
{
    [Header("Configuraci�n de Expansi�n")]
    [Tooltip("Cu�ntos elementos caben en una fila antes de necesitar expandirse")]
    public int elementsPerRow = 6;

    [Tooltip("Ancho de cada elemento")]
    public float elementWidth = 100f;

    [Tooltip("Espaciado horizontal entre elementos")]
    public float horizontalSpacing = 10f;

    [Tooltip("Margen adicional al final para asegurar que el �ltimo elemento sea visible")]
    public float rightMargin = 20f;

    [Header("Configuraci�n Visual")]
    [Tooltip("Padding izquierdo para evitar que el primer elemento se vea cortado")]
    public float leftPadding = 15f;

    [Tooltip("Alineamiento de los elementos hijos")]
    public TextAnchor childAlignment = TextAnchor.MiddleCenter;

    [Header("Configuraci�n T�cnica")]
    [Tooltip("Forzar configuraci�n de anclaje y pivote para expansi�n hacia la derecha")]
    public bool forceAnchorAndPivotSetup = true;

    // Referencia al RectTransform que se expandir�
    private RectTransform rectTransform;

    // Referencia al HorizontalLayoutGroup
    private HorizontalLayoutGroup layoutGroup;

    // Tama�o y posici�n iniciales
    private float initialWidth;
    private float initialLeftEdgePosition;
    private Vector2 initialAnchoredPosition;

    // Contador de elementos
    private int elementCount = 0;

    private void Awake()
    {
        // Obtener el RectTransform
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("HorizontalContentExpander: El GameObject debe tener un RectTransform.");
            return;
        }

        // Obtener o crear el HorizontalLayoutGroup
        layoutGroup = GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();
            Debug.Log("HorizontalContentExpander: Se ha a�adido autom�ticamente un HorizontalLayoutGroup.");
        }

        if (forceAnchorAndPivotSetup)
        {
            // Configurar el anclaje para que est� en el lado izquierdo
            rectTransform.anchorMin = new Vector2(0, 0.5f);
            rectTransform.anchorMax = new Vector2(0, 0.5f);

            // Configurar el pivote para que est� en el lado izquierdo (esto es crucial para expandir hacia la derecha)
            rectTransform.pivot = new Vector2(0, 0.5f);
        }

        // Guardar el ancho inicial y la posici�n del borde izquierdo
        initialWidth = rectTransform.rect.width;
        initialAnchoredPosition = rectTransform.anchoredPosition;
        initialLeftEdgePosition = initialAnchoredPosition.x;

        // Contar elementos iniciales
        elementCount = transform.childCount;

        // Configurar el HorizontalLayoutGroup
        ConfigureLayoutGroup();

        // Aplicar tama�o inicial
        UpdateContentSize();

        Debug.Log($"HorizontalContentExpander inicializado. Ancho inicial: {initialWidth}, Posici�n inicial: {initialAnchoredPosition}");
    }

    private void OnEnable()
    {
        // Actualizar configuraci�n y tama�o cuando se activa el objeto
        ConfigureLayoutGroup();
        UpdateContentSize();
    }

    /// <summary>
    /// Configura el HorizontalLayoutGroup seg�n las preferencias
    /// </summary>
    private void ConfigureLayoutGroup()
    {
        if (layoutGroup == null) return;

        // Configurar el alineamiento seg�n la preferencia
        layoutGroup.childAlignment = childAlignment;
        layoutGroup.spacing = horizontalSpacing;

        // Configurar el padding para evitar que el primer elemento se vea cortado
        RectOffset padding = new RectOffset();
        padding.left = Mathf.RoundToInt(leftPadding);
        padding.right = 0; // El padding derecho no es necesario ya que expandimos el contenedor
        padding.top = 0;
        padding.bottom = 0;
        layoutGroup.padding = padding;

        // Configuraci�n adicional
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;

        Debug.Log($"HorizontalLayoutGroup configurado: Alineamiento={childAlignment}, Padding Izquierdo={leftPadding}");
    }

    /// <summary>
    /// Actualiza las propiedades del HorizontalLayoutGroup en tiempo de ejecuci�n
    /// </summary>
    public void UpdateLayoutGroupProperties()
    {
        if (layoutGroup == null) return;

        // Actualizar el alineamiento
        layoutGroup.childAlignment = childAlignment;
        layoutGroup.spacing = horizontalSpacing;

        // Actualizar el padding
        RectOffset padding = layoutGroup.padding;
        padding.left = Mathf.RoundToInt(leftPadding);
        layoutGroup.padding = padding;

        // Forzar reconstrucci�n del layout
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

        // Actualizar el tama�o tambi�n
        UpdateContentSize();
    }

    /// <summary>
    /// Llamar a este m�todo cada vez que se a�ada un nuevo elemento
    /// </summary>
    public void AddElement()
    {
        elementCount++;
        UpdateContentSize();
    }

    /// <summary>
    /// Llamar a este m�todo cada vez que se elimine un elemento
    /// </summary>
    public void RemoveElement()
    {
        if (elementCount > 0)
        {
            elementCount--;
            UpdateContentSize();
        }
    }

    /// <summary>
    /// Actualiza el tama�o del contenido basado en el n�mero de elementos
    /// </summary>
    public void UpdateContentSize()
    {
        if (rectTransform == null) return;

        // Si no hay elementos, usar el tama�o inicial
        if (elementCount <= 0)
        {
            rectTransform.sizeDelta = new Vector2(initialWidth, rectTransform.sizeDelta.y);
            return;
        }

        // Calcular el ancho total necesario, incluyendo el padding izquierdo
        float requiredWidth = elementCount * (elementWidth + horizontalSpacing) - horizontalSpacing + rightMargin + leftPadding;

        // Asegurarse de que el ancho sea al menos el inicial
        requiredWidth = Mathf.Max(requiredWidth, initialWidth);

        // Aplicar el nuevo tama�o, manteniendo la altura original
        Vector2 newSize = rectTransform.sizeDelta;
        newSize.x = requiredWidth;
        rectTransform.sizeDelta = newSize;

        // IMPORTANTE: Mantener la posici�n del borde izquierdo constante
        Vector2 position = rectTransform.anchoredPosition;
        position.x = initialLeftEdgePosition;
        rectTransform.anchoredPosition = position;

        Debug.Log($"Content expandido. Nuevo ancho: {requiredWidth}, Posici�n X: {position.x}");
    }

    /// <summary>
    /// Recalcula el tama�o basado en el n�mero actual de hijos
    /// </summary>
    public void RecalculateSize()
    {
        elementCount = transform.childCount;
        UpdateContentSize();
    }

    /// <summary>
    /// M�todo para inspector que permite actualizar la configuraci�n en tiempo de ejecuci�n
    /// </summary>
    public void ApplyChanges()
    {
        ConfigureLayoutGroup();
        RecalculateSize();
    }

    // En el editor, permitir actualizar la configuraci�n desde el inspector
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && layoutGroup != null)
        {
            UpdateLayoutGroupProperties();
        }
    }
#endif
}