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

        //En caso de que se haya configurado una DialogueOption con requiresDiceRoll, y el actionId est� referenciado en una dialogueOption
        //esta acci�n pasar� a actuar como receptor, y har� diferentes acciones dependiendo del valor que el dado devuelva ("�xito" o "Fallo")

        //Ejemplo : 
        
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

    public void InvokeAction(string actionId, bool? isSuccess = null)
    {
        if (actionMap.TryGetValue(actionId, out var action))
        {
            // Acciones est�ndar (sin tirada de dado)
            if (!isSuccess.HasValue)
            {
                action?.Invoke();
            }
            else
            {
                // Evaluar �xito o fallo basado en el resultado del dado
                if (isSuccess.Value)
                {
                    Debug.Log($"Action Success for ID: {actionId}");
                    OnActionSuccess(actionId);
                }
                else
                {
                    Debug.Log($"Action Fail for ID: {actionId}");
                    OnActionFail(actionId);
                }
            }
        }
        else
        {
            Debug.LogWarning($"No action registered for ID: {actionId}");
        }
    }

    // Acci�n en caso de �xito
    private void OnActionSuccess(string actionId)
    {
        Debug.Log($"Performing success-specific action for {actionId}");
    }

    // Acci�n en caso de fallo
    private void OnActionFail(string actionId)
    {
        Debug.Log($"Performing fail-specific action for {actionId}");
    }

}
