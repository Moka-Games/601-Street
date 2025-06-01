using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MisionNotificacion : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private GameObject panelNotificacion;
    [SerializeField] private TextMeshProUGUI textoNotificacion;
    [SerializeField] private Image iconoNotificacion;

    [Header("Configuración")]
    [SerializeField] private float duracionNotificacion = 3f;
    [SerializeField] private bool mostrarNotificacionNuevaMision = true;
    [SerializeField] private bool mostrarNotificacionMisionCompletada = true;
    [SerializeField] private bool mostrarNotificacionObjetivoCompletado = true;

    [Header("Iconos")]
    [SerializeField] private Sprite iconoNuevaMision;
    [SerializeField] private Sprite iconoMisionCompletada;
    [SerializeField] private Sprite iconoObjetivoCompletado;

    [Header("Sistema de Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private bool reproducirSonidos = true;

    [Header("Sonidos de Misiones")]
    [SerializeField] private AudioClip sonidoNuevaMision;
    [SerializeField] private AudioClip sonidoMisionCompletada;
    [SerializeField] private AudioClip sonidoObjetivoCompletado;

    [Header("Configuración de Audio")]
    [Range(0f, 1f)]
    [SerializeField] private float volumenSonidos = 1f;
    [SerializeField] private bool usarAudioSourceDedicado = true;

    [Header("Animaciones (Opcional)")]
    [SerializeField] private bool usarAnimaciones = false;
    [SerializeField] private Animator animatorNotificacion;
    [SerializeField] private string animacionMostrar = "ShowNotification";
    [SerializeField] private string animacionOcultar = "HideNotification";

    private Coroutine notificacionActiva;

    private void Start()
    {
        // Inicializar
        if (panelNotificacion != null)
        {
            panelNotificacion.SetActive(false);
        }

        // Configurar AudioSource si no está asignado
        ConfigurarAudioSource();

        // Suscribirse a eventos
        if (MisionManager.Instance != null)
        {
            MisionManager.Instance.OnMisionCambiada += OnMisionCambiada;
            MisionManager.Instance.OnMisionCompletada += OnMisionCompletada;
        }
    }

    private void ConfigurarAudioSource()
    {
        if (audioSource == null && usarAudioSourceDedicado)
        {
            // Crear un AudioSource dedicado para las notificaciones
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = volumenSonidos;

            Debug.Log("AudioSource creado automáticamente para MisionNotificacion");
        }
        else if (audioSource == null && !usarAudioSourceDedicado)
        {
            // Buscar un AudioSource en el objeto o sus padres
            audioSource = GetComponentInParent<AudioSource>();

            if (audioSource == null)
            {
                Debug.LogWarning("No se encontró AudioSource. Los sonidos de misiones no se reproducirán.");
            }
        }

        // Configurar volumen
        if (audioSource != null)
        {
            audioSource.volume = volumenSonidos;
        }
    }

    private void OnDestroy()
    {
        // Desuscribirse para evitar memory leaks
        if (MisionManager.Instance != null)
        {
            MisionManager.Instance.OnMisionCambiada -= OnMisionCambiada;
            MisionManager.Instance.OnMisionCompletada -= OnMisionCompletada;
        }

        // Asegurar que se detengan todas las corrutinas
        if (notificacionActiva != null)
        {
            StopCoroutine(notificacionActiva);
        }
    }

    // Evento cuando cambia la misión
    private void OnMisionCambiada(Mision mision)
    {
        if (mision != null && mostrarNotificacionNuevaMision)
        {
            // Reproducir sonido de nueva misión
            ReproducirSonido(sonidoNuevaMision);

            // Mostrar notificación de nueva misión
            MostrarNotificacion($"Nueva misión: {mision.Nombre}", iconoNuevaMision);

            // Si es una misión con objetivos, nos suscribimos a sus eventos
            if (mision is MisionConObjetivos misionConObjetivos)
            {
                misionConObjetivos.OnObjetivoCompletado += OnObjetivoCompletado;
            }
        }
    }

    // Evento cuando se completa una misión
    private void OnMisionCompletada(Mision mision)
    {
        if (mostrarNotificacionMisionCompletada)
        {
            // Reproducir sonido de misión completada
            ReproducirSonido(sonidoMisionCompletada);

            MostrarNotificacion($"Misión completada: {mision.Nombre}", iconoMisionCompletada);
        }

        // Nos desuscribimos de los eventos de objetivos si era una misión con objetivos
        if (mision is MisionConObjetivos misionConObjetivos)
        {
            misionConObjetivos.OnObjetivoCompletado -= OnObjetivoCompletado;
        }
    }

    // Evento cuando se completa un objetivo
    private void OnObjetivoCompletado(MisionConObjetivos.ObjetivoMision objetivo)
    {
        if (mostrarNotificacionObjetivoCompletado)
        {
            // Reproducir sonido de objetivo completado
            ReproducirSonido(sonidoObjetivoCompletado);

            MostrarNotificacion($"Objetivo completado: {objetivo.descripcion}", iconoObjetivoCompletado);
        }
    }

    /// <summary>
    /// Reproduce un sonido específico
    /// </summary>
    private void ReproducirSonido(AudioClip clip)
    {
        if (!reproducirSonidos || audioSource == null || clip == null)
            return;

        try
        {
            // Usar PlayOneShot para permitir múltiples sonidos superpuestos
            audioSource.PlayOneShot(clip, volumenSonidos);

            Debug.Log($"Reproduciendo sonido de misión: {clip.name}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error al reproducir sonido de misión: {e.Message}");
        }
    }

    // Método principal para mostrar notificaciones
    public void MostrarNotificacion(string mensaje, Sprite icono = null)
    {
        // Detener notificación anterior si existe
        if (notificacionActiva != null)
        {
            StopCoroutine(notificacionActiva);
        }

        // Iniciar nueva notificación
        notificacionActiva = StartCoroutine(MostrarNotificacionCoroutine(mensaje, icono));
    }

    /// <summary>
    /// Método público para reproducir sonidos personalizados
    /// </summary>
    public void ReproducirSonidoPersonalizado(AudioClip clip, float volumen = -1f)
    {
        if (clip == null || audioSource == null)
            return;

        float vol = volumen >= 0f ? volumen : volumenSonidos;
        audioSource.PlayOneShot(clip, vol);
    }

    // Corrutina para mostrar notificación
    private IEnumerator MostrarNotificacionCoroutine(string mensaje, Sprite icono)
    {
        // Configurar la notificación
        if (textoNotificacion != null)
        {
            textoNotificacion.text = mensaje;
        }

        if (iconoNotificacion != null && icono != null)
        {
            iconoNotificacion.sprite = icono;
            iconoNotificacion.gameObject.SetActive(true);
        }
        else if (iconoNotificacion != null)
        {
            iconoNotificacion.gameObject.SetActive(false);
        }

        // Mostrar panel
        if (usarAnimaciones && animatorNotificacion != null)
        {
            panelNotificacion.SetActive(true);
            animatorNotificacion.Play(animacionMostrar);

            // Duración de la animación + tiempo de visualización
            float duracionAnimacion = GetAnimationLength(animatorNotificacion, animacionMostrar);
            yield return new WaitForSeconds(duracionAnimacion + duracionNotificacion);

            // Animar ocultar
            animatorNotificacion.Play(animacionOcultar);
            yield return new WaitForSeconds(GetAnimationLength(animatorNotificacion, animacionOcultar));

            panelNotificacion.SetActive(false);
        }
        else
        {
            // Sin animaciones
            panelNotificacion.SetActive(true);
            yield return new WaitForSeconds(duracionNotificacion);
            panelNotificacion.SetActive(false);
        }

        notificacionActiva = null;
    }

    // Obtener duración de una animación
    private float GetAnimationLength(Animator animator, string animName)
    {
        if (animator == null || string.IsNullOrEmpty(animName))
            return 0f;

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips)
        {
            if (clip.name == animName)
            {
                return clip.length;
            }
        }

        return 0.5f;
    }

    #region Métodos de Configuración Pública

    /// <summary>
    /// Habilita o deshabilita la reproducción de sonidos
    /// </summary>
    public void SetReproducirSonidos(bool activar)
    {
        reproducirSonidos = activar;
    }

    /// <summary>
    /// Cambia el volumen de los sonidos de misiones
    /// </summary>
    public void SetVolumenSonidos(float volumen)
    {
        volumenSonidos = Mathf.Clamp01(volumen);

        if (audioSource != null)
        {
            audioSource.volume = volumenSonidos;
        }
    }

    /// <summary>
    /// Cambia el AudioClip para nuevas misiones
    /// </summary>
    public void SetSonidoNuevaMision(AudioClip nuevoSonido)
    {
        sonidoNuevaMision = nuevoSonido;
    }

    /// <summary>
    /// Cambia el AudioClip para misiones completadas
    /// </summary>
    public void SetSonidoMisionCompletada(AudioClip nuevoSonido)
    {
        sonidoMisionCompletada = nuevoSonido;
    }

    /// <summary>
    /// Cambia el AudioClip para objetivos completados
    /// </summary>
    public void SetSonidoObjetivoCompletado(AudioClip nuevoSonido)
    {
        sonidoObjetivoCompletado = nuevoSonido;
    }

    #endregion

    #region Métodos de Debug

    [ContextMenu("Test Sonido Nueva Misión")]
    private void TestSonidoNuevaMision()
    {
        ReproducirSonido(sonidoNuevaMision);
    }

    [ContextMenu("Test Sonido Misión Completada")]
    private void TestSonidoMisionCompletada()
    {
        ReproducirSonido(sonidoMisionCompletada);
    }

    [ContextMenu("Test Sonido Objetivo Completado")]
    private void TestSonidoObjetivoCompletado()
    {
        ReproducirSonido(sonidoObjetivoCompletado);
    }

    #endregion
}