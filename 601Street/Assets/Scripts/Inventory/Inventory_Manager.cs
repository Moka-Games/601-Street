using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Versi�n mejorada del gestor de inventario que soporta persistencia entre escenas,
/// prefabs de interacci�n e integraci�n con el Input System
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
    [Tooltip("Transform donde se instanciar�n los prefabs de interacci�n. Debe estar en una escena persistente o en un Canvas DontDestroyOnLoad")]
    public Transform prefabContainer;

    [Header("Interaction Settings")]
    [Tooltip("Si est� marcado, se mostrar� autom�ticamente un popup al a�adir un �tem al inventario")]
    public bool showPopupOnAdd = true;
    [Tooltip("Si est� marcado, el popup no se mostrar� si ya se est� mostrando un prefab de interacci�n")]
    public bool skipPopupIfInteractionActive = true;

    [Header("Player Control")]
    [Tooltip("Si est� marcado, bloquear� autom�ticamente al jugador durante las interacciones")]
    public bool blockPlayerDuringInteraction = true;
    [Tooltip("Si est� marcado, bloquear� autom�ticamente la c�mara durante las interacciones")]
    public bool blockCameraDuringInteraction = true;

    // Input System
    private PlayerControls playerControls;
    private InputAction toggleInventoryAction;

    // Control de estado para saber por qu� est�n bloqueados
    private bool blockedByInventory = false;
    private bool blockedByInteraction = false;

    // Referencias para bloquear al jugador y la c�mara
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

    // Bandera para indicar si el objeto actual se acaba de a�adir al inventario
    private bool isNewlyAddedItem = false;

    // Nombre del �ltimo �tem a�adido (para el popup)
    private string lastAddedItemName = "";

    // Clase para almacenar datos de interacci�n de prefabs
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
            Debug.LogWarning("No se encontr� PlayerController en la escena. No se podr� bloquear al jugador.");

        if (cameraScript == null)
            Debug.LogWarning("No se encontr� Camera_Script en la escena. No se podr� bloquear la c�mara.");
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

        // Si el inventario se acaba de abrir, bloquear al jugador y la c�mara
        if (inventoryOpened)
        {
            BlockPlayerAndCameraForInventory();
            Debug.Log("Inventario abierto: Controlador y c�mara bloqueados");
        }
        // Si el inventario se acaba de cerrar, desbloquear al jugador y la c�mara
        else
        {
            UnblockPlayerAndCameraFromInventory();
            Debug.Log("Inventario cerrado: Controlador y c�mara desbloqueados");
        }
    }

    /// <summary>
    /// A�ade un nuevo �tem al inventario con un prefab de interacci�n espec�fico
    /// </summary>
    public void AddItem(ItemData item, GameObject interactionPrefab, UnityEvent onItemClick = null, bool suppressPopup = false)
    {
        if (item == null)
        {
            Debug.LogError("Intentando a�adir un �tem null al inventario");
            return;
        }

        // Crear UnityEvent por defecto si no se proporciona uno
        if (onItemClick == null)
        {
            onItemClick = new UnityEvent();
        }

        // Almacenar �tem y su configuraci�n de interacci�n
        inventoryItems.Add(item);

        PrefabInteractionData interactionData = new PrefabInteractionData
        {
            prefab = interactionPrefab,
            onItemClick = onItemClick
        };

        itemInteractions[item] = interactionData;

        // Crear elemento UI en el inventario
        InstantiateItemInUI(item);

        // Guardar el nombre del �tem para usarlo en el popup cuando se cierre la interacci�n
        lastAddedItemName = item.itemName;

        // Mostrar popup solo si no est� suprimido y est� habilitado
        if (!suppressPopup && showPopupOnAdd && (!skipPopupIfInteractionActive || activeInteractionObject == null))
        {
            DisplayPopUp(item.itemName);
        }
    }

    /// <summary>
    /// Versi�n compatible con el sistema anterior
    /// </summary>
    public void AddItem(ItemData item, UnityEvent onItemClick)
    {
        inventoryItems.Add(item);

        PrefabInteractionData interactionData = new PrefabInteractionData
        {
            prefab = null, // No hay prefab espec�fico
            onItemClick = onItemClick
        };

        itemInteractions[item] = interactionData;
        InstantiateItemInUI(item);

        // Mostrar popup siempre en la versi�n antigua
        DisplayPopUp(item.itemName);
    }

    /// <summary>
    /// Muestra el prefab de interacci�n para un �tem reci�n a�adido
    /// </summary>
    public void ShowInteractionForNewItem(GameObject prefab, string itemName)
    {
        // Cerrar cualquier interacci�n activa primero
        if (activeInteractionObject != null)
        {
            CloseActiveInteractionObject();
        }

        // Marcar que el �tem se acaba de a�adir
        isNewlyAddedItem = true;
        lastAddedItemName = itemName;

        // Instanciar el prefab
        activeInteractionObject = InstantiateInteractionPrefab(prefab, itemName, true);

        Debug.Log($"Mostrando prefab de interacci�n para el �tem reci�n a�adido: {itemName}");
    }

    private void InstantiateItemInUI(ItemData item)
    {
        // Determinar el contenedor y plantilla apropiados seg�n el tipo de �tem
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
            Debug.LogError("No se encontr� un componente TMP_Text en los hijos de: " + newItemUI.name);
        }

        // Configurar bot�n para interactuar
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
    /// M�todo llamado cuando se hace clic en un �tem del inventario
    /// </summary>
    private void OnItemClicked(ItemData item)
    {
        if (!itemInteractions.ContainsKey(item))
        {
            Debug.LogWarning($"No se encontr� configuraci�n de interacci�n para el �tem: {item.itemName}");
            return;
        }

        PrefabInteractionData interactionData = itemInteractions[item];

        // Invocar el evento de clic del �tem
        interactionData.onItemClick?.Invoke();

        // Si hay un prefab de interacci�n definido, instanciarlo
        if (interactionData.prefab != null)
        {
            // Cerrar cualquier interacci�n activa primero
            if (activeInteractionObject != null)
            {
                CloseActiveInteractionObject();
            }

            // Marcar que NO es un �tem reci�n a�adido (viene del inventario)
            isNewlyAddedItem = false;

            // Instanciar nuevo objeto interactivo
            activeInteractionObject = InstantiateInteractionPrefab(interactionData.prefab, item.itemName, false);

            Debug.Log($"Mostrando prefab de interacci�n para {item.itemName} desde el inventario");
        }
    }

    /// <summary>
    /// Instancia un prefab de interacci�n y configura su bot�n de cierre
    /// </summary>
    public GameObject InstantiateInteractionPrefab(GameObject prefab, string itemName, bool isNewItem = false)
    {
        // Asegurar que el prefabContainer exista
        EnsurePrefabContainerPersistence();

        // Instanciar el prefab
        GameObject instance = Instantiate(prefab, prefabContainer);

        // Configurar bot�n de cierre si existe
        SetupCloseButton(instance, itemName, isNewItem);

        // Establecer como objeto activo
        activeInteractionObject = instance;

        // Bloquear al jugador y la c�mara por interacci�n
        BlockPlayerAndCameraForInteraction();

        return instance;
    }

    /// <summary>
    /// Configura el bot�n de cierre en el objeto interactivo
    /// </summary>
    private void SetupCloseButton(GameObject interactionObject, string itemName, bool isNewItem)
    {
        // Buscar bot�n por su nombre especial
        Button closeButton = FindButtonInChildren(interactionObject, "Close_Interacted_Button");

        if (closeButton != null)
        {
            // A�adir listener para cerrar el objeto y mostrar popup solo si es nuevo
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => {
                bool wasNewItem = isNewlyAddedItem;

                // Destruir el objeto activo
                DestroyActiveInteractionObject();

                // Mostrar popup SOLO si era un �tem reci�n a�adido
                if (wasNewItem)
                {
                    DisplayPopUp(lastAddedItemName + " added");
                }
                // No mostrar ning�n popup para objetos del inventario
            });

            Debug.Log($"Bot�n de cierre configurado para {itemName}");
        }
        else
        {
            Debug.LogWarning($"No se encontr� bot�n de cierre en el prefab para {itemName}");
        }
    }

    /// <summary>
    /// Cierra el objeto de interacci�n activo
    /// </summary>
    public void CloseActiveInteractionObject()
    {
        if (activeInteractionObject != null)
        {
            // Buscar el bot�n de cierre y simular clic para mantener el comportamiento esperado
            Button closeButton = FindButtonInChildren(activeInteractionObject, "Close_Interacted_Button");

            if (closeButton != null)
            {
                closeButton.onClick.Invoke();
            }
            else
            {
                // Si no hay bot�n, destruir directamente
                bool wasNewItem = isNewlyAddedItem;
                DestroyActiveInteractionObject();

                // Si era un �tem nuevo, mostrar popup
                if (wasNewItem)
                {
                    DisplayPopUp(lastAddedItemName + " added");
                }
            }
        }
    }

    /// <summary>
    /// Destruye el objeto de interacci�n activo sin mostrar popup
    /// </summary>
    private void DestroyActiveInteractionObject()
    {
        if (activeInteractionObject != null)
        {
            Destroy(activeInteractionObject);
            activeInteractionObject = null;
            isNewlyAddedItem = false;

            // Desbloquear al jugador y la c�mara de la interacci�n
            UnblockPlayerAndCameraFromInteraction();
        }
    }

    /// <summary>
    /// Busca un bot�n en los hijos de un GameObject por su nombre
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
    /// Verifica si hay un objeto de interacci�n activo
    /// </summary>
    public bool HasActiveInteractionObject()
    {
        return activeInteractionObject != null;
    }

    /// <summary>
    /// Verifica si un �tem espec�fico est� en el inventario
    /// </summary>
    public bool HasItem(ItemData item)
    {
        return inventoryItems.Contains(item);
    }

    /// <summary>
    /// Verifica si un �tem con un nombre espec�fico est� en el inventario
    /// </summary>
    public bool HasItemWithName(string itemName)
    {
        return inventoryItems.Exists(item => item.itemName == itemName);
    }

    /// <summary>
    /// Bloquea al jugador y/o la c�mara por raz�n de inventario
    /// </summary>
    private void BlockPlayerAndCameraForInventory()
    {
        blockedByInventory = true;
        ApplyBlockingState();
    }

    /// <summary>
    /// Bloquea al jugador y/o la c�mara por raz�n de interacci�n
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
    /// Desbloquea elementos bloqueados por interacci�n
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
    /// Aplica el estado de bloqueo al jugador y la c�mara
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
    /// Aplica el estado de desbloqueo al jugador y la c�mara
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