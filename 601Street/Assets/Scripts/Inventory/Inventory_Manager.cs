using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// Versión mejorada del gestor de inventario que soporta persistencia entre escenas
/// </summary>
public class Inventory_Manager : MonoBehaviour
{
    public static Inventory_Manager Instance;

    public Transform noteContainer;
    public Transform objectContainer;

    [Header("Inventory UI")]
    public GameObject InventoryInterface;
    public GameObject noteTemplate;
    public GameObject objectTemplate;

    [Header("Popup Configuration")]
    public GameObject popUpParent;
    public TMP_Text popUpText;
    public float popUpDuration = 4.5f;

    [Header("Prefabs Container")]
    [Tooltip("Transform donde se instanciarán los prefabs de interacción. Debe estar en una escena persistente o en un Canvas DontDestroyOnLoad")]
    public Transform prefabContainer;

    // Listas y diccionarios para mantener el inventario
    private List<ItemData> inventoryItems = new List<ItemData>();
    private Dictionary<ItemData, PrefabInteractionData> itemInteractions = new Dictionary<ItemData, PrefabInteractionData>();

    // Control de estado
    private bool inventoryOpened = false;
    private float lastPickUpTime = -100f;

    // Referencia al objeto interactivo actualmente activo
    private GameObject activeInteractionObject;

    // Clase para almacenar datos de interacción de prefabs
    [System.Serializable]
    public class PrefabInteractionData
    {
        public GameObject prefab;
        public UnityEvent onItemClick;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("Inventory_Manager instance created.");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InventoryInterface.SetActive(false);
        popUpParent.SetActive(false);

        // Crear prefabContainer si no existe
        if (prefabContainer == null)
        {
            GameObject containerObj = new GameObject("PrefabContainer");
            prefabContainer = containerObj.transform;
            prefabContainer.SetParent(transform);
        }
    }

    private void Update()
    {
        // Abrir/cerrar inventario
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }

        // Cerrar objeto interactivo activo con la tecla E
        if (Input.GetKeyDown(KeyCode.E) && activeInteractionObject != null)
        {
            CloseActiveInteractionObject();
        }

        // Actualizar estado del popup
        if (popUpParent.activeSelf && Time.time - lastPickUpTime >= popUpDuration)
        {
            popUpParent.SetActive(false);
        }
    }

    /// <summary>
    /// Abre o cierra el inventario
    /// </summary>
    public void ToggleInventory()
    {
        inventoryOpened = !inventoryOpened;
        InventoryInterface.SetActive(inventoryOpened);
    }

    /// <summary>
    /// Añade un nuevo ítem al inventario con un prefab de interacción específico
    /// </summary>
    public void AddItem(ItemData item, GameObject interactionPrefab, UnityEvent onItemClick = null)
    {
        if (item == null)
        {
            Debug.LogError("Intentando añadir un ítem null al inventario");
            return;
        }

        // Crear UnityEvent por defecto si no se proporciona uno
        if (onItemClick == null)
        {
            onItemClick = new UnityEvent();
        }

        // Almacenar ítem y su configuración de interacción
        inventoryItems.Add(item);

        PrefabInteractionData interactionData = new PrefabInteractionData
        {
            prefab = interactionPrefab,
            onItemClick = onItemClick
        };

        itemInteractions[item] = interactionData;

        // Crear elemento UI en el inventario
        InstantiateItemInUI(item);
    }

    /// <summary>
    /// Versión compatible con el sistema anterior
    /// </summary>
    public void AddItem(ItemData item, UnityEvent onItemClick)
    {
        inventoryItems.Add(item);

        PrefabInteractionData interactionData = new PrefabInteractionData
        {
            prefab = null, // No hay prefab específico
            onItemClick = onItemClick
        };

        itemInteractions[item] = interactionData;
        InstantiateItemInUI(item);
    }

    private void InstantiateItemInUI(ItemData item)
    {
        // Determinar el contenedor y plantilla apropiados según el tipo de ítem
        Transform parentContainer = item.itemType == ItemData.ItemType.Nota ? noteContainer : objectContainer;
        GameObject template = item.itemType == ItemData.ItemType.Nota ? noteTemplate : objectTemplate;

        // Instanciar el elemento UI
        GameObject newItemUI = Instantiate(template, parentContainer);

        // Configurar imagen
        Image itemImage = newItemUI.GetComponent<Image>();
        if (itemImage != null)
        {
            itemImage.sprite = item.inventoryImage;
        }
        else
        {
            Debug.LogError("El objeto instanciado no tiene un componente Image: " + newItemUI.name);
        }

        // Configurar texto
        TMP_Text itemNameText = newItemUI.GetComponentInChildren<TMP_Text>();
        if (itemNameText != null)
        {
            itemNameText.text = item.itemName;
        }
        else
        {
            Debug.LogError("No se encontró un componente TMP_Text en los hijos de: " + newItemUI.name);
        }

        // Configurar botón para interactuar
        Button itemButton = newItemUI.GetComponent<Button>();
        if (itemButton != null)
        {
            itemButton.onClick.RemoveAllListeners();
            itemButton.onClick.AddListener(() => OnItemClicked(item));
        }
        else
        {
            Debug.LogError("El objeto instanciado no tiene un componente Button: " + newItemUI.name);
        }

        // Activar el elemento
        newItemUI.SetActive(true);
    }

    /// <summary>
    /// Método llamado cuando se hace clic en un ítem del inventario
    /// </summary>
    private void OnItemClicked(ItemData item)
    {
        if (!itemInteractions.ContainsKey(item))
        {
            Debug.LogWarning($"No se encontró configuración de interacción para el ítem: {item.itemName}");
            return;
        }

        PrefabInteractionData interactionData = itemInteractions[item];

        // Invocar el evento de clic del ítem
        interactionData.onItemClick?.Invoke();

        // Si hay un prefab de interacción definido, instanciarlo
        if (interactionData.prefab != null)
        {
            // Cerrar cualquier interacción activa primero
            if (activeInteractionObject != null)
            {
                CloseActiveInteractionObject();
            }

            // Instanciar nuevo objeto interactivo
            activeInteractionObject = Instantiate(interactionData.prefab, prefabContainer);

            // Configurar botón de cierre si existe
            SetupCloseButton(activeInteractionObject, item.itemName);
        }
    }

    /// <summary>
    /// Configura el botón de cierre en el objeto interactivo
    /// </summary>
    private void SetupCloseButton(GameObject interactionObject, string itemName)
    {
        // Buscar botón por su nombre especial
        Button closeButton = FindButtonInChildren(interactionObject, "Close_Interacted_Button");

        if (closeButton != null)
        {
            // Añadir listener para cerrar el objeto y mostrar popup
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => {
                DestroyActiveInteractionObject();
                DisplayPopUp(itemName);
            });
        }
    }

    /// <summary>
    /// Cierra el objeto de interacción activo
    /// </summary>
    public void CloseActiveInteractionObject()
    {
        if (activeInteractionObject != null)
        {
            // Buscar el botón de cierre y simular clic para mantener el comportamiento esperado
            Button closeButton = FindButtonInChildren(activeInteractionObject, "Close_Interacted_Button");

            if (closeButton != null)
            {
                closeButton.onClick.Invoke();
            }
            else
            {
                // Si no hay botón, destruir directamente
                DestroyActiveInteractionObject();
            }
        }
    }

    /// <summary>
    /// Destruye el objeto de interacción activo sin mostrar popup
    /// </summary>
    private void DestroyActiveInteractionObject()
    {
        if (activeInteractionObject != null)
        {
            Destroy(activeInteractionObject);
            activeInteractionObject = null;
        }
    }

    /// <summary>
    /// Busca un botón en los hijos de un GameObject por su nombre
    /// </summary>
    private Button FindButtonInChildren(GameObject parent, string buttonName)
    {
        // Buscar en todos los hijos, incluso los inactivos
        Transform[] allChildren = parent.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in allChildren)
        {
            if (child.name == buttonName)
            {
                return child.GetComponent<Button>();
            }
        }

        return null;
    }

    /// <summary>
    /// Muestra un popup con el nombre del ítem recogido
    /// </summary>
    public void DisplayPopUp(string itemName)
    {
        popUpParent.SetActive(true);
        popUpText.text = itemName + " added";
        lastPickUpTime = Time.time;
    }

    public bool HasActiveInteractionObject()
    {
        return activeInteractionObject != null;
    }
}