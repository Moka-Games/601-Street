#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WorldStateGraph))]
public class WorldStateGraphEditor : Editor
{
    private SerializedProperty nodesProperty;
    private SerializedProperty connectionsProperty;
    private SerializedProperty initialNodeIDProperty;
    private int selectedNodeIndex = -1;

    private void OnEnable()
    {
        nodesProperty = serializedObject.FindProperty("nodes");
        connectionsProperty = serializedObject.FindProperty("connections");
        initialNodeIDProperty = serializedObject.FindProperty("initialNodeID");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Dibujar propiedades generales
        EditorGUILayout.PropertyField(initialNodeIDProperty);

        // Mostrar lista de nodos
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Nodos del grafo", EditorStyles.boldLabel);

        for (int i = 0; i < nodesProperty.arraySize; i++)
        {
            SerializedProperty nodeProperty = nodesProperty.GetArrayElementAtIndex(i);
            SerializedProperty nameProperty = nodeProperty.FindPropertyRelative("name");
            SerializedProperty idProperty = nodeProperty.FindPropertyRelative("id");
            SerializedProperty misionProperty = nodeProperty.FindPropertyRelative("misionAsociada");

            EditorGUILayout.BeginHorizontal();

            // Botón para seleccionar nodo
            if (GUILayout.Button(nameProperty.stringValue, GUILayout.Width(150)))
            {
                selectedNodeIndex = (selectedNodeIndex == i) ? -1 : i;
            }

            // Mostrar misión asociada en línea
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(misionProperty.objectReferenceValue, typeof(Mision), false);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            // Mostrar detalles del nodo si está seleccionado
            if (selectedNodeIndex == i)
            {
                EditorGUI.indentLevel++;

                // Mostrar propiedades del nodo
                EditorGUILayout.PropertyField(nodeProperty.FindPropertyRelative("name"));
                EditorGUILayout.PropertyField(nodeProperty.FindPropertyRelative("isInitialNode"));
                EditorGUILayout.PropertyField(nodeProperty.FindPropertyRelative("activeObjectIDs"));
                EditorGUILayout.PropertyField(nodeProperty.FindPropertyRelative("inactiveObjectIDs"));
                EditorGUILayout.PropertyField(nodeProperty.FindPropertyRelative("connectedNodeIDs"));

                // Mostrar el campo de misión asociada
                EditorGUILayout.PropertyField(misionProperty, new GUIContent("Misión Asociada"));

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif