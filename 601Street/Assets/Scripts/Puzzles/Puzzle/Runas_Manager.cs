using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Runas_Manager : MonoBehaviour
{
    public GameObject piecePrefab; // Prefab de la pieza
    public Transform piecesParent; // Contenedor para las piezas en la jerarquía

    [System.Serializable]
    public class PiecePair
    {
        public Vector2 position1;
        public Vector2 position2;
        public bool isMissingPiece; // Si es true, falta la segunda pieza
    }

    public List<PiecePair> piecePairs = new List<PiecePair>(); 
    private Dictionary<Vector2, GameObject> placedPieces = new Dictionary<Vector2, GameObject>();

    public Tablero_Manager tablero; // Referencia al objeto "Tablero"

    void Start()
    {
        if (tablero == null)
        {
            Debug.LogError("Tablero_Manager no asignado en Runas_Manager");
            return;
        }

        // Esperar un frame para asegurarse de que el tablero esté listo
        StartCoroutine(InitializePiecesAfterDelay());
    }

    void InitializePieces()
    {
        Debug.Log("Inicializando piezas...");
        foreach (var pair in piecePairs)
        {
            Debug.Log($"Generando par en posiciones: {pair.position1} y {pair.position2}");

            SpawnPiece(pair.position1);

            if (!pair.isMissingPiece)
            {
                SpawnPiece(pair.position2);
            }
        }
    }

    void SpawnPiece(Vector2 position)
    {
        Debug.Log($"Verificando posición: {position}");
        if (!tablero.CasillaExiste(position, 0.01f))
        {
            Debug.LogWarning($"Posición {position} no es válida en el tablero.");
            return;
        }

        if (!placedPieces.ContainsKey(position))
        {
            GameObject newPiece = Instantiate(piecePrefab, new Vector3(position.x, position.y, 0), Quaternion.identity, piecesParent);
            placedPieces.Add(position, newPiece);
            Debug.Log($"Pieza instanciada en {position}");
        }
        else
        {
            Debug.LogWarning($"Ya existe una pieza en la posición {position}");
        }
    }
    IEnumerator InitializePiecesAfterDelay()
    {
        yield return null; // Esperar un frame
        InitializePieces();
    }
}
