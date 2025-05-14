using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FlashingLight : MonoBehaviour
{
    [System.Serializable]
    public class FlashingObject
    {
        [Tooltip("Nombre descriptivo para este grupo de objetos")]
        public string name = "Flashing Group";

        [Tooltip("Objetos que parpadear�n juntos con el mismo patr�n")]
        public List<GameObject> objects = new List<GameObject>();

        [Header("Duraci�n del Parpadeo")]
        [Tooltip("Duraci�n m�nima del efecto de parpadeo en segundos")]
        public float minFlashingDuration = 0.5f;

        [Tooltip("Duraci�n m�xima del efecto de parpadeo en segundos")]
        public float maxFlashingDuration = 2f;

        [Header("Ciclo de Parpadeo")]
        [Tooltip("Tiempo que las luces permanecen encendidas (segundos)")]
        public float onDuration = 0.1f;

        [Tooltip("Tiempo que las luces permanecen apagadas (segundos)")]
        public float offDuration = 0.15f;

        [Header("Tiempo Entre Episodios de Parpadeo")]
        [Tooltip("Tiempo m�nimo entre episodios de parpadeo (segundos)")]
        public float minTimeBetweenFlashing = 3.5f;

        [Tooltip("Tiempo m�ximo entre episodios de parpadeo (segundos)")]
        public float maxTimeBetweenFlashing = 12f;

        [Header("Aleatorizaci�n")]
        [Tooltip("Variaci�n m�xima para tiempo encendido (�segundos)")]
        [Range(0f, 1f)]
        public float onVariation = 0.05f;

        [Tooltip("Variaci�n m�xima para tiempo apagado (�segundos)")]
        [Range(0f, 1f)]
        public float offVariation = 0.1f;

        [Tooltip("Probabilidad de fallar un parpadeo (0-1)")]
        [Range(0f, 1f)]
        public float failChance = 0.2f;

        [Header("Comportamiento")]
        [Tooltip("Estado de las luces entre episodios de parpadeo")]
        public bool stableStateIsOn = true;

        [Tooltip("Cantidad de parpadeos r�pidos al iniciar un episodio (efecto de arranque)")]
        [Range(0, 10)]
        public int initialFlickers = 3;

        [Tooltip("Velocidad de los parpadeos iniciales")]
        public float initialFlickerSpeed = 0.05f;

        // Variables internas
        [HideInInspector]
        public Coroutine flashingCoroutine;
        [HideInInspector]
        public bool isFlashing = false;

        /// <summary>
        /// Activa o desactiva todos los objetos de este grupo
        /// </summary>
        public void SetObjectsState(bool state)
        {
            foreach (GameObject obj in objects)
            {
                if (obj != null)
                {
                    obj.SetActive(state);
                }
            }
        }
    }

    [Header("Configuraci�n General")]
    [Tooltip("Duraci�n total del sistema en segundos (0 = infinito)")]
    public float systemDuration = 0f;

    [Tooltip("Iniciar autom�ticamente al activar el objeto")]
    public bool autoStart = true;

    [Tooltip("Si est� activado, se a�adir� variaci�n aleatoria a los tiempos")]
    public bool useRandomVariation = true;

    [Header("Grupos de Objetos Parpadeantes")]
    [Tooltip("Lista de grupos de objetos que parpadear�n independientemente")]
    public List<FlashingObject> flashingGroups = new List<FlashingObject>();

    // Variables internas
    private float elapsedSystemTime = 0f;
    private bool isSystemActive = false;

    private void Start()
    {
        // Establecer el estado inicial de los objetos
        foreach (FlashingObject group in flashingGroups)
        {
            group.SetObjectsState(group.stableStateIsOn);
        }

        // Iniciar autom�ticamente si est� configurado
        if (autoStart)
        {
            StartFlashing();
        }
    }

    /// <summary>
    /// Inicia el sistema de parpadeo para todos los grupos
    /// </summary>
    public void StartFlashing()
    {
        if (isSystemActive)
            return;

        isSystemActive = true;
        elapsedSystemTime = 0f;

        // Iniciar cada grupo de objetos
        foreach (FlashingObject group in flashingGroups)
        {
            if (group.objects.Count > 0 && !group.isFlashing)
            {
                group.flashingCoroutine = StartCoroutine(ManageFlashingObjectCoroutine(group));
                group.isFlashing = true;
            }
        }
    }

    /// <summary>
    /// Detiene el sistema de parpadeo para todos los grupos
    /// </summary>
    public void StopFlashing(bool turnOn = true)
    {
        if (!isSystemActive)
            return;

        // Detener todos los grupos
        foreach (FlashingObject group in flashingGroups)
        {
            if (group.isFlashing && group.flashingCoroutine != null)
            {
                StopCoroutine(group.flashingCoroutine);
                group.flashingCoroutine = null;
                group.isFlashing = false;
                group.SetObjectsState(turnOn);
            }
        }

        isSystemActive = false;
        elapsedSystemTime = 0f;
    }

    /// <summary>
    /// Corrutina principal que gestiona cu�ndo ocurre un episodio de parpadeo
    /// </summary>
    private IEnumerator ManageFlashingObjectCoroutine(FlashingObject group)
    {
        // Asegurarse de que los objetos est�n en su estado estable inicial
        group.SetObjectsState(group.stableStateIsOn);

        // Bucle principal: alternamos entre estado estable y episodios de parpadeo
        while (systemDuration <= 0 || elapsedSystemTime < systemDuration)
        {
            // Esperamos un tiempo aleatorio entre episodios de parpadeo
            float waitTime = Random.Range(group.minTimeBetweenFlashing, group.maxTimeBetweenFlashing);
            yield return new WaitForSeconds(waitTime);

            // Actualizar tiempo del sistema
            if (systemDuration > 0)
            {
                elapsedSystemTime += waitTime;
                if (elapsedSystemTime >= systemDuration)
                    break; // Salir si hemos excedido la duraci�n total del sistema
            }

            // Calcular cu�nto durar� este episodio de parpadeo
            float flashingDuration = Random.Range(group.minFlashingDuration, group.maxFlashingDuration);

            // Iniciar un episodio de parpadeo
            yield return StartCoroutine(FlashingEpisodeCoroutine(group, flashingDuration));

            // Volver al estado estable tras el episodio
            group.SetObjectsState(group.stableStateIsOn);
        }

        // Finalizaci�n del bucle principal
        group.isFlashing = false;
        group.flashingCoroutine = null;

        // Verificar si todos los grupos han terminado para marcar el sistema como inactivo
        bool anyGroupActive = false;
        foreach (FlashingObject g in flashingGroups)
        {
            if (g.isFlashing)
            {
                anyGroupActive = true;
                break;
            }
        }

        if (!anyGroupActive)
        {
            isSystemActive = false;
        }
    }

    /// <summary>
    /// Corrutina que ejecuta un episodio completo de parpadeo con duraci�n espec�fica
    /// </summary>
    private IEnumerator FlashingEpisodeCoroutine(FlashingObject group, float duration)
    {
        float episodeElapsedTime = 0f;

        // Efecto de parpadeos iniciales
        if (group.initialFlickers > 0)
        {
            yield return StartCoroutine(InitialFlickersCoroutine(group));

            // Actualizar el tiempo transcurrido
            float flickersDuration = group.initialFlickers * 2 * group.initialFlickerSpeed;
            episodeElapsedTime += flickersDuration;

            // Verificar si ya hemos superado la duraci�n
            if (episodeElapsedTime >= duration)
            {
                yield break;
            }
        }

        // Bucle principal de parpadeo durante el episodio
        bool isOn = group.stableStateIsOn;

        while (episodeElapsedTime < duration)
        {
            // Ciclo de un parpadeo completo (apagado-encendido)
            for (int j = 0; j < 2; j++)
            {
                // Verificar si ya hemos superado la duraci�n
                if (episodeElapsedTime >= duration)
                {
                    break;
                }

                // Alternar el estado
                isOn = !isOn;
                group.SetObjectsState(isOn);

                // Determinar la duraci�n de este estado
                float stateDuration;

                if (isOn)
                {
                    stateDuration = useRandomVariation
                        ? group.onDuration + Random.Range(-group.onVariation, group.onVariation)
                        : group.onDuration;
                }
                else
                {
                    stateDuration = useRandomVariation
                        ? group.offDuration + Random.Range(-group.offVariation, group.offVariation)
                        : group.offDuration;

                    // Posibilidad de que falle el parpadeo y permanezca apagado m�s tiempo
                    if (Random.value < group.failChance)
                    {
                        stateDuration *= Random.Range(2f, 4f);
                    }
                }

                // Asegurar que la duraci�n no sea negativa
                stateDuration = Mathf.Max(0.01f, stateDuration);

                // Esperar la duraci�n calculada
                yield return new WaitForSeconds(stateDuration);

                // Actualizar tiempo transcurrido
                episodeElapsedTime += stateDuration;
            }

            // Peque�a pausa entre parpadeos
            if (episodeElapsedTime < duration)
            {
                float pauseDuration = Random.Range(0.05f, 0.2f);
                yield return new WaitForSeconds(pauseDuration);
                episodeElapsedTime += pauseDuration;
            }
        }
    }

    /// <summary>
    /// Corrutina para los parpadeos iniciales r�pidos
    /// </summary>
    private IEnumerator InitialFlickersCoroutine(FlashingObject group)
    {
        bool state = false;

        // Realizar varios parpadeos r�pidos
        for (int i = 0; i < group.initialFlickers * 2; i++)
        {
            state = !state;
            group.SetObjectsState(state);
            yield return new WaitForSeconds(group.initialFlickerSpeed);
        }
    }

    private void Update()
    {
        // Actualizar el tiempo del sistema
        if (isSystemActive && systemDuration > 0)
        {
            elapsedSystemTime += Time.deltaTime;

            // Verificar si debemos detener el sistema por haber superado su duraci�n
            if (elapsedSystemTime >= systemDuration)
            {
                StopFlashing(true);
            }
        }
    }

    private void OnDisable()
    {
        // Asegurarse de detener todas las corrutinas si el objeto se desactiva
        StopAllCoroutines();

        foreach (FlashingObject group in flashingGroups)
        {
            group.isFlashing = false;
            group.flashingCoroutine = null;
        }

        isSystemActive = false;
    }
}