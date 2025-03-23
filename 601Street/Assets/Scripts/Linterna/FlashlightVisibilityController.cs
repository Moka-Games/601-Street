using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Este componente hace que los objetos en la capa "HidenObject" solo sean visibles
/// cuando est�n dentro del cono de luz de la linterna.
/// </summary>
public class FlashlightVisibilityController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("La luz spot de la linterna")]
    [SerializeField] private Light spotLight;

    [Tooltip("Referencia a la c�mara principal")]
    [SerializeField] private Camera mainCamera;

    [Header("Configuraci�n de Capas")]
    [Tooltip("La capa donde est�n los objetos ocultos")]
    [SerializeField] private LayerMask hiddenObjectsLayer;

    [Tooltip("La capa a la que se mover�n temporalmente los objetos iluminados")]
    [SerializeField] private int visibleLayer = 0; // Default layer

    [Header("Configuraci�n de Detecci�n")]
    [Tooltip("Frecuencia de actualizaci�n de objetos visibles (segundos)")]
    [SerializeField] private float updateFrequency = 0.1f;

    [Tooltip("Distancia m�xima de detecci�n")]
    [SerializeField] private float maxDetectionDistance = 20f;

    [Tooltip("Agregar un margen al �ngulo del spotlight para detecci�n")]
    [SerializeField] private float angleMargin = 5f;

    [Tooltip("Tiempo de permanencia adicional tras perder la iluminaci�n (segundos)")]
    [SerializeField] private float visibilityPersistence = 0.0f;

    [Tooltip("Visualizar rayos de depuraci�n")]
    [SerializeField] private bool showDebugRays = false;

    // Lista de objetos actualmente visibles
    private List<VisibleObject> visibleObjects = new List<VisibleObject>();
    private HashSet<GameObject> objectsInLightBeam = new HashSet<GameObject>();
    private int originalCullingMask;

    // Estructura para controlar objetos visibles
    private class VisibleObject
    {
        public GameObject gameObject;
        public int originalLayer;
        public float visibleUntil; // Tiempo hasta que el objeto vuelve a ser invisible
        public bool inCurrentBeam; // Si est� actualmente en el haz de luz

        public VisibleObject(GameObject go, int layer)
        {
            gameObject = go;
            originalLayer = layer;
            visibleUntil = 0f; // Se actualizar� en cada frame
            inCurrentBeam = true;
        }
    }

    void Start()
    {
        // Buscar referencias si no est�n asignadas
        if (spotLight == null)
        {
            spotLight = GetComponentInChildren<Light>();
            if (spotLight == null || spotLight.type != LightType.Spot)
            {
                Debug.LogError("FlashlightVisibilityController requiere una luz de tipo Spot");
                enabled = false;
                return;
            }
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("FlashlightVisibilityController requiere una referencia a la c�mara principal");
                enabled = false;
                return;
            }
        }

        // Guardar el culling mask original de la c�mara
        originalCullingMask = mainCamera.cullingMask;

        // Asegurarse de que la capa de objetos ocultos existe
        if (hiddenObjectsLayer.value == 0)
        {
            // Intentar obtener la capa por nombre
            hiddenObjectsLayer = 1 << LayerMask.NameToLayer("HidenObject");

            if (hiddenObjectsLayer.value == 0)
            {
                Debug.LogWarning("Capa 'HidenObject' no encontrada. Aseg�rate de que existe en las capas del proyecto.");
            }
        }

        // Iniciar la b�squeda de objetos
        InvokeRepeating("UpdateVisibleObjects", 0f, updateFrequency);
    }

    void OnDisable()
    {
        // Restaurar todos los objetos a su capa original cuando se desactiva el script
        RestoreAllObjects();
    }

    void OnDestroy()
    {
        // Asegurarse de restaurar todos los objetos al destruir
        RestoreAllObjects();
    }

    void Update()
    {
        // Verificar objetos que ya no deber�an ser visibles
        CheckVisibilityTimeout();
    }

    /// <summary>
    /// Actualiza la lista de objetos que est�n dentro del cono de luz
    /// </summary>
    void UpdateVisibleObjects()
    {
        // Limpiar la lista de objetos en el haz actual
        objectsInLightBeam.Clear();

        // Solo procesamos si la luz est� encendida
        if (spotLight == null || !spotLight.enabled || spotLight.intensity <= 0.01f)
        {
            // Marcar todos los objetos como fuera del haz de luz
            foreach (VisibleObject visObj in visibleObjects)
            {
                visObj.inCurrentBeam = false;
                // El objeto debe desaparecer inmediatamente cuando sale del haz de luz
                visObj.visibleUntil = Time.time;
            }
            return;
        }

        // Obtenemos la posici�n y direcci�n de la luz
        Vector3 lightPosition = spotLight.transform.position;
        Vector3 lightDirection = spotLight.transform.forward;

        // Obtenemos todos los objetos en la capa de objetos ocultos
        Collider[] hiddenObjects = Physics.OverlapSphere(lightPosition, maxDetectionDistance, hiddenObjectsLayer);

        foreach (Collider collider in hiddenObjects)
        {
            GameObject hiddenObject = collider.gameObject;

            // Calculamos la direcci�n hacia el objeto
            Vector3 directionToObject = (collider.bounds.center - lightPosition).normalized;

            // Calculamos el �ngulo entre la direcci�n de la luz y la direcci�n al objeto
            float angle = Vector3.Angle(lightDirection, directionToObject);

            // Calculamos la distancia al objeto
            float distance = Vector3.Distance(lightPosition, collider.bounds.center);

            // Verificamos si el objeto est� dentro del cono de luz (con un margen para suavizar)
            if (angle <= (spotLight.spotAngle / 2f + angleMargin) && distance <= maxDetectionDistance)
            {
                // Lanzamos un rayo para comprobar si hay obst�culos entre la luz y el objeto
                RaycastHit hit;
                if (Physics.Raycast(lightPosition, directionToObject, out hit, distance, ~hiddenObjectsLayer))
                {
                    // Si el rayo golpea algo que no es el objeto, entonces hay un obst�culo
                    if (hit.collider.gameObject != hiddenObject)
                    {
                        if (showDebugRays)
                        {
                            Debug.DrawRay(lightPosition, directionToObject * distance, Color.red);
                        }
                        continue;
                    }
                }

                // El objeto est� dentro del cono de luz y no hay obst�culos
                if (showDebugRays)
                {
                    Debug.DrawRay(lightPosition, directionToObject * distance, Color.green);
                }

                // A�adimos el objeto a la lista de objetos en el haz actual
                objectsInLightBeam.Add(hiddenObject);

                // Hacemos el objeto visible
                MakeObjectVisible(hiddenObject);
            }
            else
            {
                // El objeto est� fuera del cono de luz
                if (showDebugRays)
                {
                    Debug.DrawRay(lightPosition, directionToObject * distance, Color.yellow);
                }
            }
        }

        // Marcar los objetos que ya no est�n en el haz de luz
        foreach (VisibleObject visObj in visibleObjects)
        {
            if (visObj.gameObject != null && !objectsInLightBeam.Contains(visObj.gameObject))
            {
                visObj.inCurrentBeam = false;
                // El objeto debe desaparecer inmediatamente cuando sale del haz de luz
                visObj.visibleUntil = Time.time;
            }
        }
    }

    /// <summary>
    /// Hace que un objeto sea visible cambi�ndolo a la capa visible
    /// </summary>
    void MakeObjectVisible(GameObject hiddenObject)
    {
        // Verificamos si el objeto ya est� en la lista de objetos visibles
        VisibleObject existingObject = visibleObjects.Find(obj => obj.gameObject == hiddenObject);

        if (existingObject != null)
        {
            // Si ya est� en la lista, lo marcamos como dentro del haz actual
            existingObject.inCurrentBeam = true;
            // Mantener visible solo mientras est� en el haz
            existingObject.visibleUntil = Time.time + updateFrequency * 1.5f;
        }
        else
        {
            // Si no est� en la lista, lo a�adimos
            int originalLayer = hiddenObject.layer;
            hiddenObject.layer = visibleLayer;

            visibleObjects.Add(new VisibleObject(hiddenObject, originalLayer));
        }
    }

    /// <summary>
    /// Verifica qu� objetos ya no deber�an ser visibles y los restaura
    /// </summary>
    void CheckVisibilityTimeout()
    {
        if (visibleObjects.Count == 0) return;

        float currentTime = Time.time;

        // Iteramos la lista en reversa para poder eliminar elementos sin problemas
        for (int i = visibleObjects.Count - 1; i >= 0; i--)
        {
            VisibleObject visObj = visibleObjects[i];

            // Si el objeto ha sido destruido
            if (visObj.gameObject == null)
            {
                visibleObjects.RemoveAt(i);
                continue;
            }

            // Si est� en el haz de luz actual, se mantiene visible
            if (visObj.inCurrentBeam)
            {
                continue;
            }

            // Si el tiempo ha expirado
            if (currentTime > visObj.visibleUntil)
            {
                // Restauramos la capa original
                visObj.gameObject.layer = visObj.originalLayer;

                // Eliminamos de la lista
                visibleObjects.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Restaura todos los objetos a su capa original
    /// </summary>
    void RestoreAllObjects()
    {
        foreach (VisibleObject visObj in visibleObjects)
        {
            if (visObj.gameObject != null)
            {
                visObj.gameObject.layer = visObj.originalLayer;
            }
        }

        visibleObjects.Clear();
    }

    /// <summary>
    /// Dibuja gizmos en el editor para visualizar el cono de detecci�n
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (spotLight == null) return;

        Vector3 pos = spotLight.transform.position;
        Vector3 dir = spotLight.transform.forward;

        // Dibujamos el cono de luz
        Gizmos.color = new Color(1, 1, 0, 0.3f);

        float angle = spotLight.spotAngle / 2f + angleMargin;
        float distance = maxDetectionDistance;

        Vector3 forward = dir * distance;
        Vector3 right = Quaternion.Euler(0, angle, 0) * forward;
        Vector3 left = Quaternion.Euler(0, -angle, 0) * forward;
        Vector3 up = Quaternion.Euler(angle, 0, 0) * forward;
        Vector3 down = Quaternion.Euler(-angle, 0, 0) * forward;

        Gizmos.DrawLine(pos, pos + forward);
        Gizmos.DrawLine(pos, pos + right);
        Gizmos.DrawLine(pos, pos + left);
        Gizmos.DrawLine(pos, pos + up);
        Gizmos.DrawLine(pos, pos + down);

        // Dibujamos un wireframe para la esfera de detecci�n m�xima
        Gizmos.color = new Color(0, 1, 1, 0.1f);
        Gizmos.DrawWireSphere(pos, maxDetectionDistance);
    }
}