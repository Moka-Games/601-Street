using System;
using System.Collections.Generic;
using UnityEngine;

public class ActionController : MonoBehaviour
{
    public static ActionController Instance { get; private set; }

    private Dictionary<string, DialogueAction> actionMap = new Dictionary<string, DialogueAction>();

    [Header("Interior Comisaria")]
    public string pensamientoPostInteracción;

    private ComisariManager comisariaManager;

    void Start()
    {


        //DESCRIPCIÓN FUNCIONALIDAD
        // 
        //Se registran acciones según los parámetros: 1. Acción Estandard, 2.Acción cuando el dado devuelve éxito
        //3. Acción cuando el dado devuelve fallo
        //En este caso ("Opcion_1" es el actionID que le damos a nuestra acción, de forma que si configuramos nuestra
        //Dialogue Option con este Id se realizará una de las 3 acciones dependiendo del contexto
        //
        //

        RegisterAction("Option_1", new DialogueAction(
        () => Actions_Script.Instance.PoliciaInteractuado(), // Acción estandard
        () => Actions_Script.Instance.PoliciaInteractuado(), // Acción de éxito
        () => Actions_Script.Instance.PoliciaInteractuado()  // Acción de fracaso
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
        
        RegisterAction("policia_1_id", new DialogueAction(
        () => Actions_Script.Instance.PoliciaSucess(),
        () => Actions_Script.Instance.PoliciaSucess(),
        () => Actions_Script.Instance.PoliciaFail()  
    ));

        //Ejemplo Acción registrada para final de conversación
        RegisterAction("ConversationEnd_1", new DialogueAction(
    () => Actions_Script.Instance.MostrarPensamiento(pensamientoPostInteracción),
    null,
    null
));
            RegisterAction("Conversacion_Policia_1", new DialogueAction(
    () => Actions_Script.Instance.ActivarObjetosPolicia1(),
    null,
    null
));
        RegisterAction("FracasoBar", new DialogueAction(
    () => Actions_Script.Instance.ActivarObjetosBar(),
    null,
    null
));
        RegisterAction("Nakamura-Terminado", new DialogueAction(
    () => Actions_Script.Instance.CambiarPoliciasComisaria(),
    null,
    null
));

        RegisterAction("Policia_SegudoEncuentro", new DialogueAction(
    () => Actions_Script.Instance.ActivarLlamadaDaichiPostComisaria(),
    () => Actions_Script.Instance.ActivarObjetosPolicia2(),
    () => Actions_Script.Instance.ActivarObjetosPolicia2_Fracaso()
));    
        RegisterAction("Fracaso_Sectario_2", new DialogueAction(
    () => PuzzleCollectionManager.Instance.FracasoSectario_2(),
    null,
    null
));
        RegisterAction("Quemar-Casa", new DialogueAction(
    () => Actions_Script.Instance.QuemarCasaCall(),
    null,
    null
));
        RegisterAction("No-Quemar-Casa", new DialogueAction(
    () => Actions_Script.Instance.NoQuemarCasaCall(),
    null,
    null
));
        RegisterAction("Riku_Universidad", new DialogueAction(
        () => print("Permormed Standard Action"),
        () => Riku_Parque.Instance.Follow_Riku(),
        () => Riku_Parque.Instance.PuertaPortal()
    ));
        
        RegisterAction("Riku_Parque", new DialogueAction(
        () => print("Permormed Standard Action"),
        () => PuzzleCollectionManager.Instance.ActivateApple(),
        () => PuzzleCollectionManager.Instance.FracasoRiku()
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
            // Ejecuta la acción dependiendo del éxito o fracaso
            if (isSuccess == true)
            {
                action.Execute(isSuccess); // Acción para éxito
            }
            else if (isSuccess == false)
            {
                action.Execute(isSuccess); // Acción para fallo
            }
            else
            {
                action.Execute(); // Acción predeterminada (si no se tira el dado)
            }
        }
        else
        {
            Debug.LogWarning($"No action registered for ID: {actionId}");
        }
    }
}
