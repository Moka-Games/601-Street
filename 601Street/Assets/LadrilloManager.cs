using UnityEngine;

public class LadrilloManager : MonoBehaviour
{
    public GameObject puertaSecreta;

    private void Update()
    {
        CheckLadrillos();
    }
    public void CheckLadrillos()
    {
        if (Ladrillo.laddrillo_1_Interacted && Ladrillo.laddrillo_2_Interacted && Ladrillo.laddrillo_3_Interacted)
        {
            puertaSecreta.SetActive(false);
        }
        else
        {
            Debug.Log("Not all bricks have been interacted with yet.");
        }
    }
}
