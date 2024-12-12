using UnityEngine;
using TMPro; // Importar la biblioteca para TextMeshPro
using UnityEngine.UI; // Importar para botones y UI
using System.Collections;

public class Dice_Manager : MonoBehaviour
{
    // Referencia al componente TextMeshPro para mostrar el resultado
    [SerializeField] private TMP_Text diceResultText;
    [SerializeField] private TMP_Text difficultyClass;

    // Referencia a la imagen que rotará
    [SerializeField] private Image diceImage;

    public int minNumber;

    private bool canThrow = true; // Bandera para controlar si el dado puede lanzarse

    public bool bonus_1_activated;
    public bool bonus_2_activated;
    public bool bonus_3_activated;

    [Header("Indicadores Bonus")]
    public GameObject bonus_1_Object;
    public GameObject bonus_2_Object;
    public GameObject bonus_3_Object;

    [Header("Pop-Up's")]
    public GameObject popUp_Bonus_1;
    public GameObject popUp_Bonus_2;
    public GameObject popUp_Bonus_3;

    private int bonus_1 = 2;
    private int bonus_2 = 3;
    private int bonus_3 = 4;

    private void Start()
    {
        difficultyClass.text = "" + minNumber;

        if (bonus_1_activated)
        {
            bonus_1_Object.SetActive(true);
        }
        else
        {
            bonus_1_Object.SetActive(false);
        }
        if (bonus_2_activated)
        {
            bonus_2_Object.SetActive(true);
        }
        else
        {
            bonus_2_Object.SetActive(false);
        }
        if (bonus_3_activated)
        {
            bonus_3_Object.SetActive(true);
        }
        else
        {
            bonus_3_Object.SetActive(false);
        }
    }

    public void OnRollDiceButtonClicked()
    {
        if (canThrow)
        {
            StartCoroutine(RollDiceSequence());
        }
    }

    private IEnumerator RollDiceSequence()
    {
        canThrow = false; // Desactivar la capacidad de lanzar el dado

        // Mostrar números aleatorios durante 2 segundos
        float elapsedTime = 0f;
        while (elapsedTime < 2f)
        {
            if (diceResultText != null)
            {
                int randomValue = Random.Range(1, 7);
                diceResultText.text = "" + randomValue;
            }

            if (diceImage != null)
            {
                // Rotar la imagen gradualmente
                diceImage.transform.Rotate(0f, 0f, -36f); // Rotar 36 grados cada 0.1 segundos
            }

            elapsedTime += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        // Generar el resultado final
        int result = Random.Range(1, 7);

        Debug.Log("Dado lanzado: " + result);

        if (result < minNumber)
        {
            Debug.Log("El resultado es menor que 6.");
        }

        if (diceResultText != null)
        {
            diceResultText.text = "" + result;
        }
        else
        {
            Debug.LogWarning("No se ha asignado un componente TMP_Text para mostrar el resultado del dado.");
        }

        yield return new WaitForSeconds(1);

        if (diceResultText != null && bonus_1_activated)
        {
            diceResultText.text = "" + (result + bonus_1);
            popUp_Bonus_1.SetActive(true);
        }
        if (diceResultText != null && bonus_2_activated)
        {
            diceResultText.text = "" + (result + bonus_2);
            popUp_Bonus_2.SetActive(true);
        }
        if (diceResultText != null && bonus_3_activated)
        {
            diceResultText.text = "" + (result + bonus_3);
            popUp_Bonus_3.SetActive(true);
        }

        canThrow = true; // Reactivar la capacidad de lanzar el dado
    }

    private int Bonus(int bonus)
    {
        return bonus;
    }
}
