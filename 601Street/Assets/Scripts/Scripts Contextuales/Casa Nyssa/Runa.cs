using UnityEngine;
using UnityEngine.SceneManagement;

public class Runa : MonoBehaviour
{
    public static bool runeInteracted = false;

    public void SetInteracted()
    {
        runeInteracted = true;
        print("RunaInteractuada");
    }
}
