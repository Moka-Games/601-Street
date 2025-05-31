using System.Collections;
using UnityEngine;

/// <summary>
/// Componente para objetos que otorgan bonuses al sistema de dados
/// Integrado con el nuevo BonusManager flexible
/// </summary>
public class DiceBonus : MonoBehaviour
{
    [Header("Configuración del Bonus")]
    [Tooltip("Nombre descriptivo del bonus")]
    [SerializeField] private string bonusName = "Bonus Misterioso";

    [Tooltip("Valor que se añadirá al resultado del dado")]
    [SerializeField] private int bonusValue = 2;

    [Tooltip("Descripción del bonus (opcional)")]
    [TextArea(2, 4)]
    [SerializeField] private string bonusDescription = "";

    [Header("Configuración Visual")]
    [Tooltip("Icono del bonus para mostrar en la interfaz")]
    [SerializeField] private Sprite bonusIcon;

    [Tooltip("Si está marcado, el objeto se desactivará después de ser recogido")]
    [SerializeField] private bool disableAfterCollection = true;

    [Header("Efectos Visuales")]
    [Tooltip("Partículas que se reproducen al recoger el bonus")]
    [SerializeField] private GameObject collectionEffect;

    [Tooltip("Sonido que se reproduce al recoger el bonus")]
    [SerializeField] private AudioClip collectionSound;

    [Header("Debug")]
    [Tooltip("Si está marcado, el objeto mostrará mensajes de debug")]
    [SerializeField] private bool showDebugMessages = true;

    // Referencias
    private BonusManager bonusManager;
    private AudioSource audioSource;
    private bool hasBeenCollected = false;

    public GameObject bonus_Mesh;

    private void Start()
    {
        // Buscar el BonusManager en la escena
        bonusManager = BonusManager.Instance;
        if (bonusManager == null)
        {
            Debug.LogError($"DiceBonus en {gameObject.name}: No se encontró BonusManager en la escena");
        }

        // Buscar AudioSource para efectos de sonido
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && collectionSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Verificar que existe el componente InteractableObject
        InteractableObject interactable = GetComponent<InteractableObject>();
        if (interactable == null)
        {
            Debug.LogError($"DiceBonus en {gameObject.name}: Este objeto necesita un componente InteractableObject para funcionar");
            enabled = false;
            return;
        }

        // Configurar descripción automática si no se proporcionó
        if (string.IsNullOrEmpty(bonusDescription))
        {
            bonusDescription = $"Añade +{bonusValue} al resultado del dado";
        }

        if (showDebugMessages)
        {
            Debug.Log($"DiceBonus inicializado: {bonusName} (+{bonusValue})");
        }
    }

    /// <summary>
    /// Método principal para activar/recoger el bonus
    /// Se llama desde el evento OnInteraction del InteractableObject
    /// </summary>
    public void CollectBonus()
    {
        if (hasBeenCollected)
        {
            if (showDebugMessages)
                Debug.LogWarning($"Intento de recoger bonus ya recogido: {bonusName}");
            return;
        }

        // Buscar BonusManager si no está asignado
        if (bonusManager == null)
        {
            bonusManager = BonusManager.Instance;
        }

        if (bonusManager == null)
        {
            Debug.LogError($"No se puede recoger bonus {bonusName}: BonusManager no encontrado en la escena");
            Debug.LogError("Asegúrate de que hay un GameObject con BonusManager en la escena");
            return;
        }

        if (showDebugMessages)
        {
            Debug.Log($"=== RECOGIENDO BONUS ===");
            Debug.Log($"Bonus: {bonusName} (+{bonusValue})");
            Debug.Log($"BonusManager encontrado: {bonusManager.name}");
        }

        // Marcar como recogido
        hasBeenCollected = true;

        // Añadir al sistema de bonuses
        bonusManager.AddBonus(bonusName, bonusValue, bonusDescription, bonusIcon);

        // Efectos visuales y sonoros
        PlayCollectionEffects();

        if (showDebugMessages)
            Debug.Log($"Bonus {bonusName} recogido exitosamente");

        // Desactivar objeto si está configurado
        if (disableAfterCollection)
        {
            StartCoroutine(DisableObject(1.5f));
        }
    }

    /// <summary>
    /// Versión sobrecargada para especificar parámetros personalizados
    /// </summary>
    public void CollectBonus(int customValue)
    {
        // Actualizar el valor temporalmente
        int originalValue = bonusValue;
        bonusValue = customValue;

        CollectBonus();

        // Restaurar valor original
        bonusValue = originalValue;
    }

    /// <summary>
    /// Versión sobrecargada para especificar nombre y valor personalizados
    /// </summary>
    public void CollectBonus(string customName, int customValue)
    {
        // Actualizar valores temporalmente
        string originalName = bonusName;
        int originalValue = bonusValue;

        bonusName = customName;
        bonusValue = customValue;

        CollectBonus();

        // Restaurar valores originales
        bonusName = originalName;
        bonusValue = originalValue;
    }

    private void PlayCollectionEffects()
    {
        // Reproducir efecto de partículas
        if (collectionEffect != null)
        {
            GameObject effect = Instantiate(collectionEffect, transform.position, transform.rotation);

            // Destruir el efecto después de un tiempo si no se destruye automáticamente
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(effect, 2f); // Fallback
            }
        }

        // Reproducir sonido
        if (audioSource != null && collectionSound != null)
        {
            audioSource.PlayOneShot(collectionSound);
        }
    }

    IEnumerator DisableObject(float delay)
    {
        if (showDebugMessages)
            Debug.Log($"Desactivando objeto de bonus: {bonusName}");

        bonus_Mesh.SetActive(false);

        yield return new WaitForSeconds(delay);

        Destroy(gameObject);
    }

    #region Métodos de Configuración Pública

    /// <summary>
    /// Configura el bonus con nuevos valores
    /// </summary>
    public void ConfigureBonus(string name, int value, string description = "")
    {
        bonusName = name;
        bonusValue = value;
        bonusDescription = string.IsNullOrEmpty(description) ? $"Añade +{value} al resultado del dado" : description;

        if (showDebugMessages)
            Debug.Log($"Bonus reconfigurado: {bonusName} (+{bonusValue})");
    }

    /// <summary>
    /// Establece el icono del bonus
    /// </summary>
    public void SetBonusIcon(Sprite icon)
    {
        bonusIcon = icon;
    }

    /// <summary>
    /// Obtiene el valor actual del bonus
    /// </summary>
    public int GetBonusValue()
    {
        return bonusValue;
    }

    /// <summary>
    /// Obtiene el nombre del bonus
    /// </summary>
    public string GetBonusName()
    {
        return bonusName;
    }

    /// <summary>
    /// Verifica si el bonus ya ha sido recogido
    /// </summary>
    public bool HasBeenCollected()
    {
        return hasBeenCollected;
    }

    #endregion

    #region Métodos de Debug

    [ContextMenu("Test Collect Bonus")]
    public void TestCollectBonus()
    {
        if (Application.isPlaying)
        {
            CollectBonus();
        }
        else
        {
            Debug.Log($"Test: Se recogerá bonus {bonusName} (+{bonusValue})");
        }
    }

    [ContextMenu("Debug Bonus Info")]
    public void DebugBonusInfo()
    {
        Debug.Log($"=== DICE BONUS INFO ===");
        Debug.Log($"Nombre: {bonusName}");
        Debug.Log($"Valor: +{bonusValue}");
        Debug.Log($"Descripción: {bonusDescription}");
        Debug.Log($"Ya recogido: {hasBeenCollected}");
        Debug.Log($"Desactivar después: {disableAfterCollection}");
        Debug.Log($"BonusManager encontrado: {bonusManager != null}");
        Debug.Log($"======================");
    }

    #endregion

    #region Compatibilidad con Sistema Anterior

    /// <summary>
    /// Método de compatibilidad con el sistema anterior
    /// OBSOLETO: Usar CollectBonus() en su lugar
    /// </summary>
    [System.Obsolete("Use CollectBonus() instead")]
    public void ActivateBonus()
    {
        if (showDebugMessages)
            Debug.LogWarning("ActivateBonus() está obsoleto. Usando CollectBonus() en su lugar.");

        CollectBonus();
    }

    #endregion

    private void OnValidate()
    {
        // Validaciones en el editor
        if (bonusValue < 0)
        {
            Debug.LogWarning($"DiceBonus en {gameObject.name}: El valor del bonus no debería ser negativo");
        }

        if (string.IsNullOrEmpty(bonusName))
        {
            bonusName = $"Bonus +{bonusValue}";
        }
    }
}