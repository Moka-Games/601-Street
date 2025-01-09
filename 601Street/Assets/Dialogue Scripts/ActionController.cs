using System;
using System.Collections.Generic;
using UnityEngine;

public class ActionController : MonoBehaviour
{
    public static ActionController Instance { get; private set; }

    private Dictionary<string, Action> actionMap = new Dictionary<string, Action>();


    void Start()
    {
        //Aqu� registro acciones, las cuales pueden estar declaradas en el propio script o en un ajeno
        //En este caso "SayHi" es el ID que le damos a la acci�n
        //Si ponemos este ID en el valor actionID del scriptable object de la "DialogueOption", se realizar� esa acci�n al acabar la conversaci�n
        ActionController.Instance.RegisterAction("SayHi", DialogueManager.Instance.RandomFunction);
        ActionController.Instance.RegisterAction("SayBye", SayBye);
    }
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterAction(string actionId, Action action)
    {
        if (!actionMap.ContainsKey(actionId))
        {
            actionMap[actionId] = action;
        }
        else
        {
            Debug.LogWarning($"Action with ID {actionId} already registered.");
        }
    }

    public void InvokeAction(string actionId)
    {
        if (actionMap.TryGetValue(actionId, out var action))
        {
            action?.Invoke();
        }
        else
        {
            Debug.LogWarning($"No action found for ID {actionId}");
        }
    }

    public void SayHi()
    {
        print("Hi");
    }
    public void SayBye()
    {
        print("Bye");
    }
}
