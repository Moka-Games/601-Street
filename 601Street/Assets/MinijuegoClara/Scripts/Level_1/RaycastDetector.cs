using UnityEngine;
using UnityEngine.SceneManagement;

public class RaycastDetector : MonoBehaviour
{
    [Header("Fuente Raycast")]
    public GameObject raycastSource1;
    public GameObject raycastSource2;

    [Header("Configuraci�n Raycast 1")]
    public Vector3 raycastDirection1 = Vector3.forward;
    public float raycastDistance1 = 5f;

    [Header("Configuraci�n Raycast 2")]
    public Vector3 raycastDirection2 = Vector3.forward;
    public float raycastDistance2 = 5f;


    void Update()
    {
        bool detectedByRaycast1 = PerformRaycastAndDetectTag(raycastSource1, raycastDirection1, raycastDistance1);
        bool detectedByRaycast2 = PerformRaycastAndDetectTag(raycastSource2, raycastDirection2, raycastDistance2);

        // Verificar si ambos objetos detectan el tag en el mismo frame
        if (detectedByRaycast1 && detectedByRaycast2)
        {
            Debug.Log("�Victoria!");
            Invoke("CambioEscena", 11f);
        }
    }

    bool PerformRaycastAndDetectTag(GameObject raycastSource, Vector3 raycastDirection, float raycastDistance)
    {
        bool detected = false;

        if (raycastSource != null)
        {
            // Obtener la posici�n del objeto fuente del raycast
            Vector3 raycastOrigin = raycastSource.transform.position;

            // Realizar el Raycast
            RaycastHit hit;
            if (Physics.Raycast(raycastOrigin, raycastDirection, out hit, raycastDistance))
            {
                // Verificar si el objeto impactado tiene el tag deseado
                if (hit.collider.CompareTag("Victory_Cube"))
                {
                    Debug.Log($"{raycastSource.name} detect� un objeto con el tag 'Victory_Cube'");
                    detected = true;
                }
            }

            // Visualizar el Raycast en la escena a trav�s de Gizmos
            Debug.DrawRay(raycastOrigin, raycastDirection * raycastDistance, Color.green);
        }

        return detected;
    }

    void CambioEscena()
    {
        SceneManager.LoadScene("MinigameSelectionClara 2");
    }
}
