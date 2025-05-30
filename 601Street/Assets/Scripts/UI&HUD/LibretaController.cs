using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LibretaController : MonoBehaviour
{
    [SerializeField] private List<GameObject> paginas;
    [SerializeField] private Button botonIzquierda;
    [SerializeField] private Button botonDerecha;
    [SerializeField] private float tiempoCooldown = 0.5f; // Tiempo de espera en segundos

    private int paginaActual = 0;
    private bool enCooldown = false;

    private void Start()
    {
        // Verificar que tenemos todas las referencias necesarias
        if (paginas == null || paginas.Count == 0)
        {
            Debug.LogError("No hay páginas asignadas a la libreta");
            return;
        }
        if (botonIzquierda == null || botonDerecha == null)
        {
            Debug.LogError("Falta asignar uno o ambos botones de navegación");
            return;
        }

        // Limpiar y volver a asignar los eventos de los botones para evitar duplicados
        botonIzquierda.onClick.RemoveAllListeners();
        botonDerecha.onClick.RemoveAllListeners();
        botonIzquierda.onClick.AddListener(PaginaAnterior);
        botonDerecha.onClick.AddListener(PaginaSiguiente);

        Debug.Log($"Libreta inicializada con {paginas.Count} páginas");

        // Asegurarse de que todas las páginas estén desactivadas inicialmente
        for (int i = 0; i < paginas.Count; i++)
        {
            if (paginas[i] != null)
            {
                paginas[i].SetActive(false);
                Debug.Log($"Página {i} inicializada y desactivada: {paginas[i].name}");
            }
            else
            {
                Debug.LogError($"La página en el índice {i} es nula");
            }
        }

        // Mostrar la primera página
        MostrarPaginaActual();
    }

    public void PaginaAnterior()
    {
        // Verificar si estamos en cooldown
        if (enCooldown)
        {
            Debug.Log("Botón en cooldown, ignorando clic");
            return;
        }

        Debug.Log($"Botón Izquierda presionado. Página actual antes: {paginaActual}");

        if (paginaActual > 0)
        {
            paginaActual--;
            Debug.Log($"Cambiando a página anterior: {paginaActual}");
            MostrarPaginaActual();
            IniciarCooldown();
        }
        else
        {
            Debug.Log("Ya estamos en la primera página, no se puede retroceder más");
        }
    }

    public void PaginaSiguiente()
    {
        // Verificar si estamos en cooldown
        if (enCooldown)
        {
            Debug.Log("Botón en cooldown, ignorando clic");
            return;
        }

        Debug.Log($"Botón Derecha presionado. Página actual antes: {paginaActual}");

        if (paginaActual < paginas.Count - 1)
        {
            paginaActual++;
            Debug.Log($"Cambiando a página siguiente: {paginaActual}");
            MostrarPaginaActual();
            IniciarCooldown();
        }
        else
        {
            Debug.Log("Ya estamos en la última página, no se puede avanzar más");
        }
    }

    private void IniciarCooldown()
    {
        if (!enCooldown)
        {
            StartCoroutine(CooldownCoroutine());
        }
    }

    private IEnumerator CooldownCoroutine()
    {
        enCooldown = true;
        Debug.Log($"Iniciando cooldown de {tiempoCooldown} segundos");

        // Opcional: Cambiar la apariencia de los botones durante el cooldown
        Color colorOriginal = botonIzquierda.image.color;
        Color colorCooldown = new Color(colorOriginal.r, colorOriginal.g, colorOriginal.b, 0.5f);

        botonIzquierda.image.color = colorCooldown;
        botonDerecha.image.color = colorCooldown;

        yield return new WaitForSeconds(tiempoCooldown);

        // Restaurar color original
        botonIzquierda.image.color = colorOriginal;
        botonDerecha.image.color = colorOriginal;

        enCooldown = false;
        Debug.Log("Cooldown terminado");
    }

    private void MostrarPaginaActual()
    {
        // Verificar que el índice es válido
        if (paginaActual < 0 || paginaActual >= paginas.Count)
        {
            Debug.LogError($"Índice de página inválido: {paginaActual}");
            return;
        }

        // Ocultar todas las páginas
        for (int i = 0; i < paginas.Count; i++)
        {
            if (paginas[i] != null)
            {
                bool shouldBeActive = (i == paginaActual);
                paginas[i].SetActive(shouldBeActive);
                Debug.Log($"Página {i} ({paginas[i].name}): {(shouldBeActive ? "activada" : "desactivada")}");
            }
        }

        // Actualizar estado de los botones
        ActualizarBotones();
    }

    private void ActualizarBotones()
    {
        bool mostrarBotonIzquierda = (paginaActual > 0);
        bool mostrarBotonDerecha = (paginaActual < paginas.Count - 1);

        botonIzquierda.gameObject.SetActive(mostrarBotonIzquierda);
        botonDerecha.gameObject.SetActive(mostrarBotonDerecha);

        Debug.Log($"Botón Izquierda: {(mostrarBotonIzquierda ? "visible" : "oculto")}, " +
                  $"Botón Derecha: {(mostrarBotonDerecha ? "visible" : "oculto")}");
    }
}