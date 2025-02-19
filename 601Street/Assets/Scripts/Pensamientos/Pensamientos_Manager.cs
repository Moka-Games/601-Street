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

    private Pensamiento[] todosPensamientos;

    private void Start()
    {
        todosPensamientos = Object.FindObjectsByType<Pensamiento>(FindObjectsSortMode.None);

        pensamientoUI.SetActive(false);
        if (pensamientoInicial)
        {
            MostrarPensamiento(pensamientoInicioTexto);
        }
    }

    public Pensamiento GetPensamientoByID(int id)
    {
        if (todosPensamientos == null || todosPensamientos.Length == 0)
        {
            todosPensamientos = Object.FindObjectsByType<Pensamiento>(FindObjectsSortMode.None);
        }

        foreach (Pensamiento pensamiento in todosPensamientos)
        {
            if (pensamiento.Id == id)  
            {
                return pensamiento;
            }
        }

        Debug.LogWarning($"No se encontró ningún pensamiento con ID {id}");
        return null;
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

    public void MostrarPensamiento(string texto)
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