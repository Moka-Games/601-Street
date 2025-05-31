using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Dice Manager mejorado con feedback visual para bonuses y display de dificultad
/// Incluye sistema para mantener textos sin rotación mientras siguen al dado
/// </summary>
public class Dice_Manager : MonoBehaviour
{
    [Header("Referencias de la Interfaz")]
    [SerializeField] private TMP_Text diceResultText;
    [SerializeField] private TMP_Text bonusText; // El componente "Bonus" de la captura
    [SerializeField] private TMP_Text difficultyText; // NUEVO: Texto para mostrar la dificultad
    [SerializeField] private Button throwDiceButton;
    [SerializeField] private GameObject diceObject; // El dado 3D (contenedor principal)

    // NUEVO: Referencia al modelo del dado que será el que rote
    [SerializeField] private Transform diceModelTransform; // El modelo 3D del dado (hijo del diceObject)

    [Header("Configuración de Animaciones")]
    [SerializeField] private float diceRollDuration = 2f;
    [SerializeField] private float bonusDisplayDelay = 0.5f;
    [SerializeField] private float bonusDisplayDuration = 1f;
    [SerializeField] private float finalResultDelay = 0.5f;

    [Header("Configuración de Movimiento del Dado")]
    [SerializeField] private Vector2 minBounds = new Vector2(-180f, -133f); // Límites mínimos X, Y
    [SerializeField] private Vector2 maxBounds = new Vector2(180f, 150f);   // Límites máximos X, Y
    [SerializeField] private float diceMovementSpeed = 0.15f; // Velocidad de cada movimiento

    [Header("Configuración Visual del Bonus")]
    [SerializeField] private Color bonusColor = Color.green;
    [SerializeField] private float bonusScaleAnimation = 1.2f;
    [SerializeField] private Ease bonusAnimationEase = Ease.OutBack;

    [Header("Configuración Visual de Dificultad")]
    [SerializeField] private Color difficultyTextColor = Color.white;
    [SerializeField] private string difficultyTextPrefix = "DC: "; // Prefijo para el texto de dificultad

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

    // NUEVO: Guardar rotación y posición inicial
    private Quaternion initialDiceModelRotation;
    private Vector3 initialDicePosition;

    // Callbacks
    public System.Action<bool> OnRollComplete;

    private void Start()
    {
        // Buscar referencias
        bonusManager = BonusManager.Instance;

        // NUEVO: Si no se asignó el modelo del dado, intentar encontrarlo
        if (diceModelTransform == null && diceObject != null)
        {
            // Buscar el hijo que contenga el modelo 3D del dado
            // Asumiendo que el primer hijo es el modelo
            if (diceObject.transform.childCount > 0)
            {
                // Buscar un hijo que NO sea un texto
                for (int i = 0; i < diceObject.transform.childCount; i++)
                {
                    Transform child = diceObject.transform.GetChild(i);
                    if (child.GetComponent<TMP_Text>() == null && child.GetComponent<Text>() == null)
                    {
                        diceModelTransform = child;
                        Debug.Log($"Modelo del dado encontrado automáticamente: {diceModelTransform.name}");
                        break;
                    }
                }
            }
        }

        // NUEVO: Si no se asignó el texto de dificultad, intentar encontrarlo
        if (difficultyText == null)
        {
            // Buscar por nombre común
            GameObject difficultyObj = GameObject.Find("Difficulty_Text") ??
                                     GameObject.Find("DC_Text") ??
                                     GameObject.Find("Minimum_Number");

            if (difficultyObj != null)
            {
                difficultyText = difficultyObj.GetComponent<TMP_Text>();
                Debug.Log($"Texto de dificultad encontrado automáticamente: {difficultyObj.name}");
            }
        }

        // NUEVO: Guardar la rotación inicial del modelo y posición del contenedor
        if (diceModelTransform != null)
        {
            initialDiceModelRotation = diceModelTransform.localRotation;
        }

        if (diceObject != null)
        {
            initialDicePosition = diceObject.transform.localPosition;
        }

        // Configurar interfaz inicial
        SetupInitialUI();

        // Configurar botón
        if (throwDiceButton != null)
        {
            throwDiceButton.onClick.AddListener(ThrowDice);
        }

        Debug.Log("Dice_Manager inicializado con sistema de feedback de bonus, rotación independiente y display de dificultad");
    }

    private void SetupInitialUI()
    {
        // Ocultar texto de bonus inicialmente
        if (bonusText != null)
        {
            bonusText.gameObject.SetActive(false);
            bonusText.color = bonusColor;
        }

        // Configurar texto del resultado
        if (diceResultText != null)
        {
            diceResultText.text = "0";
        }

        // NUEVO: Configurar texto de dificultad
        if (difficultyText != null)
        {
            difficultyText.color = difficultyTextColor;
            UpdateDifficultyDisplay();
        }
    }

    /// <summary>
    /// NUEVO: Actualiza el display de dificultad en la interfaz
    /// </summary>
    private void UpdateDifficultyDisplay()
    {
        if (difficultyText != null)
        {
            difficultyText.text = $"{difficultyTextPrefix}{difficultyClass}";
            Debug.Log($"Dificultad actualizada en UI: {difficultyTextPrefix}{difficultyClass}");
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
        Debug.Log($"Dificultad objetivo: {difficultyClass}"); // NUEVO: Log de la dificultad

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
    /// MODIFICADO: Mueve el contenedor dentro de los límites y rota solo el modelo
    /// </summary>
    private IEnumerator AnimateDiceRoll()
    {
        Debug.Log("Animando tirada del dado...");

        if (diceModelTransform != null && diceObject != null)
        {
            // Crear una secuencia para coordinar movimiento y rotación
            Sequence diceSequence = DOTween.Sequence();

            // Añadir efecto de "punch" al lanzar el dado
            diceSequence.Append(diceObject.transform.DOPunchScale(new Vector3(0.3f, 0.3f, 0.3f), 0.5f, 5, 0.5f));

            // Variables para las rotaciones acumuladas
            float totalDuration = 0f;
            int rotationSteps = 8;

            // Animación de rotación y movimiento
            for (int i = 0; i < rotationSteps; i++)
            {
                // Generar posición aleatoria dentro de los límites
                float randomX = Random.Range(minBounds.x, maxBounds.x);
                float randomY = Random.Range(minBounds.y, maxBounds.y);

                // Mantener la Z actual (no modificar la profundidad)
                Vector3 newPosition = new Vector3(randomX, randomY, initialDicePosition.z);

                // Mover el contenedor principal dentro de los límites de la interfaz
                diceSequence.Append(diceObject.transform.DOLocalMove(
                    newPosition,
                    diceMovementSpeed
                ).SetEase(Ease.Linear));

                // Rotar SOLO el modelo del dado
                diceSequence.Join(diceModelTransform.DOLocalRotate(new Vector3(
                    Random.Range(0, 360),
                    Random.Range(0, 360),
                    Random.Range(0, 360)
                ), diceMovementSpeed, RotateMode.LocalAxisAdd).SetEase(Ease.Linear));

                totalDuration += diceMovementSpeed;
            }

            // Volver a la posición Y rotación inicial
            // Primero volver a la posición inicial con animación suave
            diceSequence.Append(diceObject.transform.DOLocalMove(
                initialDicePosition, 0.5f).SetEase(Ease.OutBack));

            // IMPORTANTE: Volver a la rotación inicial exacta para que el dado mire al jugador
            diceSequence.Join(diceModelTransform.DOLocalRotateQuaternion(
                initialDiceModelRotation, 0.5f).SetEase(Ease.OutBack));

            // Ejecutar la secuencia
            diceSequence.Play();

            // Mientras tanto, mostrar números cambiantes
            float elapsedTime = 0f;
            while (elapsedTime < totalDuration + 0.5f) // Incluir el tiempo de vuelta
            {
                if (diceResultText != null)
                {
                    int randomNumber = Random.Range(1, 21);
                    diceResultText.text = randomNumber.ToString();
                }

                elapsedTime += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }

            // Esperar a que termine completamente la animación
            yield return diceSequence.WaitForCompletion();
        }
        else
        {
            // Fallback si no hay referencias correctas
            Debug.LogWarning("Referencias de dado no configuradas correctamente");
            yield return new WaitForSeconds(diceRollDuration);
        }

        yield return new WaitForSeconds(0.2f);

        Debug.Log($"Animación del dado completada - Dado en posición inicial ({initialDicePosition.x}, {initialDicePosition.y})");
    }

    /// <summary>
    /// Muestra el resultado base del dado
    /// </summary>
    private void ShowBaseResult()
    {
        Debug.Log($"Mostrando resultado base: {baseResult}");

        if (diceResultText != null)
        {
            diceResultText.text = baseResult.ToString();

            // Animación de énfasis en el resultado
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

        // PASO 2: Animar aparición del bonus
        // El texto NO rotará porque es hijo del contenedor principal, no del modelo
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
    /// Establece la clase de dificultad para la tirada y actualiza la UI
    /// MODIFICADO: Ahora actualiza también el display visual
    /// </summary>
    public void SetDifficultyClass(int dc)
    {
        difficultyClass = dc;
        UpdateDifficultyDisplay(); // NUEVO: Actualizar display cuando cambie la DC
        Debug.Log($"Clase de dificultad establecida: {dc}");
    }

    /// <summary>
    /// NUEVO: Obtiene la dificultad actual
    /// </summary>
    public int GetDifficultyClass()
    {
        return difficultyClass;
    }

    /// <summary>
    /// NUEVO: Configura el prefijo del texto de dificultad
    /// </summary>
    public void SetDifficultyTextPrefix(string prefix)
    {
        difficultyTextPrefix = prefix;
        UpdateDifficultyDisplay();
        Debug.Log($"Prefijo de dificultad cambiado a: {prefix}");
    }

    /// <summary>
    /// NUEVO: Configura el color del texto de dificultad
    /// </summary>
    public void SetDifficultyTextColor(Color color)
    {
        difficultyTextColor = color;
        if (difficultyText != null)
        {
            difficultyText.color = color;
        }
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
    /// MODIFICADO: Ahora también resetea el display de dificultad
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

        // NUEVO: Resetear el display de dificultad
        UpdateDifficultyDisplay();

        // NUEVO: Resetear la rotación del modelo del dado y posición del contenedor
        if (diceModelTransform != null)
        {
            diceModelTransform.localRotation = initialDiceModelRotation;
        }

        if (diceObject != null)
        {
            diceObject.transform.localPosition = initialDicePosition;
        }

        // Detener animaciones en progreso
        DOTween.Kill(diceResultText?.transform);
        DOTween.Kill(bonusText?.transform);
        DOTween.Kill(diceObject?.transform);
        DOTween.Kill(diceModelTransform); // NUEVO: Detener animaciones del modelo

        isRolling = false;
        isWaitingForDialogueContinuation = false;

        Debug.Log("Interfaz del dado reseteada con display de dificultad actualizado");
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

    [ContextMenu("Test Different Difficulties")]
    public void TestDifferentDifficulties()
    {
        if (Application.isPlaying)
        {
            // Probar diferentes dificultades
            int[] testDCs = { 5, 10, 15, 20 };
            int randomDC = testDCs[Random.Range(0, testDCs.Length)];
            SetDifficultyClass(randomDC);
            Debug.Log($"Dificultad de prueba establecida: {randomDC}");
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
        Debug.Log($"DiceModelTransform asignado: {diceModelTransform != null}");
        Debug.Log($"DifficultyText asignado: {difficultyText != null}"); // NUEVO
        Debug.Log($"Límites de movimiento: X({minBounds.x}, {maxBounds.x}), Y({minBounds.y}, {maxBounds.y})");

        if (bonusManager != null)
        {
            Debug.Log($"Bonus activo: {bonusManager.HasActiveBBonus()}");
            if (bonusManager.HasActiveBBonus())
            {
                Debug.Log($"Valor del bonus activo: {bonusManager.GetActiveBonusValue()}");
                Debug.Log($"Nombre del bonus activo: {bonusManager.GetActiveBonusName()}");
            }
        }

        // NUEVO: Debug específico para el display de dificultad
        if (difficultyText != null)
        {
            Debug.Log($"Texto de dificultad actual: '{difficultyText.text}'");
            Debug.Log($"Prefijo configurado: '{difficultyTextPrefix}'");
        }
        else
        {
            Debug.LogWarning("difficultyText no está asignado - El display de dificultad no funcionará");
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
        DOTween.Kill(diceModelTransform); // NUEVO: Limpiar tweens del modelo
    }
}