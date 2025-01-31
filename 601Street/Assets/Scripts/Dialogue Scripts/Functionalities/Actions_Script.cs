using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Actions_Script : MonoBehaviour
{

    public static Actions_Script Instance { get; private set; }

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
    public void Option1_Action1()
    {
        print("1111");
    }
    public void Option2_Action2()
    {
        print("1111");

    }
    public void Option3_Action3()
    {
        print("3333");

    }
}
