using System;
using System.Collections;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;

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
    [SerializeField] private Transform diceTransform;
    [SerializeField] private Transform diceModelTransform; // Nueva referencia al modelo del dado (hijo)
    private Quaternion initialDiceModelRotation;



    [Header("Success/Fail Feedback")]
    [SerializeField] private GameObject failObject;
    [SerializeField] private GameObject successObject;
    [SerializeField] private RectTransform resultPanel;

    [Header("Bonus Indicators")]
    [SerializeField] private GameObject bonus1Object;
    [SerializeField] private GameObject bonus2Object;
    [SerializeField] private GameObject bonus3Object;

    [Header("Bonus Pop-Ups")]
    [SerializeField] private GameObject bonus1Popup;
    [SerializeField] private GameObject bonus2Popup;
    [SerializeField] private GameObject bonus3Popup;
    [SerializeField] private RectTransform bonusesPanel;

    [Header("Animation Settings")]
    [SerializeField] private float diceRollDuration = 2f;
    [SerializeField] private float resultHighlightDuration = 0.5f;
    [SerializeField] private float bonusPanelAnimationDuration = 0.3f;
    [SerializeField] private float buttonPulseDuration = 0.8f;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI throwButtonText; // Texto "Click To Throw"
    [SerializeField] private CanvasGroup diceResultGroup;

    // Variables para el sistema de dados
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

    // Tweens
    private Sequence diceTweener;
    private Tween throwButtonTextTween; // Modificado para animar solo el texto
    private Tween bonusesPanelTween;

    private void Start()
    {
        diceInferface.SetActive(false);

        // Inicializar componentes si no están asignados
        if (diceTransform == null && diceObject != null)
            diceTransform = diceObject.transform;

        if (diceResultGroup == null && diceResultText != null)
            diceResultGroup = diceResultText.GetComponent<CanvasGroup>()
                ?? diceResultText.gameObject.AddComponent<CanvasGroup>();

        if (diceModelTransform != null)
        {
            initialDiceModelRotation = diceModelTransform.localRotation;
        }

        ResetUI();
        InitializeAnimations();
    }

    private void OnEnable()
    {
        // Iniciar animaciones cuando la interfaz se active
        if (throwButtonTextTween != null) throwButtonTextTween.Play();
    }

    private void OnDisable()
    {
        // Pausar animaciones cuando la interfaz se desactive
        if (throwButtonTextTween != null) throwButtonTextTween.Pause();
    }

    private void Update()
    {
        bonus1Object.SetActive(bonus1Activated);
        bonus2Object.SetActive(bonus2Activated);
        bonus3Object.SetActive(bonus3Activated);

        // Si algún bonus cambia de estado, animarlo
        if (bonus1Object.activeSelf != bonus1Activated)
            AnimateBonusActivation(bonus1Object, bonus1Activated);

        if (bonus2Object.activeSelf != bonus2Activated)
            AnimateBonusActivation(bonus2Object, bonus2Activated);

        if (bonus3Object.activeSelf != bonus3Activated)
            AnimateBonusActivation(bonus3Object, bonus3Activated);
    }

    #region Core Functionality

    public void SetDifficultyClass(int difficultyClass)
    {
        currentDifficultyClass = difficultyClass;

        // Animar el cambio de texto con DOTween
        difficultyClassText.transform.DOScale(1.2f, 0.2f).OnComplete(() => {
            difficultyClassText.text = difficultyClass.ToString();
            difficultyClassText.transform.DOScale(1f, 0.2f);
        });

        // Mostrar el botón de lanzamiento sin animación
        ShowRollButton();
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
        // Detener cualquier animación previa
        if (diceTweener != null) diceTweener.Kill();

        canRoll = false;

        // Ocultar el botón sin animación
        HideRollButton();

        // Iniciar la secuencia de animación del dado
        StartDiceRollAnimation(difficultyClass);
    }

    public void Continue()
    {
        // Simplemente desactivar la interfaz sin animación
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

        // Asegurar que los tweens se detengan
        DOTween.Kill(diceTransform);
        DOTween.Kill(diceModelTransform); 
        DOTween.Kill(diceResultText.transform);

        // Restablecer la rotación inicial si es necesario
        if (diceModelTransform != null)
        {
            diceModelTransform.localRotation = initialDiceModelRotation;
        }
    }

    #endregion

    #region Animation Methods

    private void InitializeAnimations()
    {
        // Animar SOLO el texto del botón "Click To Throw" con un efecto pulsante
        if (throwButtonText != null)
        {
            throwButtonTextTween = throwButtonText.transform.DOScale(1.1f, buttonPulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        // Inicializar el panel de bonuses
        if (bonusesPanel != null)
        {
            // Guardar la posición original y ocultar inicialmente
            Vector2 originalPos = bonusesPanel.anchoredPosition;
            bonusesPanel.anchoredPosition = new Vector2(originalPos.x, originalPos.y - 100);

            // Animar la entrada con un rebote
            bonusesPanelTween = bonusesPanel.DOAnchorPosY(originalPos.y, bonusPanelAnimationDuration)
                .SetEase(Ease.OutBack)
                .SetAutoKill(false)
                .Pause(); // Inicialmente pausado
        }
    }

    private void StartDiceRollAnimation(int difficultyClass)
    {
        // Crear una secuencia para la animación completa
        diceTweener = DOTween.Sequence();

        // Añadir efecto de "punch" al lanzar el dado (afecta al padre)
        diceTweener.Append(diceTransform.DOPunchScale(new Vector3(0.3f, 0.3f, 0.3f), 0.5f, 5, 0.5f));

        // Rotaciones aleatorias rápidas para simular el lanzamiento
        for (int i = 0; i < 8; i++)
        {
            // Mover el padre (mantiene la funcionalidad de movimiento)
            diceTweener.Append(diceTransform.DOMove(
                diceTransform.position + new Vector3(
                    UnityEngine.Random.Range(-0.1f, 0.1f),
                    UnityEngine.Random.Range(-0.1f, 0.1f),
                    UnityEngine.Random.Range(-0.1f, 0.1f)
                ), 0.15f).SetEase(Ease.Linear));

            // Rotar solo el modelo del dado
            diceTweener.Join(diceModelTransform.DOLocalRotate(new Vector3(
                UnityEngine.Random.Range(0, 360),
                UnityEngine.Random.Range(0, 360),
                UnityEngine.Random.Range(0, 360)
            ), 0.15f, RotateMode.FastBeyond360).SetEase(Ease.Linear));

            // Cambiar el número mostrado durante la rotación
            int randomValue = UnityEngine.Random.Range(1, 21);
            diceTweener.Join(DOTween.To(
                () => int.Parse(diceResultText.text == "" ? "1" : diceResultText.text),
                (x) => diceResultText.text = x.ToString(),
                randomValue, 0.15f
            ));
        }

        // Volver a la posición original con rebote (para el padre)
        diceTweener.Append(diceTransform.DOMove(
            diceTransform.position, 0.5f).SetEase(Ease.OutBack));

        // Volver a la rotación inicial con rebote (para el modelo)
        diceTweener.Join(diceModelTransform.DOLocalRotateQuaternion(
            initialDiceModelRotation, 0.5f).SetEase(Ease.OutBack));

        // Generar y mostrar el resultado real del dado (simplificado)
        diceTweener.AppendCallback(() => {
            baseRoll = UnityEngine.Random.Range(1, 21);
            totalRoll = baseRoll;

            // Mostrar directamente el número resultante sin animaciones adicionales
            diceResultText.text = baseRoll.ToString();
        });

        // Pausa para apreciar el resultado base
        diceTweener.AppendInterval(1f);

        // Aplicar bonuses de manera simplificada
        diceTweener.AppendCallback(() => {
            // Aplicar cada bonus activo de forma más directa
            if (bonus1Activated)
            {
                totalRoll += bonus1;
                ShowBonusPopup(bonus1Popup, bonus1);
            }

            if (bonus2Activated)
            {
                totalRoll += bonus2;
                ShowBonusPopup(bonus2Popup, bonus2);
            }

            if (bonus3Activated)
            {
                totalRoll += bonus3;
                ShowBonusPopup(bonus3Popup, bonus3);
            }

            // Actualizar el texto al valor total (sin animación)
            diceResultText.text = totalRoll.ToString();
        });

        // Comprobar el resultado final
        diceTweener.AppendInterval(0.5f);
        diceTweener.AppendCallback(() => {
            bool isSuccess = totalRoll >= difficultyClass;

            // Mostrar popup de éxito o fracaso
            ShowResultIndicator(isSuccess);

            // Invocar el callback de resultado
            OnRollComplete?.Invoke(isSuccess);
        });

        // Activar el botón de continuar
        diceTweener.AppendInterval(1f);
        diceTweener.AppendCallback(() => {
            ShowContinueButton();
            canRoll = true;
        });

        // Iniciar la secuencia
        diceTweener.Play();
    }

    /* private void HighlightDiceResult(int result)
     {
         // Guardar el color original
         Color originalColor = diceResultText.color;

         // Crear secuencia para destacar el resultado
         Sequence highlightSequence = DOTween.Sequence();

         // Actualizar el texto
         diceResultText.text = result.ToString();

         // Hacer que el texto crezca y cambie a un color llamativo
         highlightSequence.Append(diceResultText.transform.DOScale(1.5f, resultHighlightDuration).SetEase(Ease.OutBack));
         highlightSequence.Join(diceResultText.DOColor(Color.yellow, resultHighlightDuration));

         // Volver al tamaño original manteniendo el color
         highlightSequence.Append(diceResultText.transform.DOScale(1f, resultHighlightDuration / 2).SetEase(Ease.InOutQuad));

         // Hacer parpadear el texto para mayor énfasis - Modificar esto para asegurar visibilidad final
         highlightSequence.Append(diceResultText.DOColor(originalColor, resultHighlightDuration / 2).SetLoops(2, LoopType.Yoyo));

         // Asegurarse de que el texto termine con su color original y visible
         highlightSequence.OnComplete(() => {
             diceResultText.color = originalColor;
             if (diceResultGroup != null) diceResultGroup.alpha = 1f;
         });
     }
 */
    private void ApplyBonuses()
    {
        Sequence bonusSequence = DOTween.Sequence();

        // Aplicar cada bonus activo con una animación
        if (bonus1Activated)
        {
            bonusSequence.AppendCallback(() => {
                totalRoll += bonus1;
                ShowBonusPopup(bonus1Popup, bonus1);
            });
            bonusSequence.AppendInterval(0.7f);
        }

        if (bonus2Activated)
        {
            bonusSequence.AppendCallback(() => {
                totalRoll += bonus2;
                ShowBonusPopup(bonus2Popup, bonus2);
            });
            bonusSequence.AppendInterval(0.7f);
        }

        if (bonus3Activated)
        {
            bonusSequence.AppendCallback(() => {
                totalRoll += bonus3;
                ShowBonusPopup(bonus3Popup, bonus3);
            });
            bonusSequence.AppendInterval(0.7f);
        }

        // Actualizar el texto del resultado con el total
        bonusSequence.AppendCallback(() => {
            // Animar el cambio al resultado total
            DOTween.To(
                () => int.Parse(diceResultText.text),
                (x) => diceResultText.text = x.ToString(),
                totalRoll, 0.5f
            ).SetEase(Ease.OutQuad);
        });
    }

    private void ShowBonusPopup(GameObject popup, int bonusValue)
    {
        // Configurar el popup
        popup.SetActive(true);
        popup.transform.localScale = Vector3.zero;

        // Texto del bonus
        TMP_Text bonusText = popup.GetComponentInChildren<TMP_Text>();
        if (bonusText != null)
            bonusText.text = "+" + bonusValue;

        // Animar la aparición de forma simple
        popup.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack)
            .OnComplete(() => {
                // Hacer desaparecer después de un breve tiempo
                DOVirtual.DelayedCall(0.7f, () => {
                    popup.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack)
                        .OnComplete(() => popup.SetActive(false));
                });
            });
    }

    private void ShowResultIndicator(bool isSuccess)
    {
        GameObject indicator = isSuccess ? successObject : failObject;

        // Configurar el indicador
        indicator.SetActive(true);
        indicator.transform.localScale = Vector3.zero;

        // Animar la aparición
        Sequence indicatorSequence = DOTween.Sequence();

        // Aparecer con efecto de rebote
        indicatorSequence.Append(indicator.transform.DOScale(1.3f, 0.4f).SetEase(Ease.OutBack));
        indicatorSequence.Append(indicator.transform.DOScale(1f, 0.2f));

        // Añadir efecto de brillo o parpadeo
        if (indicator.GetComponent<Image>() != null)
        {
            Image indicatorImage = indicator.GetComponent<Image>();
            Color originalColor = indicatorImage.color;
            Color glowColor = isSuccess ? new Color(0.5f, 1f, 0.5f, 1f) : new Color(1f, 0.5f, 0.5f, 1f);

            indicatorSequence.Append(indicatorImage.DOColor(glowColor, 0.3f).SetLoops(3, LoopType.Yoyo));
            indicatorSequence.Append(indicatorImage.DOColor(originalColor, 0.3f));
        }

        // Hacer parpadear el popup de fallo si es necesario
        if (!isSuccess)
        {
            failPopup.SetActive(true);
            failPopup.transform.localScale = Vector3.zero;

            indicatorSequence.Append(failPopup.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
        }
    }

    private void AnimateBonusActivation(GameObject bonusObject, bool activate)
    {
        if (activate)
        {
            // Efecto de activación
            bonusObject.transform.DOPunchScale(new Vector3(0.3f, 0.3f, 0.3f), 0.5f, 5, 0.5f);

            // Si tiene imagen, hacer parpadear con color
            Image bonusImage = bonusObject.GetComponent<Image>();
            if (bonusImage != null)
            {
                Color originalColor = bonusImage.color;

                // Secuencia de color
                Sequence colorSequence = DOTween.Sequence();
                colorSequence.Append(bonusImage.DOColor(Color.yellow, 0.2f));
                colorSequence.Append(bonusImage.DOColor(originalColor, 0.3f));
                colorSequence.SetLoops(2);
            }
        }
    }

    private void ShowRollButton()
    {
        // Simplemente activar el botón sin animación
        rollButton.SetActive(true);
    }

    private void HideRollButton()
    {
        // Simplemente desactivar el botón sin animación
        rollButton.SetActive(false);
    }

    private void ShowContinueButton()
    {
        // Simplemente activar el botón sin animación
        continueButton.SetActive(true);
    }

    private void ToggleBonusesPanel(bool show)
    {
        if (bonusesPanel != null && bonusesPanelTween != null)
        {
            // Detener cualquier animación en curso
            bonusesPanelTween.Pause();

            // Configurar la dirección
            Vector2 originalPos = bonusesPanel.anchoredPosition;
            float startY = show ? originalPos.y - 100 : originalPos.y;
            float endY = show ? originalPos.y : originalPos.y - 100;

            // Crear nueva animación
            bonusesPanelTween = bonusesPanel.DOAnchorPosY(endY, bonusPanelAnimationDuration)
                .SetEase(show ? Ease.OutBack : Ease.InBack);

            bonusesPanelTween.Play();
        }
    }

    #endregion
}