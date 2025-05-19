using UnityEngine;

public class WorldStateMissionConnector : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private WorldStateGraphRunner stateRunner;

    [Header("Configuraci�n")]
    [SerializeField] private bool iniciarMisionAlActivarNodo = true;
    [SerializeField] private bool completarMisionAlCambiarNodo = true;

    private void Awake()
    {
        // Buscar WorldStateGraphRunner si no est� asignado
        if (stateRunner == null)
        {
            stateRunner = FindFirstObjectByType<WorldStateGraphRunner>();
            if (stateRunner == null)
            {
                Debug.LogError("No se encontr� un WorldStateGraphRunner en la escena");
                enabled = false;
                return;
            }
        }
    }

    private void OnEnable()
    {
        // Suscribirse al evento de cambio de estado
        if (stateRunner != null)
        {
            stateRunner.OnStateChanged += OnWorldStateChanged;
        }
    }

    private void OnDisable()
    {
        // Desuscribirse del evento
        if (stateRunner != null)
        {
            stateRunner.OnStateChanged -= OnWorldStateChanged;
        }
    }

    private void OnWorldStateChanged(string oldStateID, string newStateID)
    {
        // Obtener los nodos del grafo
        if (stateRunner.stateGraph == null) return;

        WorldStateNode oldNode = null;
        WorldStateNode newNode = null;

        if (!string.IsNullOrEmpty(oldStateID))
        {
            oldNode = stateRunner.stateGraph.FindNodeByID(oldStateID);
        }

        if (!string.IsNullOrEmpty(newStateID))
        {
            newNode = stateRunner.stateGraph.FindNodeByID(newStateID);
        }

        // Completar misi�n anterior si es necesario
        if (completarMisionAlCambiarNodo && oldNode != null && oldNode.misionAsociada != null)
        {
            if (MisionManager.Instance != null && MisionManager.Instance.TieneMisionActiva)
            {
                // Verificar si la misi�n actual es la del nodo anterior
                if (MisionManager.Instance.MisionActual.ID == oldNode.misionAsociada.ID)
                {
                    Debug.Log($"Completando misi�n '{oldNode.misionAsociada.Nombre}' al cambiar de nodo");
                    MisionManager.Instance.CompletarMisionActual();
                }
            }
        }

        // Iniciar nueva misi�n si es necesario
        if (iniciarMisionAlActivarNodo && newNode != null && newNode.misionAsociada != null)
        {
            if (MisionManager.Instance != null)
            {
                Debug.Log($"Iniciando misi�n '{newNode.misionAsociada.Nombre}' al activar nodo '{newNode.name}'");
                MisionManager.Instance.IniciarMision(newNode.misionAsociada);
            }
        }
    }

    // M�todo para pruebas y depuraci�n
    public void TestMissionConnection()
    {
        if (stateRunner == null || stateRunner.stateGraph == null)
        {
            Debug.LogError("WorldStateGraphRunner o stateGraph no disponibles");
            return;
        }

        string currentStateID = stateRunner.GetCurrentStateID();
        WorldStateNode currentNode = stateRunner.stateGraph.FindNodeByID(currentStateID);

        if (currentNode != null)
        {
            Debug.Log($"Estado actual: {currentNode.name} (ID: {currentNode.id})");
            if (currentNode.misionAsociada != null)
            {
                Debug.Log($"Misi�n asociada: {currentNode.misionAsociada.Nombre} (ID: {currentNode.misionAsociada.ID})");
            }
            else
            {
                Debug.Log("No hay misi�n asociada a este nodo");
            }
        }
    }
}