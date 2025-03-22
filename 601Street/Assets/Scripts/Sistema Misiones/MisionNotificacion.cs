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

    [Header("Configuraci�n")]
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
            // Esto lo haremos din�micamente cuando cambien las misiones
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

    // Evento cuando cambia la misi�n
    private void OnMisionCambiada(Mision mision)
    {
        if (mision != null && mostrarNotificacionNuevaMision)
        {
            // Mostrar notificaci�n de nueva misi�n
            MostrarNotificacion($"Nueva misi�n: {mision.Nombre}", iconoNuevaMision);

            // Si es una misi�n con objetivos, nos suscribimos a sus eventos
            if (mision is MisionConObjetivos misionConObjetivos)
            {
                misionConObjetivos.OnObjetivoCompletado += OnObjetivoCompletado;
            }
        }
    }

    // Evento cuando se completa una misi�n
    private void OnMisionCompletada(Mision mision)
    {
        if (mostrarNotificacionMisionCompletada)
        {
            MostrarNotificacion($"Misi�n completada: {mision.Nombre}", iconoMisionCompletada);
        }

        // Nos desuscribimos de los eventos de objetivos si era una misi�n con objetivos
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

    // M�todo principal para mostrar notificaciones
    public void MostrarNotificacion(string mensaje, Sprite icono = null)
    {
        // Detener notificaci�n anterior si existe
        if (notificacionActiva != null)
        {
            StopCoroutine(notificacionActiva);
        }

        // Iniciar nueva notificaci�n
        notificacionActiva = StartCoroutine(MostrarNotificacionCoroutine(mensaje, icono));
    }

    // Corrutina para mostrar notificaci�n
    private IEnumerator MostrarNotificacionCoroutine(string mensaje, Sprite icono)
    {
        // Configurar la notificaci�n
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

            // Duraci�n de la animaci�n + tiempo de visualizaci�n
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

    // Obtener duraci�n de una animaci�n
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