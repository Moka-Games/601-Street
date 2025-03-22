using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MisionUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private TextMeshProUGUI textoNombreMision;
    [SerializeField] private TextMeshProUGUI textoDescripcionMision;

    [Header("Panel de la Misión")]
    [SerializeField] private GameObject panelMision;
    [SerializeField] private bool ocultarSiNoHayMision = true;

    [Header("Animaciones")]
    [SerializeField] private bool usarAnimaciones = true;
    [SerializeField] private Animator animatorPanel;
    [SerializeField] private string animacionMostrar = "Show";
    [SerializeField] private string animacionOcultar = "Hide";
    [SerializeField] private float duracionAnimacionOcultar = 2f; // Duración de la animación de ocultamiento

    // Variable para rastrear la misión asignada
    private Mision misionAsignada;
    private bool panelVisible = false;
    private Coroutine animacionCoroutine;

    private void Awake()
    {
        // Asegurarnos de que el panel esté oculto inicialmente
        if (panelMision != null)
        {
            panelMision.SetActive(false);
            panelVisible = false;
        }
    }

    private void OnEnable()
    {
        // Ya no nos suscribimos a eventos del MisionManager
        // Ahora esperamos que se nos asigne la misión directamente
    }

    private void OnDisable()
    {
        // Detener cualquier corrutina en progreso
        if (animacionCoroutine != null)
        {
            StopCoroutine(animacionCoroutine);
            animacionCoroutine = null;
        }
    }

    // Método para asignar una misión a este componente UI
    public void AsignarMision(Mision mision)
    {
        misionAsignada = mision;

        if (mision != null)
        {
            // Actualizar los textos con la información de la misión
            if (textoNombreMision != null)
            {
                textoNombreMision.text = mision.Nombre;
            }

            if (textoDescripcionMision != null)
            {
                textoDescripcionMision.text = mision.Descripcion;
            }
        }
        else if (ocultarSiNoHayMision)
        {
            // No hay misión, ocultar panel
            OcultarPanel();
        }
    }

    // Método para actualizar manualmente la UI
    public void ActualizarUI()
    {
        if (misionAsignada != null)
        {
            // Actualizar los textos con la información de la misión asignada
            if (textoNombreMision != null)
            {
                textoNombreMision.text = misionAsignada.Nombre;
            }

            if (textoDescripcionMision != null)
            {
                textoDescripcionMision.text = misionAsignada.Descripcion;
            }

            // Mostrar el panel si no está visible
            if (!panelVisible)
            {
                MostrarPanel();
            }
        }
        else if (ocultarSiNoHayMision)
        {
            // No hay misión asignada, ocultar panel
            OcultarPanel();
        }
    }

    // Mostrar el panel de misión (con o sin animación)
    public void MostrarPanel()
    {
        // Si ya estamos mostrando el panel o hay una animación en curso, cancelamos
        if (panelVisible || (animacionCoroutine != null && panelMision.activeSelf))
        {
            return;
        }

        // Detener cualquier animación previa si existe
        if (animacionCoroutine != null)
        {
            StopCoroutine(animacionCoroutine);
            animacionCoroutine = null;
        }

        if (panelMision != null)
        {
            if (usarAnimaciones && animatorPanel != null)
            {
                // Usar animación para mostrar
                panelMision.SetActive(true);
                animatorPanel.Play(animacionMostrar);

                // Iniciar corrutina para marcar cuando termina la animación
                animacionCoroutine = StartCoroutine(FinalizarAnimacionMostrar());
            }
            else
            {
                // Mostrar inmediatamente
                panelMision.SetActive(true);
                panelVisible = true;
            }
        }
    }

    // Ocultar el panel de misión (con o sin animación)
    public void OcultarPanel()
    {
        // Si el panel ya está oculto o hay una animación de ocultamiento en curso, salimos
        if (!panelVisible && (animacionCoroutine != null && !panelMision.activeSelf))
        {
            return;
        }

        // Detener cualquier animación previa si existe
        if (animacionCoroutine != null)
        {
            StopCoroutine(animacionCoroutine);
            animacionCoroutine = null;
        }

        if (panelMision != null)
        {
            if (usarAnimaciones && animatorPanel != null)
            {
                // Asegurarse de que el panel esté activo para poder animar
                if (!panelMision.activeSelf)
                {
                    panelMision.SetActive(true);
                }

                // Reproducir animación de ocultamiento
                animatorPanel.Play(animacionOcultar);

                // Iniciar corrutina para desactivar después de la animación
                animacionCoroutine = StartCoroutine(FinalizarAnimacionOcultar());
            }
            else
            {
                // Ocultar inmediatamente
                panelMision.SetActive(false);
                panelVisible = false;
            }
        }
    }

    // Corrutina para finalizar la animación de mostrar
    private IEnumerator FinalizarAnimacionMostrar()
    {
        // Esperar a que termine la animación
        if (animatorPanel != null)
        {
            yield return new WaitForSeconds(GetAnimationLength(animatorPanel, animacionMostrar));
        }
        else
        {
            yield return new WaitForSeconds(0.5f); // Valor por defecto
        }

        panelVisible = true;
        animacionCoroutine = null;
    }

    // Corrutina para finalizar la animación de ocultar
    private IEnumerator FinalizarAnimacionOcultar()
    {
        // Esperar el tiempo específico de la animación
        yield return new WaitForSeconds(duracionAnimacionOcultar);

        // Desactivar el panel
        if (panelMision != null)
        {
            panelMision.SetActive(false);
        }

        panelVisible = false;
        animacionCoroutine = null;
    }

    // Método para obtener la duración de una animación
    private float GetAnimationLength(Animator animator, string animName)
    {
        if (animator == null || string.IsNullOrEmpty(animName))
            return 0.5f;

        if (animator.runtimeAnimatorController == null)
            return 0.5f;

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        if (clips == null || clips.Length == 0)
            return 0.5f;

        foreach (AnimationClip clip in clips)
        {
            if (clip.name == animName)
            {
                return clip.length;
            }
        }

        return 0.5f;
    }

    // Métodos públicos para usar con botones en la UI

    public void CompletarMisionActual()
    {
        if (MisionManager.Instance != null && MisionManager.Instance.TieneMisionActiva)
        {
            MisionManager.Instance.CompletarMisionActual();
        }
    }

    public void CancelarMisionActual()
    {
        if (MisionManager.Instance != null && MisionManager.Instance.TieneMisionActiva)
        {
            MisionManager.Instance.CancelarMisionActual();
        }
    }
}