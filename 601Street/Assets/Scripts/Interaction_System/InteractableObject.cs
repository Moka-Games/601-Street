using UnityEngine;
using UnityEngine.Events;

// Esta interfaz define el comportamiento b�sico para objetos interactuables
public interface IInteractable
{
    void Interact();
    string GetInteractionID();
}

// Clase base para objetos con los que se puede interactuar
public class InteractableObject : MonoBehaviour, IInteractable
{
    [Header("Configuraci�n b�sica")]
    [SerializeField] private string interactionID;
    [SerializeField] private string interactionPrompt = "Presiona E para interactuar";
    [Tooltip("Evento que se disparar� cuando el jugador interact�e con este objeto")]
    [SerializeField] private UnityEvent onInteraction;

    // M�todo que se llama cuando el jugador interact�a con este objeto
    public virtual void Interact()
    {
        Debug.Log($"Interactuando con objeto: {gameObject.name} (ID: {interactionID})");
        onInteraction.Invoke();
    }

    // Devuelve el ID �nico de interacci�n
    public string GetInteractionID()
    {
        return interactionID;
    }

    // Devuelve el mensaje de interacci�n para mostrar al jugador
    public string GetInteractionPrompt()
    {
        return interactionPrompt;
    }
}