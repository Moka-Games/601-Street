using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LibretaController : MonoBehaviour
{
    [SerializeField] private List<GameObject> paginas;
    [SerializeField] private Button botonIzquierda;
    [SerializeField] private Button botonDerecha;

    private int paginaActual = 0;

    private void Start()
    {
        // Verificar que tenemos todas las referencias necesarias
        if (paginas == null || paginas.Count == 0)
        {
            Debug.LogError("No hay p�ginas asignadas a la libreta");
            return;
        }

        if (botonIzquierda == null || botonDerecha == null)
        {
            Debug.LogError("Falta asignar uno o ambos botones de navegaci�n");
            return;
        }

        // Limpiar y volver a asignar los eventos de los botones para evitar duplicados
        botonIzquierda.onClick.RemoveAllListeners();
        botonDerecha.onClick.RemoveAllListeners();

        botonIzquierda.onClick.AddListener(PaginaAnterior);
        botonDerecha.onClick.AddListener(PaginaSiguiente);

        Debug.Log($"Libreta inicializada con {paginas.Count} p�ginas");

        // Asegurarse de que todas las p�ginas est�n desactivadas inicialmente
        for (int i = 0; i < paginas.Count; i++)
        {
            if (paginas[i] != null)
            {
                paginas[i].SetActive(false);
                Debug.Log($"P�gina {i} inicializada y desactivada: {paginas[i].name}");
            }
            else
            {
                Debug.LogError($"La p�gina en el �ndice {i} es nula");
            }
        }

        // Mostrar la primera p�gina
        MostrarPaginaActual();
    }

    public void PaginaAnterior()
    {
        Debug.Log($"Bot�n Izquierda presionado. P�gina actual antes: {paginaActual}");

        if (paginaActual > 0)
        {
            paginaActual--;
            Debug.Log($"Cambiando a p�gina anterior: {paginaActual}");
            MostrarPaginaActual();
        }
        else
        {
            Debug.Log("Ya estamos en la primera p�gina, no se puede retroceder m�s");
        }
    }

    public void PaginaSiguiente()
    {
        Debug.Log($"Bot�n Derecha presionado. P�gina actual antes: {paginaActual}");

        if (paginaActual < paginas.Count - 1)
        {
            paginaActual++;
            Debug.Log($"Cambiando a p�gina siguiente: {paginaActual}");
            MostrarPaginaActual();
        }
        else
        {
            Debug.Log("Ya estamos en la �ltima p�gina, no se puede avanzar m�s");
        }
    }

    private void MostrarPaginaActual()
    {
        // Verificar que el �ndice es v�lido
        if (paginaActual < 0 || paginaActual >= paginas.Count)
        {
            Debug.LogError($"�ndice de p�gina inv�lido: {paginaActual}");
            return;
        }

        // Ocultar todas las p�ginas
        for (int i = 0; i < paginas.Count; i++)
        {
            if (paginas[i] != null)
            {
                bool shouldBeActive = (i == paginaActual);
                paginas[i].SetActive(shouldBeActive);
                Debug.Log($"P�gina {i} ({paginas[i].name}): {(shouldBeActive ? "activada" : "desactivada")}");
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

        Debug.Log($"Bot�n Izquierda: {(mostrarBotonIzquierda ? "visible" : "oculto")}, " +
                  $"Bot�n Derecha: {(mostrarBotonDerecha ? "visible" : "oculto")}");
    }
}