using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Helper para configurar autom�ticamente la navegaci�n de men�s
/// Detecta y organiza autom�ticamente los elementos navegables
/// </summary>
public class MenuAutoSetup : MonoBehaviour
{
    [Header("Configuraci�n Autom�tica")]
    [SerializeField] private bool setupOnStart = true;
    [SerializeField] private bool organizeByPosition = true;
    [SerializeField] private bool includeInactiveElements = false;

    [Header("Filtros")]
    [SerializeField] private List<string> excludeTags = new List<string>();
    [SerializeField] private List<string> excludeNames = new List<string>();

    private UINavigationManager navigationManager;

    private void Start()
    {
        if (setupOnStart)
        {
            SetupMenu();
        }
    }

    [ContextMenu("Setup Menu")]
    public void SetupMenu()
    {
        // Obtener o crear UINavigationManager
        navigationManager = GetComponent<UINavigationManager>();
        if (navigationManager == null)
        {
            navigationManager = gameObject.AddComponent<UINavigationManager>();
        }

        // Buscar todos los elementos navegables
        List<Selectable> foundElements = FindNavigableElements();

        // Organizar por posici�n si est� habilitado
        if (organizeByPosition)
        {
            foundElements = OrganizeByPosition(foundElements);
        }

        // Configurar elementos en el navigation manager
        ConfigureNavigationManager(foundElements);

        Debug.Log($"Menu configurado autom�ticamente con {foundElements.Count} elementos");
    }

    private List<Selectable> FindNavigableElements()
    {
        List<Selectable> elements = new List<Selectable>();

        // Buscar todos los Selectables en los hijos
        Selectable[] allSelectables = GetComponentsInChildren<Selectable>(includeInactiveElements);

        foreach (var selectable in allSelectables)
        {
            // Aplicar filtros
            if (ShouldIncludeElement(selectable))
            {
                elements.Add(selectable);
            }
        }

        return elements;
    }

    private bool ShouldIncludeElement(Selectable selectable)
    {
        // Verificar si est� en la lista de exclusi�n por nombre
        if (excludeNames.Contains(selectable.name))
            return false;

        // Verificar si tiene alg�n tag excluido
        foreach (string tag in excludeTags)
        {
            if (selectable.CompareTag(tag))
                return false;
        }

        // Verificar si es interactuable (opcional)
        if (!includeInactiveElements && !selectable.interactable)
            return false;

        return true;
    }

    private List<Selectable> OrganizeByPosition(List<Selectable> elements)
    {
        // Ordenar elementos por posici�n (de arriba a abajo, izquierda a derecha)
        elements.Sort((a, b) =>
        {
            Vector3 posA = a.transform.position;
            Vector3 posB = b.transform.position;

            // Primero por Y (arriba a abajo)
            int yComparison = posB.y.CompareTo(posA.y);
            if (yComparison != 0)
                return yComparison;

            // Luego por X (izquierda a derecha)
            return posA.x.CompareTo(posB.x);
        });

        return elements;
    }

    private void ConfigureNavigationManager(List<Selectable> elements)
    {
        // Limpiar elementos existentes
        navigationManager.NavigableElements.Clear();

        // A�adir nuevos elementos
        foreach (var element in elements)
        {
            navigationManager.AddNavigableElement(element);
        }
    }

    // M�todos p�blicos para configuraci�n manual
    public void AddElementToNavigation(Selectable element)
    {
        if (navigationManager != null)
        {
            navigationManager.AddNavigableElement(element);
        }
    }

    public void RemoveElementFromNavigation(Selectable element)
    {
        if (navigationManager != null)
        {
            navigationManager.RemoveNavigableElement(element);
        }
    }

    public void RefreshNavigation()
    {
        SetupMenu();
    }
}