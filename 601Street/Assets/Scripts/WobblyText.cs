using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WobblyText : MonoBehaviour
{
    public TMP_Text textComponent;

    [Header("Ajustes del efecto")]
    public float verticalDisplacement; // Parámetro para ajustar el desplazamiento vertical.

    private void Update()
    {
        textComponent.ForceMeshUpdate();
        var textInfo = textComponent.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];

            if (!charInfo.isVisible)
            {
                continue;
            }

            var verts = textInfo.meshInfo[charInfo.materialReferenceIndex].vertices;

            for (int j = 0; j < 4; ++j)
            {
                var orig = verts[charInfo.vertexIndex + j];

                // Usamos el parámetro verticalDisplacement para controlar la amplitud del efecto.
                float wobbleOffset = Mathf.Sin(Time.time * 2f + orig.x * 0.01f) * verticalDisplacement;
                verts[charInfo.vertexIndex + j] = new Vector3(orig.x, orig.y + wobbleOffset, orig.z);
            }
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            var meshInfo = textInfo.meshInfo[i];
            meshInfo.mesh.vertices = meshInfo.vertices;
            textComponent.UpdateGeometry(meshInfo.mesh, i);
        }
    }
}
