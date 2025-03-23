using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Este componente hace que los objetos en la capa "HidenObject" solo sean visibles
/// exactamente mientras est�n dentro del cono de luz de la linterna.
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
    [SerializeField] private float updateFrequency = 0.05f;

    [Tooltip("Distancia m�xima de detecci�n")]
    [SerializeField] private float maxDetectionDistance = 20f;

    [Tooltip("Agregar un margen al �ngulo del spotlight para detecci�n")]
    [SerializeField] private float angleMargin = 5f;

    [Tooltip("Visualizar rayos de depuraci�n")]
    [SerializeField] private bool showDebugRays = false;

    // Lista de objetos actualmente visibles
    private List<VisibleObject> visibleObjects = new List<VisibleObject>();
    private HashSet<GameObject> objectsInLightBeam = new HashSet<GameObject>();
    private int originalCullingMask;

    // Control del tiempo de actualizaci�n
    private float lastUpdateTime = 0f;
    private bool isUpdating = false;

    // Estructura para controlar objetos visibles
    private class VisibleObject
    {
        public GameObject gameObject;
        public int originalLayer;
        public bool inCurrentBeam; // Si est� actualmente en el haz de luz

        public VisibleObject(GameObject go, int layer)
        {
            gameObject = go;
            originalLayer = layer;
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
        // Actualizar los objetos visibles con una frecuencia controlada
        if (Time.time - lastUpdateTime >= updateFrequency && !isUpdating)
        {
            StartCoroutine(UpdateVisibleObjectsCoroutine());
        }
    }

    /// <summary>
    /// Actualiza la lista de objetos que est�n dentro del cono de luz
    /// </summary>
    IEnumerator UpdateVisibleObjectsCoroutine()
    {
        isUpdating = true;
        lastUpdateTime = Time.time;

        // Guardar la lista de objetos actualmente en el haz
        HashSet<GameObject> previousObjectsInBeam = new HashSet<GameObject>(objectsInLightBeam);

        // Limpiar la lista para la nueva b�squeda
        objectsInLightBeam.Clear();

        // Solo procesamos si la luz est� encendida
        if (spotLight == null || !spotLight.enabled || spotLight.intensity <= 0.01f)
        {
            // Si la luz est� apagada, restaurar todos los objetos a su capa original
            RestoreAllObjects();
            isUpdating = false;
            yield break;
        }

        // Obtenemos la posici�n y direcci�n de la luz
        Vector3 lightPosition = spotLight.transform.position;
        Vector3 lightDirection = spotLight.transform.forward;

        // Obtenemos todos los objetos en la capa de objetos ocultos y la capa visible (para detectar los que ya cambiamos)
        Collider[] potentialObjects = Physics.OverlapSphere(lightPosition, maxDetectionDistance,
                                                         hiddenObjectsLayer | (1 << visibleLayer));

        // Crear una lista temporal para registrar objetos que entrar�n en el haz de luz
        List<GameObject> objectsToMakeVisible = new List<GameObject>();

        foreach (Collider collider in potentialObjects)
        {
            GameObject currentObject = collider.gameObject;

            // Calculamos la direcci�n hacia el objeto
            Vector3 directionToObject = (collider.bounds.center - lightPosition).normalized;

            // Calculamos el �ngulo entre la direcci�n de la luz y la direcci�n al objeto
            float angle = Vector3.Angle(lightDirection, directionToObject);

            // Calculamos la distancia al objeto
            float distance = Vector3.Distance(lightPosition, collider.bounds.center);

            // Verificamos si el objeto est� dentro del cono de luz
            if (angle <= (spotLight.spotAngle / 2f + angleMargin) && distance <= maxDetectionDistance)
            {
                // Lanzamos un rayo para comprobar si hay obst�culos entre la luz y el objeto
                RaycastHit hit;
                if (Physics.Raycast(lightPosition, directionToObject, out hit, distance))
                {
                    // Si el rayo golpea algo que no es el objeto, entonces hay un obst�culo
                    if (hit.collider.gameObject != currentObject &&
                        !hit.collider.gameObject.transform.IsChildOf(currentObject.transform))
                    {
                        if (showDebugRays)
                        {
                            Debug.DrawRay(lightPosition, directionToObject * distance, Color.red, updateFrequency);
                        }
                        continue;
                    }
                }

                // El objeto est� dentro del cono de luz y no hay obst�culos
                if (showDebugRays)
                {
                    Debug.DrawRay(lightPosition, directionToObject * distance, Color.green, updateFrequency);
                }

                // A�adimos el objeto a la lista de objetos en el haz actual
                objectsInLightBeam.Add(currentObject);
                objectsToMakeVisible.Add(currentObject);
            }
            else
            {
                // El objeto est� fuera del cono de luz
                if (showDebugRays)
                {
                    Debug.DrawRay(lightPosition, directionToObject * distance, Color.yellow, updateFrequency);
                }
            }

            // Pausa para evitar sobrecarga de procesamiento si hay muchos objetos
            if (potentialObjects.Length > 10 && potentialObjects.Length % 10 == 0)
            {
                yield return null;
            }
        }

        // Restaurar objetos que ya no est�n en el haz de luz
        for (int i = visibleObjects.Count - 1; i >= 0; i--)
        {
            VisibleObject visObj = visibleObjects[i];

            // Si el objeto ha sido destruido o ya no est� en el haz de luz
            if (visObj.gameObject == null || !objectsInLightBeam.Contains(visObj.gameObject))
            {
                // Restauramos la capa original
                if (visObj.gameObject != null)
                {
                    RestoreObjectLayer(visObj);
                }

                // Eliminamos de la lista
                visibleObjects.RemoveAt(i);
            }
        }

        // Hacer visibles los objetos que est�n en el haz de luz
        foreach (GameObject obj in objectsToMakeVisible)
        {
            MakeObjectVisible(obj);
        }

        isUpdating = false;
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
        }
        else
        {
            // Si no est� en la lista, lo a�adimos
            int originalLayer = hiddenObject.layer;

            // Cambiamos la capa del objeto a la capa visible
            hiddenObject.layer = visibleLayer;

            // Crear el nuevo objeto visible
            VisibleObject newObject = new VisibleObject(hiddenObject, originalLayer);
            visibleObjects.Add(newObject);

            // Si tiene hijos, tambi�n cambiamos su capa
            foreach (Transform child in hiddenObject.transform)
            {
                SetLayerRecursively(child.gameObject, visibleLayer);
            }
        }
    }

    /// <summary>
    /// Establece la capa de un GameObject y todos sus hijos recursivamente
    /// </summary>
    void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;

        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    /// <summary>
    /// Restaura la capa original de un objeto y sus hijos
    /// </summary>
    void RestoreObjectLayer(VisibleObject visObj)
    {
        if (visObj.gameObject == null) return;

        visObj.gameObject.layer = visObj.originalLayer;

        // Restaurar tambi�n las capas de los hijos
        foreach (Transform child in visObj.gameObject.transform)
        {
            SetLayerRecursively(child.gameObject, visObj.originalLayer);
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
                RestoreObjectLayer(visObj);
            }
        }

        visibleObjects.Clear();
        objectsInLightBeam.Clear();
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