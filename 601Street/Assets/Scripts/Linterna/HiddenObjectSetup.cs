using UnityEngine;

/// <summary>
/// Script de utilidad para configurar objetos ocultos de manera f�cil.
/// Se puede a�adir a cualquier objeto para marcarlo como "objeto oculto".
/// </summary>
public class HiddenObjectSetup : MonoBehaviour
{
    [Header("Configuraci�n de Visibilidad")]
    [Tooltip("Nombre de la capa para objetos ocultos")]
    [SerializeField] private string hiddenLayerName = "HidenObject";

    [Tooltip("�Cambiar la capa de todos los hijos tambi�n?")]
    [SerializeField] private bool includeChildren = true;

    [Tooltip("Efecto visual opcional cuando el objeto se revela")]
    [SerializeField] private GameObject revealEffectPrefab;

    [Tooltip("Duraci�n del efecto de revelado en segundos")]
    [SerializeField] private float revealEffectDuration = 1.5f;

    [Header("Configuraci�n de Materiales")]
    [Tooltip("Material a usar cuando el objeto est� oculto")]
    [SerializeField] private Material hiddenMaterial;

    [Tooltip("Material a usar cuando el objeto es visible")]
    [SerializeField] private Material visibleMaterial;

    [Tooltip("�Cambiar el material cuando se revela?")]
    [SerializeField] private bool changeMaterial = false;

    // Variables privadas
    private int hiddenLayer;
    private int defaultLayer;
    private Renderer[] renderers;
    private Material[] originalMaterials;
    private bool wasRevealed = false;

    // Variable para prevenir cambios r�pidos de material
    private float lastLayerChangeTime = 0f;
    private float minTimeBetweenChanges = 0.1f; // Tiempo m�nimo entre cambios de material

    void Awake()
    {
        // Obtener el �ndice de la capa oculta
        hiddenLayer = LayerMask.NameToLayer(hiddenLayerName);
        if (hiddenLayer == -1)
        {
            Debug.LogError($"Capa '{hiddenLayerName}' no encontrada. Aseg�rate de que existe en tu proyecto.");
            return;
        }

        // Guardar la capa original
        defaultLayer = gameObject.layer;

        // Obtener todos los renderers
        if (changeMaterial)
        {
            renderers = includeChildren ? GetComponentsInChildren<Renderer>() : new Renderer[] { GetComponent<Renderer>() };
            originalMaterials = new Material[renderers.Length];

            // Guardar materiales originales
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    originalMaterials[i] = renderers[i].material;

                    // Aplicar material oculto si est� asignado
                    if (hiddenMaterial != null)
                    {
                        renderers[i].material = hiddenMaterial;
                    }
                }
            }
        }

        // Establecer la capa oculta
        SetToHiddenLayer();
    }

    void OnEnable()
    {
        // Asegurarnos de que el objeto est� en la capa oculta cuando se activa
        SetToHiddenLayer();
        wasRevealed = false;
        lastLayerChangeTime = 0f;
    }

    void OnDisable()
    {
        // Restaurar la capa original cuando se desactiva
        gameObject.layer = defaultLayer;
    }

    /// <summary>
    /// Asigna este objeto y sus hijos (si est� configurado) a la capa oculta
    /// </summary>
    private void SetToHiddenLayer()
    {
        // Establecer la capa de este objeto
        gameObject.layer = hiddenLayer;

        // Si includeChildren est� activado, establecer la capa de todos los hijos
        if (includeChildren)
        {
            foreach (Transform child in transform)
            {
                SetLayerRecursively(child.gameObject, hiddenLayer);
            }
        }
    }

    /// <summary>
    /// Asigna la capa especificada a un GameObject y todos sus hijos recursivamente
    /// </summary>
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    /// <summary>
    /// Este m�todo es llamado cuando el objeto cambia de layer
    /// </summary>
    private void CheckLayerChange()
    {
        // Evitar m�ltiples cambios en un corto per�odo de tiempo
        if (Time.time - lastLayerChangeTime < minTimeBetweenChanges)
        {
            return;
        }

        if (gameObject.layer != hiddenLayer && !wasRevealed)
        {
            // El objeto ha sido revelado
            OnObjectRevealed();
            wasRevealed = true;
            lastLayerChangeTime = Time.time;
        }
        else if (gameObject.layer == hiddenLayer && wasRevealed)
        {
            // El objeto ha vuelto a ocultarse
            OnObjectHidden();
            wasRevealed = false;
            lastLayerChangeTime = Time.time;
        }
    }

    /// <summary>
    /// Este m�todo es llamado cuando el objeto es revelado por la linterna
    /// </summary>
    private void OnObjectRevealed()
    {
        Debug.Log($"Objeto revelado: {gameObject.name}");

        // Cambiar materiales si est� configurado
        if (changeMaterial && visibleMaterial != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].material = visibleMaterial;
                }
            }
        }

        // Crear efecto visual si est� configurado
        if (revealEffectPrefab != null)
        {
            GameObject effect = Instantiate(revealEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, revealEffectDuration);
        }

        // Puedes a�adir aqu� eventos personalizados
        SendMessage("OnReveal", SendMessageOptions.DontRequireReceiver);
    }

    /// <summary>
    /// Este m�todo es llamado cuando el objeto vuelve a ocultarse
    /// </summary>
    private void OnObjectHidden()
    {
        Debug.Log($"Objeto ocultado: {gameObject.name}");

        // Restaurar materiales originales si est� configurado
        if (changeMaterial && hiddenMaterial != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].material = hiddenMaterial;
                }
            }
        }

        // Puedes a�adir aqu� eventos personalizados
        SendMessage("OnHide", SendMessageOptions.DontRequireReceiver);
    }

    /// <summary>
    /// LateUpdate verifica cambios en la capa despu�s de que todas las actualizaciones se hayan procesado
    /// </summary>
    void LateUpdate()
    {
        // Comprobar si la capa ha cambiado
        CheckLayerChange();
    }
}