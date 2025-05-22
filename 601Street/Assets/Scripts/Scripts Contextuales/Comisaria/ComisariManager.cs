using UnityEngine;

public class ComisariManager : MonoBehaviour
{
    public GameObject ganzúa;
    public GameObject puertaCaja;
    public GameStateController gameStateController;

    [SerializeField] private string SiguienteEstadoPolicia_1 = "Policia_Interactuado";

    [SerializeField] private string SiguienteEstadoPolicia_2_EXITO = "Policia_Interactuado";
    [SerializeField] private string SiguienteEstadoPolicia_2_FRACASO = "Policia_Interactuado";

    /// <summary>
    /// Los objetos que se activan si tenemos EXITO con el policia 2
    /// </summary>
    public void ObjetosPostPolicia()
    {
        puertaCaja.SetActive(false);
        gameStateController.ChangeGameState(SiguienteEstadoPolicia_2_EXITO);
    }
    /// <summary>
    /// Los objetos que se activan si FRACASAMOS con el policia 2
    /// </summary>
    public void ObjetosPostPoliciaFracaso()
    {
        ganzúa.SetActive(true);
        gameStateController.ChangeGameState(SiguienteEstadoPolicia_2_FRACASO);
    }

    public void Policia_1_Interactuado()
    {
        print("Policia_1_Interactuado");
        gameStateController.ChangeGameState(SiguienteEstadoPolicia_1);
    }
}
