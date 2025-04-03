using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class SafeSystem : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Componente de texto que mostrará la contraseña actual")]
    public TMP_Text passwordDisplay;

    [Tooltip("Contraseña correcta para desbloquear la caja fuerte")]
    public string correctPassword = "1234";

    [Tooltip("Objeto con material emisivo para feedback visual")]
    public Renderer feedbackLight;

    [Header("Materiales para la luz de feedback")]
    public Material defaultMaterial;   // Material original/neutral
    public Material redMaterial;       // Material para errores (emisivo rojo)
    public Material greenMaterial;     // Material para éxito (emisivo verde)
    public Material purpleMaterial;    // Material para reseteo (emisivo morado)

    [Header("Audio")]
    [Tooltip("Fuente de audio para reproducir sonidos")]
    public AudioSource audioSource;

    [Tooltip("Sonido al pulsar un botón numérico")]
    public AudioClip buttonPressSound;

    [Tooltip("Sonido al pulsar Enter con contraseña correcta")]
    public AudioClip correctPasswordSound;

    [Tooltip("Sonido al pulsar Enter con contraseña incorrecta")]
    public AudioClip wrongPasswordSound;

    [Tooltip("Sonido al limpiar la contraseña")]
    public AudioClip clearPasswordSound;

    [Header("Configuración")]
    [Tooltip("Distancia máxima para detectar botones")]
    public float maxRaycastDistance = 3f;

    [Tooltip("Capas que serán detectadas por el raycast")]
    public LayerMask buttonLayerMask = -1; // Por defecto, todas las capas

    [Tooltip("Cantidad máxima de dígitos permitidos")]
    public int maxDigits = 4;

    [Tooltip("Duración del parpadeo de la luz en segundos")]
    public float blinkDuration = 1f;

    [Header("Eventos")]
    public UnityEvent OnSafeUnlocked;
    public UnityEvent OnWrongPassword;
    public UnityEvent OnButtonPressed;

    // Contraseña actual que el jugador está introduciendo
    private string currentPassword = "";

    // Referencia a la cámara principal
    private Camera mainCamera;

    // Estado de la caja fuerte
    private bool isSafeUnlocked = false;

    // Para controlar la corrutina del parpadeo
    private Coroutine blinkCoroutine;

    void Start()
    {
        // Obtener la cámara principal
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("No se encontró la cámara principal. Asegúrate de que exista una cámara con tag MainCamera.");
        }

        // Verificar que tengamos una referencia al display de la contraseña
        if (passwordDisplay == null)
        {
            Debug.LogError("No se ha asignado el componente de texto para mostrar la contraseña.");
        }

        // Verificar que tengamos los materiales necesarios
        if (feedbackLight == null)
        {
            Debug.LogWarning("No se ha asignado el objeto de luz de feedback. El feedback visual no funcionará.");
        }
        else if (defaultMaterial == null || redMaterial == null || greenMaterial == null || purpleMaterial == null)
        {
            Debug.LogWarning("Faltan algunos materiales para la luz de feedback. El feedback visual podría no funcionar correctamente.");
        }
        else
        {
            // Asegurarse de que la luz tenga el material por defecto al inicio
            feedbackLight.material = defaultMaterial;
        }

        // Verificar que tengamos una fuente de audio
        if (audioSource == null)
        {
            // Si no hay una fuente de audio asignada, intentamos añadirla
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                Debug.Log("Se ha añadido automáticamente un componente AudioSource a la caja fuerte.");
            }
        }

        // Inicializar el display de la contraseña
        UpdatePasswordDisplay();
    }

    void Update()
    {
        // Detectar entrada del mouse
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }
    }

    void HandleMouseClick()
    {
        // Si la caja fuerte ya está desbloqueada, no procesar más clics
        if (isSafeUnlocked)
            return;

        // Crear un rayo desde la cámara hasta el punto donde hizo clic el usuario
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Verificar si el rayo golpea algo en la capa de botones
        if (Physics.Raycast(ray, out hit, maxRaycastDistance, buttonLayerMask))
        {
            // Intentar obtener el componente SafeButton del objeto golpeado
            SafeButton button = hit.collider.GetComponent<SafeButton>();

            if (button != null)
            {
                // Procesar el botón presionado
                ProcessButtonPress(button);
            }
        }
    }

    void ProcessButtonPress(SafeButton button)
    {
        // Activar la animación o efecto del botón si tiene uno
        button.PressButton();

        // Disparar el evento de botón presionado
        OnButtonPressed?.Invoke();

        // Procesar según el tipo de botón
        switch (button.buttonType)
        {
            case SafeButtonType.Number:
                // Reproducir sonido de botón numérico
                PlaySound(buttonPressSound);

                // Añadir el número al password actual si no hemos llegado al máximo
                if (currentPassword.Length < maxDigits)
                {
                    currentPassword += button.buttonValue;
                    UpdatePasswordDisplay();
                }
                break;

            case SafeButtonType.Enter:
                // Verificar si tenemos el número correcto de dígitos antes de intentar verificar
                if (currentPassword.Length < maxDigits)
                {
                    Debug.Log("Se requieren " + maxDigits + " dígitos para verificar la contraseña.");

                    // Reproducir sonido de contraseña incorrecta (mismo que para contraseña errónea)
                    PlaySound(wrongPasswordSound);

                    // Mostrar luz roja parpadeante si faltan dígitos
                    BlinkLight(redMaterial);
                }
                else
                {
                    // Verificar si la contraseña es correcta
                    CheckPassword();
                }
                break;

            case SafeButtonType.Delete:
                // Reproducir sonido de botón
                PlaySound(buttonPressSound);

                // Borrar el último dígito si hay alguno
                if (currentPassword.Length > 0)
                {
                    currentPassword = currentPassword.Substring(0, currentPassword.Length - 1);
                    UpdatePasswordDisplay();
                }
                break;

            case SafeButtonType.Clear:
                // Reproducir sonido de limpiar
                PlaySound(clearPasswordSound);

                // Limpiar toda la contraseña
                currentPassword = "";
                UpdatePasswordDisplay();

                // Cambiar la luz a morada al limpiar
                if (feedbackLight != null && purpleMaterial != null)
                {
                    feedbackLight.material = purpleMaterial;

                    // Programar que vuelva al material por defecto después de un tiempo
                    StartCoroutine(ResetLightAfterDelay(blinkDuration));
                }
                break;
            case SafeButtonType.Exit:
                // Reproducir sonido (puedes usar el mismo que Clear u otro)
                PlaySound(clearPasswordSound);

                // Buscar y notificar al SafeGameplayManager para salir del modo
                SafeGameplayManager gameplayManager = FindAnyObjectByType<SafeGameplayManager>();
                if (gameplayManager != null)
                {
                    gameplayManager.ExitSafeMode();
                }
                else
                {
                    Debug.LogWarning("No se encontró el SafeGameplayManager para salir del modo caja fuerte.");
                }
                break;
        }
    }

    void UpdatePasswordDisplay()
    {
        // Actualizar el texto en el display
        if (passwordDisplay != null)
        {
            // Podemos mostrar asteriscos para mayor seguridad, o los dígitos reales
            // passwordDisplay.text = new string('*', currentPassword.Length);
            passwordDisplay.text = currentPassword;

            // Alternativa: Rellenar con espacios o guiones hasta maxDigits
            // string displayText = currentPassword.PadRight(maxDigits, '_');
            // passwordDisplay.text = displayText;
        }
    }

    void CheckPassword()
    {
        // Verificar si la contraseña coincide con la correcta
        if (currentPassword == correctPassword)
        {
            Debug.Log("¡Contraseña correcta! Caja fuerte desbloqueada.");

            // Reproducir sonido de éxito
            PlaySound(correctPasswordSound);

            // Marcar la caja fuerte como desbloqueada
            isSafeUnlocked = true;

            // Mostrar luz verde permanente
            if (feedbackLight != null && greenMaterial != null)
            {
                feedbackLight.material = greenMaterial;
            }

            // Opcionalmente, actualizar el display para mostrar algo como "ABIERTO"
            if (passwordDisplay != null)
            {
                passwordDisplay.text = "OPEN";
            }

            // Disparar el evento para que se active la secuencia de desbloqueo
            OnSafeUnlocked?.Invoke();
        }
        else
        {
            Debug.Log("Contraseña incorrecta. Inténtalo de nuevo.");

            // Reproducir sonido de error
            PlaySound(wrongPasswordSound);

            // Disparar el evento de contraseña incorrecta
            OnWrongPassword?.Invoke();

            // Mostrar luz roja parpadeante
            BlinkLight(redMaterial);

            // Limpiar la contraseña tras el intento fallido
            currentPassword = "";
            UpdatePasswordDisplay();
        }
    }

    // Método para hacer parpadear la luz con un material específico
    void BlinkLight(Material blinkMaterial)
    {
        // Cancelar cualquier parpadeo anterior si existe
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }

        // Iniciar nueva corrutina de parpadeo
        if (feedbackLight != null && blinkMaterial != null && defaultMaterial != null)
        {
            blinkCoroutine = StartCoroutine(BlinkLightCoroutine(blinkMaterial));
        }
    }

    // Corrutina para parpadear la luz
    IEnumerator BlinkLightCoroutine(Material blinkMaterial)
    {
        float elapsedTime = 0;
        bool isBlinkOn = true;

        // Parpadear durante el tiempo especificado
        while (elapsedTime < blinkDuration)
        {
            // Alternar entre el material de parpadeo y el material por defecto
            feedbackLight.material = isBlinkOn ? blinkMaterial : defaultMaterial;
            isBlinkOn = !isBlinkOn;

            // Esperar un corto período
            yield return new WaitForSeconds(0.1f);
            elapsedTime += 0.1f;
        }

        // Asegurar que volvemos al material por defecto al final
        feedbackLight.material = defaultMaterial;
        blinkCoroutine = null;
    }

    // Corrutina para restablecer la luz después de un retraso
    IEnumerator ResetLightAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Asegurarse de que la luz vuelva al material por defecto
        if (feedbackLight != null && defaultMaterial != null)
        {
            feedbackLight.material = defaultMaterial;
        }
    }

    // Método para reproducir un sonido
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    // Método público para verificar si la caja fuerte está desbloqueada
    public bool IsSafeUnlocked()
    {
        return isSafeUnlocked;
    }

    // Método público para reiniciar la caja fuerte (puede ser llamado desde otros scripts)
    public void ResetSafe()
    {
        // Solo permitir resetear si la caja no está desbloqueada
        if (!isSafeUnlocked)
        {
            currentPassword = "";
            UpdatePasswordDisplay();

            // Opcional: restablecer la luz a su estado por defecto
            if (feedbackLight != null && defaultMaterial != null)
            {
                feedbackLight.material = defaultMaterial;
            }

            // Reproducir sonido de limpiar
            PlaySound(clearPasswordSound);
        }
    }

    // Método público para resetear completamente la caja fuerte
    // Esto permitiría volver a usarla incluso después de haber sido desbloqueada
    public void HardResetSafe()
    {
        isSafeUnlocked = false;
        currentPassword = "";
        UpdatePasswordDisplay();

        if (feedbackLight != null && defaultMaterial != null)
        {
            feedbackLight.material = defaultMaterial;
        }

        // Reproducir sonido de limpiar
        PlaySound(clearPasswordSound);
    }
}
