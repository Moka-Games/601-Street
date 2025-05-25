using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class BarInterior : MonoBehaviour
{

    public GameObject botella;
    public GameObject misiónBotella;
    public GameObject activadorPolicias;
    public GameObject salida;

    public UnityEvent OnNakamuraEnded;
    public GameStateController gameStateController;
    [SerializeField] private string SiguienteEstado = "Nakamura_Interactuado";
    private void Start()
    {
        botella.SetActive(false);
        activadorPolicias.SetActive(false);
    }
    public void ActivarBotella()
    {
        botella.SetActive(true);
        misiónBotella.SetActive(true);
    }

    public void CambiarPolicias()
    {
        activadorPolicias.SetActive(true);
        salida.SetActive(true);
        OnNakamuraEnded.Invoke();
        gameStateController.ChangeGameState(SiguienteEstado);
        StartCoroutine(NoteWithDelay(1.5f));
    }


    private IEnumerator NoteWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        OnNakamuraEnded.Invoke();

    }
}
