using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class Dice_Manager : MonoBehaviour
{
    [Header("Dice Interface")]
    public GameObject diceInferface;


    [Header("Interface Objects")]

    [SerializeField] private TMP_Text diceResultText;
    [SerializeField] private TMP_Text difficultyClassText;
    [SerializeField] private GameObject failPopup;
    [SerializeField] private GameObject continueButton;
    [SerializeField] private GameObject rollButton;
    [SerializeField] private GameObject diceObject;

    [Header("Success/Fail Feedback")]
    [SerializeField] private GameObject failObject;
    [SerializeField] private GameObject successObject;

    [Header("Bonus Indicators")]
    [SerializeField] private GameObject bonus1Object;
    [SerializeField] private GameObject bonus2Object;
    [SerializeField] private GameObject bonus3Object;

    [Header("Bonus Pop-Ups")]
    [SerializeField] private GameObject bonus1Popup;
    [SerializeField] private GameObject bonus2Popup;
    [SerializeField] private GameObject bonus3Popup;

    public bool bonus1Activated;
    public bool bonus2Activated;
    public bool bonus3Activated;

    private int bonus1 = 2;
    private int bonus2 = 3;
    private int bonus3 = 4;

    private int baseRoll;
    private int totalRoll;
    private bool canRoll = true;
    private int currentDifficultyClass;

    public Action<bool> OnRollComplete;


    private void Start()
    {
        diceInferface.SetActive(false);


        bonus1Object.SetActive(bonus1Activated);
        bonus2Object.SetActive(bonus2Activated);
        bonus3Object.SetActive(bonus3Activated);

        ResetUI();
    }

    private void FixedUpdate()
    {
        SetDifficultyClass(currentDifficultyClass);
    }

    public void SetDifficultyClass(int difficultyClass)
    {
        currentDifficultyClass = difficultyClass;
        difficultyClassText.text = difficultyClass.ToString();
        rollButton.SetActive(true);
    }

    public void OnRollButtonClicked()
    {
        if (canRoll)
        {
            RollDice(currentDifficultyClass);
        }
    }

    private void RollDice(int difficultyClass)
    {
        StartCoroutine(RollDiceSequence(difficultyClass));
    }

    private IEnumerator RollDiceSequence(int difficultyClass)
    {
        canRoll = false;
        rollButton.SetActive(false);

        // "Animación" Dado
        float rotationTime = 2f;
        float elapsedTime = 0f;

        while (elapsedTime < rotationTime)
        {
            elapsedTime += Time.deltaTime;
            diceObject.transform.Rotate(Vector3.back, 360 * Time.deltaTime / rotationTime);

            int randomRoll = UnityEngine.Random.Range(1, 21);
            diceResultText.text = randomRoll.ToString();

            yield return null;
        }

        // Acabar la tirada
        baseRoll = UnityEngine.Random.Range(1, 21);
        diceResultText.text = baseRoll.ToString();
        totalRoll = baseRoll;

        yield return new WaitForSeconds(1f);

        // Aplicar Bonuses
        if (bonus1Activated)
        {
            totalRoll += bonus1;
            bonus1Popup.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            bonus1Popup.SetActive(false);
        }

        if (bonus2Activated)
        {
            totalRoll += bonus2;
            bonus2Popup.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            bonus2Popup.SetActive(false);
        }

        if (bonus3Activated)
        {
            totalRoll += bonus3;
            bonus3Popup.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            bonus3Popup.SetActive(false);
        }

        diceResultText.text = totalRoll.ToString();

        // Comprobar resultado
        bool isSuccess = totalRoll >= difficultyClass;
        failPopup.SetActive(!isSuccess);
        OnRollComplete?.Invoke(isSuccess);

        if(totalRoll >= difficultyClass)
        {
            successObject.SetActive(true );
            Debug.Log("Exito");
        }
        else
        {
            failObject.SetActive(true);
            Debug.Log("Fail");
        }
        


        continueButton.SetActive(true);
        canRoll = true;
    }

    public void Continue()
    {
        diceInferface.SetActive(false);
        continueButton.SetActive(false);
        ResetUI();
    }

    public void ResetUI()
    {
        diceResultText.text = "";
        difficultyClassText.text = "";
        failPopup.SetActive(false);
        continueButton.SetActive(false);
        rollButton.SetActive(false);

        successObject.SetActive(false);
        failObject.SetActive(false);

        bonus1Popup.SetActive(false);
        bonus2Popup.SetActive(false);
        bonus3Popup.SetActive(false);
    }
}
