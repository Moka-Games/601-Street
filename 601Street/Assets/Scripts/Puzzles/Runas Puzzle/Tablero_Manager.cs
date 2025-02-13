using System.Collections.Generic;
using UnityEngine;

public class Tablero_Manager : MonoBehaviour
{
    public GameObject casillaPrefab; // Prefab de la casilla
    public Transform casillasParent; // Contenedor de las casillas

    [System.Serializable]
    public class CasillaData
    {
        public Vector2 position;
    }

    public List<CasillaData> casillasDefinidas = new List<CasillaData>(); // Lista de posiciones de las casillas
    public List<Vector2> casillasPosiciones = new List<Vector2>(); // Lista interna de posiciones

    void Start()
    {
        GenerarTablero();
    }

    void GenerarTablero()
    {
        casillasPosiciones.Clear();

        foreach (CasillaData casilla in casillasDefinidas)
        {
            Vector2 posicion = casilla.position;
            GameObject nuevaCasilla = Instantiate(casillaPrefab, posicion, Quaternion.identity, casillasParent);
            casillasPosiciones.Add(posicion);
        }
    }

    public bool CasillaExiste(Vector2 posicion, float tolerancia = 0.01f)
    {
        foreach (Vector2 casillaPos in casillasPosiciones)
        {
            if (Vector2.Distance(casillaPos, posicion) < tolerancia)
            {
                return true;
            }
        }
        return false;
    }
}
