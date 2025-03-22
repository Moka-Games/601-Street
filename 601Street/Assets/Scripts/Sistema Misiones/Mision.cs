using System;
using System.Collections.Generic;
using UnityEngine;

// Clase base para definir una misión (ScriptableObject)
[CreateAssetMenu(fileName = "Nueva Misión", menuName = "Misiones/Crear Misión")]
public class Mision : ScriptableObject
{
    [Header("Información Básica")]
    [SerializeField] private string misionID; // ID único de la misión
    [SerializeField] private string misionNombre; // Nombre descriptivo de la misión
    [SerializeField] private string descripcion; // Descripción de la misión

    [Header("Configuración de Avance")]
    [SerializeField] private Mision siguienteMision; // Misión que se activará al completar esta (opcional)

    // Eventos para personalizar comportamientos
    public event Action<Mision> OnMisionIniciada;
    public event Action<Mision> OnMisionCompletada;
    public event Action<Mision> OnMisionCancelada;

    // Getters públicos
    public string ID => misionID;
    public string Nombre => misionNombre;
    public string Descripcion => descripcion;
    public Mision SiguienteMision => siguienteMision;

    // Métodos virtuales que pueden ser sobrescritos en clases derivadas

    // Método llamado cuando se inicia la misión
    public virtual void IniciarMision()
    {
        Debug.Log($"Misión iniciada: {misionNombre} (ID: {misionID})");
        OnMisionIniciada?.Invoke(this);
    }

    // Método llamado cuando se completa la misión
    public virtual void CompletarMision()
    {
        Debug.Log($"Misión completada: {misionNombre} (ID: {misionID})");
        OnMisionCompletada?.Invoke(this);
    }

    // Método llamado cuando se cancela la misión
    public virtual void CancelarMision()
    {
        Debug.Log($"Misión cancelada: {misionNombre} (ID: {misionID})");
        OnMisionCancelada?.Invoke(this);
    }

    // Método para validar la configuración de la misión en el editor
    public virtual void ValidarMision()
    {
        if (string.IsNullOrEmpty(misionID))
        {
            Debug.LogError($"Misión '{name}' no tiene un ID asignado.");
        }

        if (string.IsNullOrEmpty(misionNombre))
        {
            Debug.LogWarning($"Misión con ID '{misionID}' no tiene un nombre asignado.");
        }
    }

    // Este método permite reiniciar la misión a su estado inicial
    public virtual void Reiniciar()
    {
        // Implementación básica, las clases derivadas pueden añadir comportamiento
        Debug.Log($"Misión reiniciada: {misionNombre} (ID: {misionID})");
    }
}