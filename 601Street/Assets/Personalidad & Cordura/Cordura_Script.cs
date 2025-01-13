using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cordura_Script : MonoBehaviour
{

    public static Cordura_Script Instance { get; private set; }

    [SerializeField] private int cordura = 0;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AumentoCordura(int aumento)
    {
        cordura = cordura + aumento;
    }

    public int CheckCordura()
    {
        return cordura;
    }
}
