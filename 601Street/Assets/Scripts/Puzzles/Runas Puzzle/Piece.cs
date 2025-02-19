using UnityEngine;

public class Piece : MonoBehaviour
{
    public int id;
    public int pairID; // ID único del par al que pertenece esta pieza

    private Path_Drawer pathDrawer; // Referencia al Path_Drawer
    private Vector2 startPosition;

    void Start()
    {
        // Obtener la referencia al Path_Drawer
        pathDrawer = FindAnyObjectByType<Path_Drawer>();
    }

    void OnMouseDown()
    {
        // Guardar la posición inicial
        startPosition = transform.position;

        // Iniciar el camino
        if (pathDrawer != null)
        {
            pathDrawer.StartPath(this, startPosition);
        }
    }

    void OnMouseUp()
    {
        // Finalizar el camino
        if (pathDrawer != null)
        {
            pathDrawer.EndPath(true);
        }
    }

    public void SetPairID(int id)
    {
        pairID = id;
    }
}