using UnityEngine;
using UnityEngine.SceneManagement;

public class UITemplateManager : MonoBehaviour
{
    // Singleton instance
    private static UITemplateManager instance;

    // Referencias a los indicadores de Inventory_Item
    private GameObject nearItemFeedbackTemplate;
    private GameObject inputFeedbackTemplate;

    // Referencias a los indicadores de InteractableObject
    private GameObject nearInteractableFeedbackTemplate;
    private GameObject inputInteractableFeedbackTemplate;

    // Propiedad para acceder al Singleton
    public static UITemplateManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<UITemplateManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("UITemplateManager");
                    instance = obj.AddComponent<UITemplateManager>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            InitializeTemplates();
            SceneManager.sceneLoaded += OnSceneLoaded; // Suscribirse al evento de carga de escena
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Asegurarse de que los indicadores originales estén desactivados al cargar una nueva escena
        EnsureTemplatesAreInactive();
    }

    private void InitializeTemplates()
    {
        // Buscar los objetos originales para Inventory_Item
        nearItemFeedbackTemplate = GameObject.Find("Near_Item_Feedback");
        inputFeedbackTemplate = GameObject.Find("Input_Feedback");

        // Buscar los objetos originales para InteractableObject
        nearInteractableFeedbackTemplate = GameObject.Find("Near_Interactable_Item_Feedback");
        inputInteractableFeedbackTemplate = GameObject.Find("Input_Interactable_Feedback");

        // Desactivar los originales para que no se muestren en la escena
        EnsureTemplatesAreInactive();
    }

    public void EnsureTemplatesAreInactive()
    {
        if (nearItemFeedbackTemplate != null && nearItemFeedbackTemplate.activeSelf)
        {
            nearItemFeedbackTemplate.SetActive(false);
        }
        if (inputFeedbackTemplate != null && inputFeedbackTemplate.activeSelf)
        {
            inputFeedbackTemplate.SetActive(false);
        }
        if (nearInteractableFeedbackTemplate != null && nearInteractableFeedbackTemplate.activeSelf)
        {
            nearInteractableFeedbackTemplate.SetActive(false);
        }
        if (inputInteractableFeedbackTemplate != null && inputInteractableFeedbackTemplate.activeSelf)
        {
            inputInteractableFeedbackTemplate.SetActive(false);
        }
    }

    // Métodos para obtener las plantillas de Inventory_Item
    public GameObject GetNearItemFeedbackTemplate()
    {
        if (nearItemFeedbackTemplate != null && nearItemFeedbackTemplate.activeSelf)
        {
            nearItemFeedbackTemplate.SetActive(false); // Asegurarse de que esté desactivado
        }
        return nearItemFeedbackTemplate;
    }

    public GameObject GetInputFeedbackTemplate()
    {
        if (inputFeedbackTemplate != null && inputFeedbackTemplate.activeSelf)
        {
            inputFeedbackTemplate.SetActive(false); // Asegurarse de que esté desactivado
        }
        return inputFeedbackTemplate;
    }

    // Métodos para obtener las plantillas de InteractableObject
    public GameObject GetNearInteractableFeedbackTemplate()
    {
        if (nearInteractableFeedbackTemplate != null && nearInteractableFeedbackTemplate.activeSelf)
        {
            nearInteractableFeedbackTemplate.SetActive(false); // Asegurarse de que esté desactivado
        }
        return nearInteractableFeedbackTemplate;
    }

    public GameObject GetInputInteractableFeedbackTemplate()
    {
        if (inputInteractableFeedbackTemplate != null && inputInteractableFeedbackTemplate.activeSelf)
        {
            inputInteractableFeedbackTemplate.SetActive(false); // Asegurarse de que esté desactivado
        }
        return inputInteractableFeedbackTemplate;
    }
}