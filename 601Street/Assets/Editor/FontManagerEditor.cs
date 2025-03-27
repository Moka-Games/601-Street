using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[CustomEditor(typeof(FontManager))]
public class FontManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Dibujar el inspector por defecto
        DrawDefaultInspector();
        
        // Obtener referencia al FontManager
        FontManager fontManager = (FontManager)target;
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Herramientas", EditorStyles.boldLabel);
        
        // Botón para aplicar fuentes a todas las escenas cargadas
        if (GUILayout.Button("Aplicar fuente global a todas las escenas"))
        {
            if (Application.isPlaying)
            {
                fontManager.RefreshAllFonts();
                Debug.Log("FontManager: Aplicación de fuentes iniciada manualmente.");
            }
            else
            {
                Debug.LogWarning("Esta función solo está disponible en modo Play.");
            }
        }
        
        // Botón para aplicar fuentes específicamente a la escena persistente
        if (GUILayout.Button("Aplicar fuente a escena persistente"))
        {
            if (Application.isPlaying)
            {
                fontManager.ApplyFontToPersistentScene();
                Debug.Log("FontManager: Aplicación de fuentes a escena persistente iniciada manualmente.");
            }
            else
            {
                Debug.LogWarning("Esta función solo está disponible en modo Play.");
            }
        }
        
        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox("Para garantizar que todos los textos usen la fuente global:\n" +
                              "1. Coloca este script en la escena persistente\n" +
                              "2. Asigna una fuente TMP_FontAsset en el campo 'Global Font'\n" +
                              "3. Si hay problemas, usa los botones durante el modo Play para aplicar manualmente", MessageType.Info);
    }
}
#endif