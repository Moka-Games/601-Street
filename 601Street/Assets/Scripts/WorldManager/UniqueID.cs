using UnityEngine;
using System;

[ExecuteInEditMode]
public class UniqueID : MonoBehaviour
{
    [SerializeField]
    private string id = Guid.NewGuid().ToString();

    public string ID => id;

    // Para acceso desde el editor
    public string GetID() => id;

    private void Reset()
    {
        // Regenerar ID cuando se resetea el componente
        RegenerateID();
    }

    private void OnValidate()
    {
        // Asegurarse de que el ID no está vacío
        if (string.IsNullOrEmpty(id))
        {
            RegenerateID();
        }
    }

    // Método para generar un ID amigable basado en el nombre
    public void GenerateFriendlyID()
    {
        string objectName = gameObject.name.Replace(" ", "_");
        objectName = objectName.Replace("(", "").Replace(")", "");
        id = $"{objectName}_{DateTime.Now.Ticks % 10000}";
    }

    // Regenerar ID
    public void RegenerateID()
    {
        id = Guid.NewGuid().ToString();
    }
}