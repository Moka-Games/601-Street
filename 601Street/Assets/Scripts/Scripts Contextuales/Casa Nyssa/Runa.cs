using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(InteractableObject))]
[RequireComponent(typeof(SceneTransitionPoint))]
public class Runa : MonoBehaviour
{
    [Header("Configuraci�n de la Runa")]
    [Tooltip("Nombre del cap�tulo que aparecer� en el t�tulo")]
    [SerializeField] private string chapterTitle = "CHAPTER 1";

    [Tooltip("Descripci�n del cap�tulo")]
    [SerializeField] private string chapterDescription = "Umi's disappearance and Mom's case.";

    [Tooltip("Prefab que se instanciar� al recoger la runa")]
    [SerializeField] private GameObject chapterCompletedPrefab;

    [Header("Referencias (opcional)")]
    [Tooltip("Referencia al SceneTransitionPoint (se autocompletar� si est� vac�o)")]
    [SerializeField] private SceneTransitionPoint sceneTransitionPoint;

    [Header("Configuraci�n de Audio")]
    [SerializeField] private AudioClip runeCollectedSound;
    [SerializeField] private float volume = 1f;

    // Referencias a componentes
    private InteractableObject interactableObject;
    private bool hasBeenCollected = false;

    private void Awake()
    {
        // Obtener referencias a componentes
        interactableObject = GetComponent<InteractableObject>();

        if (sceneTransitionPoint == null)
        {
            sceneTransitionPoint = GetComponent<SceneTransitionPoint>();
        }

        // Verificar que tenemos todas las referencias necesarias
        if (interactableObject == null || sceneTransitionPoint == null)
        {
            Debug.LogError("Falta alg�n componente requerido en el objeto Runa.");
            enabled = false;
            return;
        }

        // Suscribirse al evento de interacci�n
        interactableObject.onInteraction.AddListener(OnRuneInteracted);
    }

    private void OnDestroy()
    {
        // Limpieza de eventos para evitar memory leaks
        if (interactableObject != null)
        {
            interactableObject.onInteraction.RemoveListener(OnRuneInteracted);
        }
    }

    // M�todo llamado cuando el jugador interact�a con la runa
    private void OnRuneInteracted()
    {
        if (hasBeenCollected)
            return;

        // Marcar como recogida
        hasBeenCollected = true;

        // Reproducir sonido si hay uno configurado
        if (runeCollectedSound != null)
        {
            AudioSource.PlayClipAtPoint(runeCollectedSound, transform.position, volume);
        }

        // Instanciar el prefab de cap�tulo completado
        ShowCompletionScreen();

        // Opcional: Desactivar o destruir la runa
        // gameObject.SetActive(false);
    }

    // Mostrar la pantalla de cap�tulo completado
    private void ShowCompletionScreen()
    {
        if (chapterCompletedPrefab == null)
        {
            Debug.LogError("No se ha asignado el prefab de cap�tulo completado.");
            return;
        }

        // Instanciar el prefab
        GameObject completionScreen = Instantiate(chapterCompletedPrefab, Vector3.zero, Quaternion.identity);

        // Configurar los textos del prefab
        ConfigureCompletionTexts(completionScreen);

        // Configurar el bot�n para que llame a la transici�n de escena
        ConfigureNextChapterButton(completionScreen);

        // Bloquear el movimiento del jugador mientras se muestra la pantalla
        DisablePlayerMovement();
    }

    // Configurar los textos del prefab
    private void ConfigureCompletionTexts(GameObject completionScreen)
    {
        // Buscar y configurar el t�tulo
        TextMeshProUGUI titleText = completionScreen.transform.Find("Animation_Parent/CHAPTER_TITLE_Title")?.GetComponent<TextMeshProUGUI>();
        if (titleText != null)
        {
            titleText.text = chapterTitle;
        }

        // Buscar y configurar la descripci�n
        TextMeshProUGUI descriptionText = completionScreen.transform.Find("Animation_Parent/CHAPTER_DESCRIPTION")?.GetComponent<TextMeshProUGUI>();
        if (descriptionText != null)
        {
            descriptionText.text = chapterDescription;
        }

        // Buscar y configurar el texto de completado (opcional)
        TextMeshProUGUI completedText = completionScreen.transform.Find("Animation_Parent/Completed_Txt")?.GetComponent<TextMeshProUGUI>();
        if (completedText != null)
        {
            completedText.text = "Completed";
        }
    }

    // Configurar el bot�n de siguiente cap�tulo para que llame a la transici�n
    private void ConfigureNextChapterButton(GameObject completionScreen)
    {
        // Encontrar el bot�n en el prefab
        Button nextChapterButton = completionScreen.transform.Find("Animation_Parent/NEXT_CHAPTER_BUTTON")?.GetComponent<Button>();

        if (nextChapterButton != null)
        {
            // Agregar listener al bot�n
            nextChapterButton.onClick.AddListener(() => {
                // Destruir la pantalla de completado
                Destroy(completionScreen);

                // Habilitar nuevamente el movimiento del jugador
                EnablePlayerMovement();

                // Llamar al m�todo de transici�n de escena
                if (sceneTransitionPoint != null)
                {
                    sceneTransitionPoint.TransitionToScene();
                }
            });
        }
        else
        {
            Debug.LogError("No se encontr� el bot�n NEXT_CHAPTER_BUTTON en el prefab.");
        }
    }

    // M�todos auxiliares para habilitar/deshabilitar el movimiento del jugador
    private void DisablePlayerMovement()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.SetMovementEnabled(false);
        }

        // Tambi�n podemos usar el Enabler si est� disponible
        Enabler enabler = Enabler.Instance;
        if (enabler != null)
        {
            enabler.BlockPlayer();
        }
    }

    private void EnablePlayerMovement()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.SetMovementEnabled(true);
        }

        // Tambi�n podemos usar el Enabler si est� disponible
        Enabler enabler = Enabler.Instance;
        if (enabler != null)
        {
            enabler.ReleasePlayer();
        }
    }
}