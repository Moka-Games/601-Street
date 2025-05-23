using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Versión mejorada del gestor de inventario que soporta persistencia entre escenas,
/// prefabs de interacción e integración con el Input System
/// </summary>
public class Inventory_Manager : MonoBehaviour
{
    public static Inventory_Manager Instance;

    [Header("Inventory UI")]
    public Transform noteContainer;
    public Transform objectContainer;
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

    [Header("Interaction Settings")]
    [Tooltip("Si está marcado, se mostrará automáticamente un popup al añadir un ítem al inventario")]
    public bool showPopupOnAdd = true;
    [Tooltip("Si está marcado, el popup no se mostrará si ya se está mostrando un prefab de interacción")]
    public bool skipPopupIfInteractionActive = true;

    [Header("Player Control")]
    [Tooltip("Si está marcado, bloqueará automáticamente al jugador durante las interacciones")]
    public bool blockPlayerDuringInteraction = true;
    [Tooltip("Si está marcado, bloqueará automáticamente la cámara durante las interacciones")]
    public bool blockCameraDuringInteraction = true;

    // Input System
    private PlayerControls playerControls;
    private InputAction toggleInventoryAction;

    // Control de estado para saber por qué están bloqueados
    private bool blockedByInventory = false;
    private bool blockedByInteraction = false;

    // Referencias para bloquear al jugador y la cámara
    private PlayerController playerController;
    private Camera_Script cameraScript;

    // Listas y diccionarios para mantener el inventario
    private List<ItemData> inventoryItems = new List<ItemData>();
    private Dictionary<ItemData, PrefabInteractionData> itemInteractions = new Dictionary<ItemData, PrefabInteractionData>();

    // Control de estado
    private bool inventoryOpened = false;
    private float lastPickUpTime = -100f;

    // Referencia al objeto interactivo actualmente activo
    private GameObject activeInteractionObject;

    // Bandera para indicar si el objeto actual se acaba de añadir al inventario
    private bool isNewlyAddedItem = false;

    // Nombre del último ítem añadido (para el popup)
    private string lastAddedItemName = "";

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
            Debug.Log("Inventory_Manager instance created.");
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Inicializar Input System
        InitializeInputSystem();
    }

    private void InitializeInputSystem()
    {
        playerControls = new PlayerControls();
        toggleInventoryAction = playerControls.Gameplay.ToggleInventory;

        // Suscribirse al evento de toggle inventory
        toggleInventoryAction.performed += OnToggleInventoryInput;
    }

    private void OnEnable()
    {
        playerControls?.Gameplay.Enable();
    }

    private void OnDisable()
    {
        playerControls?.Gameplay.Disable();
    }

    private void OnDestroy()
    {
        if (toggleInventoryAction != null)
        {
            toggleInventoryAction.performed -= OnToggleInventoryInput;
        }

        playerControls?.Dispose();
    }

    private void Start()
    {
        InventoryInterface.SetActive(false);
        popUpParent.SetActive(false);

        // Configurar prefabContainer para que persista entre escenas
        EnsurePrefabContainerPersistence();

        // Buscar referencias necesarias para bloquear al jugador
        playerController = FindAnyObjectByType<PlayerController>();
        cameraScript = FindAnyObjectByType<Camera_Script>();

        if (playerController == null)
            Debug.LogWarning("No se encontró PlayerController en la escena. No se podrá bloquear al jugador.");

        if (cameraScript == null)
            Debug.LogWarning("No se encontró Camera_Script en la escena. No se podrá bloquear la cámara.");
    }

    // Callback para el Input System
    private void OnToggleInventoryInput(InputAction.CallbackContext context)
    {
        ToggleInventory();
    }

    /// <summary>
    /// Asegura que el contenedor de prefabs exista y persista entre escenas
    /// </summary>
    private void EnsurePrefabContainerPersistence()
    {
        if (prefabContainer == null)
        {
            GameObject containerObj = new GameObject("PrefabContainer");
            prefabContainer = containerObj.transform;
            prefabContainer.SetParent(transform);
            Debug.Log("PrefabContainer creado y configurado para persistir entre escenas");
        }
        else if (prefabContainer.parent != transform)
        {
            prefabContainer.SetParent(transform);
            Debug.Log("PrefabContainer existente configurado para persistir entre escenas");
        }
    }

    private void Update()
    {
        // Actualizar estado del popup
        if (popUpParent.activeSelf && Time.time - lastPickUpTime >= popUpDuration)
        {
            popUpParent.SetActive(false);
        }
    }

    public void ToggleInventory()
    {
        inventoryOpened = !inventoryOpened;
        InventoryInterface.SetActive(inventoryOpened);

        // Si el inventario se acaba de abrir, bloquear al jugador y la cámara
        if (inventoryOpened)
        {
            BlockPlayerAndCameraForInventory();
            Debug.Log("Inventario abierto: Controlador y cámara bloqueados");
        }
        // Si el inventario se acaba de cerrar, desbloquear al jugador y la cámara
        else
        {
            UnblockPlayerAndCameraFromInventory();
            Debug.Log("Inventario cerrado: Controlador y cámara desbloqueados");
        }
    }

    /// <summary>
    /// Añade un nuevo ítem al inventario con un prefab de interacción específico
    /// </summary>
    public void AddItem(ItemData item, GameObject interactionPrefab, UnityEvent onItemClick = null, bool suppressPopup = false)
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

        // Guardar el nombre del ítem para usarlo en el popup cuando se cierre la interacción
        lastAddedItemName = item.itemName;

        // Mostrar popup solo si no está suprimido y está habilitado
        if (!suppressPopup && showPopupOnAdd && (!skipPopupIfInteractionActive || activeInteractionObject == null))
        {
            DisplayPopUp(item.itemName);
        }
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

        // Mostrar popup siempre en la versión antigua
        DisplayPopUp(item.itemName);
    }

    /// <summary>
    /// Muestra el prefab de interacción para un ítem recién añadido
    /// </summary>
    public void ShowInteractionForNewItem(GameObject prefab, string itemName)
    {
        // Cerrar cualquier interacción activa primero
        if (activeInteractionObject != null)
        {
            CloseActiveInteractionObject();
        }

        // Marcar que el ítem se acaba de añadir
        isNewlyAddedItem = true;
        lastAddedItemName = itemName;

        // Instanciar el prefab
        activeInteractionObject = InstantiateInteractionPrefab(prefab, itemName, true);

        Debug.Log($"Mostrando prefab de interacción para el ítem recién añadido: {itemName}");
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

            // Marcar que NO es un ítem recién añadido (viene del inventario)
            isNewlyAddedItem = false;

            // Instanciar nuevo objeto interactivo
            activeInteractionObject = InstantiateInteractionPrefab(interactionData.prefab, item.itemName, false);

            Debug.Log($"Mostrando prefab de interacción para {item.itemName} desde el inventario");
        }
    }

    /// <summary>
    /// Instancia un prefab de interacción y configura su botón de cierre
    /// </summary>
    public GameObject InstantiateInteractionPrefab(GameObject prefab, string itemName, bool isNewItem = false)
    {
        // Asegurar que el prefabContainer exista
        EnsurePrefabContainerPersistence();

        // Instanciar el prefab
        GameObject instance = Instantiate(prefab, prefabContainer);

        // Configurar botón de cierre si existe
        SetupCloseButton(instance, itemName, isNewItem);

        // Establecer como objeto activo
        activeInteractionObject = instance;

        // Bloquear al jugador y la cámara por interacción
        BlockPlayerAndCameraForInteraction();

        return instance;
    }

    /// <summary>
    /// Configura el botón de cierre en el objeto interactivo
    /// </summary>
    private void SetupCloseButton(GameObject interactionObject, string itemName, bool isNewItem)
    {
        // Buscar botón por su nombre especial
        Button closeButton = FindButtonInChildren(interactionObject, "Close_Interacted_Button");

        if (closeButton != null)
        {
            // Añadir listener para cerrar el objeto y mostrar popup solo si es nuevo
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => {
                bool wasNewItem = isNewlyAddedItem;

                // Destruir el objeto activo
                DestroyActiveInteractionObject();

                // Mostrar popup SOLO si era un ítem recién añadido
                if (wasNewItem)
                {
                    DisplayPopUp(lastAddedItemName + " added");
                }
                // No mostrar ningún popup para objetos del inventario
            });

            Debug.Log($"Botón de cierre configurado para {itemName}");
        }
        else
        {
            Debug.LogWarning($"No se encontró botón de cierre en el prefab para {itemName}");
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
                bool wasNewItem = isNewlyAddedItem;
                DestroyActiveInteractionObject();

                // Si era un ítem nuevo, mostrar popup
                if (wasNewItem)
                {
                    DisplayPopUp(lastAddedItemName + " added");
                }
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
            isNewlyAddedItem = false;

            // Desbloquear al jugador y la cámara de la interacción
            UnblockPlayerAndCameraFromInteraction();
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
    /// Muestra un popup con el mensaje especificado
    /// </summary>
    public void DisplayPopUp(string message)
    {
        popUpParent.SetActive(true);
        popUpText.text = message;
        lastPickUpTime = Time.time;
    }

    /// <summary>
    /// Verifica si hay un objeto de interacción activo
    /// </summary>
    public bool HasActiveInteractionObject()
    {
        return activeInteractionObject != null;
    }

    /// <summary>
    /// Verifica si un ítem específico está en el inventario
    /// </summary>
    public bool HasItem(ItemData item)
    {
        return inventoryItems.Contains(item);
    }

    /// <summary>
    /// Verifica si un ítem con un nombre específico está en el inventario
    /// </summary>
    public bool HasItemWithName(string itemName)
    {
        return inventoryItems.Exists(item => item.itemName == itemName);
    }

    /// <summary>
    /// Bloquea al jugador y/o la cámara por razón de inventario
    /// </summary>
    private void BlockPlayerAndCameraForInventory()
    {
        blockedByInventory = true;
        ApplyBlockingState();
    }

    /// <summary>
    /// Bloquea al jugador y/o la cámara por razón de interacción
    /// </summary>
    private void BlockPlayerAndCameraForInteraction()
    {
        blockedByInteraction = true;
        ApplyBlockingState();
    }

    private void UnblockPlayerAndCameraFromInventory()
    {
        blockedByInventory = false;
        // Solo desbloqueamos si no hay otro motivo de bloqueo
        if (!blockedByInteraction)
        {
            ApplyUnblockingState();
        }
    }

    /// <summary>
    /// Desbloquea elementos bloqueados por interacción
    /// </summary>
    private void UnblockPlayerAndCameraFromInteraction()
    {
        blockedByInteraction = false;
        // Solo desbloqueamos si no hay otro motivo de bloqueo
        if (!blockedByInventory)
        {
            ApplyUnblockingState();
        }
    }

    /// <summary>
    /// Aplica el estado de bloqueo al jugador y la cámara
    /// </summary>
    private void ApplyBlockingState()
    {
        if (blockPlayerDuringInteraction && playerController != null)
        {
            playerController.SetMovementEnabled(false);
        }

        if (blockCameraDuringInteraction && cameraScript != null)
        {
            cameraScript.FreezeCamera();
        }
    }

    /// <summary>
    /// Aplica el estado de desbloqueo al jugador y la cámara
    /// </summary>
    private void ApplyUnblockingState()
    {
        if (playerController != null)
        {
            playerController.SetMovementEnabled(true);
        }

        if (cameraScript != null)
        {
            cameraScript.UnfreezeCamera();
        }
    }

    /// <summary>
    /// Obtiene el estado actual del inventario
    /// </summary>
    public bool IsInventoryOpen()
    {
        return inventoryOpened;
    }
}