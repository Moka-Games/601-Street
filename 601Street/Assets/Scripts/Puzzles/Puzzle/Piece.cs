using UnityEngine;
using System.Collections.Generic;

public class Piece : MonoBehaviour
{
    public int id; 
    private bool isDragging = false;
    private Vector2 startPosition;

    public int pairID; // ID único del par al que pertenece esta pieza

    void Update()
    {
        if (isDragging)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            transform.position = mousePos;
        }
    }

    void OnMouseDown()
    {
        isDragging = true;
        startPosition = transform.position;
    }

    void OnMouseUp()
    {
        isDragging = false;
        CheckConnection();
    }

    void CheckConnection()
    {
        Debug.Log($"Pieza {id} solté en {transform.position}");
    }
    public void SetPairID(int id)
    {
        pairID = id;
    }
}
