using UnityEngine;
using System.Collections;

/// <summary>
/// Añade efectos visuales especiales a la linterna, como parpadeo cuando se detectan objetos ocultos,
/// variación de intensidad, y efectos de partículas.
/// </summary>
public class FlashlightSpecialEffects : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("La luz spot de la linterna")]
    [SerializeField] private Light spotLight;

    [Tooltip("Sistema de partículas opcional")]
    [SerializeField] private ParticleSystem dustParticles;

    [Header("Efectos de Detección")]
    [Tooltip("¿Activar parpadeo cuando se detecta un objeto oculto?")]
    [SerializeField] private bool flickerOnDetection = true;

    [Tooltip("Intensidad del parpadeo (0-1)")]
    [Range(0, 1)]
    [SerializeField] private float flickerIntensity = 0.2f;

    [Tooltip("Duración del parpadeo en segundos")]
    [SerializeField] private float flickerDuration = 0.5f;

    [Header("Efectos Ambientales")]
    [Tooltip("¿Activar variación aleatoria de intensidad?")]
    [SerializeField] private bool randomIntensityVariation = true;

    [Tooltip("Magnitud de la variación de intensidad (0-1)")]
    [Range(0, 1)]
    [SerializeField] private float intensityVariationMagnitude = 0.05f;

    [Tooltip("Velocidad de la variación de intensidad")]
    [SerializeField] private float intensityVariationSpeed = 1.0f;

    [Header("Efectos de Partículas")]
    [Tooltip("¿Mostrar partículas de polvo en el haz de luz?")]
    [SerializeField] private bool showDustParticles = true;

    [Tooltip("Densidad de partículas (0-1)")]
    [Range(0, 1)]
    [SerializeField] private float dustDensity = 0.5f;

    // Variables privadas
    private float originalIntensity;
    private bool isFlickering = false;
    private float intensityVariationTime = 0f;
    private FlashlightVisibilityController visibilityController;

    void Start()
    {
        // Buscar referencias si no están asignadas
        if (spotLight == null)
        {
            spotLight = GetComponentInChildren<Light>();
            if (spotLight == null || spotLight.type != LightType.Spot)
            {
                Debug.LogError("FlashlightSpecialEffects requiere una luz de tipo Spot");
                enabled = false;
                return;
            }
        }

        // Guardar intensidad original
        originalIntensity = spotLight.intensity;

        // Buscar controlador de visibilidad
        visibilityController = GetComponent<FlashlightVisibilityController>();

        // Configurar sistema de partículas si existe
        if (dustParticles != null && showDustParticles)
        {
            SetupDustParticles();
        }
    }

    void Update()
    {
        // No hacer nada si la luz está apagada
        if (!spotLight.enabled || spotLight.intensity <= 0.01f)
            return;

        // Aplicar variación de intensidad si está activada
        if (randomIntensityVariation && !isFlickering)
        {
            ApplyIntensityVariation();
        }
    }

    /// <summary>
    /// Configura el sistema de partículas para el haz de luz
    /// </summary>
    private void SetupDustParticles()
    {
        var main = dustParticles.main;
        var emission = dustParticles.emission;
        var shape = dustParticles.shape;

        // Adaptar forma del sistema de partículas al ángulo del spotlight
        shape.angle = spotLight.spotAngle / 2f;

        // Ajustar emisión según densidad
        emission.rateOverTimeMultiplier = dustDensity * 50f;

        // Activar/desactivar partículas según configuración
        dustParticles.gameObject.SetActive(showDustParticles);
    }

    /// <summary>
    /// Aplica una variación suave a la intensidad de la luz
    /// </summary>
    private void ApplyIntensityVariation()
    {
        intensityVariationTime += Time.deltaTime * intensityVariationSpeed;

        // Usar ruido Perlin para una variación suave
        float noise = Mathf.PerlinNoise(intensityVariationTime, 0f) * 2f - 1f;
        float variation = noise * intensityVariationMagnitude;

        // Aplicar variación a la intensidad original
        spotLight.intensity = originalIntensity * (1f + variation);
    }

    /// <summary>
    /// Hace que la linterna parpadee por un tiempo determinado
    /// </summary>
    public void TriggerFlicker()
    {
        if (!isFlickering)
        {
            StartCoroutine(FlickerEffect());
        }
    }

    /// <summary>
    /// Corrutina para el efecto de parpadeo
    /// </summary>
    private IEnumerator FlickerEffect()
    {
        isFlickering = true;
        float elapsed = 0f;

        while (elapsed < flickerDuration)
        {
            // Oscilación rápida de la intensidad
            float flickerValue = originalIntensity * (1f - (Random.value * flickerIntensity));
            spotLight.intensity = flickerValue;

            // Esperar un tiempo aleatorio para el siguiente parpadeo
            float waitTime = Random.Range(0.01f, 0.05f);
            yield return new WaitForSeconds(waitTime);

            elapsed += waitTime;
        }

        // Restaurar intensidad original
        spotLight.intensity = originalIntensity;
        isFlickering = false;
    }

    /// <summary>
    /// Este método puede ser llamado cuando se detecta un objeto oculto
    /// </summary>
    public void OnHiddenObjectDetected()
    {
        if (flickerOnDetection)
        {
            TriggerFlicker();
        }

        // Aumentar temporalmente la emisión de partículas
        if (dustParticles != null && showDustParticles)
        {
            var emission = dustParticles.emission;
            StartCoroutine(TemporarilyBoostParticles(emission.rateOverTimeMultiplier * 3f));
        }
    }

    /// <summary>
    /// Aumenta temporalmente la emisión de partículas
    /// </summary>
    private IEnumerator TemporarilyBoostParticles(float boostedRate)
    {
        if (dustParticles == null) yield break;

        var emission = dustParticles.emission;
        float originalRate = emission.rateOverTimeMultiplier;
        emission.rateOverTimeMultiplier = boostedRate;

        yield return new WaitForSeconds(0.5f);

        emission.rateOverTimeMultiplier = originalRate;
    }

    /// <summary>
    /// Ajusta la densidad de las partículas
    /// </summary>
    public void SetDustDensity(float density)
    {
        dustDensity = Mathf.Clamp01(density);

        if (dustParticles != null)
        {
            var emission = dustParticles.emission;
            emission.rateOverTimeMultiplier = dustDensity * 50f;
        }
    }

    /// <summary>
    /// Activa/desactiva las partículas de polvo
    /// </summary>
    public void SetDustParticlesActive(bool active)
    {
        showDustParticles = active;

        if (dustParticles != null)
        {
            dustParticles.gameObject.SetActive(active);
        }
    }
}