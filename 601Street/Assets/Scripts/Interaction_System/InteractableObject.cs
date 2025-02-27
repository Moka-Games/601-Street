using UnityEngine;
using UnityEngine.Events;

// Esta interfaz define el comportamiento básico para objetos interactuables
public interface IInteractable
{
    void Interact();
    string GetInteractionID();
}

// Clase base para objetos con los que se puede interactuar
public class InteractableObject : MonoBehaviour, IInteractable
{
    [Header("Configuración básica")]
    [SerializeField] private string interactionID;
    [SerializeField] private string interactionPrompt = "Presiona E para interactuar";
    [Tooltip("Evento que se disparará cuando el jugador interactúe con este objeto")]
    [SerializeField] private UnityEvent onInteraction;

    // Método que se llama cuando el jugador interactúa con este objeto
    public virtual void Interact()
    {
        Debug.Log($"Interactuando con objeto: {gameObject.name} (ID: {interactionID})");
        onInteraction.Invoke();
    }

    // Devuelve el ID único de interacción
    public string GetInteractionID()
    {
        return interactionID;
    }

    // Devuelve el mensaje de interacción para mostrar al jugador
    public string GetInteractionPrompt()
    {
        return interactionPrompt;
    }
}