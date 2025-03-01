using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;
using Cinemachine;

public class Eco_Callejon : MonoBehaviour
{
    public float raycastDistance = 10f;

    public Transform ecoLookAt;
    public string pensamientoNyssa;

    private Pensamientos_Manager pensamientosManager;
    private PlayerController playerController;

    private CinemachineFreeLook playerCamera;
    public CinemachineVirtualCamera callejonCamera;

    public GameObject temporalColliders;

    private bool playerDetected;
    private bool ecoInteracted;

    public GameObject imagenEco;


    private void Start()
    {
        ecoInteracted = false;
        playerDetected = false;
        temporalColliders.SetActive(false);
        imagenEco.SetActive(false);

        //Referencias
        playerCamera = FindAnyObjectByType<CinemachineFreeLook>();
        pensamientosManager = FindAnyObjectByType<Pensamientos_Manager>();
        playerController = FindAnyObjectByType<PlayerController>();

        callejonCamera.enabled = false;
    }
    void Update()
    {
        if (!playerDetected)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, raycastDistance))
            {
                if (hit.collider.CompareTag("Player"))
                {
                    print("Jugador detectado");
                    StartEcoSequence();
                    playerDetected = true;
                }
            }
        }
        
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * raycastDistance);
    }

    private void StartEcoSequence()
    {
        StartCoroutine(EcoSecuence());
    }

    IEnumerator EcoSecuence()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        callejonCamera.Follow = player.transform;
        pensamientosManager.MostrarPensamiento(pensamientoNyssa);
        playerController.SetMovementEnabled(false);
        playerCamera.enabled = false;
        callejonCamera.enabled = true;
        temporalColliders.SetActive(true);


        yield return new WaitForSeconds(2f);
        
        

        playerController.SetMovementEnabled(true);
    }

    private void CameraSwap(CinemachineVirtualCamera virtualCamera_, CinemachineFreeLook playerCamera_, bool value_1, bool value_2)
    {
        virtualCamera_.enabled = value_2;
        playerCamera_.enabled = value_1;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player") && !ecoInteracted)
        {
            playerController.SetMovementEnabled(false);
            imagenEco.SetActive(true);
            ecoInteracted = true;
        }
    }

    public void CloseEcoImage()
    {
        temporalColliders.SetActive(false); 
        playerController.SetMovementEnabled(true);
        imagenEco.SetActive(false);
        callejonCamera.enabled=false;
        playerCamera.enabled=true;
    }

}