using UnityEngine.PlayerLoop;
using UnityEditor;
using UnityEngine;

public class GameStateController : MonoBehaviour
{
    private WorldStateGraphRunner stateRunner;

    void Start()
    {
        stateRunner = FindFirstObjectByType<WorldStateGraphRunner>();
    }

    // Llamar a este m�todo para cambiar a un nuevo estado
    public void ChangeGameState(string stateID)
    {
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