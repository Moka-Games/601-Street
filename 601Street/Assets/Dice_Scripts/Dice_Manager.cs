using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class Dice_Manager : MonoBehaviour
{
    [SerializeField] private TMP_Text diceResultText;
    [SerializeField] private TMP_Text difficultyClassText;

    [SerializeField] private Image diceImage;

    public int difficultyClass;

    private bool canThrow = true;

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

    public GameObject FAIL;

    private void Start()
    {
        difficultyClassText.text = "" + difficultyClass;

        bonus_1_Object.SetActive(bonus_1_activated);
        bonus_2_Object.SetActive(bonus_2_activated);
        bonus_3_Object.SetActive(bonus_3_activated);
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
        canThrow = false;

        float elapsedTime = 0f;
        while (elapsedTime < 2f)
        {
            if (diceResultText != null)
            {
                int randomValue = Random.Range(1, 20);
                diceResultText.text = "" + randomValue;
            }

            if (diceImage != null)
            {
                diceImage.transform.Rotate(0f, 0f, -36f);
            }

            elapsedTime += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        int result = Random.Range(1, 20);
        int totalResult = result;

        Debug.Log("Dado lanzado: " + result);

        if (diceResultText != null)
        {
            diceResultText.text = "" + result;
        }
        else
        {
            Debug.LogWarning("No se ha asignado un componente TMP_Text para mostrar el resultado del dado.");
        }

        yield return new WaitForSeconds(1f);

        if (bonus_1_activated)
        {
            totalResult += bonus_1;
            diceResultText.text = "" + totalResult;
            popUp_Bonus_1.SetActive(true);
        }
        if (bonus_2_activated)
        {
            totalResult += bonus_2;
            diceResultText.text = "" + totalResult;
            popUp_Bonus_2.SetActive(true);
        }
        if (bonus_3_activated)
        {
            totalResult += bonus_3;
            diceResultText.text = "" + totalResult;
            popUp_Bonus_3.SetActive(true);
        }

        // Verificar si el resultado total es menor que la dificultad
        if (totalResult < difficultyClass)
        {
            FAIL.SetActive(true);
        }
        else
        {
            FAIL.SetActive(false);
            Debug.Log("Success: Resultado igual o mayor a la dificultad.");
        }

        yield return new WaitForSeconds(1);

        canThrow = true;
    }

    private int Bonus(int bonus)
    {
        return bonus;
    }
}
