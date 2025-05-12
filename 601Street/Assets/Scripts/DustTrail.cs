using UnityEngine;

public class DustTrail : MonoBehaviour
{
    [SerializeField] private GameObject dustPrefab;

    [Header("Posiciones de instanciaci�n -- Caminando Recto")]
    [SerializeField] private Transform position_1_Straight;
    [SerializeField] private Transform position_2_Straight;

    [Header("Posiciones de instanciaci�n -- Caminando Derecha")]
    [SerializeField] private Transform position_1_Right;
    [SerializeField] private Transform position_2_Right;

    [Header("Posiciones de instanciaci�n -- Caminando Izquierda")]
    [SerializeField] private Transform position_1_Left;
    [SerializeField] private Transform position_2_Left;

    // Variable para rastrear la alternancia entre posiciones
    private bool useFirstPosition = true;

    /// <summary>
    /// Instancia un efecto de polvo alternando entre las dos posiciones configuradas.
    /// El efecto se destruye autom�ticamente despu�s de 2 segundos.
    /// </summary>
    public void CreateDustEffect()
    {
        if (dustPrefab == null)
        {
            Debug.LogWarning("No se ha asignado un prefab de polvo en el script DustTrail");
            return;
        }

        if (position_1_Straight == null || position_2_Straight == null)
        {
            Debug.LogWarning("Se deben asignar ambas posiciones en el script DustTrail");
            return;
        }

        // Seleccionar la posici�n basada en la alternancia
        Transform selectedPosition = useFirstPosition ? position_1_Straight : position_2_Straight;

        // Instanciar el prefab en la posici�n seleccionada
        GameObject dustInstance = Instantiate(dustPrefab, selectedPosition.position, selectedPosition.rotation);

        // Destruir la instancia despu�s de 2 segundos
        Destroy(dustInstance, 2f);

        // Alternar para la pr�xima llamada
        useFirstPosition = !useFirstPosition;
    }

    public void CreateDustEffect_Right()
    {
        if (dustPrefab == null)
        {
            Debug.LogWarning("No se ha asignado un prefab de polvo en el script DustTrail");
            return;
        }

        if (position_1_Right == null || position_2_Right == null)
        {
            Debug.LogWarning("Se deben asignar ambas posiciones en el script DustTrail");
            return;
        }

        // Seleccionar la posici�n basada en la alternancia
        Transform selectedPosition = useFirstPosition ? position_1_Right : position_2_Right;

        // Instanciar el prefab en la posici�n seleccionada
        GameObject dustInstance = Instantiate(dustPrefab, selectedPosition.position, selectedPosition.rotation);

        // Destruir la instancia despu�s de 2 segundos
        Destroy(dustInstance, 2f);

        // Alternar para la pr�xima llamada
        useFirstPosition = !useFirstPosition;
    }

    public void CreateDustEffect_Left()
    {
        if (dustPrefab == null)
        {
            Debug.LogWarning("No se ha asignado un prefab de polvo en el script DustTrail");
            return;
        }

        if (position_1_Left == null || position_2_Left == null)
        {
            Debug.LogWarning("Se deben asignar ambas posiciones en el script DustTrail");
            return;
        }

        // Seleccionar la posici�n basada en la alternancia
        Transform selectedPosition = useFirstPosition ? position_1_Left : position_2_Left;

        // Instanciar el prefab en la posici�n seleccionada
        GameObject dustInstance = Instantiate(dustPrefab, selectedPosition.position, selectedPosition.rotation);

        // Destruir la instancia despu�s de 2 segundos
        Destroy(dustInstance, 2f);

        // Alternar para la pr�xima llamada
        useFirstPosition = !useFirstPosition;
    }
}