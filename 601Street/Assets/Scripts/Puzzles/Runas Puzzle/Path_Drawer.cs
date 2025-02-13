using UnityEngine;
using System.Collections.Generic;

public class Path_Drawer : MonoBehaviour
{
    public Tablero_Manager tablero; // Referencia al Tablero_Manager
    public GameObject pathPiecePrefab; // Prefab para las piezas del camino
    public Transform pathParent; // Contenedor para las piezas del camino

    private List<GameObject> currentPath = new List<GameObject>(); // Lista de piezas del camino actual
    private Piece currentDraggingPiece; // Pieza que se está arrastrando
    private Vector2 lastValidPosition; // Última posición válida en el tablero
    private bool isDrawingPath = false; // Indica si se está dibujando un camino

    void Update()
    {
        // Verificar si se está manteniendo pulsado el clic izquierdo y se está dibujando un camino
        if (Input.GetMouseButton(0) && isDrawingPath)
        {
            // Obtener la posición del mouse en el mundo
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            // Verificar si la posición del mouse es válida en el tablero
            if (tablero.CasillaExiste(mousePos, 0.01f))
            {
                // Verificar si la nueva posición es adyacente a la última posición válida
                if (IsAdjacentPosition(mousePos, lastValidPosition))
                {
                    // Actualizar la última posición válida
                    lastValidPosition = mousePos;

                    // Crear una nueva pieza del camino
                    GameObject newPathPiece = Instantiate(pathPiecePrefab, mousePos, Quaternion.identity, pathParent);
                    currentPath.Add(newPathPiece);

                    // Verificar si el camino llega al par de la pieza
                    if (CheckConnection(mousePos, currentDraggingPiece.pairID))
                    {
                        Debug.Log("Camino válido: conectado con el par.");
                        EndPath(true); // Finalizar el camino como válido
                    }
                }
            }
            else
            {
                // Si el ratón está fuera del tablero, finalizar el camino como inválido
                Debug.Log("Camino inválido: fuera del tablero.");
                EndPath(false);
            }
        }

        // Si se suelta el clic izquierdo, finalizar el camino
        if (Input.GetMouseButtonUp(0) && isDrawingPath)
        {
            Debug.Log("Camino inválido: clic soltado.");
            EndPath(false);
        }
    }

    // Iniciar el dibujo del camino
    public void StartPath(Piece piece, Vector2 startPosition)
    {
        currentDraggingPiece = piece;
        lastValidPosition = startPosition;
        isDrawingPath = true; // Establecer isDrawingPath en true

        // Crear la primera pieza del camino en la posición inicial
        GameObject newPathPiece = Instantiate(pathPiecePrefab, startPosition, Quaternion.identity, pathParent);
        currentPath.Add(newPathPiece);
    }

    // Finalizar el dibujo del camino
    public void EndPath(bool isValid)
    {
        if (isValid)
        {
            Debug.Log("Camino válido: conectado con el par.");
            // Aquí puedes agregar lógica adicional para manejar un camino válido
        }
        else
        {
            Debug.Log("Camino inválido: eliminando camino.");
            ClearPath();
        }

        // Reiniciar variables
        currentDraggingPiece = null;
        isDrawingPath = false;
        currentPath.Clear();
    }

    // Verificar si el camino conecta con la otra pieza del par
    private bool CheckConnection(Vector2 endPosition, int pairID)
    {
        // Buscar en el tablero si hay una pieza con el mismo pairID en la posición final
        foreach (Vector2 pos in tablero.casillasPosiciones)
        {
            if (pos == endPosition)
            {
                // Aquí debes implementar la lógica para verificar si hay una pieza con el mismo pairID
                // Por ejemplo, buscar en el tablero si hay una pieza en esa posición con el mismo pairID
                Debug.Log($"Verificando conexión en {endPosition} para el par {pairID}");
                return true; // Cambia esto según tu lógica
            }
        }
        return false;
    }

    // Eliminar el camino actual
    private void ClearPath()
    {
        foreach (GameObject pathPiece in currentPath)
        {
            Destroy(pathPiece);
        }
        currentPath.Clear();
    }

    // Verificar si dos posiciones son adyacentes (en X o Y, pero no en diagonal)
    private bool IsAdjacentPosition(Vector2 pos1, Vector2 pos2)
    {
        float deltaX = Mathf.Abs(pos1.x - pos2.x);
        float deltaY = Mathf.Abs(pos1.y - pos2.y);

        return (deltaX <= 1 && deltaY == 0) || (deltaY <= 1 && deltaX == 0);
    }
}