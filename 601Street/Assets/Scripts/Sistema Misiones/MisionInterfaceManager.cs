using System.Collections;
using UnityEngine;

public class MisionInterfaceManager : MonoBehaviour
{
    // Singleton para acceso global
    public static MisionInterfaceManager Instance { get; private set; }

    [Header("Prefab de Interfaz")]
    [SerializeField] private GameObject misionInterfacePrefab;
    [Tooltip("Posici�n donde se instanciar� la interfaz (puede ser un parent, como el Canvas)")]
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
        // Configuraci�n de singleton
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
            Debug.LogError("�No se ha asignado el prefab de interfaz de misiones!");
        }

        // Verificar que se ha asignado el canvas parent
        if (canvasParent == null)
        {
            // Intentar encontrar un canvas en la escena
            canvas = FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                canvasParent = canvas.transform;
                Debug.LogWarning("No se asign� un canvas parent. Usando el primer Canvas encontrado en la escena.");
            }
            else
            {
                Debug.LogError("�No se ha asignado un canvas parent y no se encontr� ning�n Canvas en la escena!");
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

            // Si ya hay una misi�n activa, mostrar la interfaz inmediatamente
            if (MisionManager.Instance.TieneMisionActiva)
            {
                InstanciarInterfaz(MisionManager.Instance.MisionActual);
            }
        }
        else
        {
            Debug.LogError("�No se encontr� el MisionManager en la escena!");
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

    // Manejador del evento de cambio de misi�n
    private void ManejarCambioMision(Mision mision)
    {
        // Si estamos en medio de una transici�n, guardar la misi�n para mostrarla despu�s
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
                // Hay una interfaz activa, necesitamos hacer transici�n
                enTransicionEntreMisiones = true;
                misionEnEspera = mision;

                // Iniciar la secuencia de destrucci�n y posterior creaci�n
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
            // No hay misi�n activa (se ha cancelado o completado sin siguiente misi�n)
            if (destructionCoroutine == null && interfazActual != null)
            {
                destructionCoroutine = StartCoroutine(DestruirInterfazDespuesDeDelay());
            }
        }
    }

    // Manejador del evento de misi�n completada
    private void ManejarMisionCompletada(Mision mision)
    {
        // Si no hay siguiente misi�n configurada, la interfaz se destruir�
        // cuando se reciba el evento OnMisionCambiada con mision=null

        // Si hay una siguiente misi�n, el MisionManager la iniciar� autom�ticamente
        // y se recibir� otro evento OnMisionCambiada con la nueva misi�n
    }

    // M�todo para instanciar la interfaz
    private void InstanciarInterfaz(Mision mision)
    {
        // Si ya hay una corrutina de destrucci�n en marcha, la cancelamos
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

        // Actualizar la UI con la misi�n
        if (mision != null)
        {
            // Asignar la misi�n al componente MisionUI
            componenteMisionUI.AsignarMision(mision);

            // Mostrar el panel (iniciar� la animaci�n de entrada si est� configurada)
            componenteMisionUI.MostrarPanel();
        }
    }

    // Corrutina para la transici�n entre misiones
    private IEnumerator TransicionEntreMisiones()
    {
        // Ocultar la interfaz actual
        if (componenteMisionUI != null)
        {
            componenteMisionUI.OcultarPanel();
        }

        // Esperar a que termine la animaci�n
        yield return new WaitForSeconds(tiempoDestruccionInterfaz);

        // Destruir la interfaz actual
        if (interfazActual != null)
        {
            Destroy(interfazActual);
            interfazActual = null;
            componenteMisionUI = null;
        }

        // Verificar si hay una misi�n en espera
        if (misionEnEspera != null)
        {
            // Crear la nueva interfaz con la misi�n en espera
            InstanciarInterfaz(misionEnEspera);
            misionEnEspera = null;
        }

        // Finalizar el estado de transici�n
        enTransicionEntreMisiones = false;
        destructionCoroutine = null;
    }

    // Corrutina para destruir la interfaz despu�s de un delay
    private IEnumerator DestruirInterfazDespuesDeDelay()
    {
        // Si hay un componente MisionUI con animaci�n, iniciar la animaci�n de ocultamiento
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

    // M�todo para forzar el cierre de la interfaz actual
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