using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cordura_Script : MonoBehaviour
{
    [SerializeField] private int cordura = 0;

    public void AumentoCordura(int aumento)
    {
        cordura = cordura + aumento;
    }

    public int CheckCordura()
    {
        return cordura;
    }
}
