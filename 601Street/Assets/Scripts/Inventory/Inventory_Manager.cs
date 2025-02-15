using UnityEngine;
using UnityEngine.Events; // Necesario para usar UnityEvent
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class Inventory_Manager : MonoBehaviour
{
    public static Inventory_Manager Instance;

    public Transform noteContainer;
    public Transform objectContainer;

    [Header("Inventory UI")]
    public GameObject InventoryInterface;
    public GameObject noteTemplate;
    public GameObject objectTemplate;

    // Referencias al Pop-Up
    public GameObject popUpParent;  // El objeto que activa el pop-up
    public TMP_Text popUpText;      // El texto que se actualizar� en el Pop-Up

    private List<ItemData> inventoryItems = new List<ItemData>();
    private Dictionary<ItemData, UnityEvent> itemEvents = new Dictionary<ItemData, UnityEvent>();

    private bool inventoryOpened = false;
    private float popUpDuration = 4.5f; // Duraci�n antes de desactivar el Pop-Up
    private float lastPickUpTime = -100f; // Para saber el tiempo de la �ltima recolecci�n

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
        }
    }

    private void Start()
    {
        InventoryInterface.SetActive(false);
        popUpParent.SetActive(false); // Aseguramos que el Pop-Up est� inactivo al inicio
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (!inventoryOpened)
            {
                InventoryInterface.SetActive(true);
                inventoryOpened = true;
            }
            else
            {
                InventoryInterface.SetActive(false);
                inventoryOpened = false;
            }
        }

        // Si el Pop-Up est� activo y ha pasado el tiempo, desactivarlo
        if (popUpParent.activeSelf && Time.time - lastPickUpTime >= popUpDuration)
        {
            popUpParent.SetActive(false);
        }
    }

    public void AddItem(ItemData item, UnityEvent onItemClick)
    {
        inventoryItems.Add(item);
        itemEvents[item] = onItemClick; // Guardar el evento asociado al �tem
        InstantiateItemInUI(item);

        // Mostrar el Pop-Up con el nombre del �tem recogido
        DisplayPopUp(item.itemName);
    }

    private void InstantiateItemInUI(ItemData item)
    {
        // Seleccionar el contenedor y el template seg�n el tipo de �tem
        Transform parentContainer = item.itemType == ItemData.ItemType.Nota ? noteContainer : objectContainer;
        GameObject template = item.itemType == ItemData.ItemType.Nota ? noteTemplate : objectTemplate;

        // Instanciar el nuevo objeto de UI
        GameObject newItemUI = Instantiate(template, parentContainer);

        // Obtener el componente Image del nuevo objeto de UI
        Image itemImage = newItemUI.GetComponent<Image>();
        if (itemImage != null)
        {
            // Asignar el Sprite al componente Image
            itemImage.sprite = item.inventoryImage;
        }
        else
        {
            Debug.LogError("El objeto instanciado no tiene un componente Image: " + newItemUI.name);
        }

        // Obtener el componente TMP_Text del nuevo objeto de UI
        TMP_Text itemNameText = newItemUI.GetComponentInChildren<TMP_Text>();
        if (itemNameText != null)
        {
            // Asignar el nombre del �tem al texto
            itemNameText.text = item.itemName;
        }
        else
        {
            Debug.LogError("No se encontr� un componente TMP_Text en los hijos de: " + newItemUI.name);
        }

        // Configurar el bot�n para que active el evento onItemClick
        Button itemButton = newItemUI.GetComponent<Button>();
        if (itemButton != null)
        {
            // Asignar el evento onItemClick del ItemData al bot�n
            itemButton.onClick.AddListener(() => itemEvents[item].Invoke());
        }
        else
        {
            Debug.LogError("El objeto instanciado no tiene un componente Button: " + newItemUI.name);
        }

        // Activar el objeto de UI
        newItemUI.SetActive(true);
    }

    private void DisplayPopUp(string itemName)
    {
        // Activar el Pop-Up
        popUpParent.SetActive(true);

        // Actualizar el texto del Pop-Up con el nombre del �tem + " a�adido"
        popUpText.text = itemName + " a�adido";

        // Actualizar el tiempo de la �ltima recolecci�n
        lastPickUpTime = Time.time;
    }
}
