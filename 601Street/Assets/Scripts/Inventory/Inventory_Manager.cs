using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Versión final corregida del gestor de inventario donde ToggleInventory funciona como verdadero TOGGLE
/// - ToggleInventory puede abrir Y cerrar el inventario
/// - Cancel está completamente eliminado del sistema de inventario
/// - La cámara se congela automáticamente al abrir el inventario y se descongela al cerrarlo
/// - Sistema de eventos para notificar cuando se cierran prefabs de interacción
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

    [Header("Camera Control")]
    [Tooltip("Si está marcado, la cámara se congelará automáticamente al abrir el inventario")]
    public bool freezeCameraOnInventoryOpen = true;

    [Header("Audio")]
    [SerializeField] private AudioClip closePrefabSound;
    private AudioSource audioSource;

    // Input System - CAMBIO IMPORTANTE: Mantenemos ambas referencias activas
    private PlayerControls playerControls;
    private InputAction toggleInventoryGameplay; // Para cuando está cerrado
    private InputAction toggleInventoryUI;       // Para cuando está abierto (necesitamos crearlo)

    // Control de estado para saber por qué están bloqueados
    private bool blockedByInventory = false;
    private bool blockedByInteraction = false;

    // Referencias para bloquear al jugador y la cámara
    private PlayerController playerController;
    private Camera_Script cameraScript;

    // Listas y diccionarios para mantener el inventario
    private List<ItemData> inventoryItems = new List<ItemData>();
    private Dictionary<ItemData, PrefabInteractionData> itemInteractions = new Dictionary<ItemData, PrefabInteractionData>();

    // NUEVO: Sistema de registro de ítems para eventos de cierre
    private List<Inventory_Item> registeredItems = new List<Inventory_Item>();
    private ItemData currentlyOpenItemData = null; // Rastrea qué ítem tiene el prefab abierto actualmente

    // Control de estado
    private bool inventoryOpened = false;
    private float lastPickUpTime = -100f;

    // Referencia al objeto interactivo actualmente activo
    private GameObject activeInteractionObject;

    // Bandera para indicar si el objeto actual se acaba de añadir al inventario
    private bool isNewlyAddedItem = false;

    // Nombre del último ítem añadido (para el popup)
    private string lastAddedItemName = "";

    [Header("Navigation System")]
    [SerializeField] private InventoryNavigationManager inventoryNavigation;

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

        // Obtener la acción original de Gameplay
        toggleInventoryGameplay = playerControls.Gameplay.ToggleInventory;

        // CREAR una acción temporal para UI con el mismo binding
        // Esto es un workaround hasta que podamos modificar el Input Action Asset
        toggleInventoryUI = new InputAction("ToggleInventoryUI", InputActionType.Button);
        toggleInventoryUI.AddBinding("<Gamepad>/buttonNorth");
        toggleInventoryUI.AddBinding("<Keyboard>/tab");

        // Suscribirse a ambas acciones
        toggleInventoryGameplay.performed += OnToggleInventoryInput;
        toggleInventoryUI.performed += OnToggleInventoryInput;

        Debug.Log("Inventory_Manager: Input System inicializado con ToggleInventory en ambos estados");
    }

    private void OnEnable()
    {
        // CRÍTICO: Solo habilitar Gameplay al inicio, NUNCA UI
        if (playerControls != null)
        {
            playerControls.Gameplay.Enable();
            playerControls.UI.Disable(); // Asegurar que UI esté deshabilitado inicialmente
            toggleInventoryUI?.Disable(); // Asegurar que la acción personalizada esté deshabilitada
        }
        Debug.Log("Inventory_Manager: Solo Gameplay habilitado al inicio");
    }

    private void OnDisable()
    {
        // Deshabilitar todo de forma segura
        if (playerControls != null)
        {
            playerControls.Gameplay.Disable();
            playerControls.UI.Disable();
        }

        if (toggleInventoryUI != null)
        {
            toggleInventoryUI.Disable();
        }

        Debug.Log("Inventory_Manager: All actions disabled");
    }

    private void OnDestroy()
    {
        // Limpiar suscripciones
        if (toggleInventoryGameplay != null)
        {
            toggleInventoryGameplay.performed -= OnToggleInventoryInput;
        }

        if (toggleInventoryUI != null)
        {
            toggleInventoryUI.performed -= OnToggleInventoryInput;
            toggleInventoryUI.Dispose();
        }

        playerControls?.Dispose();

        // NUEVO: Limpiar lista de ítems registrados
        registeredItems.Clear();
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.volume = 0.25f;

        InventoryInterface.SetActive(false);
        popUpParent.SetActive(false);

        EnsurePrefabContainerPersistence();

        inventoryNavigation = GetComponent<InventoryNavigationManager>();
        if (inventoryNavigation == null)
        {
            inventoryNavigation = FindAnyObjectByType<InventoryNavigationManager>();
        }

        if (inventoryNavigation != null)
        {
            // Configurar contenedores en el sistema de navegación
            inventoryNavigation.SetContainers(noteContainer, objectContainer);
            Debug.Log("Sistema de navegación del inventario configurado");
        }
        else
        {
            Debug.LogWarning("InventoryNavigationManager no encontrado. La navegación del inventario no funcionará.");
        }

        playerController = FindAnyObjectByType<PlayerController>();
        cameraScript = FindAnyObjectByType<Camera_Script>();

        if (playerController == null)
            Debug.LogWarning("No se encontró PlayerController en la escena. No se podrá bloquear al jugador.");

        if (cameraScript == null)
            Debug.LogWarning("No se encontró Camera_Script en la escena. No se podrá congelar la cámara automáticamente.");
    }

    #region Sistema de Eventos de Cierre de Prefabs

    /// <summary>
    /// NUEVO: Registra un ítem para recibir eventos de cierre de prefab
    /// </summary>
    /// <param name="item">El Inventory_Item que se registra</param>
    public void RegisterItemForCloseEvents(Inventory_Item item)
    {
        if (item != null && !registeredItems.Contains(item))
        {
            registeredItems.Add(item);
            Debug.Log($"Ítem {item.gameObject.name} registrado para eventos de cierre de prefab");
        }
    }

    /// <summary>
    /// NUEVO: Desregistra un ítem de los eventos de cierre de prefab
    /// </summary>
    /// <param name="item">El Inventory_Item que se desregistra</param>
    public void UnregisterItemForCloseEvents(Inventory_Item item)
    {
        if (item != null && registeredItems.Contains(item))
        {
            registeredItems.Remove(item);
            Debug.Log($"Ítem {item.gameObject.name} desregistrado de eventos de cierre de prefab");
        }
    }

    /// <summary>
    /// NUEVO: Verifica si un ítem está registrado para eventos de cierre
    /// </summary>
    /// <param name="item">El Inventory_Item a verificar</param>
    /// <returns>True si está registrado, false en caso contrario</returns>
    public bool IsItemRegisteredForCloseEvents(Inventory_Item item)
    {
        return item != null && registeredItems.Contains(item);
    }

    /// <summary>
    /// NUEVO: Notifica a todos los ítems registrados que se ha cerrado un prefab
    /// </summary>
    /// <param name="closedItemData">Los datos del ítem cuyo prefab se cerró</param>
    private void NotifyPrefabClosed(ItemData closedItemData)
    {
        if (closedItemData == null)
        {
            Debug.LogWarning("Intentando notificar cierre para un ItemData null");
            return;
        }

        Debug.Log($"¡NOTIFICANDO CIERRE DE PREFAB PARA ÍTEM: {closedItemData.itemName}!");
        Debug.Log($"Ítems registrados: {registeredItems.Count}");

        // Crear una copia de la lista para evitar problemas si se modifica durante la iteración
        var itemsCopy = new List<Inventory_Item>(registeredItems);
        int notifiedCount = 0;

        foreach (var item in itemsCopy)
        {
            if (item != null)
            {
                try
                {
                    Debug.Log($"Notificando a ítem: {item.gameObject.name}, ItemData: {(item.itemData != null ? item.itemData.itemName : "NULL")}");
                    item.OnPrefabClosedInternal(closedItemData);
                    notifiedCount++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error al notificar cierre de prefab a {item.gameObject.name}: {e.Message}");
                }
            }
        }

        Debug.Log($"Notificación completada: {notifiedCount} ítems notificados");

        // Limpiar ítems nulos de la lista
        CleanupRegisteredItems();
    }

    /// <summary>
    /// NUEVO: Limpia ítems nulos de la lista de registrados
    /// </summary>
    private void CleanupRegisteredItems()
    {
        registeredItems.RemoveAll(item => item == null);
    }

    #endregion

    /// <summary>
    /// Callback ÚNICO para ToggleInventory - funciona tanto para abrir como cerrar
    /// </summary>
    private void OnToggleInventoryInput(InputAction.CallbackContext context)
    {
        Debug.Log($"ToggleInventory activado - Control: {context.control?.path}");
        Debug.Log($"Estado actual del inventario: {(inventoryOpened ? "ABIERTO" : "CERRADO")}");

        // Verificar que realmente sea el botón correcto (Button North o Tab)
        if (context.control != null)
        {
            string controlPath = context.control.path;
            bool isCorrectButton = controlPath.Contains("buttonNorth") || controlPath.Contains("tab");

            if (!isCorrectButton)
            {
                Debug.LogWarning($"ToggleInventory activado por control incorrecto: {controlPath} - Ignorando");
                return;
            }
        }

        // TOGGLE real: si está abierto, cerrar; si está cerrado, abrir
        if (inventoryOpened)
        {
            Debug.Log("Cerrando inventario con ToggleInventory");
            CloseInventory();
        }
        else
        {
            Debug.Log("Abriendo inventario con ToggleInventory");
            OpenInventory();
        }
    }

    /// <summary>
    /// Abre el inventario
    /// </summary>
    public void OpenInventory()
    {
        if (inventoryOpened)
        {
            Debug.Log("OpenInventory llamado pero el inventario ya está abierto - ignorando");
            return; // Ya está abierto
        }

        inventoryOpened = true;
        InventoryInterface.SetActive(true);

        // CAMBIO CRÍTICO: Cambiar Action Maps Y habilitar la acción de toggle para UI
        playerControls.Gameplay.Disable();
        playerControls.UI.Enable();
        toggleInventoryUI.Enable(); // Habilitar la acción personalizada para cuando esté abierto

        // CRÍTICO: Activar navegación específica del inventario
        if (inventoryNavigation == null)
        {
            inventoryNavigation = GetComponent<InventoryNavigationManager>();
            if (inventoryNavigation == null)
            {
                // Buscar en el GameObject del sistema de inventario
                inventoryNavigation = FindAnyObjectByType<InventoryNavigationManager>();
            }
        }

        if (inventoryNavigation != null)
        {
            // Esperar un frame para que la UI se active completamente
            StartCoroutine(ActivateNavigationDelayed());
        }
        else
        {
            Debug.LogWarning("InventoryNavigationManager no encontrado");
        }

        // Usar NavigationPriorityManager si está disponible
        if (NavigationPriorityManager.Instance != null)
        {
            NavigationPriorityManager.Instance.ActivateInventoryNavigation();
        }

        BlockPlayerAndCameraForInventory();

        // NUEVO: Congelar la cámara al abrir el inventario
        if (freezeCameraOnInventoryOpen && cameraScript != null)
        {
            cameraScript.FreezeCamera();
            Debug.Log("Cámara congelada automáticamente al abrir el inventario");
        }

        Debug.Log("Inventario ABIERTO: Gameplay disabled, UI enabled, Navigation activated, Camera frozen");
    }

    // AÑADIR ESTE NUEVO MÉTODO:
    private IEnumerator ActivateNavigationDelayed()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.1f); // Pequeño delay para asegurar que todo esté listo

        if (inventoryNavigation != null)
        {
            inventoryNavigation.ActivateInventoryNavigation();
            Debug.Log("Navegación del inventario activada con delay");
        }
    }

    /// <summary>
    /// Cierra el inventario
    /// </summary>
    public void CloseInventory()
    {
        if (!inventoryOpened)
        {
            Debug.Log("CloseInventory llamado pero el inventario ya está cerrado - ignorando");
            return; // Ya está cerrado
        }

        // CRÍTICO: Desactivar navegación específica del inventario PRIMERO
        if (inventoryNavigation != null)
        {
            inventoryNavigation.DeactivateInventoryNavigation();
            Debug.Log("Navegación del inventario desactivada");
        }

        // Usar NavigationPriorityManager si está disponible
        if (NavigationPriorityManager.Instance != null)
        {
            NavigationPriorityManager.Instance.DeactivateInventoryNavigation();
        }

        inventoryOpened = false;
        InventoryInterface.SetActive(false);

        // CAMBIO CRÍTICO: Volver a Gameplay y deshabilitar la acción de UI
        playerControls.UI.Disable();
        toggleInventoryUI.Disable(); // Deshabilitar la acción personalizada
        playerControls.Gameplay.Enable();

        UnblockPlayerAndCameraFromInventory();

        // NUEVO: Descongelar la cámara al cerrar el inventario
        if (freezeCameraOnInventoryOpen && cameraScript != null)
        {
            cameraScript.UnfreezeCamera();
            Debug.Log("Cámara descongelada automáticamente al cerrar el inventario");
        }

        Debug.Log("Inventario CERRADO: UI disabled, Navigation deactivated, Gameplay enabled, Camera unfrozen");
    }

    /// <summary>
    /// Método legacy para compatibilidad - redirige al nuevo sistema
    /// </summary>
    public void ToggleInventory()
    {
        Debug.Log("ToggleInventory() llamado directamente");

        if (inventoryOpened)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
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

        // NUEVO: Limpiar periódicamente ítems nulos de la lista de registrados
        if (Time.frameCount % 300 == 0) // Cada 5 segundos aprox a 60fps
        {
            CleanupRegisteredItems();
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
    // Añadir en ShowInteractionForNewItem para asegurar que se rastrea correctamente el ítem
    public void ShowInteractionForNewItem(GameObject prefab, string itemName)
    {
        // Buscar el ItemData correspondiente a este ítem
        ItemData correspondingItemData = inventoryItems.Find(item => item.itemName == itemName);

        // Cerrar cualquier interacción activa primero
        if (activeInteractionObject != null)
        {
            CloseActiveInteractionObject();
        }

        // Marcar que el ítem se acaba de añadir
        isNewlyAddedItem = true;
        lastAddedItemName = itemName;

        // NUEVO: Asignar el ItemData correspondiente
        currentlyOpenItemData = correspondingItemData;

        // Instanciar el prefab
        activeInteractionObject = InstantiateInteractionPrefab(prefab, itemName, true);

        Debug.Log($"Mostrando prefab de interacción para el ítem recién añadido: {itemName} (ItemData: {(currentlyOpenItemData != null ? "asignado" : "NULL")})");
    }
    private void InstantiateItemInUI(ItemData item)
    {
        // Determinar el contenedor y plantilla apropiados según el tipo de ítem
        Transform parentContainer = item.itemType == ItemData.ItemType.Nota ? noteContainer : objectContainer;
        GameObject template = item.itemType == ItemData.ItemType.Nota ? noteTemplate : objectTemplate;

        // Instanciar el elemento UI
        GameObject newItemUI = Instantiate(template, parentContainer);
        StartCoroutine(RefreshUISetupDelayed());

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
        StartCoroutine(NotifyNavigationChanges());
        StartCoroutine(RefreshUISetupDelayed());
    }

    private IEnumerator NotifyNavigationChanges()
    {
        yield return new WaitForEndOfFrame();

        // Notificar al sistema de navegación que hay nuevos elementos
        if (inventoryNavigation != null && inventoryNavigation.IsNavigationActive())
        {
            inventoryNavigation.ForceRefreshElements();
            Debug.Log("Navegación del inventario actualizada con nuevos elementos");
        }

        // Limpiar EventSystems duplicados si es necesario
        EventSystemManager.OnUIContentInstantiated();
    }

    /// <summary>
    /// Método llamado cuando se hace clic en un ítem del inventario
    /// MODIFICADO: Ahora rastrea qué ítem se está abriendo
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

            // NUEVO: Marcar qué ítem tiene el prefab abierto actualmente
            currentlyOpenItemData = item;

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
        EventSystemManager.OnUIContentInstantiated();

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
    /// MODIFICADO: Ahora también maneja la notificación de eventos de cierre
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
                ItemData itemToNotify = currentlyOpenItemData; // Capturar el ítem antes de limpiar

                // CRÍTICO: Notificar ANTES de destruir el objeto activo
                if (!wasNewItem && itemToNotify != null)
                {
                    NotifyPrefabClosed(itemToNotify);
                    Debug.Log($"Evento de cierre notificado directamente desde botón para: {itemToNotify.itemName}");
                }

                // Destruir el objeto activo
                DestroyActiveInteractionObject();

                // Mostrar popup SOLO si era un ítem recién añadido
                if (wasNewItem)
                {
                    DisplayPopUp(lastAddedItemName + " added");
                }
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
    /// MODIFICADO: Ahora también maneja la notificación de eventos de cierre
    /// </summary>
    public void CloseActiveInteractionObject()
    {
        if (activeInteractionObject != null)
        {
            // Capturar referencias antes de cualquier operación
            ItemData itemToNotify = currentlyOpenItemData;
            bool wasNewItem = isNewlyAddedItem;

            // Buscar el botón de cierre y simular clic para mantener el comportamiento esperado
            Button closeButton = FindButtonInChildren(activeInteractionObject, "Close_Interacted_Button");

            if (closeButton != null)
            {
                // IMPORTANTE: Notificar ANTES de invocar el onClick para asegurar que se reciba el evento
                if (!wasNewItem && itemToNotify != null)
                {
                    NotifyPrefabClosed(itemToNotify);
                    Debug.Log($"Evento de cierre notificado para: {itemToNotify.itemName}");
                }

                closeButton.onClick.Invoke();
            }
            else
            {
                // Si no hay botón, destruir directamente
                DestroyActiveInteractionObject();

                // Notificar que se cerró el prefab (solo si no era un ítem nuevo)
                if (!wasNewItem && itemToNotify != null)
                {
                    NotifyPrefabClosed(itemToNotify);
                    Debug.Log($"Evento de cierre notificado para: {itemToNotify.itemName}");
                }

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
    /// MODIFICADO: Ahora limpia también la referencia del ítem abierto
    /// </summary>
    private void DestroyActiveInteractionObject()
    {
        if (activeInteractionObject != null)
        {
            Destroy(activeInteractionObject);
            activeInteractionObject = null;
            isNewlyAddedItem = false;

            // NUEVO: Limpiar la referencia del ítem abierto
            currentlyOpenItemData = null;

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

    /// <summary>
    /// Método público para forzar el cierre del inventario desde otros sistemas
    /// </summary>
    public void ForceCloseInventory()
    {
        if (inventoryOpened)
        {
            Debug.Log("Forzando cierre del inventario");
            CloseInventory();
        }
    }

    private IEnumerator RefreshUISetupDelayed()
    {
        yield return new WaitForEndOfFrame();

        InventoryUISetup uiSetup = GetComponent<InventoryUISetup>();
        if (uiSetup != null)
        {
            uiSetup.RefreshNavigation();
        }

        // También notificar sobre cleanup de EventSystems
        EventSystemManager.OnUIContentInstantiated();
    }

    #region Debug Methods

    [ContextMenu("Debug Close Events System")]
    public void DebugCloseEventsSystem()
    {
        Debug.Log("=== SISTEMA DE EVENTOS DE CIERRE ===");
        Debug.Log($"Ítems registrados: {registeredItems.Count}");
        Debug.Log($"Ítem actualmente abierto: {(currentlyOpenItemData != null ? currentlyOpenItemData.itemName : "NINGUNO")}");
        Debug.Log($"Objeto de interacción activo: {(activeInteractionObject != null ? activeInteractionObject.name : "NINGUNO")}");
        Debug.Log($"Es ítem recién añadido: {isNewlyAddedItem}");

        if (registeredItems.Count > 0)
        {
            Debug.Log("--- ÍTEMS REGISTRADOS ---");
            for (int i = 0; i < registeredItems.Count; i++)
            {
                var item = registeredItems[i];
                if (item != null)
                {
                    Debug.Log($"[{i}] {item.gameObject.name} - ItemData: {(item.itemData != null ? item.itemData.itemName : "NULL")}");
                }
                else
                {
                    Debug.Log($"[{i}] NULL ITEM (será limpiado)");
                }
            }
        }
        Debug.Log("====================================");
    }

    [ContextMenu("Test Close Event Notification")]
    public void TestCloseEventNotification()
    {
        if (Application.isPlaying && currentlyOpenItemData != null)
        {
            Debug.Log($"Probando notificación de cierre para: {currentlyOpenItemData.itemName}");
            NotifyPrefabClosed(currentlyOpenItemData);
        }
        else if (Application.isPlaying)
        {
            Debug.LogWarning("No hay ítem abierto actualmente para probar");
        }
        else
        {
            Debug.LogWarning("El test solo funciona en modo Play");
        }
    }

    [ContextMenu("Force Cleanup Registered Items")]
    public void ForceCleanupRegisteredItems()
    {
        int beforeCount = registeredItems.Count;
        CleanupRegisteredItems();
        int afterCount = registeredItems.Count;
        Debug.Log($"Limpieza de ítems registrados: {beforeCount} -> {afterCount} (eliminados: {beforeCount - afterCount})");
    }

    #endregion
}