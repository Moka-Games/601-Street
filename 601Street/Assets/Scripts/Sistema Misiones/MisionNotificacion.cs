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

        // Suscribirse a eventos
        if (MisionManager.Instance != null)
        {
            MisionManager.Instance.OnMisionCambiada += OnMisionCambiada;
            MisionManager.Instance.OnMisionCompletada += OnMisionCompletada;

            // Suscribirse a eventos de objetivos completados si hay misiones con objetivos
            // Esto lo haremos dinámicamente cuando cambien las misiones
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
            MostrarNotificacion($"Objetivo completado: {objetivo.descripcion}", iconoObjetivoCompletado);
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
}