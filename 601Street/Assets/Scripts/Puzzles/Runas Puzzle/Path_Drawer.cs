using UnityEngine;
using System.Collections.Generic;

public class Path_Drawer : MonoBehaviour
{
    public Tablero_Manager tablero; // Referencia al Tablero_Manager
    public GameObject pathPiecePrefab; // Prefab para las piezas del camino
    public Transform pathParent; // Contenedor para las piezas del camino

    private List<GameObject> currentPath = new List<GameObject>(); // Lista de piezas del camino actual
    private Piece currentDraggingPiece; // Pieza que se est� arrastrando
    private Vector2 lastValidPosition; // �ltima posici�n v�lida en el tablero
    private bool isDrawingPath = false; // Indica si se est� dibujando un camino

    void Update()
    {
        // Verificar si se est� manteniendo pulsado el clic izquierdo y se est� dibujando un camino
        if (Input.GetMouseButton(0) && isDrawingPath)
        {
            // Obtener la posici�n del mouse en el mundo
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            // Verificar si la posici�n del mouse es v�lida en el tablero
            if (tablero.CasillaExiste(mousePos, 0.01f))
            {
                // Verificar si la nueva posici�n es adyacente a la �ltima posici�n v�lida
                if (IsAdjacentPosition(mousePos, lastValidPosition))
                {
                    // Actualizar la �ltima posici�n v�lida
                    lastValidPosition = mousePos;

                    // Crear una nueva pieza del camino
                    GameObject newPathPiece = Instantiate(pathPiecePrefab, mousePos, Quaternion.identity, pathParent);
                    currentPath.Add(newPathPiece);

                    // Verificar si el camino llega al par de la pieza
                    if (CheckConnection(mousePos, currentDraggingPiece.pairID))
                    {
                        Debug.Log("Camino v�lido: conectado con el par.");
                        EndPath(true); // Finalizar el camino como v�lido
                    }
                }
            }
            else
            {
                // Si el rat�n est� fuera del tablero, finalizar el camino como inv�lido
                Debug.Log("Camino inv�lido: fuera del tablero.");
                EndPath(false);
            }
        }

        // Si se suelta el clic izquierdo, finalizar el camino
        if (Input.GetMouseButtonUp(0) && isDrawingPath)
        {
            Debug.Log("Camino inv�lido: clic soltado.");
            EndPath(false);
        }
    }

    // Iniciar el dibujo del camino
    public void StartPath(Piece piece, Vector2 startPosition)
    {
        currentDraggingPiece = piece;
        lastValidPosition = startPosition;
        isDrawingPath = true; // Establecer isDrawingPath en true

        // Crear la primera pieza del camino en la posici�n inicial
        GameObject newPathPiece = Instantiate(pathPiecePrefab, startPosition, Quaternion.identity, pathParent);
        currentPath.Add(newPathPiece);
    }

    // Finalizar el dibujo del camino
    public void EndPath(bool isValid)
    {
        if (isValid)
        {
            Debug.Log("Camino v�lido: conectado con el par.");
            // Aqu� puedes agregar l�gica adicional para manejar un camino v�lido
        }
        else
        {
            Debug.Log("Camino inv�lido: eliminando camino.");
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
        // Buscar en el tablero si hay una pieza con el mismo pairID en la posici�n final
        foreach (Vector2 pos in tablero.casillasPosiciones)
        {
            if (pos == endPosition)
            {
                // Aqu� debes implementar la l�gica para verificar si hay una pieza con el mismo pairID
                // Por ejemplo, buscar en el tablero si hay una pieza en esa posici�n con el mismo pairID
                Debug.Log($"Verificando conexi�n en {endPosition} para el par {pairID}");
                return true; // Cambia esto seg�n tu l�gica
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