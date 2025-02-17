using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ContactManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject contactRowPrefab;  // Prefab de la fila
    public GameObject contactItemPrefab; // Prefab del contacto (Image + Text)
    public Transform contactListParent;  // Parent del layout vertical -> En nuestro caso siempre es el objeto "Content) que es hijo del Mask en "Contactos"

    [Header("Contacts List")]
    public List<Contact> contacts = new List<Contact>();

    private GameObject currentRow;
    private const int MaxContactsPerRow = 2;

    private Dictionary<int, Action> contactActionsByID = new Dictionary<int, Action>();

    //Referencias a scripts externos
    private Telefono_Manager telefonoManager;

    private void Start()
    {
        telefonoManager = FindAnyObjectByType<Telefono_Manager>();

        CreateNewRow();

        foreach (var contact in contacts)
        {
            AddContactToUI(contact);
        }

        SetContactActionByID(1, () => telefonoManager.MostrarPensamientoDeseado());
        SetContactActionByID(2, () => telefonoManager.ClosePhone());
        SetContactActionByID(3, () => telefonoManager.OpenApps());
    }

    public void AddContactToUI(Contact contact)
    {
        if (currentRow.transform.childCount >= MaxContactsPerRow)
        {
            CreateNewRow();
        }

        GameObject newContactItem = Instantiate(contactItemPrefab, currentRow.transform);
        newContactItem.transform.Find("Contact_Image").GetComponent<Image>().sprite = contact.contactImage;
        newContactItem.transform.Find("Contact_Name").GetComponent<TMP_Text>().text = contact.contactName;

        Button contactButton = newContactItem.transform.Find("Contact_Image").gameObject.AddComponent<Button>();
        contactButton.onClick.AddListener(() => OnContactClicked(contact.contactID));
    }

    private void CreateNewRow()
    {
        currentRow = Instantiate(contactRowPrefab, contactListParent);
    }

    public void SetContactActionByID(int contactID, Action action)
    {
        contactActionsByID[contactID] = action;
    }

    private void OnContactClicked(int contactID)
    {
        if (contactActionsByID.TryGetValue(contactID, out Action action))
        {
            action?.Invoke();
        }
    }
}
