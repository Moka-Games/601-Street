using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Teléfono : MonoBehaviour
{
    public GameObject telefonoUI;
    public GameObject telefonoHUD;

    public Image feedbackSlider;

    public float tiempoRequerido = 1f;
    private float tiempoPresionado = 0f;

    void Start()
    {
        telefonoUI.SetActive(false);
        telefonoHUD.SetActive(true);

        feedbackSlider.fillAmount = 0;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.F))
        {
            tiempoPresionado += Time.deltaTime;
            feedbackSlider.fillAmount = tiempoPresionado / tiempoRequerido;

            if (tiempoPresionado >= tiempoRequerido)
            {
                telefonoUI.SetActive(true);
                telefonoHUD.SetActive(false);
                feedbackSlider.fillAmount = 0;
                tiempoPresionado = 0f;
            }
        }
        else
        {
            feedbackSlider.fillAmount = 0;
            tiempoPresionado = 0f;
        }

        if(telefonoUI.activeSelf == true)
        {
            Time.timeScale = 0f;
        }
        else if (telefonoUI.activeSelf == false)
        {
            Time.timeScale = 1f;
        }
    }

    public void CerrarUI()
    {
        telefonoUI.SetActive(false);
        telefonoHUD.SetActive(true);
    }
}



