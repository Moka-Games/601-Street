using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gestiona la interfaz de usuario del sistema de llamadas telefónicas.
/// </summary>
public class CallUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private GameObject callPopupPanel;
    [SerializeField] private GameObject callerInfoPanel;
    [SerializeField] private Image callerAvatar;
    [SerializeField] private TMP_Text callerNameText;
    [SerializeField] private TMP_Text callerDescriptionText;
    [SerializeField] private TMP_Text callStatusText;
    [SerializeField] private Button acceptCallButton;
    [SerializeField] private Button rejectCallButton;
    [SerializeField] private GameObject controlsInfoPanel;
    [SerializeField] private TMP_Text acceptKeyText;
    [SerializeField] private TMP_Text rejectKeyText;

    [Header("Referencias Audio")]
    [SerializeField] private AudioSource ringtoneAudioSource;
    [SerializeField] private float ringtoneVolume = 0.5f;

    [Header("Animaciones")]
    [SerializeField] private Animator callPopupAnimator;
    [SerializeField] private string showPopupAnimName = "ShowCallPopup";
    [SerializeField] private string hidePopupAnimName = "HideCallPopup";

    [Header("Opciones")]
    [SerializeField] private Sprite defaultCallerAvatar;
    [SerializeField] private KeyCode acceptCallKey = KeyCode.F;
    [SerializeField] private KeyCode rejectCallKey = KeyCode.Escape;

    // Referencias al sistema
    private CallSystem callSystem;

    private void Awake()
    {
        // Obtener el componente animator si no está asignado
        if (callPopupAnimator == null && callPopupPanel != null)
        {
            callPopupAnimator = callPopupPanel.GetComponent<Animator>();
        }

        // Configurar botones
        SetupButtons();

        // Ocultar el panel al inicio
        if (callPopupPanel != null)
        {
            callPopupPanel.SetActive(false);
        }
    }

    private void Start()
    {
        // Buscar el sistema de llamadas
        callSystem = FindAnyObjectByType<CallSystem>();
        if (callSystem == null)
        {
            Debug.LogError("No se encontró CallSystem en la escena");
            return;
        }

        // Actualizar textos de control
        if (acceptKeyText != null)
        {
            acceptKeyText.text = $"Pulsa [{acceptCallKey}] para aceptar";
        }

        if (rejectKeyText != null)
        {
            rejectKeyText.text = $"Pulsa [{rejectCallKey}] para rechazar";
        }
    }

    private void SetupButtons()
    {
        // Configurar botón de aceptar
        if (acceptCallButton != null)
        {
            acceptCallButton.onClick.AddListener(() => {
                if (callSystem != null) callSystem.AcceptCall();
            });
        }

        // Configurar botón de rechazar
        if (rejectCallButton != null)
        {
            rejectCallButton.onClick.AddListener(() => {
                if (callSystem != null) callSystem.RejectCall();
            });
        }
    }

    /// <summary>
    /// Configura la UI de la llamada entrante.
    /// </summary>
    public void SetupIncomingCall(string callerName, string description, Sprite avatar = null)
    {
        // Configurar nombre
        if (callerNameText != null)
        {
            callerNameText.text = callerName;
        }

        // Configurar descripción
        if (callerDescriptionText != null)
        {
            callerDescriptionText.text = description;
        }

        // Configurar avatar
        if (callerAvatar != null)
        {
            callerAvatar.sprite = avatar != null ? avatar : defaultCallerAvatar;
            callerAvatar.gameObject.SetActive(true);
        }

        // Configurar estado de la llamada
        if (callStatusText != null)
        {
            callStatusText.text = "Llamada Entrante";
        }

        // Activar el panel
        ShowCallPopup();
    }

    /// <summary>
    /// Muestra el popup de llamada con animación.
    /// </summary>
    public void ShowCallPopup()
    {
        if (callPopupPanel != null)
        {
            callPopupPanel.SetActive(true);

            // Reproducir animación si existe
            if (callPopupAnimator != null)
            {
                callPopupAnimator.Play(showPopupAnimName);
            }
        }

        // Reproducir tono de llamada
        if (ringtoneAudioSource != null)
        {
            ringtoneAudioSource.volume = ringtoneVolume;
            ringtoneAudioSource.loop = true;
            ringtoneAudioSource.Play();
        }
    }

    /// <summary>
    /// Oculta el popup de llamada con animación.
    /// </summary>
    public void HideCallPopup()
    {
        if (callPopupAnimator != null && callPopupPanel != null && callPopupPanel.activeSelf)
        {
            // Reproducir animación de ocultar
            callPopupAnimator.Play(hidePopupAnimName);
            StartCoroutine(DisablePopupAfterAnimation(callPopupAnimator, hidePopupAnimName));
        }
        else if (callPopupPanel != null)
        {
            // Si no hay animador, ocultar inmediatamente
            callPopupPanel.SetActive(false);
        }

        // Detener el tono de llamada
        if (ringtoneAudioSource != null && ringtoneAudioSource.isPlaying)
        {
            ringtoneAudioSource.Stop();
        }
    }

    /// <summary>
    /// Desactiva el popup después de que termine la animación.
    /// </summary>
    private System.Collections.IEnumerator DisablePopupAfterAnimation(Animator animator, string animName)
    {
        if (animator != null)
        {
            // Obtener la duración de la animación
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            float animDuration = 1f; // Valor por defecto

            foreach (AnimationClip clip in clips)
            {
                if (clip.name == animName)
                {
                    animDuration = clip.length;
                    break;
                }
            }

            yield return new WaitForSeconds(animDuration);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        // Desactivar el panel
        if (callPopupPanel != null)
        {
            callPopupPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Actualiza el estado mostrado de la llamada.
    /// </summary>
    public void UpdateCallStatus(string status)
    {
        if (callStatusText != null)
        {
            callStatusText.text = status;
        }
    }
}