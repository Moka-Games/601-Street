using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Modifica el Inventory_Manager para expandir horizontalmente los contenedores
/// </summary>
public class InventoryModifier : MonoBehaviour
{
    [Header("Referencias del Inventory Manager")]
    public Inventory_Manager inventoryManager;

    [Header("Configuración de Expansión Horizontal")]
    [Tooltip("Elementos por fila antes de expandirse")]
    public int elementsPerRow = 6;

    [Tooltip("Ancho de cada elemento en el inventario")]
    public float elementWidth = 100f;

    [Tooltip("Espaciado horizontal entre elementos")]
    public float horizontalSpacing = 10f;

    [Tooltip("Margen adicional a la derecha")]
    public float rightMargin = 20f;

    // Componentes de expansión para cada contenedor
    private HorizontalContentExpander noteContentExpander;
    private HorizontalContentExpander objectContentExpander;

    // Contador de elementos
    private int noteItemCount = 0;
    private int objectItemCount = 0;

    // Referencias a los contenedores originales
    private RectTransform noteContainer;
    private RectTransform objectContainer;

    void Start()
    {
        if (inventoryManager == null)
        {
            // Intentar obtener el Inventory_Manager del mismo GameObject
            inventoryManager = GetComponent<Inventory_Manager>();

            if (inventoryManager == null)
            {
                Debug.LogError("InventoryModifier: No se encontró el componente Inventory_Manager.");
                return;
            }
        }

        // Obtener los contenedores del Inventory_Manager
        noteContainer = inventoryManager.noteContainer as RectTransform;
        objectContainer = inventoryManager.objectContainer as RectTransform;

        if (noteContainer == null || objectContainer == null)
        {
            Debug.LogError("InventoryModifier: Los contenedores del Inventory_Manager no son RectTransforms.");
            return;
        }

        // Configurar los expandidores para cada contenedor
        SetupContentExpander(noteContainer, out noteContentExpander);
        SetupContentExpander(objectContainer, out objectContentExpander);

        // Hookear en el método AddItem del Inventory_Manager
        // Esto se hace mediante MonoPatching en tiempo de ejecución
        HookInventoryManagerMethods();
    }

    /// <summary>
    /// Configura un expandidor de contenido para el contenedor dado
    /// </summary>
    private void SetupContentExpander(RectTransform container, out HorizontalContentExpander expander)
    {
        if (container == null)
        {
            expander = null;
            return;
        }

        // Verificar si ya tiene un expandidor, si no, añadirlo
        expander = container.GetComponent<HorizontalContentExpander>();
        if (expander == null)
        {
            expander = container.gameObject.AddComponent<HorizontalContentExpander>();
        }

        // Configurar el expandidor
        expander.elementsPerRow = elementsPerRow;
        expander.elementWidth = elementWidth;
        expander.horizontalSpacing = horizontalSpacing;
        expander.rightMargin = rightMargin;

        // Asegurarse de que el contenedor tenga HorizontalLayoutGroup para organizar los elementos
        HorizontalLayoutGroup layoutGroup = container.GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = container.gameObject.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = horizontalSpacing;
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
        }

        // Inicializar con los elementos actuales
        expander.RecalculateSize();
    }

    /// <summary>
    /// Hookea los métodos relevantes del Inventory_Manager
    /// Nota: Esta no es la mejor forma de hacerlo, pero es la menos intrusiva
    /// para mantener la compatibilidad con el código existente
    /// </summary>
    private void HookInventoryManagerMethods()
    {
        // No podemos modificar directamente los métodos, pero podemos crear
        // un wrapper para nuestro propio objeto y monitorear los cambios
        StartCoroutine(CheckForInventoryChanges());
    }

    /// <summary>
    /// Monitorea cambios en los contenedores del inventario
    /// </summary>
    private System.Collections.IEnumerator CheckForInventoryChanges()
    {
        int lastNoteCount = 0;
        int lastObjectCount = 0;

        while (true)
        {
            // Comprobar cambios en el contenedor de notas
            if (noteContainer != null)
            {
                int currentNoteCount = noteContainer.childCount;
                if (currentNoteCount != lastNoteCount)
                {
                    if (noteContentExpander != null)
                    {
                        noteContentExpander.RecalculateSize();
                    }
                    lastNoteCount = currentNoteCount;
                }
            }

            // Comprobar cambios en el contenedor de objetos
            if (objectContainer != null)
            {
                int currentObjectCount = objectContainer.childCount;
                if (currentObjectCount != lastObjectCount)
                {
                    if (objectContentExpander != null)
                    {
                        objectContentExpander.RecalculateSize();
                    }
                    lastObjectCount = currentObjectCount;
                }
            }

            // Esperar un poco antes de volver a comprobar
            yield return new WaitForSeconds(0.5f);
        }
    }

    /// <summary>
    /// Método público que se puede llamar desde el Inventory_Manager cuando se añade un item
    /// </summary>
    public void OnItemAdded(ItemData item)
    {
        if (item == null) return;

        // Expandir el contenedor correspondiente según el tipo de ítem
        if (item.itemType == ItemData.ItemType.Nota)
        {
            noteItemCount++;
            if (noteContentExpander != null)
            {
                noteContentExpander.AddElement();
            }
        }
        else
        {
            objectItemCount++;
            if (objectContentExpander != null)
            {
                objectContentExpander.AddElement();
            }
        }
    }
}