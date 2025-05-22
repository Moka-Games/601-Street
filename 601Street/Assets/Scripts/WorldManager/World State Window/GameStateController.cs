using UnityEngine.PlayerLoop;
using UnityEditor;
using UnityEngine;
using System.Diagnostics; // A�adido para usar StackTrace
using Debug = UnityEngine.Debug; // Explicitly alias UnityEngine.Debug

public class GameStateController : MonoBehaviour
{
    private WorldStateGraphRunner stateRunner;

    void Start()
    {
        stateRunner = FindFirstObjectByType<WorldStateGraphRunner>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            ChangeGameState("Nota_Interactuada");
        }
    }

    public void ChangeGameState(string stateID)
    {
        // Obtener el Stack Trace para ver desde d�nde se llama la funci�n
        StackTrace stackTrace = new StackTrace(true);

        // El frame 0 es esta funci�n, el frame 1 es quien la llam�
        StackFrame callingFrame = stackTrace.GetFrame(1);

        if (callingFrame != null)
        {
            string fileName = callingFrame.GetFileName();
            string methodName = callingFrame.GetMethod().Name;
            string className = callingFrame.GetMethod().DeclaringType?.Name;
            int lineNumber = callingFrame.GetFileLineNumber();

            Debug.Log($"[ChangeGameState] Llamada desde: {className}.{methodName}() " +
                     $"en l�nea {lineNumber} " +
                     $"(Archivo: {System.IO.Path.GetFileName(fileName)}) " +
                     $"- StateID: {stateID}");
        }
        else
        {
            Debug.Log($"[ChangeGameState] No se pudo obtener informaci�n del caller - StateID: {stateID}");
        }

        if (stateRunner != null)
        {
            stateRunner.ActivateState(stateID);
        }
        else
        {
            Debug.LogError("No se encontr� el WorldStateGraphRunner");
        }
    }
}
