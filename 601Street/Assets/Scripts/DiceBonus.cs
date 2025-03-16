using UnityEngine;

public class DiceBonus : MonoBehaviour
{
    // Referencia al Dice_Manager en la escena
    private Dice_Manager diceManager;

    // Tipo de bonificaci�n que activar� este objeto
    public enum BonusType
    {
        Bonus1,
        Bonus2,
        Bonus3
    }

    [Tooltip("Selecciona qu� bonificaci�n activar� este objeto")]
    public BonusType bonusType;

    [Tooltip("Si est� marcado, el objeto mostrar� un mensaje en consola al activarse")]
    public bool showDebugMessage = true;

    private void Start()
    {
        // Buscar el Dice_Manager en la escena
        diceManager = FindAnyObjectByType<Dice_Manager>();
        if (diceManager == null)
        {
            Debug.LogError("No se encontr� Dice_Manager en la escena. El objeto de bonus no funcionar� correctamente.");
        }

        // Verificar que exista el componente InteractableObject
        InteractableObject interactable = GetComponent<InteractableObject>();
        if (interactable == null)
        {
            Debug.LogError("Este objeto necesita un componente InteractableObject para funcionar.");
            enabled = false;
            return;
        }
    }

    public void ActivateBonus()
    {
        if (diceManager == null) return;

        // Activar la bonificaci�n correspondiente seg�n el tipo seleccionado
        switch (bonusType)
        {
            case BonusType.Bonus1:
                diceManager.bonus1Activated = true;
                if (showDebugMessage) Debug.Log("Bonus 1 activado");
                break;

            case BonusType.Bonus2:
                diceManager.bonus2Activated = true;
                if (showDebugMessage) Debug.Log("Bonus 2 activado");
                break;

            case BonusType.Bonus3:
                diceManager.bonus3Activated = true;
                if (showDebugMessage) Debug.Log("Bonus 3 activado");
                break;
        }
    }
}