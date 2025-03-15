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

    public GameObject popUpParent;
    public TMP_Text popUpText;

    private List<ItemData> inventoryItems = new List<ItemData>();
    private Dictionary<ItemData, UnityEvent> itemEvents = new Dictionary<ItemData, UnityEvent>();

    private bool inventoryOpened = false;
    private float popUpDuration = 4.5f;
    private float lastPickUpTime = -100f;

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
        popUpParent.SetActive(false);
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

        if (popUpParent.activeSelf && Time.time - lastPickUpTime >= popUpDuration)
        {
            popUpParent.SetActive(false);
        }
    }

    public void AddItem(ItemData item, UnityEvent onItemClick)
    {
        inventoryItems.Add(item);
        itemEvents[item] = onItemClick;
        InstantiateItemInUI(item);
    }

    private void InstantiateItemInUI(ItemData item)
    {
        Transform parentContainer = item.itemType == ItemData.ItemType.Nota ? noteContainer : objectContainer;
        GameObject template = item.itemType == ItemData.ItemType.Nota ? noteTemplate : objectTemplate;

        GameObject newItemUI = Instantiate(template, parentContainer);

        Image itemImage = newItemUI.GetComponent<Image>();
        if (itemImage != null)
        {
            itemImage.sprite = item.inventoryImage;
        }
        else
        {
            Debug.LogError("El objeto instanciado no tiene un componente Image: " + newItemUI.name);
        }

        TMP_Text itemNameText = newItemUI.GetComponentInChildren<TMP_Text>();
        if (itemNameText != null)
        {
            itemNameText.text = item.itemName;
        }
        else
        {
            Debug.LogError("No se encontró un componente TMP_Text en los hijos de: " + newItemUI.name);
        }

        Button itemButton = newItemUI.GetComponent<Button>();
        if (itemButton != null)
        {
            itemButton.onClick.AddListener(() => itemEvents[item].Invoke());
        }
        else
        {
            Debug.LogError("El objeto instanciado no tiene un componente Button: " + newItemUI.name);
        }

        newItemUI.SetActive(true);
    }

    public void DisplayPopUp(string itemName)
    {
        popUpParent.SetActive(true);
        popUpText.text = itemName + " added";
        lastPickUpTime = Time.time;
    }


}
