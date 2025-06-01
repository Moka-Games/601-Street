using UnityEngine;
using System.Collections;

/// <summary>
/// Gestor para reproducir sonidos que persisten después de que los objetos se destruyan
/// Crea objetos temporales con AudioSource que se auto-destruyen
/// </summary>
public class AudioPlaybackManager : MonoBehaviour
{
    private static AudioPlaybackManager _instance;
    public static AudioPlaybackManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Buscar una instancia existente
                _instance = FindAnyObjectByType<AudioPlaybackManager>();

                if (_instance == null)
                {
                    // Crear una nueva instancia
                    GameObject managerObject = new GameObject("AudioPlaybackManager");
                    _instance = managerObject.AddComponent<AudioPlaybackManager>();
                    DontDestroyOnLoad(managerObject);
                }
            }
            return _instance;
        }
    }

    [Header("Configuración de Audio")]
    [SerializeField] private float defaultVolume = 1f;
    [SerializeField] private bool enableDebugLogs = true;

    private void Awake()
    {
        // Implementar singleton
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (enableDebugLogs)
                Debug.Log("AudioPlaybackManager inicializado como singleton");
        }
        else if (_instance != this)
        {
            if (enableDebugLogs)
                Debug.Log("AudioPlaybackManager duplicado destruido");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Reproduce un clip de audio en una posición específica del mundo
    /// </summary>
    /// <param name="clip">Clip de audio a reproducir</param>
    /// <param name="worldPosition">Posición mundial donde reproducir el sonido</param>
    /// <param name="volume">Volumen del sonido (0-1)</param>
    /// <param name="pitch">Pitch del sonido (default: 1)</param>
    /// <param name="autoDestroyDelay">Tiempo en segundos antes de destruir el objeto (default: 3)</param>
    /// <param name="is3D">Si el sonido debe ser 3D espacial</param>
    /// <param name="maxDistance">Distancia máxima para sonido 3D</param>
    public void PlaySoundAtPosition(AudioClip clip, Vector3 worldPosition, float volume = -1f,
                                   float pitch = 1f, float autoDestroyDelay = 3f, bool is3D = true,
                                   float maxDistance = 50f)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioPlaybackManager: No se puede reproducir un clip nulo");
            return;
        }

        // Usar volumen por defecto si no se especifica
        if (volume < 0f)
            volume = defaultVolume;

        StartCoroutine(CreateAndPlayAudioSource(clip, worldPosition, volume, pitch, autoDestroyDelay, is3D, maxDistance));
    }

    /// <summary>
    /// Reproduce un clip de audio sin posición específica (2D)
    /// </summary>
    /// <param name="clip">Clip de audio a reproducir</param>
    /// <param name="volume">Volumen del sonido (0-1)</param>
    /// <param name="pitch">Pitch del sonido (default: 1)</param>
    /// <param name="autoDestroyDelay">Tiempo en segundos antes de destruir el objeto (default: 3)</param>
    public void PlaySound2D(AudioClip clip, float volume = -1f, float pitch = 1f, float autoDestroyDelay = 3f)
    {
        PlaySoundAtPosition(clip, Vector3.zero, volume, pitch, autoDestroyDelay, false, 0f);
    }

    /// <summary>
    /// Corrutina que crea un GameObject temporal con AudioSource y lo destruye automáticamente
    /// </summary>
    private IEnumerator CreateAndPlayAudioSource(AudioClip clip, Vector3 position, float volume,
                                                float pitch, float autoDestroyDelay, bool is3D, float maxDistance)
    {
        // Crear GameObject temporal
        GameObject audioObject = new GameObject($"TempAudio_{clip.name}");
        audioObject.transform.position = position;

        // Configurar AudioSource
        AudioSource audioSource = audioObject.AddComponent<AudioSource>();
        ConfigureAudioSource(audioSource, clip, volume, pitch, is3D, maxDistance);

        // Reproducir el sonido
        audioSource.Play();

        if (enableDebugLogs)
        {
            Debug.Log($"Reproduciendo sonido '{clip.name}' en posición {position}. " +
                     $"Duración: {clip.length}s, Auto-destrucción en: {autoDestroyDelay}s");
        }

        // Esperar el tiempo especificado antes de destruir
        yield return new WaitForSeconds(autoDestroyDelay);

        // Destruir el objeto temporal
        if (audioObject != null)
        {
            if (enableDebugLogs)
                Debug.Log($"Destruyendo objeto de audio temporal '{audioObject.name}'");

            Destroy(audioObject);
        }
    }

    /// <summary>
    /// Configura las propiedades del AudioSource
    /// </summary>
    private void ConfigureAudioSource(AudioSource audioSource, AudioClip clip, float volume,
                                     float pitch, bool is3D, float maxDistance)
    {
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.pitch = pitch;
        audioSource.playOnAwake = false;

        if (is3D)
        {
            // Configuración para sonido 3D espacial
            audioSource.spatialBlend = 1f; // Completamente 3D
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = maxDistance;
            audioSource.dopplerLevel = 1f;
        }
        else
        {
            // Configuración para sonido 2D
            audioSource.spatialBlend = 0f; // Completamente 2D
        }
    }

    /// <summary>
    /// Método conveniente para reproducir sonido de interacción de objetos
    /// </summary>
    /// <param name="clip">Clip a reproducir</param>
    /// <param name="objectPosition">Posición del objeto que interactúa</param>
    /// <param name="volume">Volumen (opcional)</param>
    public void PlayInteractionSound(AudioClip clip, Vector3 objectPosition, float volume = -1f)
    {
        PlaySoundAtPosition(clip, objectPosition, volume, 1f, 3f, true, 30f);
    }

    /// <summary>
    /// Método conveniente para reproducir sonido de recogida de items
    /// </summary>
    /// <param name="clip">Clip a reproducir</param>
    /// <param name="itemPosition">Posición del item recogido</param>
    /// <param name="volume">Volumen (opcional)</param>
    public void PlayPickupSound(AudioClip clip, Vector3 itemPosition, float volume = -1f)
    {
        PlaySoundAtPosition(clip, itemPosition, volume, 1.2f, 2f, true, 20f);
    }

    /// <summary>
    /// Configura el volumen por defecto
    /// </summary>
    public void SetDefaultVolume(float volume)
    {
        defaultVolume = Mathf.Clamp01(volume);
    }

    /// <summary>
    /// Habilita o deshabilita los logs de debug
    /// </summary>
    public void SetDebugLogs(bool enable)
    {
        enableDebugLogs = enable;
    }

    /// <summary>
    /// Método para limpiar todos los objetos de audio temporales (útil para cambios de escena)
    /// </summary>
    public void CleanupTemporaryAudioObjects()
    {
        GameObject[] tempAudioObjects = GameObject.FindGameObjectsWithTag("TempAudio");

        foreach (GameObject obj in tempAudioObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }

        if (enableDebugLogs)
            Debug.Log($"Limpiados {tempAudioObjects.Length} objetos de audio temporales");
    }

    // Métodos de debug para el inspector
    [ContextMenu("Test Audio Playback")]
    public void TestAudioPlayback()
    {
        if (Application.isPlaying)
        {
            Debug.Log("Prueba de AudioPlaybackManager - Reproduciendo sonido de prueba");
            // Aquí podrías cargar un clip de prueba desde Resources si tienes uno
            // AudioClip testClip = Resources.Load<AudioClip>("TestSound");
            // if (testClip != null)
            //     PlaySound2D(testClip, 0.5f);
        }
        else
        {
            Debug.LogWarning("El test solo funciona en modo Play");
        }
    }

    [ContextMenu("Cleanup Audio Objects")]
    public void CleanupAudioObjectsFromContext()
    {
        CleanupTemporaryAudioObjects();
    }

    [ContextMenu("Debug Audio Manager State")]
    public void DebugManagerState()
    {
        Debug.Log("=== AUDIO PLAYBACK MANAGER STATE ===");
        Debug.Log($"Instance: {(_instance != null ? "Active" : "NULL")}");
        Debug.Log($"Default Volume: {defaultVolume}");
        Debug.Log($"Debug Logs: {enableDebugLogs}");
        Debug.Log($"GameObject: {gameObject.name}");
        Debug.Log($"DontDestroyOnLoad: {gameObject.scene.name == "DontDestroyOnLoad"}");

        // Contar objetos de audio temporales activos
        GameObject[] tempObjects = GameObject.FindGameObjectsWithTag("TempAudio");
        Debug.Log($"Objetos de audio temporales activos: {tempObjects.Length}");

        foreach (GameObject obj in tempObjects)
        {
            AudioSource source = obj.GetComponent<AudioSource>();
            if (source != null)
            {
                Debug.Log($"  - {obj.name}: Playing={source.isPlaying}, Clip={source.clip?.name ?? "NULL"}");
            }
        }

        Debug.Log("=====================================");
    }
}