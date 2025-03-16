using UnityEngine;

public class DiceBonus : MonoBehaviour
{
    // Referencia al Dice_Manager en la escena
    private Dice_Manager diceManager;

    // Tipo de bonificación que activará este objeto
    public enum BonusType
    {
        Bonus1,
        Bonus2,
        Bonus3
    }

    [Tooltip("Selecciona qué bonificación activará este objeto")]
    public BonusType bonusType;

    [Tooltip("Si está marcado, el objeto mostrará un mensaje en consola al activarse")]
    public bool showDebugMessage = true;

    private void Start()
    {
        // Buscar el Dice_Manager en la escena
        diceManager = FindAnyObjectByType<Dice_Manager>();
        if (diceManager == null)
        {
            Debug.LogError("No se encontró Dice_Manager en la escena. El objeto de bonus no funcionará correctamente.");
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

        // Activar la bonificación correspondiente según el tipo seleccionado
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