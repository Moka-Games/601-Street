using System;
using System.Collections.Generic;
using UnityEngine;

public class ActionController : MonoBehaviour
{
    public static ActionController Instance { get; private set; }

    private Dictionary<string, DialogueAction> actionMap = new Dictionary<string, DialogueAction>();

    [Header("Interior Comisaria")]
    public string pensamientoPostInteracci�n;

    void Start()
    {
        //DESCRIPCI�N FUNCIONALIDAD
        // 
        //Se registran acciones seg�n los par�metros: 1. Acci�n Estandard, 2.Acci�n cuando el dado devuelve �xito
        //3. Acci�n cuando el dado devuelve fallo
        //En este caso ("Opcion_1" es el actionID que le damos a nuestra acci�n, de forma que si configuramos nuestra
        //Dialogue Option con este Id se realizar� una de las 3 acciones dependiendo del contexto
        //
        //

        RegisterAction("Option_1", new DialogueAction(
        () => Actions_Script.Instance.PoliciaInteractuado(), // Acci�n estandard
        () => Actions_Script.Instance.PoliciaInteractuado(), // Acci�n de �xito
        () => Actions_Script.Instance.PoliciaInteractuado()  // Acci�n de fracaso
    )); 

        RegisterAction("Option_2", new DialogueAction(
        () => Actions_Script.Instance.PoliciaInteractuado(), 
        () => Actions_Script.Instance.PoliciaInteractuado(), 
        () => Actions_Script.Instance.PoliciaInteractuado()
    ));
        RegisterAction("Option_3", new DialogueAction(
        () => Actions_Script.Instance.PoliciaInteractuado(), 
        () => Actions_Script.Instance.PoliciaInteractuado(),
        () => Actions_Script.Instance.PoliciaInteractuado()  
    ));

        //Ejemplo Acci�n registrada para final de conversaci�n
        RegisterAction("ConversationEnd_1", new DialogueAction(
    () => Actions_Script.Instance.MostrarPensamiento(pensamientoPostInteracci�n),
    null,
    null
));

        RegisterAction("Bar", new DialogueAction(
    () => BarInterior.conversaci�nPoliciaTerminada = true,
    null,
    null
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
