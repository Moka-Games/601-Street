using System.Collections.Generic;
using System;
using UnityEngine;

public class MisionManager : MonoBehaviour
{
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
    [SerializeField] private bool iniciarMisionAlComenzar = false;
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
        if (iniciarMisionAlComenzar && misionInicial != null)
        {
            IniciarMision(misionInicial);
        }
    }

    private void CargarMisiones()
    {
        // Encuentra todas las misiones en el proyecto
        Mision[] todasLasMisiones = Resources.LoadAll<Mision>("Misiones");

        // Registra cada misión en el diccionario para acceso rápido
        foreach (Mision mision in todasLasMisiones)
        {
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
            Debug.LogError("Se intentó iniciar una misión nula.");
            return false;
        }

        // Si hay una misión activa actualmente, la cancelamos
        if (misionActual != null)
        {
            misionActual.CancelarMision();
        }

        // Establecer la nueva misión actual
        misionActual = mision;
        misionActual.IniciarMision();

        // Notificar el cambio
        OnMisionCambiada?.Invoke(misionActual);

        return true;
    }

    // Iniciar una misión por ID
    public bool IniciarMision(string misionID)
    {
        if (string.IsNullOrEmpty(misionID))
        {
            Debug.LogError("Se intentó iniciar una misión con ID nulo o vacío.");
            return false;
        }

        if (misionesPorID.TryGetValue(misionID, out Mision mision))
        {
            return IniciarMision(mision);
        }
        else
        {
            Debug.LogError($"No se encontró una misión con el ID: {misionID}");
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
        if (misionActual != null && misionActual.ID == misionID)
        {
            return CompletarMisionActual();
        }
        else if (misionesPorID.TryGetValue(misionID, out Mision mision))
        {
            // Si la misión existe pero no es la actual, la marcamos como completada
            // pero no la iniciamos ni afecta a la misión actual
            mision.CompletarMision();

            if (!misionesCompletadas.Contains(mision))
            {
                misionesCompletadas.Add(mision);
            }

            OnMisionCompletada?.Invoke(mision);
            return true;
        }

        Debug.LogError($"No se encontró una misión con el ID: {misionID}");
        return false;
    }

    // Completar la misión actual e iniciar una específica
    public bool CompletarEIniciar(Mision siguienteMision)
    {
        if (CompletarMisionActual())
        {
            return IniciarMision(siguienteMision);
        }

        return false;
    }

    // Completar la misión actual e iniciar otra por ID
    public bool CompletarEIniciar(string siguienteMisionID)
    {
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
}