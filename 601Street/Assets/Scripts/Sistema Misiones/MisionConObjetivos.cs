using System.Collections.Generic;
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Nueva Misi�n con Objetivos", menuName = "Misiones/Crear Misi�n con Objetivos")]
public class MisionConObjetivos : Mision
{
    [Serializable]
    public class ObjetivoMision
    {
        public string id;
        public string descripcion;
        public bool completado;

        public ObjetivoMision(string id, string descripcion)
        {
            this.id = id;
            this.descripcion = descripcion;
            this.completado = false;
        }
    }

    [Header("Objetivos")]
    [SerializeField] private List<ObjetivoMision> objetivos = new List<ObjetivoMision>();
    [SerializeField] private bool completarAutomaticamente = true; // Si es verdadero, la misi�n se completa cuando todos los objetivos est�n completos

    // Eventos adicionales
    public event Action<ObjetivoMision> OnObjetivoCompletado;

    // Propiedades p�blicas
    public IReadOnlyList<ObjetivoMision> Objetivos => objetivos;
    public int TotalObjetivos => objetivos.Count;
    public int ObjetivosCompletados => objetivos.Count(o => o.completado);

    public override void IniciarMision()
    {
        // Reiniciar el estado de los objetivos
        foreach (var objetivo in objetivos)
        {
            objetivo.completado = false;
        }

        base.IniciarMision();
    }

    public override void Reiniciar()
    {
        // Reiniciar el estado de los objetivos
        foreach (var objetivo in objetivos)
        {
            objetivo.completado = false;
        }

        base.Reiniciar();
    }

    // Completar un objetivo espec�fico por ID
    public bool CompletarObjetivo(string objetivoID)
    {
        var objetivo = objetivos.Find(o => o.id == objetivoID);

        if (objetivo != null && !objetivo.completado)
        {
            objetivo.completado = true;
            Debug.Log($"Objetivo completado: {objetivo.descripcion}");

            OnObjetivoCompletado?.Invoke(objetivo);

            // Si todos los objetivos est�n completos y se debe completar autom�ticamente
            if (completarAutomaticamente && objetivos.All(o => o.completado))
            {
                CompletarMision();
            }

            return true;
        }

        if (objetivo == null)
        {
            Debug.LogWarning($"No se encontr� un objetivo con ID: {objetivoID}");
        }

        return false;
    }

    // Verificar si un objetivo espec�fico est� completo
    public bool EstaObjetivoCompleto(string objetivoID)
    {
        var objetivo = objetivos.Find(o => o.id == objetivoID);

        if (objetivo != null)
        {
            return objetivo.completado;
        }

        Debug.LogWarning($"No se encontr� un objetivo con ID: {objetivoID}");
        return false;
    }

    // Verificar si todos los objetivos est�n completos
    public bool EstanTodosObjetivosCompletos()
    {
        return objetivos.Count > 0 && objetivos.All(o => o.completado);
    }

    // Agregar un nuevo objetivo (�til para objetivos din�micos)
    public void AgregarObjetivo(string id, string descripcion)
    {
        if (objetivos.Any(o => o.id == id))
        {
            Debug.LogWarning($"Ya existe un objetivo con ID: {id}");
            return;
        }

        objetivos.Add(new ObjetivoMision(id, descripcion));
        Debug.Log($"Objetivo a�adido: {descripcion}");
    }

    // Eliminar un objetivo
    public bool EliminarObjetivo(string objetivoID)
    {
        int indice = objetivos.FindIndex(o => o.id == objetivoID);

        if (indice >= 0)
        {
            objetivos.RemoveAt(indice);
            return true;
        }

        return false;
    }
}

// Extensi�n para funciones �tiles de enumerables
public static class EnumerableExtensions
{
    // M�todo Count con predicado (equivalente a LINQ Count)
    public static int Count<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        int count = 0;
        foreach (T element in source)
        {
            if (predicate(element))
            {
                count++;
            }
        }
        return count;
    }

    // M�todo All (equivalente a LINQ All)
    public static bool All<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        foreach (T element in source)
        {
            if (!predicate(element))
            {
                return false;
            }
        }
        return true;
    }

    // M�todo Any (equivalente a LINQ Any con predicado)
    public static bool Any<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        foreach (T element in source)
        {
            if (predicate(element))
            {
                return true;
            }
        }
        return false;
    }
}