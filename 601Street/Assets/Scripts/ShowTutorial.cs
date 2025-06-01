using UnityEngine;
using System.Collections.Generic;

public class ShowTutorial : MonoBehaviour
{
    [Header("Tutorial Configuration")]
    public GameObject tutorial_To_Show;

    [Header("Tutorial ID")]
    [Tooltip("ID único para este tutorial. Si ya se mostró un tutorial con este ID, no se mostrará de nuevo.")]
    public string tutorialID = "default_tutorial";

    // Diccionario estático para registrar qué IDs de tutorial se han mostrado
    private static HashSet<string> shownTutorialIDs = new HashSet<string>();

    public void ShowTutorial_()
    {
        // Verificar si este ID de tutorial ya se ha mostrado
        if (shownTutorialIDs.Contains(tutorialID))
        {
            print($"Tutorial con ID '{tutorialID}' ya se ha mostrado anteriormente - NO se mostrará de nuevo");
            return;
        }

        // Si no se ha mostrado, registrar el ID y mostrar el tutorial
        shownTutorialIDs.Add(tutorialID);

        if (tutorial_To_Show != null)
        {
            tutorial_To_Show.SetActive(true);
            print($"Tutorial con ID '{tutorialID}' mostrado por primera vez");
        }
        else
        {
            print($"Tutorial con ID '{tutorialID}' no se puede mostrar - GameObject no asignado");
        }
    }

    /// <summary>
    /// Método público para verificar si un tutorial específico ya se ha mostrado
    /// </summary>
    public static bool HasTutorialBeenShown(string id)
    {
        return shownTutorialIDs.Contains(id);
    }

    /// <summary>
    /// Método para resetear un tutorial específico (útil para testing)
    /// </summary>
    public static void ResetTutorial(string id)
    {
        shownTutorialIDs.Remove(id);
        Debug.Log($"Tutorial con ID '{id}' reseteado");
    }

    /// <summary>
    /// Método para resetear todos los tutoriales (útil para testing)
    /// </summary>
    public static void ResetAllTutorials()
    {
        shownTutorialIDs.Clear();
        Debug.Log("Todos los tutoriales han sido reseteados");
    }

    /// <summary>
    /// Método para forzar mostrar un tutorial ignorando si ya se mostró
    /// </summary>
    public void ForceShowTutorial()
    {
        if (tutorial_To_Show != null)
        {
            tutorial_To_Show.SetActive(true);
            print($"Tutorial con ID '{tutorialID}' mostrado forzosamente (ignorando estado previo)");
        }
        else
        {
            print($"Tutorial con ID '{tutorialID}' no se puede mostrar - GameObject no asignado");
        }
    }

    [ContextMenu("Debug - Show Tutorial Count")]
    private void DebugShowTutorialCount()
    {
        Debug.Log($"Total de tutoriales únicos mostrados: {shownTutorialIDs.Count}");
        foreach (string id in shownTutorialIDs)
        {
            Debug.Log($"  - Tutorial mostrado: {id}");
        }
    }

    [ContextMenu("Debug - Reset This Tutorial")]
    private void DebugResetThisTutorial()
    {
        ResetTutorial(tutorialID);
    }

    [ContextMenu("Debug - Force Show This Tutorial")]
    private void DebugForceShowTutorial()
    {
        ForceShowTutorial();
    }
}