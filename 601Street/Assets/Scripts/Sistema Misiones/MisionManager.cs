using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Gestor principal de misiones (MonoBehaviour)
public class MisionManager : MonoBehaviour
{
    // Singleton para acceso global
    private static MisionManager instance;
    public static MisionManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("MisionManager");
                instance = obj.AddComponent<MisionManager>();
                DontDestroyOnLoad(obj);
            }
            return instance;
        }
    }

    [Header("Configuración")]
    [SerializeField] private bool iniciarMisionAlDespertar = false;
    [SerializeField] private Mision misionInicial;

    [Header("Estado")]
    [SerializeField] private Mision misionActual;

    // Lista de todas las misiones completadas durante la sesión
    private List<Mision> misionesCompletadas = new List<Mision>();

    // Diccionario para acceder rápidamente a misiones por ID
    private Dictionary<string, Mision> misionesPorID = new Dictionary<string, Mision>();

    // Evento para notificar cambios en la misión actual
    public event Action<Mision> OnMisionCambiada;
    public event Action<Mision> OnMisionCompletada;

    // Propiedad pública para acceder a la misión actual
    public Mision MisionActual => misionActual;

    // Propiedad para verificar si hay una misión activa
    public bool TieneMisionActiva => misionActual != null;
    
    [Header("Depuración")]
    [SerializeField] private bool mostrarOrigenLlamadas = true;
    private void Awake()
    {
        // Singleton setup
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // Cargar misiones registradas en el proyecto
        CargarMisiones();
    }

    private void Start()
    {
        // Iniciar misión inicial si está configurado
        if (iniciarMisionAlDespertar && misionInicial != null)
        {
            IniciarMision(misionInicial);
        }
    }

    private void OnEnable()
    {
        // Suscribirse a eventos internos
        OnMisionCompletada += ManejarCompletadoInterno;
    }

    private void OnDisable()
    {
        // Desuscribirse de eventos internos
        OnMisionCompletada -= ManejarCompletadoInterno;
    }

    private void CargarMisiones()
    {
        // Encuentra todas las misiones en el proyecto
        Mision[] todasLasMisiones = Resources.LoadAll<Mision>("Misiones");

        Debug.Log($"Se encontraron {todasLasMisiones.Length} misiones en Resources/Misiones");

        // Registra cada misión en el diccionario para acceso rápido
        foreach (Mision mision in todasLasMisiones)
        {
            Debug.Log($"Misión encontrada: {mision.name}, ID: {mision.ID}");

            if (!string.IsNullOrEmpty(mision.ID))
            {
                if (!misionesPorID.ContainsKey(mision.ID))
                {
                    misionesPorID.Add(mision.ID, mision);
                    mision.ValidarMision(); // Validar configuración
                }
                else
                {
                    Debug.LogError($"ID de misión duplicado: {mision.ID}. Cada ID debe ser único.");
                }
            }
        }

        Debug.Log($"Sistema de misiones inicializado. {misionesPorID.Count} misiones registradas.");
    }

    // MÉTODOS PÚBLICOS PARA GESTIONAR MISIONES

    // Iniciar una misión específica
    public bool IniciarMision(Mision mision)
    {
        if (mision == null)
        {
            Debug.LogError("Se intentу iniciar una misiуn nula.");
            return false;
        }

        // Verificar si la misión que se quiere iniciar es la misma que la actual
        if (misionActual != null && misionActual.ID == mision.ID)
        {
            Debug.Log($"Se intentó iniciar la misión '{mision.ID}' pero ya está activa. Operación ignorada.");
            return false;
        }

        // Resto del código original...
        // Guardamos una referencia a la misiуn actual antes de cambiarla
        Mision misionAnterior = misionActual;

        // Si hay una misiуn activa actualmente, la completamos
        if (misionActual != null)
        {
            // Desactivar notificaciуn del evento para evitar que se ejecute la lуgica estбndar
            // que podrнa interferir con nuestra transiciуn personalizada
            OnMisionCompletada -= ManejarCompletadoInterno;

            // Completar la misiуn anterior sin iniciar automбticamente la siguiente
            misionActual.CompletarMision();

            // Aсadir a la lista de misiones completadas si no estб ya
            if (!misionesCompletadas.Contains(misionAnterior))
            {
                misionesCompletadas.Add(misionAnterior);
            }

            // Notificar manualmente pero sin cambiar aъn la misiуn actual
            OnMisionCompletada?.Invoke(misionAnterior);

            // Reactivar el evento para futuras misiones
            OnMisionCompletada += ManejarCompletadoInterno;
        }

        // Establecer la nueva misiуn actual
        misionActual = mision;
        misionActual.IniciarMision();

        // Notificar el cambio de misiуn
        OnMisionCambiada?.Invoke(misionActual);

        return true;
    }

    // Iniciar una misión por ID
    public bool IniciarMision(string misionID)
    {
        if (string.IsNullOrEmpty(misionID))
        {
            Debug.LogError("Se intentу iniciar una misiуn con ID nulo o vacнo.");
            return false;
        }

        // Verificar si la misión que se quiere iniciar es la misma que la actual
        if (misionActual != null && misionActual.ID == misionID)
        {
            Debug.Log($"Se intentó iniciar la misión '{misionID}' pero ya está activa. Operación ignorada.");
            return false;
        }

        if (misionesPorID.TryGetValue(misionID, out Mision mision))
        {
            return IniciarMision(mision);
        }
        else
        {
            Debug.LogError($"No se encontrу una misiуn con el ID: {misionID}");
            return false;
        }
    }


    // Iniciar una misión por nombre
    public bool IniciarMisionPorNombre(string nombreMision)
    {
        if (string.IsNullOrEmpty(nombreMision))
        {
            Debug.LogError("Se intentó iniciar una misión con nombre nulo o vacío.");
            return false;
        }

        foreach (var mision in misionesPorID.Values)
        {
            if (mision.Nombre == nombreMision)
            {
                return IniciarMision(mision);
            }
        }

        Debug.LogError($"No se encontró una misión con el nombre: {nombreMision}");
        return false;
    }

    // Completar la misión actual
    public bool CompletarMisionActual()
    {
        if (misionActual == null)
        {
            Debug.LogWarning("No hay misión activa para completar.");
            return false;
        }

        // Obtener información del origen de la llamada si está habilitado
        if (mostrarOrigenLlamadas)
        {
            string origenLlamada = MisionDebugExtensions.ObtenerOrigenLlamada();
            Debug.Log($"[MisionManager] Misión '{misionActual.Nombre}' completada desde: {origenLlamada}");
        }

        Mision misionCompletada = misionActual;
        misionCompletada.CompletarMision();

        // Añadir a la lista de misiones completadas
        if (!misionesCompletadas.Contains(misionCompletada))
        {
            misionesCompletadas.Add(misionCompletada);
        }

        // Notificar a los listeners
        OnMisionCompletada?.Invoke(misionCompletada);

        // Si hay una siguiente misión configurada, la iniciamos
        if (misionCompletada.SiguienteMision != null)
        {
            IniciarMision(misionCompletada.SiguienteMision);
        }
        else
        {
            // Si no hay siguiente misión, limpiamos la misión actual
            misionActual = null;
            OnMisionCambiada?.Invoke(null);
        }

        return true;
    }

    // Completar la misión por ID
    public bool CompletarMision(string misionID)
    {
        // Si la misión actual ya está completada o no coincide con el ID, no hacer nada
        if (misionActual != null && misionActual.ID == misionID)
        {
            return CompletarMisionActual();
        }
        else if (misionesPorID.TryGetValue(misionID, out Mision mision))
        {
            // Si la misión existe pero no es la actual, verificar si ya está completada
            if (misionesCompletadas.Contains(mision))
            {
                Debug.Log($"La misión '{misionID}' ya está completada. Operación ignorada.");
                return false;
            }

            // Si no está completada, marcarla como completada
            mision.CompletarMision();

            if (!misionesCompletadas.Contains(mision))
            {
                misionesCompletadas.Add(mision);
            }

            OnMisionCompletada?.Invoke(mision);
            return true;
        }

        Debug.LogError($"No se encontrу una misiуn con el ID: {misionID}");
        return false;
    }

    // Completar la misión actual e iniciar una específica
    public bool CompletarEIniciar(Mision siguienteMision)
    {
        // Obtener información del origen de la llamada si está habilitado
        if (mostrarOrigenLlamadas)
        {
            string origenLlamada = MisionDebugExtensions.ObtenerOrigenLlamada();
            Debug.Log($"[MisionManager] Completar e iniciar nueva misión llamado desde: {origenLlamada}");
        }

        if (CompletarMisionActual())
        {
            return IniciarMision(siguienteMision);
        }

        return false;
    }


    // Completar la misión actual e iniciar otra por ID
    public bool CompletarEIniciar(string siguienteMisionID)
    {
        // Obtener información del origen de la llamada si está habilitado
        if (mostrarOrigenLlamadas)
        {
            string origenLlamada = MisionDebugExtensions.ObtenerOrigenLlamada();
            Debug.Log($"[MisionManager] Completar e iniciar misión con ID '{siguienteMisionID}' llamado desde: {origenLlamada}");
        }

        if (CompletarMisionActual())
        {
            return IniciarMision(siguienteMisionID);
        }

        return false;
    }

    // Cancelar la misión actual
    public bool CancelarMisionActual()
    {
        if (misionActual == null)
        {
            Debug.LogWarning("No hay misión activa para cancelar.");
            return false;
        }

        misionActual.CancelarMision();
        misionActual = null;

        // Notificar el cambio
        OnMisionCambiada?.Invoke(null);

        return true;
    }

    // Verificar si una misión está completada
    public bool EstaMisionCompletada(string misionID)
    {
        if (misionesPorID.TryGetValue(misionID, out Mision mision))
        {
            return misionesCompletadas.Contains(mision);
        }

        Debug.LogWarning($"No se encontró una misión con el ID: {misionID}");
        return false;
    }

    // Obtener una misión por ID
    public Mision ObtenerMision(string misionID)
    {
        if (misionesPorID.TryGetValue(misionID, out Mision mision))
        {
            return mision;
        }

        return null;
    }

    // Obtener todas las misiones completadas
    public List<Mision> ObtenerMisionesCompletadas()
    {
        return new List<Mision>(misionesCompletadas);
    }

    // Reiniciar el sistema de misiones
    public void ReiniciarSistema()
    {
        // Cancelar la misión actual si existe
        if (misionActual != null)
        {
            misionActual.CancelarMision();
            misionActual = null;
        }

        // Limpiar misiones completadas
        misionesCompletadas.Clear();

        // Reiniciar cada misión
        foreach (var mision in misionesPorID.Values)
        {
            mision.Reiniciar();
        }

        // Iniciar misión inicial si está configurada
        if (misionInicial != null)
        {
            IniciarMision(misionInicial);
        }

        Debug.Log("Sistema de misiones reiniciado.");
    }

    // Método para manejar el completado de misiones internamente
    private void ManejarCompletadoInterno(Mision mision)
    {
        // Este método se usa para escuchar el evento de misión completada
        // y realizar acciones específicas que queremos evitar durante transiciones personalizadas
        // En la implementación actual no hacemos nada especial, pero podría extenderse si es necesario
    }
}