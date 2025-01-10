using System;
using System.Collections.Generic;
using UnityEngine;

public class ActionController : MonoBehaviour
{
    public static ActionController Instance { get; private set; }

    private Dictionary<string, DialogueAction> actionMap = new Dictionary<string, DialogueAction>();

    void Start()
    {
        //DESCRIPCI�N FUNCIONALIDAD
        // 
        //Se registraci�n acciones seg�n los par�mtros 1. Acci�n Estandard, 2.Acci�n cuando el dado devuelve �xito
        //3. Acci�n cuando el dado devuelve fallo
        //En este caso ("Opcion_1" es el actionID que le damos a nuestra acci�n, de forma que si configuramos nuestra
        //Dialogue Option con este Id se realizar� una de las 3 acciones dependiendo del contexto
        //
        //

        RegisterAction("Option_1", new DialogueAction(
        () => Debug.Log("Opci�n 1 respuesta estandard"),
        () => Debug.Log("Opci�n 1 respuesta de �xito"),
        () => Debug.Log("Opci�n 1 respuesta de fallo")
    ));

        RegisterAction("Option_2", new DialogueAction(
            () => Debug.Log("Opci�n 2 respuesta estandard"),
            () => Debug.Log("Opci�n 2 respuesta de �xito"),
            () => Debug.Log("Opci�n 2 respuesta de fallo")
        ));
        RegisterAction("Option_3", new DialogueAction(
            () => Debug.Log("Opci�n 3 respuesta estandard"),
            () => Debug.Log("Opci�n 3 respuesta de �xito"),
            () => Debug.Log("Opci�n 3 respuesta de fallo")
        ));

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

    public void RegisterAction(string actionId, DialogueAction action)
    {
        if (!actionMap.ContainsKey(actionId))
        {
            actionMap[actionId] = action;
            Debug.Log($"Action registered: {actionId}");
        }
        else
        {
            Debug.LogWarning($"Action already registered for ID: {actionId}");
        }
    }

    public void InvokeAction(string actionId, bool? isSuccess = null)
    {
        if (actionMap.TryGetValue(actionId, out var action))
        {
            // Ejecuta la acci�n dependiendo del �xito o fracaso
            if (isSuccess == true)
            {
                action.Execute(isSuccess); // Acci�n para �xito
            }
            else if (isSuccess == false)
            {
                action.Execute(isSuccess); // Acci�n para fallo
            }
            else
            {
                action.Execute(); // Acci�n predeterminada (si no se tira el dado)
            }
        }
        else
        {
            Debug.LogWarning($"No action registered for ID: {actionId}");
        }
    }

}
