using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Sistema simplificado de pensamientos - solo muestra pensamientos cuando se activan
/// </summary>
public class Pensamientos_Manager : MonoBehaviour
{
    public static Pensamientos_Manager Instance;

    [Header("UI del Pensamiento")]
    public GameObject pensamientoUI;
    public TMP_Text pensamientoText;

    [Header("Configuración")]
    public float showThoughtFor = 3f; // Tiempo que se muestra el pensamiento

    private bool isShowingThought = false;
    private Coroutine currentThoughtCoroutine;

    private void Awake()
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
    }

    private void Start()
    {
        // Asegurar que el UI esté desactivado al inicio
        if (pensamientoUI != null)
        {
            pensamientoUI.SetActive(false);
        }
    }

    /// <summary>
    /// Muestra un pensamiento en la interfaz
    /// </summary>
    /// <param name="texto">Texto del pensamiento a mostrar</param>
    public void MostrarPensamiento(string texto)
    {
        if (string.IsNullOrEmpty(texto))
        {
            Debug.LogWarning("Intentando mostrar un pensamiento vacío");
            return;
        }

        // Si ya hay un pensamiento mostrándose, detener la corrutina anterior
        if (currentThoughtCoroutine != null)
        {
            StopCoroutine(currentThoughtCoroutine);
        }

        // Mostrar el nuevo pensamiento
        currentThoughtCoroutine = StartCoroutine(DisplayThoughtCoroutine(texto));
    }

    /// <summary>
    /// Corrutina que maneja la visualización del pensamiento
    /// </summary>
    private IEnumerator DisplayThoughtCoroutine(string texto)
    {
        isShowingThought = true;

        // Configurar y activar la UI
        if (pensamientoText != null)
        {
            pensamientoText.text = texto;
        }

        if (pensamientoUI != null)
        {
            pensamientoUI.SetActive(true);
        }

        Debug.Log($"Mostrando pensamiento: \"{texto}\"");

        // Esperar el tiempo configurado
        yield return new WaitForSeconds(showThoughtFor);

        // Desactivar la UI
        if (pensamientoUI != null)
        {
            pensamientoUI.SetActive(false);
        }

        isShowingThought = false;
        currentThoughtCoroutine = null;

        Debug.Log("Pensamiento ocultado");
    }

    /// <summary>
    /// Oculta inmediatamente el pensamiento actual si hay uno mostrándose
    /// </summary>
    public void OcultarPensamiento()
    {
        if (currentThoughtCoroutine != null)
        {
            StopCoroutine(currentThoughtCoroutine);
            currentThoughtCoroutine = null;
        }

        if (pensamientoUI != null)
        {
            pensamientoUI.SetActive(false);
        }

        isShowingThought = false;
        Debug.Log("Pensamiento ocultado manualmente");
    }

    /// <summary>
    /// Verifica si actualmente se está mostrando un pensamiento
    /// </summary>
    public bool IsShowingThought()
    {
        return isShowingThought;
    }

    /// <summary>
    /// Método público para configurar el tiempo de visualización
    /// </summary>
    public void SetShowTime(float newTime)
    {
        showThoughtFor = newTime;
    }

    private void OnDestroy()
    {
        // Limpiar corrutinas al destruir
        if (currentThoughtCoroutine != null)
        {
            StopCoroutine(currentThoughtCoroutine);
        }
    }
}