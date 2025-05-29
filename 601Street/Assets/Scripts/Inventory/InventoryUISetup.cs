using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Configurador automático para asegurar que los elementos del inventario
/// tengan navegación correcta configurada
/// </summary>
public class InventoryUISetup : MonoBehaviour
{
    [Header("Configuración de Navegación")]
    [SerializeField] private bool autoSetupNavigation = true;
    [SerializeField] private bool setupOnStart = true;
    [SerializeField] private bool setupOnElementAdded = true;
    [SerializeField] private bool logSetupActions = true;

    [Header("Contenedores del Inventario")]
    [SerializeField] private Transform noteContainer;
    [SerializeField] private Transform objectContainer;

    [Header("Configuración de Layout")]
    [SerializeField] private int elementsPerRow = 6;
    [SerializeField] private Navigation.Mode navigationMode = Navigation.Mode.Automatic;

    private List<Selectable> allSelectables = new List<Selectable>();
    private Selectable firstSelectable;

    // Para monitoreo de cambios
    private int lastNoteCount = 0;
    private int lastObjectCount = 0;

    private void Start()
    {
        if (setupOnStart)
        {
            // Detectar contenedores automáticamente si no están asignados
            if (noteContainer == null || objectContainer == null)
            {
                DetectContainers();
            }

            StartCoroutine(SetupNavigationDelayed());
        }

        if (setupOnElementAdded)
        {
            StartCoroutine(MonitorContainerChanges());
        }
    }

    /// <summary>
    /// Detecta automáticamente los contenedores del inventario
    /// </summary>
    private void DetectContainers()
    {
        Inventory_Manager inventoryManager = FindAnyObjectByType<Inventory_Manager>();
        if (inventoryManager != null)
        {
            noteContainer = inventoryManager.noteContainer;
            objectContainer = inventoryManager.objectContainer;

            if (logSetupActions)
                Debug.Log("Contenedores detectados automáticamente");
        }
    }

    /// <summary>
    /// Configura la navegación con un pequeño delay para asegurar que todos los elementos estén instanciados
    /// </summary>
    private IEnumerator SetupNavigationDelayed()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.1f);

        SetupInventoryNavigation();
    }

    /// <summary>
    /// Monitorea cambios en los contenedores para reconfigurar navegación automáticamente
    /// </summary>
    private IEnumerator MonitorContainerChanges()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);

            bool hasChanges = false;

            if (noteContainer != null)
            {
                int currentNoteCount = CountActiveSelectables(noteContainer);
                if (currentNoteCount != lastNoteCount)
                {
                    lastNoteCount = currentNoteCount;
                    hasChanges = true;
                }
            }

            if (objectContainer != null)
            {
                int currentObjectCount = CountActiveSelectables(objectContainer);
                if (currentObjectCount != lastObjectCount)
                {
                    lastObjectCount = currentObjectCount;
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                if (logSetupActions)
                    Debug.Log("Cambios detectados en inventario - Reconfigurando navegación");

                yield return new WaitForEndOfFrame();
                SetupInventoryNavigation();
            }
        }
    }

    /// <summary>
    /// Cuenta los selectables activos en un contenedor
    /// </summary>
    private int CountActiveSelectables(Transform container)
    {
        int count = 0;
        foreach (Transform child in container)
        {
            if (child.gameObject.activeInHierarchy)
            {
                Selectable selectable = child.GetComponent<Selectable>();
                if (selectable != null && selectable.interactable)
                {
                    count++;
                }
            }
        }
        return count;
    }

    /// <summary>
    /// Configura la navegación completa del inventario
    /// </summary>
    public void SetupInventoryNavigation()
    {
        if (!autoSetupNavigation) return;

        allSelectables.Clear();

        // Recopilar todos los selectables
        CollectSelectables(noteContainer, allSelectables);
        CollectSelectables(objectContainer, allSelectables);

        if (allSelectables.Count == 0)
        {
            if (logSetupActions)
                Debug.LogWarning("No se encontraron elementos navegables en el inventario");
            return;
        }

        // Configurar navegación según el modo
        switch (navigationMode)
        {
            case Navigation.Mode.Automatic:
                SetupAutomaticNavigation();
                break;
            case Navigation.Mode.Explicit:
                SetupExplicitNavigation();
                break;
            case Navigation.Mode.Horizontal:
                SetupHorizontalNavigation();
                break;
            case Navigation.Mode.Vertical:
                SetupVerticalNavigation();
                break;
        }

        // Establecer el primer elemento como seleccionado
        if (allSelectables.Count > 0)
        {
            firstSelectable = allSelectables[0];

            // Limpiar selección anterior
            EventSystem.current?.SetSelectedGameObject(null);

            if (logSetupActions)
                Debug.Log($"Navegación configurada para {allSelectables.Count} elementos. Modo: {navigationMode}");
        }
    }

    /// <summary>
    /// Recopila todos los selectables de un contenedor
    /// </summary>
    private void CollectSelectables(Transform container, List<Selectable> list)
    {
        if (container == null) return;

        foreach (Transform child in container)
        {
            if (child.gameObject.activeInHierarchy)
            {
                Selectable selectable = child.GetComponent<Selectable>();
                if (selectable != null && selectable.interactable)
                {
                    // Asegurar que el elemento es interactable y navegable
                    EnsureSelectableSetup(selectable);
                    list.Add(selectable);
                }
            }
        }
    }

    /// <summary>
    /// Asegura que un Selectable esté configurado correctamente
    /// </summary>
    private void EnsureSelectableSetup(Selectable selectable)
    {
        // Asegurar que el elemento sea interactable
        selectable.interactable = true;

        // Asegurar que tenga un Graphic Raycaster si es necesario
        Canvas canvas = selectable.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    /// <summary>
    /// Configuración automática de navegación
    /// </summary>
    private void SetupAutomaticNavigation()
    {
        foreach (Selectable selectable in allSelectables)
        {
            Navigation nav = selectable.navigation;
            nav.mode = Navigation.Mode.Automatic;
            selectable.navigation = nav;
        }
    }

    /// <summary>
    /// Configuración explícita de navegación basada en posición en grid
    /// </summary>
    private void SetupExplicitNavigation()
    {
        for (int i = 0; i < allSelectables.Count; i++)
        {
            Selectable current = allSelectables[i];
            Navigation nav = current.navigation;
            nav.mode = Navigation.Mode.Explicit;

            // Calcular posición en grid
            int row = i / elementsPerRow;
            int col = i % elementsPerRow;

            // Navegación horizontal
            int leftIndex = i - 1;
            int rightIndex = i + 1;

            // Navegación vertical
            int upIndex = i - elementsPerRow;
            int downIndex = i + elementsPerRow;

            // Asignar navegación si los índices son válidos
            nav.selectOnLeft = (col > 0 && leftIndex >= 0) ? allSelectables[leftIndex] : null;
            nav.selectOnRight = (col < elementsPerRow - 1 && rightIndex < allSelectables.Count) ? allSelectables[rightIndex] : null;
            nav.selectOnUp = (upIndex >= 0) ? allSelectables[upIndex] : null;
            nav.selectOnDown = (downIndex < allSelectables.Count) ? allSelectables[downIndex] : null;

            current.navigation = nav;
        }
    }

    /// <summary>
    /// Configuración de navegación horizontal
    /// </summary>
    private void SetupHorizontalNavigation()
    {
        for (int i = 0; i < allSelectables.Count; i++)
        {
            Selectable current = allSelectables[i];
            Navigation nav = current.navigation;
            nav.mode = Navigation.Mode.Explicit;

            nav.selectOnLeft = (i > 0) ? allSelectables[i - 1] : allSelectables[allSelectables.Count - 1];
            nav.selectOnRight = (i < allSelectables.Count - 1) ? allSelectables[i + 1] : allSelectables[0];
            nav.selectOnUp = null;
            nav.selectOnDown = null;

            current.navigation = nav;
        }
    }

    /// <summary>
    /// Configuración de navegación vertical
    /// </summary>
    private void SetupVerticalNavigation()
    {
        for (int i = 0; i < allSelectables.Count; i++)
        {
            Selectable current = allSelectables[i];
            Navigation nav = current.navigation;
            nav.mode = Navigation.Mode.Explicit;

            nav.selectOnUp = (i > 0) ? allSelectables[i - 1] : allSelectables[allSelectables.Count - 1];
            nav.selectOnDown = (i < allSelectables.Count - 1) ? allSelectables[i + 1] : allSelectables[0];
            nav.selectOnLeft = null;
            nav.selectOnRight = null;

            current.navigation = nav;
        }
    }

    /// <summary>
    /// Selecciona el primer elemento disponible
    /// </summary>
    public void SelectFirstElement()
    {
        if (firstSelectable != null && firstSelectable.gameObject.activeInHierarchy)
        {
            EventSystem.current?.SetSelectedGameObject(firstSelectable.gameObject);

            if (logSetupActions)
                Debug.Log($"Primer elemento seleccionado: {firstSelectable.name}");
        }
    }

    /// <summary>
    /// Refresca la configuración de navegación
    /// </summary>
    [ContextMenu("Refresh Navigation")]
    public void RefreshNavigation()
    {
        SetupInventoryNavigation();
    }

    /// <summary>
    /// Fuerza la selección del primer elemento
    /// </summary>
    [ContextMenu("Select First Element")]
    public void ForceSelectFirstElement()
    {
        SelectFirstElement();
    }

    /// <summary>
    /// Debug de la configuración actual
    /// </summary>
    [ContextMenu("Debug Navigation Setup")]
    public void DebugNavigationSetup()
    {
        Debug.Log($"=== INVENTORY UI SETUP DEBUG ===");
        Debug.Log($"Auto Setup: {autoSetupNavigation}");
        Debug.Log($"Navigation Mode: {navigationMode}");
        Debug.Log($"Elements Per Row: {elementsPerRow}");
        Debug.Log($"Total Selectables: {allSelectables.Count}");
        Debug.Log($"First Selectable: {firstSelectable?.name ?? "NULL"}");
        Debug.Log($"Current Selected: {EventSystem.current?.currentSelectedGameObject?.name ?? "NULL"}");

        foreach (var selectable in allSelectables)
        {
            Debug.Log($"  - {selectable.name}: Active={selectable.gameObject.activeInHierarchy}, Interactable={selectable.interactable}");
        }
    }
}