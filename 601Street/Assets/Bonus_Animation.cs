using UnityEngine;
using DG.Tweening;

public class Bonus_Animation : MonoBehaviour
{
    [Header("Animación de Flotación")]
    [SerializeField] private float floatAmplitude = 0.5f;      // Amplitud del movimiento vertical
    [SerializeField] private float floatDuration = 2f;        // Duración de un ciclo completo
    [SerializeField] private float rotationAmplitude = 15f;   // Amplitud de rotación
    [SerializeField] private float rotationDuration = 3f;     // Duración de rotación
    [SerializeField] private float scaleAmplitude = 0.1f;     // Amplitud de escalado
    [SerializeField] private float scaleDuration = 1.5f;      // Duración de escalado

    private Vector3 initialPosition;
    private Vector3 initialScale;
    private Sequence floatingSequence;

    void Start()
    {
        // Guardar posición y escala inicial
        initialPosition = transform.position;
        initialScale = transform.localScale;

        // Crear la animación de flotación
        CreateFloatingAnimation();
    }

    void CreateFloatingAnimation()
    {
        // Crear una secuencia para combinar todas las animaciones
        floatingSequence = DOTween.Sequence();

        // Animación de movimiento vertical (flotación principal)
        var floatTween = transform.DOMoveY(initialPosition.y + floatAmplitude, floatDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        // Animación de rotación suave
        var rotationTween = transform.DORotate(new Vector3(0, 0, rotationAmplitude), rotationDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        // Animación de escalado sutil
        var scaleTween = transform.DOScale(initialScale + Vector3.one * scaleAmplitude, scaleDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        // Agregar todas las animaciones a la secuencia
        floatingSequence.Join(floatTween);
        floatingSequence.Join(rotationTween);
        floatingSequence.Join(scaleTween);

        // Hacer que la secuencia se reproduzca infinitamente
        floatingSequence.SetLoops(-1);
    }

    void OnDestroy()
    {
        // Limpiar las animaciones al destruir el objeto
        if (floatingSequence != null)
        {
            floatingSequence.Kill();
        }
    }

    void OnDisable()
    {
        // Pausar animaciones cuando el objeto se desactive
        if (floatingSequence != null)
        {
            floatingSequence.Pause();
        }
    }

    void OnEnable()
    {
        // Reanudar animaciones cuando el objeto se reactive
        if (floatingSequence != null)
        {
            floatingSequence.Play();
        }
    }
}