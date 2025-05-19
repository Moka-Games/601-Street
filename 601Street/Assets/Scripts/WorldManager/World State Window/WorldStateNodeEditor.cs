#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WorldStateNode))]
public class WorldStateNodeEditor : Editor
{
    private SerializedProperty idProperty;
    private SerializedProperty nameProperty;
    private SerializedProperty isInitialNodeProperty;
    private SerializedProperty activeObjectIDsProperty;
    private SerializedProperty inactiveObjectIDsProperty;
    private SerializedProperty connectedNodeIDsProperty;
    private SerializedProperty misionAsociadaProperty;

    private void OnEnable()
    {
        idProperty = serializedObject.FindProperty("id");
        nameProperty = serializedObject.FindProperty("name");
        isInitialNodeProperty = serializedObject.FindProperty("isInitialNode");
        activeObjectIDsProperty = serializedObject.FindProperty("activeObjectIDs");
        inactiveObjectIDsProperty = serializedObject.FindProperty("inactiveObjectIDs");
        connectedNodeIDsProperty = serializedObject.FindProperty("connectedNodeIDs");
        misionAsociadaProperty = serializedObject.FindProperty("misionAsociada");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(nameProperty);
        EditorGUILayout.PropertyField(idProperty, new GUIContent("ID"));
        EditorGUILayout.PropertyField(isInitialNodeProperty);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Misión Asociada", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(misionAsociadaProperty, new GUIContent(""), true);

        EditorGUILayout.Space(10);
        EditorGUILayout.PropertyField(activeObjectIDsProperty);
        EditorGUILayout.PropertyField(inactiveObjectIDsProperty);
        EditorGUILayout.PropertyField(connectedNodeIDsProperty);

        serializedObject.ApplyModifiedProperties();
    }
}
#endif