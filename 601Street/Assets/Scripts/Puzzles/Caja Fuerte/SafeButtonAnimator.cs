using UnityEngine;

[RequireComponent(typeof(Animator))]
public class SafeButtonAnimator : MonoBehaviour
{
    [Tooltip("Tiempo mínimo entre activaciones de la animación en segundos")]
    public float minTimeBetweenPresses = 0.1f;

    [Tooltip("Nombre del parámetro trigger en el Animator")]
    public string pressAnimationTrigger = "Press";

    [Tooltip("Distancia que se moverá el botón al presionarse")]
    public float pressDepth = 0.005f;

    [Tooltip("Duración de la animación de presión")]
    public float pressDuration = 0.1f;

    [Tooltip("Duración de la animación de retorno")]
    public float releaseDuration = 0.1f;

    // Referencia al transform que se animará (puede ser este objeto o un hijo)
    [Tooltip("Transform que se moverá (si está vacío, se usará este objeto)")]
    public Transform buttonTransform;

    private Animator buttonAnimator;
    private float lastPressTime;
    private Vector3 originalPosition;
    private Vector3 pressedPosition;
    private bool isPressed = false;
    private float pressStartTime;
    private float releaseStartTime;

    // Para saber si estamos usando animaciones procedurales o el Animator
    private bool useProceduralAnimation = true;

    void Awake()
    {
        // Configurar el transform que se animará
        if (buttonTransform == null)
        {
            buttonTransform = transform;
        }

        // Guardar la posición original
        originalPosition = buttonTransform.localPosition;

        // Calcular la posición presionada (usando el eje Z para la profundidad)
        pressedPosition = originalPosition + new Vector3(0, 0, -pressDepth);

        // Intentar obtener el Animator
        buttonAnimator = GetComponent<Animator>();

        // Decidir qué tipo de animación usar
        useProceduralAnimation = (buttonAnimator == null) ||
                                 !HasParameter(pressAnimationTrigger);

        if (!useProceduralAnimation && buttonAnimator != null)
        {
            Debug.Log("Usando animación basada en Animator para " + gameObject.name);
        }
        else
        {
            Debug.Log("Usando animación procedural para " + gameObject.name);
        }
    }

    void Update()
    {
        // Solo actualizamos si estamos usando animación procedural
        if (!useProceduralAnimation) return;

        if (isPressed)
        {
            // Calcular el progreso de la animación de presión
            float elapsedTime = Time.time - pressStartTime;
            float t = Mathf.Clamp01(elapsedTime / pressDuration);

            // Interpolar entre la posición original y la presionada
            buttonTransform.localPosition = Vector3.Lerp(originalPosition, pressedPosition, t);

            // Si hemos terminado la animación de presión, empezar el retorno
            if (t >= 1.0f)
            {
                isPressed = false;
                releaseStartTime = Time.time;
            }
        }
        else if (buttonTransform.localPosition != originalPosition)
        {
            // Animación de retorno
            float elapsedTime = Time.time - releaseStartTime;
            float t = Mathf.Clamp01(elapsedTime / releaseDuration);

            // Interpolar de vuelta a la posición original
            buttonTransform.localPosition = Vector3.Lerp(pressedPosition, originalPosition, t);
        }
    }

    // Método para activar la animación de pulsado
    public void TriggerPressAnimation()
    {
        // Verificar si ha pasado suficiente tiempo desde la última pulsación
        if (Time.time - lastPressTime < minTimeBetweenPresses)
        {
            return;
        }

        // Actualizar tiempo de la última pulsación
        lastPressTime = Time.time;

        if (useProceduralAnimation)
        {
            // Iniciar la animación procedural
            isPressed = true;
            pressStartTime = Time.time;
        }
        else if (buttonAnimator != null)
        {
            // Usar el Animator
            buttonAnimator.SetTrigger(pressAnimationTrigger);
        }
    }

    // Utilidad para verificar si un parámetro existe en el Animator
    private bool HasParameter(string paramName)
    {
        if (buttonAnimator == null) return false;

        foreach (AnimatorControllerParameter param in buttonAnimator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }
}