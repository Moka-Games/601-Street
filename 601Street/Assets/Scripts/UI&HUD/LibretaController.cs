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

    // NUEVO: Variables para manejar el cooldown sin coroutines
    private float tiempoUltimoClic = 0f;
    private Color colorOriginalIzquierda;
    private Color colorOriginalDerecha;
    private bool coloresGuardados = false;

    private void Start()
    {
        InicializarLibreta();
    }

    private void OnEnable()
    {
        // NUEVO: Re-inicializar cuando el objeto se reactive
        if (paginas != null && paginas.Count > 0)
        {
            InicializarLibreta();
        }
    }

    private void InicializarLibreta()
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

        // NUEVO: Guardar colores originales si no se han guardado
        if (!coloresGuardados && botonIzquierda.image != null && botonDerecha.image != null)
        {
            colorOriginalIzquierda = botonIzquierda.image.color;
            colorOriginalDerecha = botonDerecha.image.color;
            coloresGuardados = true;
        }

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

        // Resetear estado del cooldown al inicializar
        enCooldown = false;
        tiempoUltimoClic = 0f;

        // Mostrar la primera página
        MostrarPaginaActual();
    }

    private void Update()
    {
        // NUEVO: Manejar el cooldown visual sin coroutines
        if (enCooldown)
        {
            // Verificar si el cooldown ha terminado
            if (Time.time - tiempoUltimoClic >= tiempoCooldown)
            {
                TerminarCooldown();
            }
            else
            {
                // Actualizar el color de los botones durante el cooldown
                ActualizarColorCooldown();
            }
        }
    }

    public void PaginaAnterior()
    {
        // Verificar si estamos en cooldown
        if (VerificarCooldown())
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
            IniciarCooldownSinCoroutine();
        }
        else
        {
            Debug.Log("Ya estamos en la primera página, no se puede retroceder más");
        }
    }

    public void PaginaSiguiente()
    {
        // Verificar si estamos en cooldown
        if (VerificarCooldown())
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
            IniciarCooldownSinCoroutine();
        }
        else
        {
            Debug.Log("Ya estamos en la última página, no se puede avanzar más");
        }
    }

    // NUEVO: Verificar cooldown sin usar coroutines
    private bool VerificarCooldown()
    {
        if (enCooldown && Time.time - tiempoUltimoClic < tiempoCooldown)
        {
            return true;
        }

        // Si el tiempo ha pasado, terminar el cooldown automáticamente
        if (enCooldown && Time.time - tiempoUltimoClic >= tiempoCooldown)
        {
            TerminarCooldown();
        }

        return false;
    }

    // NUEVO: Iniciar cooldown sin coroutines
    private void IniciarCooldownSinCoroutine()
    {
        enCooldown = true;
        tiempoUltimoClic = Time.time;

        Debug.Log($"Iniciando cooldown de {tiempoCooldown} segundos (sin coroutine)");

        // Aplicar efecto visual inicial
        ActualizarColorCooldown();
    }

    // NUEVO: Actualizar el color durante el cooldown
    private void ActualizarColorCooldown()
    {
        if (!coloresGuardados) return;

        // Crear color de cooldown (más transparente)
        Color colorCooldown = new Color(
            colorOriginalIzquierda.r,
            colorOriginalIzquierda.g,
            colorOriginalIzquierda.b,
            0.5f
        );

        if (botonIzquierda.image != null)
        {
            botonIzquierda.image.color = colorCooldown;
        }

        if (botonDerecha.image != null)
        {
            botonDerecha.image.color = colorCooldown;
        }
    }

    // NUEVO: Terminar el cooldown y restaurar colores
    private void TerminarCooldown()
    {
        enCooldown = false;

        // Restaurar colores originales
        if (coloresGuardados)
        {
            if (botonIzquierda.image != null)
            {
                botonIzquierda.image.color = colorOriginalIzquierda;
            }

            if (botonDerecha.image != null)
            {
                botonDerecha.image.color = colorOriginalDerecha;
            }
        }

        Debug.Log("Cooldown terminado (sin coroutine)");
    }

    // MÉTODO OBSOLETO: Mantener para compatibilidad, pero usar la nueva implementación
    [System.Obsolete("Este método usa coroutines que fallan en objetos inactivos. Usar IniciarCooldownSinCoroutine()")]
    private void IniciarCooldown()
    {
        // Si el objeto está activo, usar el método anterior por compatibilidad
        if (gameObject.activeInHierarchy && !enCooldown)
        {
            StartCoroutine(CooldownCoroutine());
        }
        else
        {
            // Si está inactivo o ya en cooldown, usar el nuevo método
            IniciarCooldownSinCoroutine();
        }
    }

    // MÉTODO OBSOLETO: Mantener para compatibilidad
    [System.Obsolete("Este método puede fallar en objetos inactivos. Usar el sistema basado en Update()")]
    private IEnumerator CooldownCoroutine()
    {
        enCooldown = true;
        Debug.Log($"Iniciando cooldown de {tiempoCooldown} segundos (con coroutine)");

        // Cambiar la apariencia de los botones durante el cooldown
        if (coloresGuardados)
        {
            Color colorCooldown = new Color(colorOriginalIzquierda.r, colorOriginalIzquierda.g, colorOriginalIzquierda.b, 0.5f);

            if (botonIzquierda.image != null)
            {
                botonIzquierda.image.color = colorCooldown;
            }

            if (botonDerecha.image != null)
            {
                botonDerecha.image.color = colorCooldown;
            }
        }

        yield return new WaitForSeconds(tiempoCooldown);

        // Restaurar color original
        TerminarCooldown();
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

        if (botonIzquierda != null)
        {
            botonIzquierda.gameObject.SetActive(mostrarBotonIzquierda);
        }

        if (botonDerecha != null)
        {
            botonDerecha.gameObject.SetActive(mostrarBotonDerecha);
        }

        Debug.Log($"Botón Izquierda: {(mostrarBotonIzquierda ? "visible" : "oculto")}, " +
                  $"Botón Derecha: {(mostrarBotonDerecha ? "visible" : "oculto")}");
    }

    // NUEVO: Método público para resetear la libreta desde el exterior
    public void ResetearLibreta()
    {
        paginaActual = 0;
        enCooldown = false;
        tiempoUltimoClic = 0f;
        MostrarPaginaActual();

        Debug.Log("Libreta reseteada a la primera página");
    }

    // NUEVO: Método público para ir a una página específica
    public void IrAPagina(int numeroPagina)
    {
        if (numeroPagina >= 0 && numeroPagina < paginas.Count)
        {
            paginaActual = numeroPagina;
            MostrarPaginaActual();
            Debug.Log($"Navegado a página: {numeroPagina}");
        }
        else
        {
            Debug.LogWarning($"Número de página inválido: {numeroPagina}. Debe estar entre 0 y {paginas.Count - 1}");
        }
    }

    // NUEVO: Método público para obtener información del estado actual
    public int GetPaginaActual()
    {
        return paginaActual;
    }

    public int GetTotalPaginas()
    {
        return paginas?.Count ?? 0;
    }

    public bool EstaEnCooldown()
    {
        return VerificarCooldown();
    }

    // NUEVO: Método para limpiar completamente el estado al destruir
    private void OnDestroy()
    {
        // Limpiar listeners para evitar memory leaks
        if (botonIzquierda != null)
        {
            botonIzquierda.onClick.RemoveAllListeners();
        }

        if (botonDerecha != null)
        {
            botonDerecha.onClick.RemoveAllListeners();
        }

        Debug.Log("LibretaController destruido y limpiado");
    }

    // MÉTODO DE DEBUG
    [ContextMenu("Debug Libreta State")]
    public void DebugLibretaState()
    {
        Debug.Log("=== ESTADO DE LA LIBRETA ===");
        Debug.Log($"Página actual: {paginaActual}");
        Debug.Log($"Total páginas: {GetTotalPaginas()}");
        Debug.Log($"En cooldown: {EstaEnCooldown()}");
        Debug.Log($"GameObject activo: {gameObject.activeInHierarchy}");
        Debug.Log($"Componente habilitado: {enabled}");
        Debug.Log($"Tiempo desde último clic: {Time.time - tiempoUltimoClic}");
        Debug.Log($"Cooldown configurado: {tiempoCooldown}");
        Debug.Log("============================");
    }
}