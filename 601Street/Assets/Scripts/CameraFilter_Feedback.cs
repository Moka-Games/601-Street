using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraFilter_Feedback : MonoBehaviour
{
    [Header("References")]
    private Volume postProcessVolume;
    private Transform playerTransform;
    private Collider triggerCollider;
    private bool isPlayerInside = false;

    [Header("Effect Settings")]
    [SerializeField] private float maxEffectRange = 10f;  // Distancia máxima dentro del trigger
    [SerializeField] private float minEffectRange = 2f;   // Distancia mínima para efecto máximo

    [Header("Effect Intensities")]
    [SerializeField] private float maxFilmGrainIntensity = 1f;
    [SerializeField] private float maxLensDistortionIntensity = -50f;
    [SerializeField] private float maxVignetteIntensity = 0.5f;

    private FilmGrain filmGrain;
    private LensDistortion lensDistortion;
    private Vignette vignette;

    private void Start()
    {
        postProcessVolume = GetComponent<Volume>();
        triggerCollider = GetComponent<Collider>();
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        if (triggerCollider != null && !triggerCollider.isTrigger)
        {
            Debug.LogWarning("El collider debe ser trigger. Configurándolo automáticamente.");
            triggerCollider.isTrigger = true;
        }

        if (postProcessVolume.profile.TryGet(out filmGrain)) { }
        if (postProcessVolume.profile.TryGet(out lensDistortion)) { }
        if (postProcessVolume.profile.TryGet(out vignette)) { }

        if (playerTransform == null)
        {
            Debug.LogError("No se encontró el jugador. Asegúrate de que tiene el tag 'Player'");
            enabled = false;
            return;
        }

        if (triggerCollider == null)
        {
            Debug.LogError("No se encontró el Collider en el objeto");
            enabled = false;
            return;
        }

        if (filmGrain == null || lensDistortion == null || vignette == null)
        {
            Debug.LogError("Faltan algunos efectos de post-procesamiento en el volumen");
            enabled = false;
            return;
        }

        UpdateEffects(0);
    }

    private void Update()
    {
        if (!isPlayerInside)
        {
            UpdateEffects(0);
            return;
        }

        Vector3 colliderCenter = triggerCollider.bounds.center;

        float distanceToPlayer = Vector3.Distance(colliderCenter, playerTransform.position);

        float effectIntensity = 1f - Mathf.Clamp01((distanceToPlayer - minEffectRange) / (maxEffectRange - minEffectRange));

        UpdateEffects(effectIntensity);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            UpdateEffects(0); 
        }
    }

    private void UpdateEffects(float intensity)
    {
        // Actualizar Film Grain
        if (filmGrain != null && filmGrain.active)
        {
            filmGrain.intensity.value = maxFilmGrainIntensity * intensity;
        }

        // Actualizar Lens Distortion
        if (lensDistortion != null && lensDistortion.active)
        {
            lensDistortion.intensity.value = maxLensDistortionIntensity * intensity;
        }

        // Actualizar Vignette
        if (vignette != null && vignette.active)
        {
            vignette.intensity.value = maxVignetteIntensity * intensity;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (triggerCollider != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(triggerCollider.bounds.center, 0.5f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(triggerCollider.bounds.center, maxEffectRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(triggerCollider.bounds.center, minEffectRange);
        }
    }
}