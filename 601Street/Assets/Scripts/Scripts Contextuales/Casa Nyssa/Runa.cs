using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(InteractableObject))]
[RequireComponent(typeof(SceneTransitionPoint))]
public class Runa : MonoBehaviour
{
    [Header("Configuración de la Runa")]
    [Tooltip("Nombre del capítulo que aparecerá en el título")]
    [SerializeField] private string chapterTitle = "CHAPTER 1";

    [Tooltip("Descripción del capítulo")]
    [SerializeField] private string chapterDescription = "Umi's disappearance and Mom's case.";

    [Tooltip("Prefab que se instanciará al recoger la runa")]
    [SerializeField] private GameObject chapterCompletedPrefab;

    [Header("Referencias (opcional)")]
    [Tooltip("Referencia al SceneTransitionPoint (se autocompletará si está vacío)")]
    [SerializeField] private SceneTransitionPoint sceneTransitionPoint;

    [Header("Configuración de Audio")]
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
            Debug.LogError("Falta algún componente requerido en el objeto Runa.");
            enabled = false;
            return;
        }

        // Suscribirse al evento de interacción
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

    // Método llamado cuando el jugador interactúa con la runa
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

        // Instanciar el prefab de capítulo completado
        ShowCompletionScreen();

        // Opcional: Desactivar o destruir la runa
        // gameObject.SetActive(false);
    }

    // Mostrar la pantalla de capítulo completado
    private void ShowCompletionScreen()
    {
        if (chapterCompletedPrefab == null)
        {
            Debug.LogError("No se ha asignado el prefab de capítulo completado.");
            return;
        }

        // Instanciar el prefab
        GameObject completionScreen = Instantiate(chapterCompletedPrefab, Vector3.zero, Quaternion.identity);

        // Configurar los textos del prefab
        ConfigureCompletionTexts(completionScreen);

        // Configurar el botón para que llame a la transición de escena
        ConfigureNextChapterButton(completionScreen);

        // Bloquear el movimiento del jugador mientras se muestra la pantalla
        DisablePlayerMovement();
    }

    // Configurar los textos del prefab
    private void ConfigureCompletionTexts(GameObject completionScreen)
    {
        // Buscar y configurar el título
        TextMeshProUGUI titleText = completionScreen.transform.Find("Animation_Parent/CHAPTER_TITLE_Title")?.GetComponent<TextMeshProUGUI>();
        if (titleText != null)
        {
            titleText.text = chapterTitle;
        }

        // Buscar y configurar la descripción
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

    // Configurar el botón de siguiente capítulo para que llame a la transición
    private void ConfigureNextChapterButton(GameObject completionScreen)
    {
        // Encontrar el botón en el prefab
        Button nextChapterButton = completionScreen.transform.Find("Animation_Parent/NEXT_CHAPTER_BUTTON")?.GetComponent<Button>();

        if (nextChapterButton != null)
        {
            // Agregar listener al botón
            nextChapterButton.onClick.AddListener(() => {
                // Destruir la pantalla de completado
                Destroy(completionScreen);

                // Habilitar nuevamente el movimiento del jugador
                EnablePlayerMovement();

                // Llamar al método de transición de escena
                if (sceneTransitionPoint != null)
                {
                    sceneTransitionPoint.TransitionToScene();
                }
            });
        }
        else
        {
            Debug.LogError("No se encontró el botón NEXT_CHAPTER_BUTTON en el prefab.");
        }
    }

    // Métodos auxiliares para habilitar/deshabilitar el movimiento del jugador
    private void DisablePlayerMovement()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.SetMovementEnabled(false);
        }

        // También podemos usar el Enabler si está disponible
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

        // También podemos usar el Enabler si está disponible
        Enabler enabler = Enabler.Instance;
        if (enabler != null)
        {
            enabler.ReleasePlayer();
        }
    }
}