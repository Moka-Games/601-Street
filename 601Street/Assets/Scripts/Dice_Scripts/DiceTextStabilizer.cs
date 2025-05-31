using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Mantiene los textos del dado con rotación fija mientras el dado gira
/// Aplica rotación inversa para contrarrestar la rotación del padre
/// </summary>
public class DiceTextStabilizer : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform diceTransform; // El transform del dado padre
    [SerializeField] private List<Transform> textsToStabilize = new List<Transform>(); // Lista de textos a estabilizar

    [Header("Configuración")]
    [SerializeField] private bool useWorldSpace = true; // Si true, mantiene rotación mundial. Si false, usa rotación local compensada
    [SerializeField] private Vector3 targetWorldRotation = Vector3.zero; // Rotación objetivo en espacio mundial

    // Rotaciones iniciales guardadas
    private Dictionary<Transform, Quaternion> initialLocalRotations = new Dictionary<Transform, Quaternion>();
    private Quaternion initialDiceRotation;

    private void Start()
    {
        InitializeStabilizer();
    }

    private void InitializeStabilizer()
    {
        // Si no se asignó el transform del dado, intentar encontrarlo
        if (diceTransform == null)
        {
            // Buscar el objeto "Dice" que es el que rota
            Transform dice = transform.Find("Dice");
            if (dice != null)
            {
                diceTransform = dice;
                Debug.Log("Transform del dado encontrado automáticamente");
            }
            else
            {
                Debug.LogError("DiceTextStabilizer: No se encontró el transform del dado. Asígnalo manualmente.");
                enabled = false;
                return;
            }
        }

        // Buscar automáticamente los textos si no se asignaron
        if (textsToStabilize.Count == 0)
        {
            FindTextComponents();
        }

        // Guardar la rotación inicial del dado
        initialDiceRotation = diceTransform.localRotation;

        // Guardar las rotaciones iniciales de los textos
        foreach (Transform textTransform in textsToStabilize)
        {
            if (textTransform != null)
            {
                initialLocalRotations[textTransform] = textTransform.localRotation;
                Debug.Log($"Rotación inicial guardada para {textTransform.name}: {textTransform.localRotation.eulerAngles}");
            }
        }

        Debug.Log($"DiceTextStabilizer inicializado con {textsToStabilize.Count} textos");
    }

    private void FindTextComponents()
    {
        // Buscar componentes específicos por nombre dentro del dado
        Transform diceResult = diceTransform.Find("Dice_Result");
        if (diceResult != null)
        {
            textsToStabilize.Add(diceResult);
            Debug.Log("Dice_Result encontrado y añadido");
        }

        Transform bonusTxt = diceTransform.Find("Bonus_Txt");
        if (bonusTxt != null)
        {
            textsToStabilize.Add(bonusTxt);
            Debug.Log("Bonus_Txt encontrado y añadido");
        }

        // Si no encontramos los componentes específicos, buscar todos los TMP_Text
        if (textsToStabilize.Count == 0)
        {
            TMPro.TMP_Text[] allTexts = diceTransform.GetComponentsInChildren<TMPro.TMP_Text>();
            foreach (var text in allTexts)
            {
                textsToStabilize.Add(text.transform);
                Debug.Log($"Texto encontrado y añadido: {text.name}");
            }
        }
    }

    private void LateUpdate()
    {
        // LateUpdate se ejecuta después de todas las actualizaciones normales
        // Esto asegura que corregimos la rotación después de que el dado haya sido rotado
        StabilizeTexts();
    }

    private void StabilizeTexts()
    {
        if (diceTransform == null) return;

        foreach (Transform textTransform in textsToStabilize)
        {
            if (textTransform != null)
            {
                if (useWorldSpace)
                {
                    // Método 1: Mantener una rotación mundial específica
                    // Los textos siempre mirarán en la dirección especificada en el espacio mundial
                    textTransform.rotation = Quaternion.Euler(targetWorldRotation);
                }
                else
                {
                    // Método 2: Compensar la rotación del dado con rotación inversa
                    // Esto mantiene la orientación relativa inicial de los textos

                    // Obtenemos cuánto ha rotado el dado desde su posición inicial
                    Quaternion diceRotationDelta = diceTransform.localRotation * Quaternion.Inverse(initialDiceRotation);

                    // Aplicamos la rotación inversa para contrarrestar el movimiento del dado
                    textTransform.localRotation = Quaternion.Inverse(diceRotationDelta) * initialLocalRotations[textTransform];
                }
            }
        }
    }

    /// <summary>
    /// Añade un texto a la lista de estabilización
    /// </summary>
    public void AddTextToStabilize(Transform textTransform)
    {
        if (!textsToStabilize.Contains(textTransform))
        {
            textsToStabilize.Add(textTransform);
            initialLocalRotations[textTransform] = textTransform.localRotation;
            Debug.Log($"Texto añadido a estabilización: {textTransform.name}");
        }
    }

    /// <summary>
    /// Remueve un texto de la lista de estabilización
    /// </summary>
    public void RemoveTextFromStabilization(Transform textTransform)
    {
        if (textsToStabilize.Contains(textTransform))
        {
            textsToStabilize.Remove(textTransform);
            initialLocalRotations.Remove(textTransform);
            Debug.Log($"Texto removido de estabilización: {textTransform.name}");
        }
    }

    /// <summary>
    /// Actualiza la rotación objetivo
    /// </summary>
    public void SetTargetRotation(Vector3 rotation)
    {
        targetWorldRotation = rotation;
    }

    /// <summary>
    /// Cambia el modo de estabilización
    /// </summary>
    public void SetUseWorldSpace(bool useWorld)
    {
        useWorldSpace = useWorld;
    }

    /// <summary>
    /// Reinicia las rotaciones guardadas al estado actual
    /// </summary>
    [ContextMenu("Reset Initial Rotations")]
    public void ResetInitialRotations()
    {
        initialDiceRotation = diceTransform.localRotation;
        initialLocalRotations.Clear();

        foreach (Transform textTransform in textsToStabilize)
        {
            if (textTransform != null)
            {
                initialLocalRotations[textTransform] = textTransform.localRotation;
            }
        }

        Debug.Log("Rotaciones iniciales reseteadas");
    }

    #region Debug Methods

    [ContextMenu("Debug Stabilizer Info")]
    public void DebugStabilizerInfo()
    {
        Debug.Log("=== DICE TEXT STABILIZER INFO ===");
        Debug.Log($"Dice Transform: {(diceTransform != null ? diceTransform.name : "NULL")}");
        Debug.Log($"Current Dice Rotation: {(diceTransform != null ? diceTransform.localRotation.eulerAngles.ToString() : "N/A")}");
        Debug.Log($"Initial Dice Rotation: {initialDiceRotation.eulerAngles}");
        Debug.Log($"Use World Space: {useWorldSpace}");
        Debug.Log($"Target World Rotation: {targetWorldRotation}");
        Debug.Log($"Texts to stabilize: {textsToStabilize.Count}");

        foreach (Transform text in textsToStabilize)
        {
            if (text != null)
            {
                Debug.Log($"- {text.name} (Current local rotation: {text.localRotation.eulerAngles})");
            }
        }

        Debug.Log("================================");
    }

    [ContextMenu("Force Find Texts")]
    public void ForceFindTexts()
    {
        textsToStabilize.Clear();
        initialLocalRotations.Clear();

        if (diceTransform == null)
        {
            Debug.LogError("Primero asigna el Dice Transform");
            return;
        }

        FindTextComponents();

        // Guardar las nuevas rotaciones iniciales
        foreach (Transform textTransform in textsToStabilize)
        {
            if (textTransform != null)
            {
                initialLocalRotations[textTransform] = textTransform.localRotation;
            }
        }
    }

    [ContextMenu("Test Rotation Compensation")]
    public void TestRotationCompensation()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Esta prueba solo funciona en Play Mode");
            return;
        }

        if (diceTransform != null)
        {
            // Rotar el dado 45 grados en Y para probar
            diceTransform.Rotate(0, 45, 0);
            Debug.Log("Dado rotado 45 grados. Los textos deberían mantenerse estables.");
        }
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        if (diceTransform != null)
        {
            // Dibujar el eje forward del dado
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(diceTransform.position, diceTransform.forward * 0.5f);

            // Dibujar líneas hacia los textos
            Gizmos.color = Color.cyan;
            foreach (Transform text in textsToStabilize)
            {
                if (text != null)
                {
                    Gizmos.DrawLine(diceTransform.position, text.position);

                    // Dibujar el eje forward del texto
                    Gizmos.color = Color.green;
                    Gizmos.DrawRay(text.position, text.forward * 0.2f);
                    Gizmos.color = Color.cyan;
                }
            }
        }
    }

    private void OnValidate()
    {
        // Validación en el editor
        if (diceTransform != null && textsToStabilize.Count > 0)
        {
            Debug.Log($"Configuración validada: {textsToStabilize.Count} textos serán estabilizados");
        }
    }
}