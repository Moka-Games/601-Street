using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Componente auxiliar para mejorar la detecci�n de objetos ocultos.
/// Este script lanza rayos adicionales para mejorar la detecci�n y
/// combate los falsos negativos en la detecci�n de objetos ocultos.
/// </summary>
public class DetectionRaycaster : MonoBehaviour
{
    [Header("Configuraci�n")]
    [Tooltip("Referencia a la luz spot principal")]
    [SerializeField] private Light spotLight;

    [Tooltip("Capa donde est�n los objetos ocultos")]
    [SerializeField] private LayerMask hiddenObjectsLayer;

    [Tooltip("Capa donde se mover�n los objetos detectados")]
    [SerializeField] private int visibleLayer = 0;

    [Tooltip("Frecuencia de lanzamiento de rayos (segundos)")]
    [SerializeField] private float raycastFrequency = 0.05f;

    [Tooltip("N�mero de rayos a lanzar por actualizaci�n")]
    [SerializeField] private int raysPerUpdate = 5;

    [Tooltip("Distancia m�xima de detecci�n")]
    [SerializeField] private float maxDistance = 20f;

    [Tooltip("Mostrar rayos de depuraci�n")]
    [SerializeField] private bool showDebugRays = true;

    // Referencia al componente principal
    private FlashlightVisibilityController mainController;

    // Para control de tiempo
    private float lastRaycastTime = 0f;

    // Almacenamiento de objetos detectados
    private HashSet<GameObject> detectedObjects = new HashSet<GameObject>();
    private Dictionary<GameObject, float> detectionTimers = new Dictionary<GameObject, float>();

    // Tiempo que un objeto permanece detectado
    private float objectRetentionTime = 0.5f;

    private void Start()
    {
        // Buscar referencias autom�ticamente si no est�n asignadas
        if (spotLight == null)
        {
            spotLight = GetComponentInChildren<Light>();
            if (spotLight == null || spotLight.type != LightType.Spot)
            {
                Debug.LogError("DetectionRaycaster requiere una luz spotlight");
                enabled = false;
                return;
            }
        }

        // Obtener referencia al controlador principal
        mainController = GetComponent<FlashlightVisibilityController>();
        if (mainController == null)
        {
            Debug.LogWarning("No se encontr� un controlador FlashlightVisibilityController. " +
                           "Este componente funciona mejor junto con �l.");
        }

        // Si la capa de objetos ocultos no est� definida, intentar obtenerla por nombre
        if (hiddenObjectsLayer.value == 0)
        {
            hiddenObjectsLayer = 1 << LayerMask.NameToLayer("HidenObject");
            if (hiddenObjectsLayer.value == 0)
            {
                Debug.LogWarning("No se encontr� la capa 'HidenObject'.");
            }
        }
    }

    private void Update()
    {
        // Solo ejecutar la detecci�n de raycast a intervalos controlados
        if (Time.time - lastRaycastTime >= raycastFrequency)
        {
            lastRaycastTime = Time.time;
            LaunchDetectionRays();
        }

        // Procesar la expiraci�n de objetos detectados
        ProcessDetectionTimers();
    }

    /// <summary>
    /// Lanza varios rayos en un patr�n c�nico para detectar objetos ocultos
    /// </summary>
    private void LaunchDetectionRays()
    {
        if (spotLight == null || !spotLight.enabled || spotLight.intensity <= 0.01f)
            return;

        Vector3 origin = spotLight.transform.position;
        Vector3 forward = spotLight.transform.forward;

        // Lanzar un rayo central primero (direcci�n principal de la luz)
        CastDetectionRay(origin, forward, maxDistance);

        // Lanzar rayos adicionales en diferentes �ngulos dentro del cono
        for (int i = 0; i < raysPerUpdate - 1; i++)
        {
            // Calcular un �ngulo aleatorio dentro del cono de luz
            float randomAngle = Random.Range(0f, spotLight.spotAngle * 0.5f);
            float randomDir = Random.Range(0f, 360f);

            // Crear un rayo con el �ngulo aleatorio
            Vector3 rayDir = Quaternion.AngleAxis(randomAngle, Random.onUnitSphere) * forward;

            // Lanzar el rayo
            CastDetectionRay(origin, rayDir, maxDistance);
        }
    }

    /// <summary>
    /// Lanza un rayo individual y procesa los resultados
    /// </summary>
    private void CastDetectionRay(Vector3 origin, Vector3 direction, float distance)
    {
        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, distance, hiddenObjectsLayer))
        {
            GameObject detectedObject = hit.collider.gameObject;

            // A�adir a la lista de objetos detectados
            detectedObjects.Add(detectedObject);

            // Actualizar o agregar el temporizador
            detectionTimers[detectedObject] = Time.time + objectRetentionTime;

            // Cambiar la capa del objeto (si no lo ha hecho ya el controlador principal)
            if (detectedObject.layer != visibleLayer)
            {
                // Guardar la capa original en un script HiddenObjectSetup si existe
                HiddenObjectSetup setupScript = detectedObject.GetComponent<HiddenObjectSetup>();

                // En lugar de cambiar directamente la capa aqu�, notificar al controlador principal
                if (mainController != null)
                {
                    // Si hay un m�todo p�blico para hacer visible un objeto, lo usar�amos
                    // mainController.MakeObjectVisible(detectedObject);

                    // Como alternativa, podemos simplemente cambiar la capa
                    detectedObject.layer = visibleLayer;

                    // Y tambi�n para sus hijos si es necesario
                    foreach (Transform child in detectedObject.transform)
                    {
                        SetLayerRecursively(child.gameObject, visibleLayer);
                    }
                }
                else
                {
                    // Sin controlador principal, cambiamos la capa directamente
                    detectedObject.layer = visibleLayer;

                    // Y tambi�n para sus hijos
                    foreach (Transform child in detectedObject.transform)
                    {
                        SetLayerRecursively(child.gameObject, visibleLayer);
                    }
                }
            }

            // Dibujar rayo de depuraci�n
            if (showDebugRays)
            {
                Debug.DrawRay(origin, direction * hit.distance, Color.magenta, raycastFrequency);
            }
        }
        else if (showDebugRays)
        {
            // Rayo que no golpea nada
            Debug.DrawRay(origin, direction * distance, Color.yellow, raycastFrequency);
        }
    }

    /// <summary>
    /// Procesa los temporizadores de detecci�n y restaura objetos que ya no est�n detectados
    /// </summary>
    private void ProcessDetectionTimers()
    {
        List<GameObject> objectsToRemove = new List<GameObject>();

        // Verificar qu� objetos han expirado su tiempo de detecci�n
        foreach (var kvp in detectionTimers)
        {
            if (Time.time > kvp.Value)
            {
                // El objeto ya no est� detectado
                objectsToRemove.Add(kvp.Key);
            }
        }

        // Eliminar objetos expirados
        foreach (GameObject obj in objectsToRemove)
        {
            detectionTimers.Remove(obj);
            detectedObjects.Remove(obj);

            // No restauramos la capa directamente aqu�, dejamos que el controlador principal se encargue
            // Ya que el objeto podr�a seguir siendo detectado por el controlador principal
        }
    }

    /// <summary>
    /// Establece la capa de un GameObject y todos sus hijos recursivamente
    /// </summary>
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;

        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}