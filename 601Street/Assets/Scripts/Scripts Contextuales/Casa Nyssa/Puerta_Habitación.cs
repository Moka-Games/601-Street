using Unity.Collections;
using UnityEngine;

public class Puerta_Habitación : MonoBehaviour
{
    private SceneChange sceneChange;

    public string EscenaACargar;
    public void CambiarEscena()
    {
        sceneChange = FindAnyObjectByType<SceneChange>();

        sceneChange.CambiarEscenaConPuntoSalida(EscenaACargar);
    }
}
