using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Runas_Manager : MonoBehaviour
{
    public GameObject piecePrefab; // Prefab de la pieza
    public Transform piecesParent; // Contenedor para las piezas en la jerarqu�a

    [System.Serializable]
    public class PiecePair
    {
        public Vector2 position1; // Posici�n de la primera pieza
        public Vector2 position2; // Posici�n de la segunda pieza
        public bool isMissingPiece; // Si es true, falta la segunda pieza
        public GameObject prefab; // Prefab para ambas piezas del par
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

        // Esperar un frame para asegurarse de que el tablero est� listo
        StartCoroutine(InitializePiecesAfterDelay());
    }

    void InitializePieces()
    {
        Debug.Log("Inicializando piezas...");
        foreach (var pair in piecePairs)
        {
            Debug.Log($"Generando par en posiciones: {pair.position1} y {pair.position2}");

            // Verificar si las posiciones existen en el tablero
            if (!tablero.CasillaExiste(pair.position1, 0.01f))
            {
                Debug.LogWarning($"Posici�n {pair.position1} no es v�lida en el tablero.");
            }
            if (!pair.isMissingPiece && !tablero.CasillaExiste(pair.position2, 0.01f))
            {
                Debug.LogWarning($"Posici�n {pair.position2} no es v�lida en el tablero.");
            }

            // Instanciar la primera pieza
            SpawnPiece(pair.position1, pair.prefab);

            // Instanciar la segunda pieza si no falta
            if (!pair.isMissingPiece)
            {
                SpawnPiece(pair.position2, pair.prefab);
            }
        }
    }
    void SpawnPiece(Vector2 position, GameObject prefab)
    {
        if (tablero == null)
        {
            Debug.LogError("Tablero_Manager no asignado en Runas_Manager");
            return;
        }

        // Verificar si la posici�n es v�lida en el tablero
        if (!tablero.CasillaExiste(position, 0.01f))
        {
            Debug.LogWarning($"Posici�n {position} no es v�lida en el tablero.");
            return;
        }

        // Verificar si ya existe una pieza en esa posici�n
        if (!placedPieces.ContainsKey(position))
        {
            // Instanciar la pieza
            GameObject newPiece = Instantiate(prefab, new Vector3(position.x, position.y, 0), Quaternion.identity, piecesParent);
            placedPieces.Add(position, newPiece);
            Debug.Log($"Pieza instanciada en {position} con prefab {prefab.name}");
        }
        else
        {
            Debug.LogWarning($"Ya existe una pieza en la posici�n {position}");
        }
    }
    IEnumerator InitializePiecesAfterDelay()
    {
        yield return null; // Esperar un frame
        InitializePieces();
    }
}
