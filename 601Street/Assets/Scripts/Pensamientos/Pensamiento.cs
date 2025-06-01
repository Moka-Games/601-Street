using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Componente simplificado de Pensamiento que se activa automáticamente al iniciarse
/// </summary>
public class Pensamiento : MonoBehaviour
{
    [Header("Configuración del Pensamiento")]
    [TextArea(3, 6)]
    [Tooltip("Texto del pensamiento que se mostrará")]
    public string textoPensamiento;

    [Header("Configuración de Activación")]
    [Tooltip("Si está marcado, el pensamiento se activará automáticamente en el Start")]
    public bool activarEnStart = true;

    [Tooltip("Delay en segundos antes de mostrar el pensamiento (0 = inmediato)")]
    public float delayAntesMostrar = 0f;

    [Tooltip("Tiempo personalizado para mostrar este pensamiento (0 = usar tiempo por defecto del manager)")]
    public float tiempoMostrar = 0f;

    // Estado interno
    private bool yaActivado = false;

    public UnityEvent OnPensamientoMostrado;

    private void Start()
    {
        // Validar configuración
        if (string.IsNullOrEmpty(textoPensamiento))
        {
            Debug.LogError($"Pensamiento '{gameObject.name}': No se ha configurado el texto del pensamiento");
            return;
        }

        // Si está configurado para activación automática, activar
        if (activarEnStart)
        {
            if (delayAntesMostrar > 0)
            {
                // Activar con delay
                Invoke(nameof(ActivarPensamiento), delayAntesMostrar);
            }
            else
            {
                // Activar inmediatamente
                ActivarPensamiento();
            }
        }
    }

    /// <summary>
    /// Activa el pensamiento (puede ser llamado manualmente o automáticamente)
    /// </summary>
    public void ActivarPensamiento()
    {
        // Verificar si ya se activó
        if (yaActivado)
        {
            Debug.Log($"Pensamiento '{gameObject.name}' ya fue activado anteriormente");
            return;
        }

        // Verificar que hay texto configurado
        if (string.IsNullOrEmpty(textoPensamiento))
        {
            Debug.LogError($"Pensamiento '{gameObject.name}': No se puede activar, no hay texto configurado");
            return;
        }

        // Verificar que existe el manager
        if (Pensamientos_Manager.Instance == null)
        {
            Debug.LogError($"Pensamiento '{gameObject.name}': No se encontró Pensamientos_Manager en la escena");
            return;
        }

        // Configurar tiempo personalizado si está especificado
        if (tiempoMostrar > 0)
        {
            float tiempoOriginal = Pensamientos_Manager.Instance.showThoughtFor;
            Pensamientos_Manager.Instance.SetShowTime(tiempoMostrar);

            // Mostrar el pensamiento
            Pensamientos_Manager.Instance.MostrarPensamiento(textoPensamiento);

            // Restaurar el tiempo original y destruir después de mostrar
            StartCoroutine(RestaurarTiempoOriginalYDestruir(tiempoOriginal, tiempoMostrar));
        }
        else
        {
            // Usar tiempo por defecto y destruir después de mostrar
            Pensamientos_Manager.Instance.MostrarPensamiento(textoPensamiento);
            StartCoroutine(DestruirDespuesDeMostrar(Pensamientos_Manager.Instance.showThoughtFor));
        }

        // Marcar como activado
        yaActivado = true;

        Debug.Log($"Pensamiento '{gameObject.name}' activado: \"{textoPensamiento}\"");
    }

    /// <summary>
    /// Restaura el tiempo original del manager y destruye el objeto después de mostrar el pensamiento
    /// </summary>
    private System.Collections.IEnumerator RestaurarTiempoOriginalYDestruir(float tiempoOriginal, float tiempoEspera)
    {
        yield return new WaitForSeconds(tiempoEspera + 0.1f); // Esperar un poco más para asegurar

        if (Pensamientos_Manager.Instance != null)
        {
            Pensamientos_Manager.Instance.SetShowTime(tiempoOriginal);
        }

        OnPensamientoMostrado.Invoke();
        // Destruir el objeto
        DestruirObjeto();
    }

    /// <summary>
    /// Destruye el objeto después de que se haya mostrado el pensamiento
    /// </summary>
    private System.Collections.IEnumerator DestruirDespuesDeMostrar(float tiempoEspera)
    {
        yield return new WaitForSeconds(tiempoEspera + 0.1f); // Esperar un poco más para asegurar
        OnPensamientoMostrado.Invoke();
        // Destruir el objeto
        DestruirObjeto();
    }

    /// <summary>
    /// Destruye el GameObject de forma segura
    /// </summary>
    private void DestruirObjeto()
    {
        Debug.Log($"Pensamiento '{gameObject.name}' completado - Destruyendo objeto");
        Destroy(gameObject);
    }

    /// <summary>
    /// Reinicia el estado del pensamiento para que pueda activarse de nuevo
    /// NOTA: Este método ya no es útil ya que el objeto se destruye después de activarse
    /// </summary>
    [System.Obsolete("Este método ya no es necesario ya que el objeto se destruye después de mostrar el pensamiento")]
    public void ReiniciarPensamiento()
    {
        yaActivado = false;
        this.enabled = true;
        Debug.Log($"Pensamiento '{gameObject.name}' reiniciado");
    }

    /// <summary>
    /// Verifica si este pensamiento ya fue activado
    /// </summary>
    public bool FueActivado()
    {
        return yaActivado;
    }

    /// <summary>
    /// Cambia el texto del pensamiento en tiempo de ejecución
    /// </summary>
    public void CambiarTexto(string nuevoTexto)
    {
        textoPensamiento = nuevoTexto;
        Debug.Log($"Pensamiento '{gameObject.name}': Texto cambiado a \"{nuevoTexto}\"");
    }

    // Métodos de debug para el inspector
    [ContextMenu("Activar Pensamiento (Test)")]
    public void TestActivarPensamiento()
    {
        if (Application.isPlaying)
        {
            ActivarPensamiento();
        }
        else
        {
            Debug.LogWarning("Este test solo funciona en Play Mode");
        }
    }
}