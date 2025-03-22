using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MisionUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private TextMeshProUGUI textoNombreMision;
    [SerializeField] private TextMeshProUGUI textoDescripcionMision;

    [Header("Panel de la Misi�n")]
    [SerializeField] private GameObject panelMision;
    [SerializeField] private bool ocultarSiNoHayMision = true;

    [Header("Animaciones")]
    [SerializeField] private bool usarAnimaciones = true;
    [SerializeField] private Animator animatorPanel;
    [SerializeField] private string animacionMostrar = "Show";
    [SerializeField] private string animacionOcultar = "Hide";
    [SerializeField] private float duracionAnimacionOcultar = 2f; // Duraci�n de la animaci�n de ocultamiento

    // Variable para rastrear la misi�n asignada
    private Mision misionAsignada;
    private bool panelVisible = false;
    private Coroutine animacionCoroutine;

    private void Awake()
    {
        // Asegurarnos de que el panel est� oculto inicialmente
        if (panelMision != null)
        {
            panelMision.SetActive(false);
            panelVisible = false;
        }
    }

    private void OnEnable()
    {
        // Ya no nos suscribimos a eventos del MisionManager
        // Ahora esperamos que se nos asigne la misi�n directamente
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

    // M�todo para asignar una misi�n a este componente UI
    public void AsignarMision(Mision mision)
    {
        misionAsignada = mision;

        if (mision != null)
        {
            // Actualizar los textos con la informaci�n de la misi�n
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
            // No hay misi�n, ocultar panel
            OcultarPanel();
        }
    }

    // M�todo para actualizar manualmente la UI
    public void ActualizarUI()
    {
        if (misionAsignada != null)
        {
            // Actualizar los textos con la informaci�n de la misi�n asignada
            if (textoNombreMision != null)
            {
                textoNombreMision.text = misionAsignada.Nombre;
            }

            if (textoDescripcionMision != null)
            {
                textoDescripcionMision.text = misionAsignada.Descripcion;
            }

            // Mostrar el panel si no est� visible
            if (!panelVisible)
            {
                MostrarPanel();
            }
        }
        else if (ocultarSiNoHayMision)
        {
            // No hay misi�n asignada, ocultar panel
            OcultarPanel();
        }
    }

    // Mostrar el panel de misi�n (con o sin animaci�n)
    public void MostrarPanel()
    {
        // Si ya estamos mostrando el panel o hay una animaci�n en curso, cancelamos
        if (panelVisible || (animacionCoroutine != null && panelMision.activeSelf))
        {
            return;
        }

        // Detener cualquier animaci�n previa si existe
        if (animacionCoroutine != null)
        {
            StopCoroutine(animacionCoroutine);
            animacionCoroutine = null;
        }

        if (panelMision != null)
        {
            if (usarAnimaciones && animatorPanel != null)
            {
                // Usar animaci�n para mostrar
                panelMision.SetActive(true);
                animatorPanel.Play(animacionMostrar);

                // Iniciar corrutina para marcar cuando termina la animaci�n
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

    // Ocultar el panel de misi�n (con o sin animaci�n)
    public void OcultarPanel()
    {
        // Si el panel ya est� oculto o hay una animaci�n de ocultamiento en curso, salimos
        if (!panelVisible && (animacionCoroutine != null && !panelMision.activeSelf))
        {
            return;
        }

        // Detener cualquier animaci�n previa si existe
        if (animacionCoroutine != null)
        {
            StopCoroutine(animacionCoroutine);
            animacionCoroutine = null;
        }

        if (panelMision != null)
        {
            if (usarAnimaciones && animatorPanel != null)
            {
                // Asegurarse de que el panel est� activo para poder animar
                if (!panelMision.activeSelf)
                {
                    panelMision.SetActive(true);
                }

                // Reproducir animaci�n de ocultamiento
                animatorPanel.Play(animacionOcultar);

                // Iniciar corrutina para desactivar despu�s de la animaci�n
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

    // Corrutina para finalizar la animaci�n de mostrar
    private IEnumerator FinalizarAnimacionMostrar()
    {
        // Esperar a que termine la animaci�n
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

    // Corrutina para finalizar la animaci�n de ocultar
    private IEnumerator FinalizarAnimacionOcultar()
    {
        // Esperar el tiempo espec�fico de la animaci�n
        yield return new WaitForSeconds(duracionAnimacionOcultar);

        // Desactivar el panel
        if (panelMision != null)
        {
            panelMision.SetActive(false);
        }

        panelVisible = false;
        animacionCoroutine = null;
    }

    // M�todo para obtener la duraci�n de una animaci�n
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

    // M�todos p�blicos para usar con botones en la UI

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