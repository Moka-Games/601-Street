using System;
using System.Collections.Generic;
using UnityEngine;

// Clase base para definir una misi�n (ScriptableObject)
[CreateAssetMenu(fileName = "Nueva Misi�n", menuName = "Misiones/Crear Misi�n")]
public class Mision : ScriptableObject
{
    [Header("Informaci�n B�sica")]
    [SerializeField] private string misionID; // ID �nico de la misi�n
    [SerializeField] private string misionNombre; // Nombre descriptivo de la misi�n
    [SerializeField] private string descripcion; // Descripci�n de la misi�n

    [Header("Configuraci�n de Avance")]
    [SerializeField] private Mision siguienteMision; // Misi�n que se activar� al completar esta (opcional)

    // Eventos para personalizar comportamientos
    public event Action<Mision> OnMisionIniciada;
    public event Action<Mision> OnMisionCompletada;
    public event Action<Mision> OnMisionCancelada;

    // Getters p�blicos
    public string ID => misionID;
    public string Nombre => misionNombre;
    public string Descripcion => descripcion;
    public Mision SiguienteMision => siguienteMision;

    // M�todos virtuales que pueden ser sobrescritos en clases derivadas

    // M�todo llamado cuando se inicia la misi�n
    public virtual void IniciarMision()
    {
        Debug.Log($"Misi�n iniciada: {misionNombre} (ID: {misionID})");
        OnMisionIniciada?.Invoke(this);
    }

    // M�todo llamado cuando se completa la misi�n
    public virtual void CompletarMision()
    {
        Debug.Log($"Misi�n completada: {misionNombre} (ID: {misionID})");
        OnMisionCompletada?.Invoke(this);
    }

    // M�todo llamado cuando se cancela la misi�n
    public virtual void CancelarMision()
    {
        Debug.Log($"Misi�n cancelada: {misionNombre} (ID: {misionID})");
        OnMisionCancelada?.Invoke(this);
    }

    // M�todo para validar la configuraci�n de la misi�n en el editor
    public virtual void ValidarMision()
    {
        if (string.IsNullOrEmpty(misionID))
        {
            Debug.LogError($"Misi�n '{name}' no tiene un ID asignado.");
        }

        if (string.IsNullOrEmpty(misionNombre))
        {
            Debug.LogWarning($"Misi�n con ID '{misionID}' no tiene un nombre asignado.");
        }
    }

    // Este m�todo permite reiniciar la misi�n a su estado inicial
    public virtual void Reiniciar()
    {
        // Implementaci�n b�sica, las clases derivadas pueden a�adir comportamiento
        Debug.Log($"Misi�n reiniciada: {misionNombre} (ID: {misionID})");
    }
}