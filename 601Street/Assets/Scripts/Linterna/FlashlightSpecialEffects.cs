using UnityEngine;
using System.Collections;

/// <summary>
/// A�ade efectos visuales especiales a la linterna, como parpadeo cuando se detectan objetos ocultos,
/// variaci�n de intensidad, y efectos de part�culas.
/// </summary>
public class FlashlightSpecialEffects : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("La luz spot de la linterna")]
    [SerializeField] private Light spotLight;

    [Tooltip("Sistema de part�culas opcional")]
    [SerializeField] private ParticleSystem dustParticles;

    [Header("Efectos de Detecci�n")]
    [Tooltip("�Activar parpadeo cuando se detecta un objeto oculto?")]
    [SerializeField] private bool flickerOnDetection = true;

    [Tooltip("Intensidad del parpadeo (0-1)")]
    [Range(0, 1)]
    [SerializeField] private float flickerIntensity = 0.2f;

    [Tooltip("Duraci�n del parpadeo en segundos")]
    [SerializeField] private float flickerDuration = 0.5f;

    [Header("Efectos Ambientales")]
    [Tooltip("�Activar variaci�n aleatoria de intensidad?")]
    [SerializeField] private bool randomIntensityVariation = true;

    [Tooltip("Magnitud de la variaci�n de intensidad (0-1)")]
    [Range(0, 1)]
    [SerializeField] private float intensityVariationMagnitude = 0.05f;

    [Tooltip("Velocidad de la variaci�n de intensidad")]
    [SerializeField] private float intensityVariationSpeed = 1.0f;

    [Header("Efectos de Part�culas")]
    [Tooltip("�Mostrar part�culas de polvo en el haz de luz?")]
    [SerializeField] private bool showDustParticles = true;

    [Tooltip("Densidad de part�culas (0-1)")]
    [Range(0, 1)]
    [SerializeField] private float dustDensity = 0.5f;

    // Variables privadas
    private float originalIntensity;
    private bool isFlickering = false;
    private float intensityVariationTime = 0f;
    private FlashlightVisibilityController visibilityController;

    void Start()
    {
        // Buscar referencias si no est�n asignadas
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

        // Configurar sistema de part�culas si existe
        if (dustParticles != null && showDustParticles)
        {
            SetupDustParticles();
        }
    }

    void Update()
    {
        // No hacer nada si la luz est� apagada
        if (!spotLight.enabled || spotLight.intensity <= 0.01f)
            return;

        // Aplicar variaci�n de intensidad si est� activada
        if (randomIntensityVariation && !isFlickering)
        {
            ApplyIntensityVariation();
        }
    }

    /// <summary>
    /// Configura el sistema de part�culas para el haz de luz
    /// </summary>
    private void SetupDustParticles()
    {
        var main = dustParticles.main;
        var emission = dustParticles.emission;
        var shape = dustParticles.shape;

        // Adaptar forma del sistema de part�culas al �ngulo del spotlight
        shape.angle = spotLight.spotAngle / 2f;

        // Ajustar emisi�n seg�n densidad
        emission.rateOverTimeMultiplier = dustDensity * 50f;

        // Activar/desactivar part�culas seg�n configuraci�n
        dustParticles.gameObject.SetActive(showDustParticles);
    }

    /// <summary>
    /// Aplica una variaci�n suave a la intensidad de la luz
    /// </summary>
    private void ApplyIntensityVariation()
    {
        intensityVariationTime += Time.deltaTime * intensityVariationSpeed;

        // Usar ruido Perlin para una variaci�n suave
        float noise = Mathf.PerlinNoise(intensityVariationTime, 0f) * 2f - 1f;
        float variation = noise * intensityVariationMagnitude;

        // Aplicar variaci�n a la intensidad original
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
            // Oscilaci�n r�pida de la intensidad
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
    /// Este m�todo puede ser llamado cuando se detecta un objeto oculto
    /// </summary>
    public void OnHiddenObjectDetected()
    {
        if (flickerOnDetection)
        {
            TriggerFlicker();
        }

        // Aumentar temporalmente la emisi�n de part�culas
        if (dustParticles != null && showDustParticles)
        {
            var emission = dustParticles.emission;
            StartCoroutine(TemporarilyBoostParticles(emission.rateOverTimeMultiplier * 3f));
        }
    }

    /// <summary>
    /// Aumenta temporalmente la emisi�n de part�culas
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
    /// Ajusta la densidad de las part�culas
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
    /// Activa/desactiva las part�culas de polvo
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