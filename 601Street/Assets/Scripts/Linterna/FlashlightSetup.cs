using UnityEngine;

[ExecuteInEditMode]
public class FlashlightSetup : MonoBehaviour
{
    [Header("Configuración de la linterna")]
    public Color lightColor = Color.white;
    public float spotAngle = 45f;
    public float range = 10f;
    public float intensity = 2f;
    public bool shadows = true;
    public LightShadowResolution shadowResolution = LightShadowResolution.Medium;

    [Header("Componentes adicionales")]
    public bool createCookieTexture = false;
    public Texture2D cookieTexture;

    private Light spotLight;
    private GameObject flashlightModel;

    void OnEnable()
    {
        SetupFlashlight();
    }

    public void SetupFlashlight()
    {
        // Asegurarse de que exista una luz en el objeto
        spotLight = GetComponentInChildren<Light>();
        if (spotLight == null)
        {
            GameObject lightObject = new GameObject("Spot Light");
            lightObject.transform.SetParent(transform);
            lightObject.transform.localPosition = Vector3.zero;
            lightObject.transform.localRotation = Quaternion.identity;
            spotLight = lightObject.AddComponent<Light>();
            spotLight.type = LightType.Spot;
        }

        // Configurar la luz
        spotLight.color = lightColor;
        spotLight.spotAngle = spotAngle;
        spotLight.range = range;
        spotLight.intensity = intensity;
        spotLight.shadows = shadows ? LightShadows.Soft : LightShadows.None;

        // Configurar sombras
        if (shadows)
        {
            switch (shadowResolution)
            {
                case LightShadowResolution.Low:
                    spotLight.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Low;
                    break;
                case LightShadowResolution.Medium:
                    spotLight.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Medium;
                    break;
                case LightShadowResolution.High:
                    spotLight.shadowResolution = UnityEngine.Rendering.LightShadowResolution.High;
                    break;
                case LightShadowResolution.VeryHigh:
                    spotLight.shadowResolution = UnityEngine.Rendering.LightShadowResolution.VeryHigh;
                    break;
            }
        }

        // Configurar cookie si está habilitado
        if (createCookieTexture && cookieTexture != null)
        {
            spotLight.cookie = cookieTexture;
        }
        else
        {
            spotLight.cookie = null;
        }
    }

    // Para uso en el editor
    public void CreateFlashlightModel()
    {
        // Implementación básica para crear un modelo simple de linterna
        // Esto sería reemplazado por un modelo 3D real en la versión final

        // Limpiar modelo anterior si existe
        if (flashlightModel != null)
        {
            DestroyImmediate(flashlightModel);
        }

        flashlightModel = new GameObject("Flashlight Model");
        flashlightModel.transform.SetParent(transform);
        flashlightModel.transform.localPosition = Vector3.zero;
        flashlightModel.transform.localRotation = Quaternion.identity;

        // Crear cuerpo de la linterna (cilindro)
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "FlashlightBody";
        body.transform.SetParent(flashlightModel.transform);
        body.transform.localPosition = new Vector3(0, 0, -0.1f);
        body.transform.localRotation = Quaternion.Euler(90, 0, 0);
        body.transform.localScale = new Vector3(0.1f, 0.2f, 0.1f);

        // Crear lente de la linterna (esfera achatada)
        GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lens.name = "FlashlightLens";
        lens.transform.SetParent(flashlightModel.transform);
        lens.transform.localPosition = new Vector3(0, 0, 0.1f);
        lens.transform.localScale = new Vector3(0.12f, 0.12f, 0.05f);
    }
}

// Enum personalizado para la resolución de sombras
public enum LightShadowResolution
{
    Low,
    Medium,
    High,
    VeryHigh
}