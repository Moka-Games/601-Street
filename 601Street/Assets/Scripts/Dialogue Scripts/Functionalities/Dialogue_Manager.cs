using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using DG.Tweening;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    public GameObject dialogueUI;
    public TMP_Text speakerNameText;
    public TMP_Text contentText;
    public TypewriterEffect typewriterEffect;
    public GameObject Next_bubble;

    public GameObject optionsUI;
    public Button[] optionButtons;

    private Conversation currentConversation;
    private NPC currentNPC;
    private int currentDialogueIndex = 0;
    public bool isTyping = false;

    public UnityEvent onConversationEnd;

    [Header("Dice Prototype Interface Variables")]
    [SerializeField] private Dice_Manager diceManager;
    [SerializeField] private GameObject diceInterface;
    [SerializeField] private GameObject dialogueInterface;
    public GameObject failObject;
    public GameObject sucessObject;

    private bool? diceRollResult = null;
    private int selectedOptionIndex = -1;

    private Conversation nextContextualConversation;

    // Referencias para el control del jugador y la cámara
    private PlayerController playerController;
    private GameObject npcCamera;

    private bool isInConversation = false;

    // Cooldown entre conversaciones
    private float conversationCooldown = 1.5f;
    private float lastConversationEndTime = 0f;

    // Variables para animación del diálogo
    [Header("Dialog Animation Settings")]
    [SerializeField] private float dialogEntryDuration = 0.3f;
    [SerializeField] private Ease dialogEntryEase = Ease.OutBack;

    [Header("Dialog Exit Settings")]
    [SerializeField] private float dialogExitDuration = 0.3f;
    [SerializeField] private Ease dialogExitEase = Ease.InBack;
    [SerializeField] private float slideDistance = 100f;

    private Vector2 initialDialoguePosition = Vector2.zero;
    private bool initialPositionSaved = false;

    // Input System
    private PlayerControls playerControls;
    private InputAction skipDialogueAction;

    [Header("Navigation Integration")]
    [SerializeField] private UINavigationManager uiNavigationManager;

    [Header("Look At System Integration")]
    [SerializeField] private bool enableLookAtSystem = true; // Activar/desactivar el sistema
    [SerializeField] private float lookAtDelay = 0.2f; // Pequeño delay antes de activar el Look At

    private bool isInPreDiceConversation = false;
    private PendingDiceRollData pendingDiceRoll = null;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Inicializar Input System
        InitializeInputSystem();
    }

    private void InitializeInputSystem()
    {
        playerControls = new PlayerControls();
        skipDialogueAction = playerControls.Gameplay.SkipDialogue;

        // Suscribirse al evento de skip dialogue
        skipDialogueAction.performed += OnSkipDialogueInput;
    }

    private void OnEnable()
    {
        playerControls?.Gameplay.Enable();
    }

    private void OnDisable()
    {
        playerControls?.Gameplay.Disable();
    }

    private void OnDestroy()
    {
        if (skipDialogueAction != null)
        {
            skipDialogueAction.performed -= OnSkipDialogueInput;
        }

        playerControls?.Dispose();
    }

    // Callback para el Input System
    private void OnSkipDialogueInput(InputAction.CallbackContext context)
    {
        // Solo procesar el input si estamos en conversación y el diálogo está activo
        if (dialogueUI != null && dialogueUI.activeSelf && isInConversation)
        {
            if (isTyping)
            {
                if (typewriterEffect != null)
                {
                    typewriterEffect.StopTyping();
                }
                isTyping = false;
            }
            else
            {
                if (Next_bubble != null)
                {
                    Next_bubble.SetActive(false);
                }
                NextDialogue();
            }
        }
    }

    void Start()
    {
        playerController = FindAnyObjectByType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning("No se encontró el PlayerController. No se podrá pausar el movimiento del jugador.");
        }

        failObject.SetActive(false);
        sucessObject.SetActive(false);

        if (dialogueUI != null)
        {
            dialogueUI.SetActive(false);
        }
        if (optionsUI != null)
        {
            optionsUI.SetActive(false);
        }
        if (Next_bubble != null)
        {
            Next_bubble.SetActive(false);
        }

        if (typewriterEffect != null && typewriterEffect.textComponent == null)
        {
            Debug.LogError("TypewriterEffect no tiene asignado textComponent. Asignando contentText por defecto.");
            typewriterEffect.textComponent = contentText;
        }

        if (contentText != null)
        {
            contentText.richText = true;
        }

        if (uiNavigationManager == null)
        {
            uiNavigationManager = FindAnyObjectByType<UINavigationManager>();
            if (uiNavigationManager == null)
            {
                Debug.LogWarning("UINavigationManager no encontrado. Las protecciones anti-doble input no funcionarán.");
            }
        }
    }

    public void StartConversation(Conversation conversation, NPC npc)
    {
        if (conversation == null)
        {
            Debug.LogError("Conversation is null");
            return;
        }

        isInConversation = true;
        currentConversation = conversation;
        currentNPC = npc;
        currentDialogueIndex = 0;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.EnterDialogueState();
        }

        // Pausar el movimiento del jugador
        if (playerController != null)
        {
            playerController.SetMovementEnabled(false);
            Debug.Log("Movimiento del jugador pausado durante la conversación");
        }

        // NUEVO: Activar el sistema Look At del NPC
        if (enableLookAtSystem && currentNPC != null)
        {
            StartCoroutine(ActivateLookAtWithDelay());
        }

        npcCamera = null;
        if (currentNPC != null)
        {
            npcCamera = currentNPC.transform.Find("NPC_Camera")?.gameObject;
            if (npcCamera != null)
            {
                npcCamera.SetActive(true);
                Debug.Log("Cámara del NPC activada: " + npcCamera.name);
            }
            else
            {
                Debug.LogWarning("No se encontró la cámara 'NPC_Camera' en el NPC: " + currentNPC.name);
            }
        }

        // Reiniciar el TypewriterEffect antes de comenzar
        if (typewriterEffect != null)
        {
            typewriterEffect.Reset();
        }

        // Activar la interfaz de diálogo y restaurar posición inicial
        if (dialogueUI != null)
        {
            dialogueUI.SetActive(true);

            // Restaurar la posición del RectTransform si necesitamos resetear
            RectTransform dialogueRect = dialogueUI.GetComponent<RectTransform>();
            if (dialogueRect != null)
            {
                // Si es la primera vez, guardar la posición inicial
                if (!initialPositionSaved)
                {
                    initialDialoguePosition = dialogueRect.anchoredPosition;
                    initialPositionSaved = true;
                    Debug.Log("Posición inicial del diálogo guardada: " + initialDialoguePosition);
                }
                else
                {
                    // Restaurar la posición original ANTES de cualquier animación
                    dialogueRect.anchoredPosition = initialDialoguePosition;
                    Debug.Log("Posición del diálogo restaurada a: " + initialDialoguePosition);
                }
            }

            // Animar la entrada del diálogo
            AnimateDialogueEntry();
        }

        if (optionsUI != null)
        {
            optionsUI.SetActive(false);
        }
        if (Next_bubble != null)
        {
            Next_bubble.SetActive(false);
        }

        // Pequeño delay para asegurar que la UI esté completamente activa antes de mostrar el diálogo
        StartCoroutine(DelayedShowDialogue());
    }

    /// <summary>
    /// NUEVO: Activar el Look At del NPC con un pequeño delay
    /// </summary>
    private IEnumerator ActivateLookAtWithDelay()
    {
        // Esperar un poco para que el NPC esté preparado
        yield return new WaitForSeconds(lookAtDelay);

        if (currentNPC != null)
        {
            // Verificar si el NPC tiene el sistema Look At configurado
            if (currentNPC.IsLookAtSystemReady())
            {
                currentNPC.StartLookingAtPlayer();
                Debug.Log($"Look At activado para {currentNPC.name}");
            }
            else
            {
                Debug.LogWarning($"Sistema Look At no está listo para {currentNPC.name}. " +
                               "Verifica que tenga un MultiAimConstraint configurado.");
            }
        }
    }

    private IEnumerator DelayedShowDialogue()
    {
        // Esperar un frame para asegurar que los componentes estén activos
        yield return null;

        // Ahora mostrar el diálogo
        ShowDialogue();
    }

    public void ShowDialogue()
    {
        if (currentConversation != null && currentDialogueIndex < currentConversation.dialogues.Length)
        {
            Dialogue currentDialogue = currentConversation.dialogues[currentDialogueIndex];
            if (speakerNameText != null)
            {
                speakerNameText.text = currentDialogue.speakerName;
            }

            isTyping = true;

            if (Next_bubble != null)
            {
                Next_bubble.SetActive(false);
            }

            if (typewriterEffect != null)
            {
                // Verificar que contentText está asignado
                if (typewriterEffect.textComponent == null)
                {
                    typewriterEffect.textComponent = contentText;
                    Debug.Log("Asignando contentText a typewriterEffect.textComponent");
                }

                // MODIFICADO: Pasar el NPC actual al typewriter effect
                typewriterEffect.StartTyping(currentDialogue.content, currentNPC);
            }
            else
            {
                // MODIFICADO: Si no hay efecto de tipeo, procesamos las etiquetas directamente con el NPC
                if (contentText != null)
                {
                    contentText.text = TextFormatHelper.ProcessTextTags(currentDialogue.content, currentNPC);
                    contentText.richText = true;
                }
                OnTypingComplete();
            }
        }
        else
        {
            ShowOptions();
        }
    }

    public void ShowOptions()
    {
        if (currentConversation != null && currentConversation.dialogueOptions != null && currentConversation.dialogueOptions.Length > 0)
        {
            if (optionsUI != null)
            {
                optionsUI.SetActive(true);
            }

            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (i < currentConversation.dialogueOptions.Length)
                {
                    optionButtons[i].gameObject.SetActive(true);
                    optionButtons[i].GetComponentInChildren<TMP_Text>().text = currentConversation.dialogueOptions[i].optionText;
                    int optionIndex = i;
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => SelectOption(optionIndex));
                }
                else
                {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }
        }
        else
        {
            EndConversation();
        }
    }

    public void SelectOption(int optionIndex)
    {
        selectedOptionIndex = optionIndex;

        if (currentConversation != null && optionIndex < currentConversation.dialogueOptions.Length)
        {
            DialogueOption selectedOption = currentConversation.dialogueOptions[optionIndex];

            if (!string.IsNullOrEmpty(selectedOption.actionId))
            {
                if (selectedOption.requiresDiceRoll)
                {
                    // NUEVO: Verificar si hay conversación previa al dado
                    if (selectedOption.preDiceConversation != null)
                    {
                        Debug.Log("Iniciando conversación previa al dado");

                        // Ocultar las opciones de diálogo
                        if (optionsUI != null)
                        {
                            optionsUI.SetActive(false);
                        }

                        // Marcar que estamos en modo "pre-dado" para saber qué hacer después
                        isInPreDiceConversation = true;

                        // Guardar los datos de la tirada para usar después de la conversación previa
                        pendingDiceRoll = new PendingDiceRollData
                        {
                            selectedOption = selectedOption,
                            optionIndex = optionIndex,
                            originalConversation = currentConversation, // NUEVO: Guardar conversación original
                            originalNPC = currentNPC // NUEVO: Guardar NPC original
                        };

                        // Iniciar la conversación previa
                        StartConversation(selectedOption.preDiceConversation, currentNPC);
                    }
                    else
                    {
                        // No hay conversación previa, proceder directamente con el dado
                        ExecuteDiceRoll(selectedOption);
                    }
                }
                else
                {
                    // Ejecutar acción estándar sin dados
                    ActionController.Instance.InvokeAction(selectedOption.actionId);

                    // Continuar con la siguiente conversación estándar (si existe)
                    Conversation nextConversation = selectedOption.nextDialogue;
                    if (nextConversation != null)
                    {
                        StartConversation(nextConversation, currentNPC);
                    }
                    else
                    {
                        EndConversation();
                    }
                }
            }
        }
    }

    // NUEVO: Método separado para ejecutar la tirada de dados
    private void ExecuteDiceRoll(DialogueOption selectedOption)
    {
        // En lugar de llamar a SelectDiceOption() que usa variables globales,
        // implementamos la lógica directamente aquí con los parámetros específicos
        Debug.Log("=== INICIANDO TIRADA DE DADOS CON BONUSES ===");

        // PROTECCIÓN CRÍTICA: Bloquear inputs del sistema de navegación temporalmente
        if (uiNavigationManager != null)
        {
            uiNavigationManager.BlockInputTemporarily(1.0f); // Bloquear por 1 segundo
            Debug.Log("Sistema de navegación bloqueado temporalmente");
        }

        // Desactivar la interfaz de diálogo
        if (dialogueInterface != null)
        {
            dialogueInterface.SetActive(false);
        }

        // Activar la interfaz de dados
        if (diceInterface != null)
        {
            diceInterface.SetActive(true);
        }

        // Resetear la interfaz de dados
        if (diceManager != null)
        {
            diceManager.ResetUI();
            diceManager.SetDifficultyClass(selectedOption.difficultyClass);
        }

        Debug.Log($"DC configurado: {selectedOption.difficultyClass}");
        Debug.Log("Interfaz de dados activada con protecciones");
        Debug.Log("============================================");

        // CRÍTICO: Configurar el callback con integración de bonuses
        if (diceManager != null)
        {
            diceManager.OnRollComplete = (isSuccess) =>
            {
                Debug.Log($"=== RESULTADO DE TIRADA CON BONUSES ===");
                Debug.Log($"Resultado final (con bonus): {diceManager.GetLastResult()}");
                Debug.Log($"DC requerido: {selectedOption.difficultyClass}");
                Debug.Log($"¿Tirada exitosa?: {(isSuccess ? "SÍ" : "NO")}");
                Debug.Log("=====================================");

                diceRollResult = isSuccess;
                nextContextualConversation = isSuccess ? selectedOption.successDialogue : selectedOption.failureDialogue;
            };
        }
    }

    public void NextDialogue()
    {
        Debug.Log("Next Dialogue");

        // Detener cualquier animación en curso
        if (typewriterEffect != null && isTyping)
        {
            typewriterEffect.StopTyping();
            isTyping = false;
        }

        if (currentConversation != null && currentDialogueIndex < currentConversation.dialogues.Length - 1)
        {
            currentDialogueIndex++;

            // Pequeño delay para asegurar que todo esté listo
            StartCoroutine(DelayedShowDialogue());
        }
        else
        {
            ShowOptions();
        }
    }

    public void EndConversation()
    {
        // NUEVO: Verificar si estamos terminando una conversación previa al dado
        if (isInPreDiceConversation && pendingDiceRoll != null)
        {
            Debug.Log("Terminando conversación previa al dado - Iniciando tirada");

            // Resetear el flag
            isInPreDiceConversation = false;

            // CRÍTICO: Restaurar el contexto original antes de ejecutar la tirada
            currentConversation = pendingDiceRoll.originalConversation;
            currentNPC = pendingDiceRoll.originalNPC;
            selectedOptionIndex = pendingDiceRoll.optionIndex;

            // Ejecutar la tirada con los datos guardados
            DialogueOption selectedOption = pendingDiceRoll.selectedOption;
            ExecuteDiceRoll(selectedOption);

            // Limpiar los datos pendientes
            pendingDiceRoll = null;

            // NO llamar al resto del método EndConversation porque no queremos
            // terminar completamente la conversación, solo la parte previa al dado
            return;
        }

        // RESTO DEL MÉTODO ORIGINAL (sin cambios)
        // Ejecutar la acción asociada a la conversación si existe
        if (currentConversation != null && !string.IsNullOrEmpty(currentConversation.actionId))
        {
            ActionController.Instance.InvokeAction(currentConversation.actionId);
        }

        // NUEVO: Desactivar el Look At del NPC antes de terminar
        if (enableLookAtSystem && currentNPC != null)
        {
            currentNPC.StopLookingAtPlayer();
            Debug.Log($"Look At desactivado para {currentNPC.name}");
        }

        // Desactivar la cámara del NPC si está activa
        if (npcCamera != null)
        {
            npcCamera.SetActive(false);
            Debug.Log("Cámara del NPC desactivada");
        }

        // Reactivar el movimiento del jugador
        if (playerController != null)
        {
            playerController.SetMovementEnabled(true);
            Debug.Log("Movimiento del jugador reactivado después de la conversación");
        }

        if (currentNPC != null)
        {
            // Pasamos la referencia de la conversación que terminó al NPC
            currentNPC.ConversationEnded(currentConversation);

            currentNPC.SetInteracted();
            currentNPC.EndCurrentConversation();

            onConversationEnd.Invoke();
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.EnterGameplayState();
        }

        // Animar la salida del diálogo ANTES de desactivarlo
        AnimateDialogueExit();

        // Actualizar el tiempo de finalización y el estado de conversación
        lastConversationEndTime = Time.time;
        isInConversation = false;

        Debug.Log("Conversación finalizada - Cooldown iniciado");
    }

    public void OnTypingComplete()
    {
        isTyping = false;
        if (Next_bubble != null)
        {
            Next_bubble.SetActive(true);
        }
    }

    public void OnDiceRollCompleteButtonPressed()
    {
        Debug.Log("=== PROCESANDO RESULTADO DE TIRADA ===");

        if (selectedOptionIndex != -1 && diceRollResult.HasValue && currentConversation != null && selectedOptionIndex < currentConversation.dialogueOptions.Length)
        {
            DialogueOption selectedOption = currentConversation.dialogueOptions[selectedOptionIndex];

            // Ocultar la interfaz de dados
            diceInterface.SetActive(false);

            // Ocultar también el panel de opciones para evitar que aparezca brevemente
            if (optionsUI != null)
            {
                optionsUI.SetActive(false);
            }

            // CRÍTICO: Mostrar información detallada del resultado
            Debug.Log($"Resultado base del dado: {diceManager.GetLastBaseResult()}");
            Debug.Log($"Bonus aplicado: +{diceManager.GetLastBonusValue()}");
            Debug.Log($"Resultado final: {diceManager.GetLastResult()}");
            Debug.Log($"DC requerido: {selectedOption.difficultyClass}");
            Debug.Log($"Resultado de la tirada: {(diceRollResult.Value ? "ÉXITO" : "FRACASO")}");

            // Ejecutar la acción con el resultado de la tirada
            ActionController.Instance.InvokeAction(selectedOption.actionId, diceRollResult.Value);

            // Reactivar el botón del dado para futuras tiradas
            if (diceManager != null)
            {
                diceManager.ReactivateDiceButton();
            }

            // Iniciar la conversación que se asignó previamente
            if (nextContextualConversation != null)
            {
                Debug.Log($"Iniciando conversación {(diceRollResult.Value ? "de éxito" : "de fracaso")}");
                StartConversation(nextContextualConversation, currentNPC);
                nextContextualConversation = null; // Limpiar la referencia tras usarla
            }
            else
            {
                Debug.Log("No hay conversación de seguimiento, terminando diálogo");
                EndConversation();
            }

            // Reset de variables
            diceRollResult = null;
            selectedOptionIndex = -1;
        }
        else
        {
            Debug.LogWarning("No hay resultado de dado disponible o ninguna opción seleccionada.");
            diceInterface.SetActive(false);

            // Reactivar el botón del dado en caso de error
            if (diceManager != null)
            {
                diceManager.ReactivateDiceButton();
            }
        }

        Debug.Log("===================================");
    }

    private void AnimateDialogueEntry()
    {
        // Asegurarnos de que dialogueUI existe y está activo
        if (dialogueUI == null || !dialogueUI.activeSelf) return;

        // Obtenemos el RectTransform
        RectTransform dialogueRect = dialogueUI.GetComponent<RectTransform>();
        if (dialogueRect == null) return;

        // IMPORTANTE: Asegurarnos de que cualquier animación anterior se cancele
        dialogueRect.DOKill();

        // Guardamos la posición inicial como referencia
        Vector2 animationStartPosition = new Vector2(initialDialoguePosition.x, initialDialoguePosition.y - slideDistance);

        // Posición inicial (abajo de su posición final)
        dialogueRect.anchoredPosition = animationStartPosition;

        // Animamos hacia la posición original guardada
        dialogueRect.DOAnchorPos(initialDialoguePosition, dialogEntryDuration)
            .SetEase(dialogEntryEase);
    }

    private void AnimateDialogueExit()
    {
        // Verificar que dialogueUI existe y está activo
        if (dialogueUI == null || !dialogueUI.activeSelf)
        {
            if (optionsUI != null)
            {
                optionsUI.SetActive(false);
            }
            return;
        }

        // Obtener el RectTransform
        RectTransform dialogueRect = dialogueUI.GetComponent<RectTransform>();
        if (dialogueRect == null)
        {
            dialogueUI.SetActive(false);
            if (optionsUI != null) optionsUI.SetActive(false);
            return;
        }

        // Detener cualquier animación actual
        dialogueRect.DOKill();

        // Posición destino (abajo de su posición inicial)
        Vector2 targetPosition = new Vector2(initialDialoguePosition.x, initialDialoguePosition.y - slideDistance);

        // Animar el movimiento hacia abajo
        dialogueRect.DOAnchorPos(targetPosition, dialogExitDuration)
            .SetEase(dialogExitEase)
            .OnComplete(() => {
                // Desactivar la interfaz de diálogo cuando la animación termine
                if (dialogueUI != null)
                {
                    dialogueUI.SetActive(false);

                    // IMPORTANTE: Restauramos la posición inicial para el próximo uso
                    dialogueRect.anchoredPosition = initialDialoguePosition;
                }
                if (optionsUI != null)
                {
                    optionsUI.SetActive(false);
                }
            });
    }

    public bool IsInConversation()
    {
        return isInConversation;
    }

    /// <summary>
    /// Obtiene el texto de la tecla de skip dialogue para mostrar al usuario
    /// </summary>
    public string GetSkipDialogueKeyDisplayText()
    {
        if (skipDialogueAction != null && skipDialogueAction.bindings.Count > 0)
        {
            // Obtener el primer binding para mostrar
            var binding = skipDialogueAction.bindings[0];
            string displayString = InputControlPath.ToHumanReadableString(binding.effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice);
            return displayString;
        }
        return "E"; // Fallback
    }

    /// <summary>
    /// Habilita o deshabilita temporalmente el input de skip dialogue
    /// </summary>
    public void SetSkipDialogueInputEnabled(bool enabled)
    {
        if (skipDialogueAction != null)
        {
            if (enabled)
            {
                skipDialogueAction.Enable();
            }
            else
            {
                skipDialogueAction.Disable();
            }
        }
    }

    /// <summary>
    /// NUEVO: Método público para configurar el sistema Look At
    /// </summary>
    public void SetLookAtSystemEnabled(bool enabled)
    {
        enableLookAtSystem = enabled;
        Debug.Log($"Sistema Look At {(enabled ? "habilitado" : "deshabilitado")} en DialogueManager");
    }

    /// <summary>
    /// NUEVO: Método público para configurar el delay del Look At
    /// </summary>
    public void SetLookAtDelay(float delay)
    {
        lookAtDelay = Mathf.Max(0f, delay);
        Debug.Log($"Delay del Look At configurado a {lookAtDelay} segundos");
    }
}

[System.Serializable]
public class PendingDiceRollData
{
    public DialogueOption selectedOption;
    public int optionIndex;
    public Conversation originalConversation; // NUEVO: Guardar la conversación original
    public NPC originalNPC; // NUEVO: Guardar el NPC original
}

[System.Serializable]
public class Dialogue
{
    public string speakerName;
    [TextArea(4, 4)]
    public string content;
}

public static class TextFormatHelper
{
    public static string ProcessTextTags(string input, NPC npc = null)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        string processed = input;

        // NUEVO: Procesar etiquetas de animación ANTES que las de formato
        if (npc != null)
        {
            processed = ProcessAnimationTags(processed, npc);
        }

        // Procesamos las etiquetas de formato existentes
        processed = ProcessFormatTags(processed);

        return processed;
    }

    /// <summary>
    /// NUEVO: Procesa las etiquetas de animación para NPCs
    /// </summary>
    private static string ProcessAnimationTags(string input, NPC npc)
    {
        if (npc == null) return input;

        string processed = input;

        // Patrón para capturar etiquetas de animación: <animationName>
        var animationPattern = @"<([a-zA-Z][a-zA-Z0-9_]*?)>";

        var matches = System.Text.RegularExpressions.Regex.Matches(processed, animationPattern);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            string fullTag = match.Value; // <happy>
            string animationName = match.Groups[1].Value; // happy

            // Verificar que no sea una etiqueta de formato conocida
            if (!IsFormatTag(animationName))
            {
                Debug.Log($"Ejecutando animación '{animationName}' en NPC {npc.name}");

                // Ejecutar la animación
                npc.PlayAnimation(animationName);

                // Remover la etiqueta del texto (para que no se muestre)
                processed = processed.Replace(fullTag, "");
            }
        }

        return processed;
    }

    /// <summary>
    /// Verifica si una etiqueta es de formato (para evitar conflictos)
    /// </summary>
    private static bool IsFormatTag(string tagName)
    {
        string[] formatTags = { "Bold", "Italic", "Underline", "Color", "Size", "b", "i", "u", "color", "size" };

        foreach (string formatTag in formatTags)
        {
            if (tagName.Equals(formatTag, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Procesa las etiquetas de formato existentes
    /// </summary>
    private static string ProcessFormatTags(string input)
    {
        string processed = input;

        // Negrita: <Bold>(texto)</Bold> -> <b>texto</b>
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"<Bold>\((.*?)\)</Bold>",
            "<b>$1</b>");

        // Versión simplificada: <Bold>texto</Bold> -> <b>texto</b>
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"<Bold>(.*?)</Bold>",
            "<b>$1</b>");

        // Cursiva: <Italic>(texto)</Italic> -> <i>texto</i>
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"<Italic>\((.*?)\)</Italic>",
            "<i>$1</i>");

        // Versión simplificada: <Italic>texto</Italic> -> <i>texto</i>
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"<Italic>(.*?)</Italic>",
            "<i>$1</i>");

        // Subrayado: <Underline>(texto)</Underline> -> <u>texto</u>
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"<Underline>\((.*?)\)</Underline>",
            "<u>$1</u>");

        // Versión simplificada: <Underline>texto</Underline> -> <u>texto</u>
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"<Underline>(.*?)</Underline>",
            "<u>$1</u>");

        // Color: <Color=red>(texto)</Color> -> <color=red>texto</color>
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"<Color=([^>]*?)>\((.*?)\)</Color>",
            "<color=$1>$2</color>");

        // Versión simplificada: <Color=red>texto</Color> -> <color=red>texto</color>
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"<Color=([^>]*?)>(.*?)</Color>",
            "<color=$1>$2</color>");

        // Tamaño: <Size=150%>(texto)</Size> -> <size=150%>texto</size>
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"<Size=([^>]*?)>\((.*?)\)</Size>",
            "<size=$1>$2</size>");

        // Versión simplificada: <Size=150%>texto</Size> -> <size=150%>texto</size>
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"<Size=([^>]*?)>(.*?)</Size>",
            "<size=$1>$2</size>");

        return processed;
    }
}