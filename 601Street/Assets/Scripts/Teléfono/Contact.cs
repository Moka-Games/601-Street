using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "NewContact", menuName = "Contact/Create New Contact")]
public class Contact : ScriptableObject
{
    public Sprite contactImage;
    public string contactName;
    public int contactID;
    public UnityEvent evento;
}

