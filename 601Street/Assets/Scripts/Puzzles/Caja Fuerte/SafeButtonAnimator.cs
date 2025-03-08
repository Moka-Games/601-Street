using UnityEngine;

[RequireComponent(typeof(Animator))]
public class SafeButtonAnimator : MonoBehaviour
{
    [Tooltip("Tiempo m�nimo entre activaciones de la animaci�n en segundos")]
    public float minTimeBetweenPresses = 0.1f;

    [Tooltip("Nombre del par�metro trigger en el Animator")]
    public string pressAnimationTrigger = "Press";

    [Tooltip("Distancia que se mover� el bot�n al presionarse")]
    public float pressDepth = 0.005f;

    [Tooltip("Duraci�n de la animaci�n de presi�n")]
    public float pressDuration = 0.1f;

    [Tooltip("Duraci�n de la animaci�n de retorno")]
    public float releaseDuration = 0.1f;

    // Referencia al transform que se animar� (puede ser este objeto o un hijo)
    [Tooltip("Transform que se mover� (si est� vac�o, se usar� este objeto)")]
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
        // Configurar el transform que se animar�
        if (buttonTransform == null)
        {
            buttonTransform = transform;
        }

        // Guardar la posici�n original
        originalPosition = buttonTransform.localPosition;

        // Calcular la posici�n presionada (usando el eje Z para la profundidad)
        pressedPosition = originalPosition + new Vector3(0, 0, -pressDepth);

        // Intentar obtener el Animator
        buttonAnimator = GetComponent<Animator>();

        // Decidir qu� tipo de animaci�n usar
        useProceduralAnimation = (buttonAnimator == null) ||
                                 !HasParameter(pressAnimationTrigger);

        if (!useProceduralAnimation && buttonAnimator != null)
        {
            Debug.Log("Usando animaci�n basada en Animator para " + gameObject.name);
        }
        else
        {
            Debug.Log("Usando animaci�n procedural para " + gameObject.name);
        }
    }

    void Update()
    {
        // Solo actualizamos si estamos usando animaci�n procedural
        if (!useProceduralAnimation) return;

        if (isPressed)
        {
            // Calcular el progreso de la animaci�n de presi�n
            float elapsedTime = Time.time - pressStartTime;
            float t = Mathf.Clamp01(elapsedTime / pressDuration);

            // Interpolar entre la posici�n original y la presionada
            buttonTransform.localPosition = Vector3.Lerp(originalPosition, pressedPosition, t);

            // Si hemos terminado la animaci�n de presi�n, empezar el retorno
            if (t >= 1.0f)
            {
                isPressed = false;
                releaseStartTime = Time.time;
            }
        }
        else if (buttonTransform.localPosition != originalPosition)
        {
            // Animaci�n de retorno
            float elapsedTime = Time.time - releaseStartTime;
            float t = Mathf.Clamp01(elapsedTime / releaseDuration);

            // Interpolar de vuelta a la posici�n original
            buttonTransform.localPosition = Vector3.Lerp(pressedPosition, originalPosition, t);
        }
    }

    // M�todo para activar la animaci�n de pulsado
    public void TriggerPressAnimation()
    {
        // Verificar si ha pasado suficiente tiempo desde la �ltima pulsaci�n
        if (Time.time - lastPressTime < minTimeBetweenPresses)
        {
            return;
        }

        // Actualizar tiempo de la �ltima pulsaci�n
        lastPressTime = Time.time;

        if (useProceduralAnimation)
        {
            // Iniciar la animaci�n procedural
            isPressed = true;
            pressStartTime = Time.time;
        }
        else if (buttonAnimator != null)
        {
            // Usar el Animator
            buttonAnimator.SetTrigger(pressAnimationTrigger);
        }
    }

    // Utilidad para verificar si un par�metro existe en el Animator
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