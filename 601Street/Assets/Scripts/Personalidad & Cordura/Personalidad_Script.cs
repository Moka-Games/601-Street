using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Personalidad_Script : MonoBehaviour
{
    public static Personalidad_Script Instance { get; private set; }

    [Header("Desarrollo Personalidad")]
    [SerializeField] private int empatia;
    [SerializeField] private int persuasion;
    [SerializeField] private int intimidacion;
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
    private void Start()
    {
        empatia = 0;
        persuasion = 0;
        intimidacion = 0;
    }
    private void Update()
    {
        if(Input.GetKeyUp(KeyCode.E))
        {
            //Debug.Log(CheckEmpatia());
        }
    }
    public void AumentoEmpatia(int aumento)
    {
        empatia = empatia + aumento;
    }
    public void AumentoPersuasion(int aumento)
    {
        persuasion = persuasion + aumento;
    }
    public void AumentoIntimidacion(int aumento)
    {
        intimidacion = intimidacion + aumento;
    }

    public int CheckEmpatia()
    {
        return empatia;
    }
    public int CheckPersuasion()
    {
        return persuasion;
    }
    public int CheckIntimidación()
    {
        return intimidacion;
    }
}
