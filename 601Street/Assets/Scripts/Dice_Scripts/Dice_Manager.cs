using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Dice Manager mejorado con feedback visual para bonuses
/// Incluye sistema para mantener textos sin rotación mientras siguen al dado
/// </summary>
public class Dice_Manager : MonoBehaviour
{
    [Header("Referencias de la Interfaz")]
    [SerializeField] private TMP_Text diceResultText;
    [SerializeField] private TMP_Text bonusText; // El componente "Bonus" de la captura
    [SerializeField] private Button throwDiceButton;
    [SerializeField] private GameObject diceObject; // El dado 3D

    [Header("Configuración de Animaciones")]
    [SerializeField] private float diceRollDuration = 2f;
    [SerializeField] private float bonusDisplayDelay = 0.5f;
    [SerializeField] private float bonusDisplayDuration = 1f;
    [SerializeField] private float finalResultDelay = 0.5f;

    [Header("Configuración Visual del Bonus")]
    [SerializeField] private Color bonusColor = Color.green;
    [SerializeField] private float bonusScaleAnimation = 1.2f;
    [SerializeField] private Ease bonusAnimationEase = Ease.OutBack;

    [Header("Sistema Legacy (Compatibilidad)")]
    public bool bonus1Activated = false;
    public bool bonus2Activated = false;
    public bool bonus3Activated = false;

    // Variables de estado
    private int baseResult = 0;
    private int bonusValue = 0;
    private int finalResult = 0;
    private int difficultyClass = 10;
    private bool isRolling = false;

    // Referencias del sistema
    private BonusManager bonusManager;

    // Control de integración con diálogos
    private bool isWaitingForDialogueContinuation = false;

    // Callbacks
    public System.Action<bool> OnRollComplete;

    private void Start()
    {
        // Buscar referencias
        bonusManager = BonusManager.Instance;

        // Configurar interfaz inicial
        SetupInitialUI();

        // Configurar botón
        if (throwDiceButton != null)
        {
            throwDiceButton.onClick.AddListener(ThrowDice);
        }

        Debug.Log("Dice_Manager inicializado con sistema de feedback de bonus");
    }

    private void SetupInitialUI()
    {
        if (bonusText != null)
        {
            bonusText.gameObject.SetActive(false);
            bonusText.color = bonusColor;
        }

        if (diceResultText != null)
        {
            diceResultText.text = "0"; // Cambiado de "00"
        }
    }

    /// <summary>
    /// Método principal para lanzar el dado con feedback de bonus
    /// </summary>
    public void ThrowDice()
    {
        if (isRolling)
        {
            Debug.LogWarning("Ya hay una tirada en progreso");
            return;
        }

        StartCoroutine(DiceRollSequence());
    }

    /// <summary>
    /// Secuencia completa de la tirada con feedback visual
    /// </summary>
    private IEnumerator DiceRollSequence()
    {
        isRolling = true;
        isWaitingForDialogueContinuation = false;

        // Notificar al BonusManager que inicia la tirada
        if (bonusManager != null)
        {
            bonusManager.OnDiceRollStarted();
        }

        // Deshabilitar botón durante la tirada
        if (throwDiceButton != null)
        {
            throwDiceButton.interactable = false;
        }

        Debug.Log("=== INICIANDO SECUENCIA DE TIRADA ===");

        // PASO 1: Obtener valores
        baseResult = Random.Range(1, 21); // D20
        bonusValue = GetCurrentBonusValue();
        finalResult = baseResult + bonusValue;

        Debug.Log($"Resultado base: {baseResult}");
        Debug.Log($"Valor del bonus: {bonusValue}");
        Debug.Log($"Resultado final: {finalResult}");

        // PASO 2: Animación del dado
        yield return StartCoroutine(AnimateDiceRoll());

        // PASO 3: Mostrar resultado base
        ShowBaseResult();
        yield return new WaitForSeconds(bonusDisplayDelay);

        // PASO 4: Mostrar bonus (si existe)
        if (bonusValue > 0)
        {
            yield return StartCoroutine(ShowBonusSequence());
        }

        // PASO 5: Mostrar resultado final
        yield return StartCoroutine(ShowFinalResult());

        // PASO 6: Esperar un momento y luego proceder automáticamente
        yield return new WaitForSeconds(1f);

        // PASO 7: Completar tirada automáticamente
        CompleteDiceRoll();

        Debug.Log("=== SECUENCIA DE TIRADA COMPLETADA ===");
    }

    /// <summary>
    /// Anima la rotación del dado durante la tirada
    /// </summary>
    private IEnumerator AnimateDiceRoll()
    {
        Debug.Log("Animando tirada del dado...");

        if (diceObject != null)
        {
            // Rotación aleatoria del dado (solo el dado, los textos no se ven afectados)
            diceObject.transform.DORotate(
                new Vector3(
                    Random.Range(0, 360) * 3,
                    Random.Range(0, 360) * 3,
                    Random.Range(0, 360) * 3
                ),
                diceRollDuration,
                RotateMode.LocalAxisAdd
            ).SetEase(Ease.OutQuart);
        }

        if (diceResultText != null)
        {
            float elapsedTime = 0f;
            while (elapsedTime < diceRollDuration)
            {
                int randomNumber = Random.Range(1, 21);
                diceResultText.text = randomNumber.ToString(); // Cambiado de ToString("00")

                elapsedTime += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }
        }

        yield return new WaitForSeconds(0.2f);

        Debug.Log("Animación del dado completada");
    }

    private void ShowBaseResult()
    {
        Debug.Log($"Mostrando resultado base: {baseResult}");

        if (diceResultText != null)
        {
            diceResultText.text = baseResult.ToString(); // Cambiado de ToString("00")
            diceResultText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 0.5f);
        }
    }

    /// <summary>
    /// Secuencia para mostrar el bonus aplicado
    /// </summary>
    private IEnumerator ShowBonusSequence()
    {
        Debug.Log("=== MOSTRANDO SECUENCIA DE BONUS ===");

        if (bonusText == null)
        {
            Debug.LogError("bonusText no está asignado - No se puede mostrar feedback de bonus");
            yield break;
        }

        // PASO 1: Configurar y mostrar texto del bonus
        bonusText.text = $"+{bonusValue}";
        bonusText.gameObject.SetActive(true);
        bonusText.transform.localScale = Vector3.zero;

        Debug.Log($"Mostrando bonus: +{bonusValue}");

        // PASO 2: Animar aparición del bonus (respetando rotación fija)
        bonusText.transform.DOScale(bonusScaleAnimation, 0.3f)
            .SetEase(bonusAnimationEase)
            .OnComplete(() => {
                bonusText.transform.DOScale(1f, 0.2f);
            });

        // Efecto de color pulsante
        bonusText.DOColor(Color.white, 0.5f)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);

        // PASO 3: Esperar que el jugador vea el bonus
        yield return new WaitForSeconds(bonusDisplayDuration);

        Debug.Log("Bonus mostrado, aplicando al resultado...");
    }

    /// <summary>
    /// Muestra el resultado final con el bonus aplicado
    /// </summary>
    private IEnumerator ShowFinalResult()
    {
        Debug.Log($"Mostrando resultado final: {baseResult} + {bonusValue} = {finalResult}");

        if (diceResultText != null)
        {
            // Animación de transición del resultado
            diceResultText.transform.DOPunchScale(Vector3.one * 0.3f, 0.4f, 8, 0.7f);

            // Cambiar color temporalmente para indicar que se aplicó el bonus
            if (bonusValue > 0)
            {
                Color originalColor = diceResultText.color;
                diceResultText.DOColor(bonusColor, 0.3f)
                    .OnComplete(() => {
                        diceResultText.DOColor(originalColor, 0.3f);
                    });
            }

            // Actualizar el texto al resultado final
            yield return new WaitForSeconds(0.2f);
            diceResultText.text = finalResult.ToString(); 

            // Animación final de énfasis
            diceResultText.transform.DOPunchScale(Vector3.one * 0.4f, 0.5f, 6, 0.8f);
        }

        // Ocultar texto del bonus después de aplicarlo
        if (bonusText != null && bonusValue > 0)
        {
            yield return new WaitForSeconds(finalResultDelay);

            bonusText.transform.DOScale(0f, 0.3f)
                .SetEase(Ease.InBack)
                .OnComplete(() => {
                    bonusText.gameObject.SetActive(false);
                });
        }

        yield return new WaitForSeconds(0.3f);
    }

    /// <summary>
    /// Finaliza la tirada y ejecuta automáticamente la continuación del diálogo
    /// </summary>
    private void CompleteDiceRoll()
    {
        isRolling = false;
        isWaitingForDialogueContinuation = true;

        // CRÍTICO: Determinar si la tirada fue exitosa usando el resultado FINAL (incluyendo bonus)
        bool isSuccess = finalResult >= difficultyClass;

        Debug.Log($"=== EVALUACIÓN DE TIRADA ===");
        Debug.Log($"Resultado base: {baseResult}");
        Debug.Log($"Bonus aplicado: +{bonusValue}");
        Debug.Log($"Resultado final: {finalResult}");
        Debug.Log($"Clase de dificultad: {difficultyClass}");
        Debug.Log($"¿Tirada exitosa?: {(isSuccess ? "SÍ" : "NO")}");
        Debug.Log("============================");

        // Notificar al BonusManager que terminó la tirada
        if (bonusManager != null)
        {
            bonusManager.OnDiceRollCompleted();
        }

        // CRÍTICO: Ejecutar callback con el resultado basado en el resultado FINAL
        OnRollComplete?.Invoke(isSuccess);

        // NUEVO: Iniciar automáticamente la continuación del diálogo
        StartCoroutine(AutoContinueToDialogue());
    }

    /// <summary>
    /// Continúa automáticamente al diálogo después de completar la tirada
    /// </summary>
    private IEnumerator AutoContinueToDialogue()
    {
        // Esperar un momento para que el jugador vea el resultado final
        yield return new WaitForSeconds(1.5f);

        Debug.Log("=== CONTINUANDO AUTOMÁTICAMENTE AL DIÁLOGO ===");

        // Buscar el DialogueManager y llamar al método de continuación
        DialogueManager dialogueManager = DialogueManager.Instance;
        if (dialogueManager != null)
        {
            // Simular presionar el botón de continuar
            dialogueManager.OnDiceRollCompleteButtonPressed();
            Debug.Log("Continuación del diálogo ejecutada automáticamente");
        }
        else
        {
            Debug.LogError("DialogueManager no encontrado - No se puede continuar automáticamente");
            // Reactivar botón como fallback
            if (throwDiceButton != null)
            {
                throwDiceButton.interactable = true;
            }
        }

        isWaitingForDialogueContinuation = false;
    }

    /// <summary>
    /// Obtiene el valor del bonus actual del BonusManager
    /// </summary>
    private int GetCurrentBonusValue()
    {
        if (bonusManager != null && bonusManager.HasActiveBBonus())
        {
            return bonusManager.GetActiveBonusValue();
        }

        // Fallback al sistema legacy
        if (bonus3Activated) return 3;
        if (bonus2Activated) return 2;
        if (bonus1Activated) return 1;

        return 0;
    }

    #region Métodos Públicos de Configuración

    /// <summary>
    /// Establece la clase de dificultad para la tirada
    /// </summary>
    public void SetDifficultyClass(int dc)
    {
        difficultyClass = dc;
        Debug.Log($"Clase de dificultad establecida: {dc}");
    }

    /// <summary>
    /// Reactiva el botón de tirada (llamado desde DialogueManager)
    /// </summary>
    public void ReactivateDiceButton()
    {
        if (throwDiceButton != null)
        {
            throwDiceButton.interactable = true;
            Debug.Log("Botón de tirada reactivado desde DialogueManager");
        }
    }

    /// <summary>
    /// Resetea la interfaz del dado
    /// </summary>
    public void ResetUI()
    {
        if (diceResultText != null)
        {
            diceResultText.text = "0"; 
            diceResultText.transform.localScale = Vector3.one;
        }

        if (bonusText != null)
        {
            bonusText.gameObject.SetActive(false);
            bonusText.transform.localScale = Vector3.one;
        }

        if (throwDiceButton != null)
        {
            throwDiceButton.interactable = true;
        }

        // Detener animaciones en progreso
        DOTween.Kill(diceResultText?.transform);
        DOTween.Kill(bonusText?.transform);
        DOTween.Kill(diceObject?.transform);

        isRolling = false;
        isWaitingForDialogueContinuation = false;

        Debug.Log("Interfaz del dado reseteada");
    }

    /// <summary>
    /// Obtiene el último resultado de la tirada
    /// </summary>
    public int GetLastResult()
    {
        return finalResult;
    }

    /// <summary>
    /// Obtiene el resultado base (sin bonus) de la última tirada
    /// </summary>
    public int GetLastBaseResult()
    {
        return baseResult;
    }

    /// <summary>
    /// Obtiene el valor del bonus aplicado en la última tirada
    /// </summary>
    public int GetLastBonusValue()
    {
        return bonusValue;
    }

    /// <summary>
    /// Verifica si hay una tirada en progreso
    /// </summary>
    public bool IsRolling()
    {
        return isRolling;
    }

    #endregion

    #region Métodos de Debug

    [ContextMenu("Test Dice Roll")]
    public void TestDiceRoll()
    {
        if (Application.isPlaying)
        {
            ThrowDice();
        }
    }

    [ContextMenu("Test With Bonus")]
    public void TestWithBonus()
    {
        if (Application.isPlaying)
        {
            // Simular bonus para testing
            bonus2Activated = true;
            ThrowDice();
        }
    }

    [ContextMenu("Debug Dice State")]
    public void DebugDiceState()
    {
        Debug.Log("=== ESTADO DEL DICE MANAGER ===");
        Debug.Log($"Último resultado base: {baseResult}");
        Debug.Log($"Último bonus aplicado: {bonusValue}");
        Debug.Log($"Último resultado final: {finalResult}");
        Debug.Log($"Clase de dificultad: {difficultyClass}");
        Debug.Log($"Tirada en progreso: {isRolling}");
        Debug.Log($"Esperando continuación de diálogo: {isWaitingForDialogueContinuation}");
        Debug.Log($"BonusManager encontrado: {bonusManager != null}");

        if (bonusManager != null)
        {
            Debug.Log($"Bonus activo: {bonusManager.HasActiveBBonus()}");
            if (bonusManager.HasActiveBBonus())
            {
                Debug.Log($"Valor del bonus activo: {bonusManager.GetActiveBonusValue()}");
                Debug.Log($"Nombre del bonus activo: {bonusManager.GetActiveBonusName()}");
            }
        }
        Debug.Log("==============================");
    }

    #endregion

    private void OnDestroy()
    {
        // Limpiar tweens al destruir
        DOTween.Kill(diceResultText?.transform);
        DOTween.Kill(bonusText?.transform);
        DOTween.Kill(diceObject?.transform);
    }
}