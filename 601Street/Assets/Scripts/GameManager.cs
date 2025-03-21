using UnityEngine;
using System;


public class GameStateManager : MonoBehaviour
{
    private static GameStateManager instance;
    public static GameStateManager Instance
    {
        get
        {
            if (instance == null)
            {
                Debug.LogError("GameStateManager no está inicializado!");
            }
            return instance;
        }
    }

    // Current game state
    private GameState currentState = GameState.OnGameplay;

    // Event that fires when the game state changes
    public event Action<GameState> OnGameStateChanged;

    // Public accessor for the current state
    public GameState CurrentState => currentState;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Method to change the current game state
    public void ChangeState(GameState newState)
    {
        if (currentState != newState)
        {
            GameState previousState = currentState;
            currentState = newState;

            Debug.Log($"Game state changed from {previousState} to {newState}");

            // Notify subscribers about the state change
            OnGameStateChanged?.Invoke(currentState);
        }
    }

    // Helper methods for specific state changes
    public void EnterDialogueState()
    {
        ChangeState(GameState.OnDialogue);
    }

    public void EnterInteractingState()
    {
        ChangeState(GameState.OnInteracting);
    }

    public void EnterGameplayState()
    {
        ChangeState(GameState.OnGameplay);
    }

    // Check if we're in gameplay state
    public bool IsInGameplayState()
    {
        return currentState == GameState.OnGameplay;
    }
}
// Define game states
public enum GameState
{
    OnGameplay,
    OnDialogue,
    OnInteracting
}

// EscenaConfig class that was in your original code
[System.Serializable]
public class EscenaConfig
{
    public string nombreEscena;
    public bool activarPensamientoInicial;
    public string pensamientoInicioTexto;
}