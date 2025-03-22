using System.Collections;
using UnityEngine;

public class MisionInterfaceManager : MonoBehaviour
{
    // Singleton para acceso global
    public static MisionInterfaceManager Instance { get; private set; }

    [Header("Prefab de Interfaz")]
    [SerializeField] private GameObject misionInterfacePrefab;
    [Tooltip("Posición donde se instanciará la interfaz (puede ser un parent, como el Canvas)")]
    [SerializeField] private Transform canvasParent;
    [Tooltip("Tiempo a esperar antes de destruir la interfaz")]
    [SerializeField] private float tiempoDestruccionInterfaz = 2f;

    // Referencias internas
    private GameObject interfazActual;
    private MisionUI componenteMisionUI;
    private Coroutine destructionCoroutine;

    // Control de estado para manejo de transiciones
    private bool enTransicionEntreMisiones = false;
    private Mision misionEnEspera = null;

    public Canvas canvas;
    private void Awake()
    {
        // Configuración de singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Si queremos que persista entre escenas
        // DontDestroyOnLoad(gameObject);

        // Verificar que se ha asignado el prefab
        if (misionInterfacePrefab == null)
        {
            Debug.LogError("¡No se ha asignado el prefab de interfaz de misiones!");
        }

        // Verificar que se ha asignado el canvas parent
        if (canvasParent == null)
        {
            // Intentar encontrar un canvas en la escena
            canvas = FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                canvasParent = canvas.transform;
                Debug.LogWarning("No se asignó un canvas parent. Usando el primer Canvas encontrado en la escena.");
            }
            else
            {
                Debug.LogError("¡No se ha asignado un canvas parent y no se encontró ningún Canvas en la escena!");
            }
        }
    }

    private void Start()
    {
        // Suscribirse a eventos del MisionManager
        if (MisionManager.Instance != null)
        {
            MisionManager.Instance.OnMisionCambiada += ManejarCambioMision;
            MisionManager.Instance.OnMisionCompletada += ManejarMisionCompletada;

            // Si ya hay una misión activa, mostrar la interfaz inmediatamente
            if (MisionManager.Instance.TieneMisionActiva)
            {
                InstanciarInterfaz(MisionManager.Instance.MisionActual);
            }
        }
        else
        {
            Debug.LogError("¡No se encontró el MisionManager en la escena!");
        }
    }

    private void OnDestroy()
    {
        // Desuscribirse de los eventos
        if (MisionManager.Instance != null)
        {
            MisionManager.Instance.OnMisionCambiada -= ManejarCambioMision;
            MisionManager.Instance.OnMisionCompletada -= ManejarMisionCompletada;
        }

        // Limpiar referencias
        if (destructionCoroutine != null)
        {
            StopCoroutine(destructionCoroutine);
        }
    }

    // Manejador del evento de cambio de misión
    private void ManejarCambioMision(Mision mision)
    {
        // Si estamos en medio de una transición, guardar la misión para mostrarla después
        if (enTransicionEntreMisiones)
        {
            misionEnEspera = mision;
            return;
        }

        if (mision != null)
        {
            // Verificar si ya hay una interfaz activa
            if (interfazActual != null)
            {
                // Hay una interfaz activa, necesitamos hacer transición
                enTransicionEntreMisiones = true;
                misionEnEspera = mision;

                // Iniciar la secuencia de destrucción y posterior creación
                destructionCoroutine = StartCoroutine(TransicionEntreMisiones());
            }
            else
            {
                // No hay interfaz activa, simplemente crear una nueva
                InstanciarInterfaz(mision);
            }
        }
        else
        {
            // No hay misión activa (se ha cancelado o completado sin siguiente misión)
            if (destructionCoroutine == null && interfazActual != null)
            {
                destructionCoroutine = StartCoroutine(DestruirInterfazDespuesDeDelay());
            }
        }
    }

    // Manejador del evento de misión completada
    private void ManejarMisionCompletada(Mision mision)
    {
        // Si no hay siguiente misión configurada, la interfaz se destruirá
        // cuando se reciba el evento OnMisionCambiada con mision=null

        // Si hay una siguiente misión, el MisionManager la iniciará automáticamente
        // y se recibirá otro evento OnMisionCambiada con la nueva misión
    }

    // Método para instanciar la interfaz
    private void InstanciarInterfaz(Mision mision)
    {
        // Si ya hay una corrutina de destrucción en marcha, la cancelamos
        if (destructionCoroutine != null)
        {
            StopCoroutine(destructionCoroutine);
            destructionCoroutine = null;
        }

        // Crear una nueva interfaz
        interfazActual = Instantiate(misionInterfacePrefab, canvasParent);
        componenteMisionUI = interfazActual.GetComponent<MisionUI>();

        if (componenteMisionUI == null)
        {
            Debug.LogError("El prefab de interfaz de misiones no tiene el componente MisionUI.");
            return;
        }

        // Actualizar la UI con la misión
        if (mision != null)
        {
            // Asignar la misión al componente MisionUI
            componenteMisionUI.AsignarMision(mision);

            // Mostrar el panel (iniciará la animación de entrada si está configurada)
            componenteMisionUI.MostrarPanel();
        }
    }

    // Corrutina para la transición entre misiones
    private IEnumerator TransicionEntreMisiones()
    {
        // Ocultar la interfaz actual
        if (componenteMisionUI != null)
        {
            componenteMisionUI.OcultarPanel();
        }

        // Esperar a que termine la animación
        yield return new WaitForSeconds(tiempoDestruccionInterfaz);

        // Destruir la interfaz actual
        if (interfazActual != null)
        {
            Destroy(interfazActual);
            interfazActual = null;
            componenteMisionUI = null;
        }

        // Verificar si hay una misión en espera
        if (misionEnEspera != null)
        {
            // Crear la nueva interfaz con la misión en espera
            InstanciarInterfaz(misionEnEspera);
            misionEnEspera = null;
        }

        // Finalizar el estado de transición
        enTransicionEntreMisiones = false;
        destructionCoroutine = null;
    }

    // Corrutina para destruir la interfaz después de un delay
    private IEnumerator DestruirInterfazDespuesDeDelay()
    {
        // Si hay un componente MisionUI con animación, iniciar la animación de ocultamiento
        if (componenteMisionUI != null)
        {
            componenteMisionUI.OcultarPanel();
        }

        // Esperar el tiempo configurado
        yield return new WaitForSeconds(tiempoDestruccionInterfaz);

        // Destruir la interfaz
        if (interfazActual != null)
        {
            Destroy(interfazActual);
            interfazActual = null;
            componenteMisionUI = null;
        }

        destructionCoroutine = null;
    }

    // Método para forzar el cierre de la interfaz actual
    public void ForzarCierreInterfaz()
    {
        if (interfazActual != null)
        {
            if (destructionCoroutine != null)
            {
                StopCoroutine(destructionCoroutine);
            }

            Destroy(interfazActual);
            interfazActual = null;
            componenteMisionUI = null;
            enTransicionEntreMisiones = false;
            misionEnEspera = null;
            destructionCoroutine = null;
        }
    }
}