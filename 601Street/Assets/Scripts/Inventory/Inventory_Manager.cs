using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Necesario para usar TMP_Text

public class Inventory_Manager : MonoBehaviour
{
    public static Inventory_Manager Instance;

    public Transform noteContainer;
    public Transform objectContainer;

    [Header("Inventory UI")]
    public GameObject InventoryInterface;
    public GameObject noteTemplate;
    public GameObject objectTemplate;

    private List<ItemData> inventoryItems = new List<ItemData>();

    private bool inventoryOpened = false;

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
    }

    public void AddItem(ItemData item)
    {
        inventoryItems.Add(item);
        InstantiateItemInUI(item);
    }

    private void InstantiateItemInUI(ItemData item)
    {
        // Seleccionar el contenedor y el template según el tipo de ítem
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
            // Asignar el nombre del ítem al texto
            itemNameText.text = item.itemName;
        }
        else
        {
            Debug.LogError("No se encontró un componente TMP_Text en los hijos de: " + newItemUI.name);
        }

        // Activar el objeto de UI
        newItemUI.SetActive(true);
    }
}