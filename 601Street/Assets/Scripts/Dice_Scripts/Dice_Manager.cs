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
    [SerializeField] private CanvasGroup diceResultGroup;

    [Header("Navigation Integration")]
    [SerializeField] private UINavigationManager navigationManager;

    [Header("Bonus System Integration")]
    [SerializeField] private BonusManager bonusManager;

    [Header("Input Protection")]
    [SerializeField] private float inputProtectionDelay = 0.8f; // Delay antes de permitir inputs
    private bool isInputProtected = true;
    private Coroutine inputProtectionCoroutine;

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
    private bool hasRolledInCurrentSession = false; // Nuevo: Previene tiradas múltiples
    private int currentDifficultyClass;

    // Referencias a los componentes Button para manejo directo
    private Button rollButtonComponent;
    private Button continueButtonComponent;

    public Action<bool> OnRollComplete;

    // Tweens
    private Sequence diceTweener;
    private Tween throwButtonTextTween;
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

        // Obtener referencias a los componentes Button
        if (rollButton != null)
            rollButtonComponent = rollButton.GetComponent<Button>();

        if (continueButton != null)
            continueButtonComponent = continueButton.GetComponent<Button>();

        // Buscar UINavigationManager si no está asignado
        if (navigationManager == null)
            navigationManager = FindAnyObjectByType<UINavigationManager>();

        // Buscar BonusManager si no está asignado
        if (bonusManager == null)
            bonusManager = BonusManager.Instance;

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

        // PROTECCIÓN CRÍTICA: Iniciar protección de input
        StartInputProtection();

        // Animar el cambio de texto con DOTween
        difficultyClassText.transform.DOScale(1.2f, 0.2f).OnComplete(() => {
            difficultyClassText.text = difficultyClass.ToString();
            difficultyClassText.transform.DOScale(1f, 0.2f);
        });

        // Mostrar el botón de lanzamiento y habilitarlo
        ShowRollButton();
    }

    public void OnRollButtonClicked()
    {
        // PROTECCIÓN CRÍTICA: Verificar si los inputs están protegidos
        if (isInputProtected)
        {
            Debug.LogWarning("Input protegido: No se puede tirar el dado todavía");
            return;
        }

        // VERIFICACIÓN MEJORADA: Múltiples comprobaciones de seguridad
        if (!canRoll)
        {
            Debug.LogWarning("No se puede tirar el dado: canRoll = false");
            return;
        }

        if (hasRolledInCurrentSession)
        {
            Debug.LogWarning("Ya se ha tirado el dado en esta sesión");
            return;
        }

        if (rollButtonComponent != null && !rollButtonComponent.interactable)
        {
            Debug.LogWarning("El botón de tirar dado no está interactuable");
            return;
        }

        Debug.Log("Iniciando tirada de dado - Input válido confirmado");
        RollDice(currentDifficultyClass);
    }

    private void RollDice(int difficultyClass)
    {
        // Detener cualquier animación previa
        if (diceTweener != null) diceTweener.Kill();

        // BLOQUEO INMEDIATO para prevenir tiradas múltiples
        canRoll = false;
        hasRolledInCurrentSession = true;

        // Notificar al BonusManager que se inició una tirada
        if (bonusManager != null)
        {
            bonusManager.OnDiceRollStarted();
        }

        // Deshabilitar completamente el botón de lanzamiento
        DisableRollButton();

        // Iniciar la secuencia de animación del dado
        StartDiceRollAnimation(difficultyClass);
    }

    public void Continue()
    {
        Debug.Log("Continuando desde Dice Manager");

        // Simplemente desactivar la interfaz
        diceInferface.SetActive(false);

        // Resetear UI para la próxima vez
        ResetUI();
    }

    public void ResetUI()
    {
        Debug.Log("Reseteando UI del Dice Manager");

        // Detener protección de input
        StopInputProtection();

        // Resetear textos
        diceResultText.text = "";
        difficultyClassText.text = "";

        // Resetear estado de tirada
        canRoll = true;
        hasRolledInCurrentSession = false;

        // Ocultar elementos
        failPopup.SetActive(false);
        continueButton.SetActive(false);
        rollButton.SetActive(false);
        successObject.SetActive(false);
        failObject.SetActive(false);

        // Ocultar popups de bonus
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

        // Habilitar botón de tirada para el próximo uso
        if (rollButtonComponent != null)
        {
            rollButtonComponent.interactable = true;
        }
    }

    #endregion

    #region Button Management

    #region Input Protection

    private void StartInputProtection()
    {
        Debug.Log($"Iniciando protección de input por {inputProtectionDelay} segundos");

        // Detener cualquier protección anterior
        if (inputProtectionCoroutine != null)
        {
            StopCoroutine(inputProtectionCoroutine);
        }

        isInputProtected = true;

        // Bloquear también el sistema de navegación temporalmente
        if (navigationManager != null)
        {
            navigationManager.BlockInputTemporarily(inputProtectionDelay);
        }

        inputProtectionCoroutine = StartCoroutine(InputProtectionCoroutine());
    }

    private IEnumerator InputProtectionCoroutine()
    {
        yield return new WaitForSecondsRealtime(inputProtectionDelay);

        isInputProtected = false;
        Debug.Log("Protección de input liberada - Ahora se puede tirar el dado");

        inputProtectionCoroutine = null;
    }

    private void StopInputProtection()
    {
        if (inputProtectionCoroutine != null)
        {
            StopCoroutine(inputProtectionCoroutine);
            inputProtectionCoroutine = null;
        }

        isInputProtected = false;
        Debug.Log("Protección de input detenida manualmente");
    }

    #endregion

    private void ShowRollButton()
    {
        Debug.Log("Mostrando botón de tirada");

        rollButton.SetActive(true);

        // Asegurar que el botón esté habilitado
        if (rollButtonComponent != null)
        {
            rollButtonComponent.interactable = true;
        }

        // Notificar al sistema de navegación para que actualice
        if (navigationManager != null)
        {
            navigationManager.ForceAutoSelectionCheck();
        }
    }

    private void DisableRollButton()
    {
        Debug.Log("Deshabilitando botón de tirada");

        // Deshabilitar el botón pero mantenerlo visible para debug
        if (rollButtonComponent != null)
        {
            rollButtonComponent.interactable = false;
        }

        // Opcionalmente ocultarlo también
        rollButton.SetActive(false);

        // Notificar al sistema de navegación
        if (navigationManager != null)
        {
            navigationManager.ForceAutoSelectionCheck();
        }
    }

    private void ShowContinueButton()
    {
        Debug.Log("Mostrando botón de continuar");

        continueButton.SetActive(true);

        // Asegurar que el botón esté habilitado
        if (continueButtonComponent != null)
        {
            continueButtonComponent.interactable = true;
        }

        // Notificar al sistema de navegación para selección automática
        if (navigationManager != null)
        {
            // Pequeño delay para asegurar que el botón esté completamente activo
            StartCoroutine(DelayedAutoSelectionNotification());
        }
    }

    private IEnumerator DelayedAutoSelectionNotification()
    {
        yield return null; // Esperar un frame
        if (navigationManager != null)
        {
            navigationManager.ForceAutoSelectionCheck();
        }
    }

    #endregion

    #region Animation Methods

    private void InitializeAnimations()
    {
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
        Debug.Log("Iniciando animación de tirada de dado");

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

        // Generar y mostrar el resultado real del dado
        diceTweener.AppendCallback(() => {
            baseRoll = UnityEngine.Random.Range(1, 21);
            totalRoll = baseRoll;
            Debug.Log($"Resultado base del dado: {baseRoll}");

            // Mostrar directamente el número resultante
            diceResultText.text = baseRoll.ToString();
        });

        // Pausa para apreciar el resultado base
        diceTweener.AppendInterval(1f);

        // Aplicar bonuses de manera directa
        diceTweener.AppendCallback(() => {
            ApplyBonuses();
        });

        // Comprobar el resultado final
        diceTweener.AppendInterval(0.5f);
        diceTweener.AppendCallback(() => {
            bool isSuccess = totalRoll >= difficultyClass;
            Debug.Log($"Resultado final: {totalRoll} vs DC {difficultyClass} = {(isSuccess ? "Éxito" : "Fallo")}");

            // Mostrar popup de éxito o fracaso
            ShowResultIndicator(isSuccess);

            // Invocar el callback de resultado
            OnRollComplete?.Invoke(isSuccess);
        });

        // Activar el botón de continuar después de un breve delay
        diceTweener.AppendInterval(1f);
        diceTweener.AppendCallback(() => {
            ShowContinueButton();

            // Notificar al BonusManager que la tirada ha terminado
            if (bonusManager != null)
            {
                bonusManager.OnDiceRollCompleted();
            }
        });

        // Iniciar la secuencia
        diceTweener.Play();
    }

    private void ApplyBonuses()
    {
        Debug.Log("Aplicando bonuses - Nuevo Sistema");

        // NUEVO SISTEMA: Usar BonusManager en lugar del sistema anterior
        if (bonusManager != null && bonusManager.HasActiveBBonus())
        {
            int bonusValue = bonusManager.GetActiveBonusValue();
            string bonusName = bonusManager.GetActiveBonusName();

            totalRoll += bonusValue;
            Debug.Log($"Bonus aplicado - {bonusName}: +{bonusValue}");

            // Mostrar popup del bonus aplicado
            ShowNewBonusPopup(bonusName, bonusValue);
        }
        else
        {
            // SISTEMA ANTERIOR (Fallback): Mantener compatibilidad
            Debug.Log("Usando sistema de bonuses anterior (fallback)");

            if (bonus1Activated)
            {
                totalRoll += bonus1;
                ShowBonusPopup(bonus1Popup, bonus1);
                Debug.Log($"Bonus 1 aplicado: +{bonus1}");
            }

            if (bonus2Activated)
            {
                totalRoll += bonus2;
                ShowBonusPopup(bonus2Popup, bonus2);
                Debug.Log($"Bonus 2 aplicado: +{bonus2}");
            }

            if (bonus3Activated)
            {
                totalRoll += bonus3;
                ShowBonusPopup(bonus3Popup, bonus3);
                Debug.Log($"Bonus 3 aplicado: +{bonus3}");
            }
        }

        // Actualizar el texto al valor total
        diceResultText.text = totalRoll.ToString();
        Debug.Log($"Total final con bonuses: {totalRoll}");
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

    /// <summary>
    /// Muestra popup para el nuevo sistema de bonuses de forma más visible
    /// </summary>
    private void ShowNewBonusPopup(string bonusName, int bonusValue)
    {
        Debug.Log($"Mostrando popup para bonus: {bonusName} (+{bonusValue})");

        // Crear un popup temporal para mostrar el bonus
        GameObject bonusVisual = new GameObject("TempBonusPopup");
        bonusVisual.transform.SetParent(diceResultText.transform.parent, false);

        // Añadir componente Canvas Group para animaciones
        CanvasGroup canvasGroup = bonusVisual.AddComponent<CanvasGroup>();

        // Crear texto para mostrar el bonus
        GameObject textObj = new GameObject("BonusText");
        textObj.transform.SetParent(bonusVisual.transform, false);

        TMP_Text bonusText = textObj.AddComponent<TMP_Text>();
        bonusText.text = $"+{bonusValue}";
        bonusText.fontSize = 36;
        bonusText.color = Color.green;
        bonusText.fontStyle = FontStyles.Bold;
        bonusText.alignment = TextAlignmentOptions.Center;

        // Configurar RectTransform
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(100, 50);
        textRect.anchoredPosition = new Vector2(80, 0); // Posición al lado del resultado del dado

        // Animación del popup
        Sequence bonusPopupSequence = DOTween.Sequence();

        // Aparecer con escalado
        bonusVisual.transform.localScale = Vector3.zero;
        bonusPopupSequence.Append(bonusVisual.transform.DOScale(1.2f, 0.3f).SetEase(Ease.OutBack));
        bonusPopupSequence.Append(bonusVisual.transform.DOScale(1f, 0.2f));

        // Mantener visible un momento
        bonusPopupSequence.AppendInterval(1.5f);

        // Mover hacia el resultado principal mientras desaparece
        bonusPopupSequence.Append(textRect.DOAnchorPos(Vector2.zero, 0.5f));
        bonusPopupSequence.Join(canvasGroup.DOFade(0, 0.5f));

        // Destruir después de la animación
        bonusPopupSequence.OnComplete(() => {
            if (bonusVisual != null)
            {
                Destroy(bonusVisual);
            }
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

    #endregion

    #region Public Interface

    /// <summary>
    /// Método para integración con sistemas externos (como DialogueManager)
    /// </summary>
    public bool CanRollDice()
    {
        return canRoll && !hasRolledInCurrentSession && rollButtonComponent != null && rollButtonComponent.interactable;
    }

    /// <summary>
    /// Método para forzar reset desde sistemas externos
    /// </summary>
    public void ForceReset()
    {
        Debug.Log("Forzando reset del Dice Manager");

        // Detener todas las animaciones
        diceTweener?.Kill();
        DOTween.Kill(this);

        // Resetear UI
        ResetUI();
    }

    /// <summary>
    /// Método para configurar el navigation manager externamente
    /// </summary>
    public void SetNavigationManager(UINavigationManager navManager)
    {
        navigationManager = navManager;
    }

    #endregion

    #region Debug Methods

    [ContextMenu("Debug Dice State")]
    public void DebugDiceState()
    {
        Debug.Log($"=== Dice Manager State ===");
        Debug.Log($"Can Roll: {canRoll}");
        Debug.Log($"Has Rolled In Session: {hasRolledInCurrentSession}");
        Debug.Log($"Roll Button Active: {rollButton.activeSelf}");
        Debug.Log($"Roll Button Interactable: {rollButtonComponent?.interactable ?? false}");
        Debug.Log($"Continue Button Active: {continueButton.activeSelf}");
        Debug.Log($"Continue Button Interactable: {continueButtonComponent?.interactable ?? false}");
        Debug.Log($"Current DC: {currentDifficultyClass}");
        Debug.Log($"Base Roll: {baseRoll}, Total Roll: {totalRoll}");
    }

    [ContextMenu("Force Reset UI")]
    public void ForceResetFromContext()
    {
        ForceReset();
    }

    #endregion
}