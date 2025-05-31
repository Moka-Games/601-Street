using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;
using Cinemachine;
using UnityEngine.Events;
using UnityEngine.Audio;

public class Eco_Callejon : MonoBehaviour
{

    [Header("Post-Procesado")]
    public GameObject postProcesadoEco;

    [Header("Raycast Settings")]
    public AudioClip sonidoEcoDetectado;
    public AudioClip sonidoEcoDesapareciendo;
    private AudioSource audioSource;
    public float audioSource_Volume = 0.25f; 

    public float raycastDistance = 10f;

    public Transform ecoLookAt;
    public string pensamientoNyssaPostInteracción;

    private Pensamientos_Manager pensamientosManager;
    private PlayerController playerController;

    private CinemachineFreeLook playerCamera;
    public CinemachineVirtualCamera callejonCamera;

    public GameObject temporalColliders;

    private bool playerDetected;
    private bool ecoInteracted;

    public GameObject imagenEco;
    public GameObject EcoMesh;

    public UnityEvent onEcoTriggered;

    public GameObject luzEco;

    private void Start()
    {
        ecoInteracted = false;
        playerDetected = false;
        temporalColliders.SetActive(false);
        imagenEco.SetActive(false);
        postProcesadoEco.SetActive(false);

        audioSource = GetComponent<AudioSource>();
        audioSource.volume = audioSource_Volume;

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
                    audioSource.PlayOneShot(sonidoEcoDetectado);
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
        onEcoTriggered.Invoke();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        callejonCamera.Follow = player.transform;
        playerController.SetMovementEnabled(false);
        playerCamera.enabled = false;
        callejonCamera.enabled = true;
        temporalColliders.SetActive(true);


        postProcesadoEco.SetActive(true);


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
        audioSource.PlayOneShot(sonidoEcoDesapareciendo);
        EcoMesh.SetActive(false);

        temporalColliders.SetActive(false);
        luzEco.SetActive(false);
        postProcesadoEco.SetActive(false); 
        playerController.SetMovementEnabled(true);
        imagenEco.SetActive(false);
        callejonCamera.enabled=false;
        playerCamera.enabled=true;



        StartCoroutine(Destroy_Eco_Delay(1f));

        pensamientosManager.MostrarPensamiento(pensamientoNyssaPostInteracción);
    
        StartCoroutine(StopAudioSource(1f));
    }

    private IEnumerator Destroy_Eco_Delay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(EcoMesh);
    }

    private IEnumerator StopAudioSource(float delay)
    {
        yield return new WaitForSeconds(delay);
        audioSource.Pause();
    }

}