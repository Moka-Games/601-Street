using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(InteractableObject))]
[RequireComponent(typeof(SceneTransitionPoint))]
public class Runa : MonoBehaviour
{
    [Header("Configuración de la Runa")]
    [SerializeField] private string chapterTitle = "CHAPTER 1";
    [SerializeField] private string chapterDescription = "Umi's disappearance and Mom's case.";
    [SerializeField] private GameObject chapterCompletedPrefab;

    [Header("Runas Restantes")]
    [Tooltip("Texto que se mostrará en el contador de runas restantes")]
    [SerializeField] private string remainingRunesText = "1/3";

    [Header("Referencias")]
    [SerializeField] private SceneTransitionPoint sceneTransitionPoint;
    [SerializeField] private AudioClip runeCollectedSound;
    [SerializeField] private float volume = 1f;

    private InteractableObject interactableObject;
    private bool hasBeenCollected = false;
    private GameObject completionScreenInstance;

    private void Awake()
    {
        interactableObject = GetComponent<InteractableObject>();

        if (sceneTransitionPoint == null)
        {
            sceneTransitionPoint = GetComponent<SceneTransitionPoint>();
        }

        if (interactableObject == null || sceneTransitionPoint == null)
        {
            Debug.LogError("Falta algún componente requerido en el objeto Runa.");
            enabled = false;
            return;
        }

        interactableObject.onInteraction.AddListener(OnRuneInteracted);
    }

    private void OnDestroy()
    {
        if (interactableObject != null)
        {
            interactableObject.onInteraction.RemoveListener(OnRuneInteracted);
        }
    }

    private void OnRuneInteracted()
    {
        if (hasBeenCollected)
            return;

        hasBeenCollected = true;

        if (runeCollectedSound != null)
        {
            AudioSource.PlayClipAtPoint(runeCollectedSound, transform.position, volume);
        }

        ShowCompletionScreen();
    }

    private void ShowCompletionScreen()
    {
        if (chapterCompletedPrefab == null)
        {
            Debug.LogError("No se ha asignado el prefab de capítulo completado.");
            return;
        }

        // 1. Instanciar el prefab de completado
        completionScreenInstance = Instantiate(chapterCompletedPrefab, Vector3.zero, Quaternion.identity);

        // 2. CONGELAR LA CÁMARA
        Camera_Script cameraScript = FindAnyObjectByType<Camera_Script>();
        if (cameraScript != null)
        {
            Debug.Log("Runa: Congelando cámara al mostrar prefab");
            cameraScript.FreezeCamera();
        }

        // 3. Configurar los textos
        ConfigureCompletionTexts(completionScreenInstance);
        ConfigureNextChapterButton(completionScreenInstance);

        // 4. Configurar el texto de runas restantes
        ConfigureRemainingRunesText(completionScreenInstance);
    }

    private void ConfigureCompletionTexts(GameObject completionScreen)
    {
        TextMeshProUGUI titleText = completionScreen.transform.Find("Animation_Parent/CHAPTER_TITLE_Títle")?.GetComponent<TextMeshProUGUI>();
        if (titleText != null)
        {
            titleText.text = chapterTitle;
        }

        TextMeshProUGUI descriptionText = completionScreen.transform.Find("Animation_Parent/CHAPTER_DESCRIPTION")?.GetComponent<TextMeshProUGUI>();
        if (descriptionText != null)
        {
            descriptionText.text = chapterDescription;
        }

        TextMeshProUGUI completedText = completionScreen.transform.Find("Animation_Parent/Completed_Txt")?.GetComponent<TextMeshProUGUI>();
        if (completedText != null)
        {
            completedText.text = "Completed";
        }
    }

    // Método nuevo para configurar el texto de REMAINING_RUNES
    private void ConfigureRemainingRunesText(GameObject completionScreen)
    {
        // Ruta completa al componente de texto REMAINING_RUNES
        TextMeshProUGUI remainingRunesTextComponent = completionScreen.transform.Find(
            "Animation_Parent/NEXT_CHAPTER_BUTTON/Rune-Pop-Up/Panel/REMAINING_RUNES")?.GetComponent<TextMeshProUGUI>();

        if (remainingRunesTextComponent != null)
        {
            remainingRunesTextComponent.text = remainingRunesText;
            Debug.Log($"Runa: Texto de runas restantes configurado a '{remainingRunesText}'");
        }
        else
        {
            Debug.LogWarning("Runa: No se encontró el componente REMAINING_RUNES en el prefab");

            // Intento secundario con búsqueda parcial
            TextMeshProUGUI[] allTexts = completionScreen.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in allTexts)
            {
                if (text.name == "REMAINING_RUNES")
                {
                    text.text = remainingRunesText;
                    Debug.Log($"Runa: Texto de runas restantes encontrado mediante búsqueda alternativa y configurado a '{remainingRunesText}'");
                    break;
                }
            }
        }
    }

    private void ConfigureNextChapterButton(GameObject completionScreen)
    {
        Button nextChapterButton = completionScreen.transform.Find("Animation_Parent/NEXT_CHAPTER_BUTTON")?.GetComponent<Button>();

        if (nextChapterButton != null)
        {
            nextChapterButton.onClick.AddListener(() => {
                // 1. Descongelar la cámara ANTES de la transición
                Camera_Script cameraScript = FindAnyObjectByType<Camera_Script>();
                if (cameraScript != null)
                {
                    Debug.Log("Runa: Descongelando cámara antes de la transición");
                    cameraScript.UnfreezeCamera();
                }

                // 2. Destruir la pantalla de completado
                Destroy(completionScreenInstance);
                completionScreenInstance = null;

                // 3. Esperar un frame y luego iniciar la transición
                StartCoroutine(DelayedTransition());
            });
        }
        else
        {
            Debug.LogError("No se encontró el botón NEXT_CHAPTER_BUTTON en el prefab.");
        }
    }

    private IEnumerator DelayedTransition()
    {
        // Esperar un frame para asegurar que todo se actualice
        yield return null;

        // Iniciar la transición
        if (sceneTransitionPoint != null)
        {
            Debug.Log("Runa: Iniciando transición de escena");
            sceneTransitionPoint.TransitionToScene();

            // Verificación de seguridad para la cámara
            StartCoroutine(SafetyCheckUnfreezeCamera());
        }
        else
        {
            Debug.LogError("SceneTransitionPoint es null en el momento de la transición");
        }
    }

    private IEnumerator SafetyCheckUnfreezeCamera()
    {
        // Esperar a que la transición esté avanzada
        yield return new WaitForSeconds(3f);

        // Verificar si la cámara está bloqueada
        Camera_Script cameraScript = FindAnyObjectByType<Camera_Script>();
        if (cameraScript != null && cameraScript.freeLookCamera != null && !cameraScript.freeLookCamera.enabled)
        {
            Debug.Log("Runa: Aplicando desbloqueo de seguridad a la cámara después de la transición");
            cameraScript.UnfreezeCamera();
        }
    }

    // Método público para establecer el texto de runas restantes desde otros scripts
    public void SetRemainingRunesText(string text)
    {
        remainingRunesText = text;
    }
}