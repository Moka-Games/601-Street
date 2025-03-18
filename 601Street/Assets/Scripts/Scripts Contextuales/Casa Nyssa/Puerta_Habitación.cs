using UnityEngine;
using UnityEngine.SceneManagement;

public class Puerta_Habitación : MonoBehaviour
{
    public string EscenaACargar;
    private bool isChangingScene = false;

    [Tooltip("Etiqueta del objeto que puede activar el cambio de escena")]
    public string triggerTag = "Player";

    [Tooltip("Añadir un collider trigger si no existe")]
    public bool addColliderIfMissing = true;

    private void Start()
    {
        // Verificar si ya tiene un trigger collider
        Collider col = GetComponent<Collider>();
        if (col == null && addColliderIfMissing)
        {
            // Agregar un BoxCollider por defecto si no tiene ninguno
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector3(1f, 2f, 0.2f); // Dimensiones típicas de una puerta
        }
        else if (col != null && !col.isTrigger)
        {
            Debug.LogWarning("El collider en la puerta no está configurado como trigger. Asegúrate de activar 'Is Trigger'.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Verificar si el objeto que entró tiene la etiqueta configurada
        if (other.CompareTag(triggerTag))
        {
            CambiarEscena();
        }
    }

    public void CambiarEscena()
    {
        // Evitar múltiples llamadas simultáneas
        if (isChangingScene)
        {
            Debug.Log("Ya se está realizando un cambio de escena, ignorando nueva solicitud");
            return;
        }

        isChangingScene = true;
        Debug.Log($"Iniciando cambio a escena: {EscenaACargar}");

        try
        {
            // Desactivar sistemas de jugador para evitar interacciones durante la transición
            DesactivarSistemasJugador();

            // Verificar si existe GameSceneManager
            if (GameSceneManager.Instance != null)
            {
                Debug.Log($"Usando GameSceneManager para cargar escena: {EscenaACargar}");
                GameSceneManager.Instance.LoadScene(EscenaACargar, true);
            }
            else
            {
                // Método alternativo usando SceneManager directamente
                Debug.LogWarning("GameSceneManager no encontrado, usando SceneManager directamente");
                SceneManager.LoadScene(EscenaACargar);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error al cambiar escena: {e.Message}");
            isChangingScene = false; // Resetear bandera si hay error

            // Intentar último recurso
            try
            {
                SceneManager.LoadSceneAsync(EscenaACargar);
            }
            catch
            {
                Debug.LogError("Error final al intentar cargar escena");
            }
        }
    }

    private void DesactivarSistemasJugador()
    {
        try
        {
            // Desactivar PlayerInteraction
            PlayerInteraction playerInteraction = FindAnyObjectByType<PlayerInteraction>();
            if (playerInteraction != null)
            {
                Debug.Log("Desactivando PlayerInteraction");
                // Deshabilitar la componente, no el GameObject
                playerInteraction.enabled = false;
            }

            // Desactivar PlayerController
            PlayerController playerController = FindAnyObjectByType<PlayerController>();
            if (playerController != null)
            {
                Debug.Log("Desactivando movimiento del jugador");
                playerController.SetMovementEnabled(false);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error al desactivar sistemas del jugador: {e.Message}");
        }
    }

    // Asegurar que la bandera se resetee si el objeto se desactiva
    private void OnDisable()
    {
        isChangingScene = false;
    }
}