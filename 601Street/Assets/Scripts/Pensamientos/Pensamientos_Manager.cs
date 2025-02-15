using UnityEngine;
using TMPro;
using System.Collections;

public class Pensamientos_Manager : MonoBehaviour
{
    public GameObject pensamientoUI;
    public TMP_Text pensamientoText; 
    public bool pensamientoInicial; 
    public string pensamientoInicioTexto;

    private Coroutine pensamientoSecundarioCoroutine;
    private Pensamiento pensamientoActual;

    private void Start()
    {
        pensamientoUI.SetActive(false);

        if (pensamientoInicial)
        {
            MostrarPensamiento(pensamientoInicioTexto);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Pensamiento nuevoPensamiento = other.GetComponent<Pensamiento>();

        if (nuevoPensamiento != null)
        {
            if (pensamientoSecundarioCoroutine != null)
            {
                StopCoroutine(pensamientoSecundarioCoroutine);
            }

            pensamientoActual = nuevoPensamiento;

            MostrarPensamiento(pensamientoActual.pensamientoPrincipal);

            pensamientoSecundarioCoroutine = StartCoroutine(RepetirPensamientoSecundario());
        }
    }

    private void MostrarPensamiento(string texto)
    {
        pensamientoText.text = texto;
        pensamientoUI.SetActive(true); 
        StartCoroutine(DesactivarPensamiento()); 
    }

    private IEnumerator DesactivarPensamiento()
    {
        yield return new WaitForSeconds(10);
        pensamientoUI.SetActive(false);
    }

    private IEnumerator RepetirPensamientoSecundario()
    {
        yield return new WaitForSeconds(60);

        while (true) 
        {
            MostrarPensamiento(pensamientoActual.pensamientoSecundario);
            yield return new WaitForSeconds(60);
        }
    }
}
