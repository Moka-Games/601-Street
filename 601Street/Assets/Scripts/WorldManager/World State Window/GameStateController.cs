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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            ChangeGameState("Police_Interacted");
        }
        
    }
    public void ChangeGameState(string stateID)
    {
        if (stateRunner != null)
        {
            stateRunner.ActivateState(stateID);
        }
        else
        {
            Debug.LogError("No se encontró el WorldStateGraphRunner");
        }
    }
}