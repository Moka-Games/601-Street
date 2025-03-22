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

    [Header("Configuraci�n")]
    [SerializeField] private bool iniciarMisionAlDespertar = false;
    [SerializeField] private Mision misionInicial;

    [Header("Estado")]
    [SerializeField] private Mision misionActual;

    // Lista de todas las misiones completadas durante la sesi�n
    private List<Mision> misionesCompletadas = new List<Mision>();

    // Diccionario para acceder r�pidamente a misiones por ID
    private Dictionary<string, Mision> misionesPorID = new Dictionary<string, Mision>();

    // Evento para notificar cambios en la misi�n actual
    public event Action<Mision> OnMisionCambiada;
    public event Action<Mision> OnMisionCompletada;

    // Propiedad p�blica para acceder a la misi�n actual
    public Mision MisionActual => misionActual;

    // Propiedad para verificar si hay una misi�n activa
    public bool TieneMisionActiva => misionActual != null;

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
        // Iniciar misi�n inicial si est� configurado
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

        // Registra cada misi�n en el diccionario para acceso r�pido
        foreach (Mision mision in todasLasMisiones)
        {
            Debug.Log($"Misi�n encontrada: {mision.name}, ID: {mision.ID}");

            if (!string.IsNullOrEmpty(mision.ID))
            {
                if (!misionesPorID.ContainsKey(mision.ID))
                {
                    misionesPorID.Add(mision.ID, mision);
                    mision.ValidarMision(); // Validar configuraci�n
                }
                else
                {
                    Debug.LogError($"ID de misi�n duplicado: {mision.ID}. Cada ID debe ser �nico.");
                }
            }
        }

        Debug.Log($"Sistema de misiones inicializado. {misionesPorID.Count} misiones registradas.");
    }

    // M�TODOS P�BLICOS PARA GESTIONAR MISIONES

    // Iniciar una misi�n espec�fica
    public bool IniciarMision(Mision mision)
    {
        if (mision == null)
        {
            Debug.LogError("Se intent� iniciar una misi�n nula.");
            return false;
        }

        // Guardamos una referencia a la misi�n actual antes de cambiarla
        Mision misionAnterior = misionActual;

        // Si hay una misi�n activa actualmente, la completamos
        if (misionActual != null)
        {
            // Desactivar notificaci�n del evento para evitar que se ejecute la l�gica est�ndar
            // que podr�a interferir con nuestra transici�n personalizada
            OnMisionCompletada -= ManejarCompletadoInterno;

            // Completar la misi�n anterior sin iniciar autom�ticamente la siguiente
            misionActual.CompletarMision();

            // A�adir a la lista de misiones completadas si no est� ya
            if (!misionesCompletadas.Contains(misionAnterior))
            {
                misionesCompletadas.Add(misionAnterior);
            }

            // Notificar manualmente pero sin cambiar a�n la misi�n actual
            OnMisionCompletada?.Invoke(misionAnterior);

            // Reactivar el evento para futuras misiones
            OnMisionCompletada += ManejarCompletadoInterno;
        }

        // Establecer la nueva misi�n actual
        misionActual = mision;
        misionActual.IniciarMision();

        // Notificar el cambio de misi�n
        OnMisionCambiada?.Invoke(misionActual);

        return true;
    }

    // Iniciar una misi�n por ID
    public bool IniciarMision(string misionID)
    {
        if (string.IsNullOrEmpty(misionID))
        {
            Debug.LogError("Se intent� iniciar una misi�n con ID nulo o vac�o.");
            return false;
        }

        if (misionesPorID.TryGetValue(misionID, out Mision mision))
        {
            return IniciarMision(mision);
        }
        else
        {
            Debug.LogError($"No se encontr� una misi�n con el ID: {misionID}");
            return false;
        }
    }

    // Iniciar una misi�n por nombre
    public bool IniciarMisionPorNombre(string nombreMision)
    {
        if (string.IsNullOrEmpty(nombreMision))
        {
            Debug.LogError("Se intent� iniciar una misi�n con nombre nulo o vac�o.");
            return false;
        }

        foreach (var mision in misionesPorID.Values)
        {
            if (mision.Nombre == nombreMision)
            {
                return IniciarMision(mision);
            }
        }

        Debug.LogError($"No se encontr� una misi�n con el nombre: {nombreMision}");
        return false;
    }

    // Completar la misi�n actual
    public bool CompletarMisionActual()
    {
        if (misionActual == null)
        {
            Debug.LogWarning("No hay misi�n activa para completar.");
            return false;
        }

        Mision misionCompletada = misionActual;
        misionCompletada.CompletarMision();

        // A�adir a la lista de misiones completadas
        if (!misionesCompletadas.Contains(misionCompletada))
        {
            misionesCompletadas.Add(misionCompletada);
        }

        // Notificar a los listeners
        OnMisionCompletada?.Invoke(misionCompletada);

        // Si hay una siguiente misi�n configurada, la iniciamos
        if (misionCompletada.SiguienteMision != null)
        {
            IniciarMision(misionCompletada.SiguienteMision);
        }
        else
        {
            // Si no hay siguiente misi�n, limpiamos la misi�n actual
            misionActual = null;
            OnMisionCambiada?.Invoke(null);
        }

        return true;
    }

    // Completar la misi�n por ID
    public bool CompletarMision(string misionID)
    {
        if (misionActual != null && misionActual.ID == misionID)
        {
            return CompletarMisionActual();
        }
        else if (misionesPorID.TryGetValue(misionID, out Mision mision))
        {
            // Si la misi�n existe pero no es la actual, la marcamos como completada
            // pero no la iniciamos ni afecta a la misi�n actual
            mision.CompletarMision();

            if (!misionesCompletadas.Contains(mision))
            {
                misionesCompletadas.Add(mision);
            }

            OnMisionCompletada?.Invoke(mision);
            return true;
        }

        Debug.LogError($"No se encontr� una misi�n con el ID: {misionID}");
        return false;
    }

    // Completar la misi�n actual e iniciar una espec�fica
    public bool CompletarEIniciar(Mision siguienteMision)
    {
        if (CompletarMisionActual())
        {
            return IniciarMision(siguienteMision);
        }

        return false;
    }

    // Completar la misi�n actual e iniciar otra por ID
    public bool CompletarEIniciar(string siguienteMisionID)
    {
        if (CompletarMisionActual())
        {
            return IniciarMision(siguienteMisionID);
        }

        return false;
    }

    // Cancelar la misi�n actual
    public bool CancelarMisionActual()
    {
        if (misionActual == null)
        {
            Debug.LogWarning("No hay misi�n activa para cancelar.");
            return false;
        }

        misionActual.CancelarMision();
        misionActual = null;

        // Notificar el cambio
        OnMisionCambiada?.Invoke(null);

        return true;
    }

    // Verificar si una misi�n est� completada
    public bool EstaMisionCompletada(string misionID)
    {
        if (misionesPorID.TryGetValue(misionID, out Mision mision))
        {
            return misionesCompletadas.Contains(mision);
        }

        Debug.LogWarning($"No se encontr� una misi�n con el ID: {misionID}");
        return false;
    }

    // Obtener una misi�n por ID
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
        // Cancelar la misi�n actual si existe
        if (misionActual != null)
        {
            misionActual.CancelarMision();
            misionActual = null;
        }

        // Limpiar misiones completadas
        misionesCompletadas.Clear();

        // Reiniciar cada misi�n
        foreach (var mision in misionesPorID.Values)
        {
            mision.Reiniciar();
        }

        // Iniciar misi�n inicial si est� configurada
        if (misionInicial != null)
        {
            IniciarMision(misionInicial);
        }

        Debug.Log("Sistema de misiones reiniciado.");
    }

    // M�todo para manejar el completado de misiones internamente
    private void ManejarCompletadoInterno(Mision mision)
    {
        // Este m�todo se usa para escuchar el evento de misi�n completada
        // y realizar acciones espec�ficas que queremos evitar durante transiciones personalizadas
        // En la implementaci�n actual no hacemos nada especial, pero podr�a extenderse si es necesario
    }
}