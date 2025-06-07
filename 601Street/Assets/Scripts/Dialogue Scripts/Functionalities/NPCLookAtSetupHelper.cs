using UnityEngine;
using UnityEngine.Animations.Rigging;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Helper script para configurar automáticamente el sistema Look At en NPCs
/// </summary>
public class NPCLookAtSetupHelper : MonoBehaviour
{
    [Header("Configuración Automática")]
    [SerializeField] private bool setupOnStart = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private NPC npcComponent;

    void Start()
    {
        if (setupOnStart)
        {
            SetupLookAtSystem();
        }
    }

    /// <summary>
    /// Configura automáticamente el sistema Look At para este NPC
    /// </summary>
    [ContextMenu("Setup Look At System")]
    public void SetupLookAtSystem()
    {
        Debug.Log($"=== Configurando sistema Look At para {gameObject.name} ===");

        // Buscar componentes necesarios
        npcComponent = GetComponent<NPC>();

        if (npcComponent == null)
        {
            Debug.LogError($"No se encontró componente NPC en {gameObject.name}");
            return;
        }

        // Verificar si ya existe un MultiAimConstraint
        MultiAimConstraint existingConstraint = GetComponentInChildren<MultiAimConstraint>();

        if (existingConstraint != null)
        {
            Debug.Log($"MultiAimConstraint encontrado: {existingConstraint.gameObject.name}");

            // Configurar el NPC para usar este constraint
            npcComponent.SetLookAtConstraint(existingConstraint);

            // Buscar si hay un target configurado en Source Objects
            var sourceObjects = existingConstraint.data.sourceObjects;
            if (sourceObjects.Count > 0 && sourceObjects[0].transform != null)
            {
                Transform firstTarget = sourceObjects[0].transform;
                npcComponent.SetLookAtTarget(firstTarget);
                Debug.Log($"Target configurado desde Source Objects: {firstTarget.name}");
            }
            else
            {
                Debug.LogWarning("No hay targets en Source Objects. Asigna manualmente el 'Look At Target' en el inspector del NPC.");
            }

            if (showDebugInfo)
            {
                LogConstraintInfo(existingConstraint);
            }
        }
        else
        {
            Debug.LogWarning($"No se encontró MultiAimConstraint en {gameObject.name}. " +
                           "Configúralo manualmente con Animation Rigging.");
        }

        Debug.Log("=== Configuración completada ===");
    }

    /// <summary>
    /// Verifica el estado del sistema Look At
    /// </summary>
    [ContextMenu("Check Look At Status")]
    public void CheckLookAtStatus()
    {
        if (npcComponent == null)
        {
            npcComponent = GetComponent<NPC>();
        }

        if (npcComponent != null)
        {
            Debug.Log("=== VERIFICANDO ESTADO DEL SISTEMA LOOK AT ===");

            bool isReady = npcComponent.IsLookAtSystemReady();
            Debug.Log($"Sistema Look At para {gameObject.name}: {(isReady ? "✅ LISTO" : "❌ NO CONFIGURADO")}");

            if (!isReady)
            {
                Debug.Log("💡 Ejecuta 'Setup Look At System' para configurarlo automáticamente");
                Debug.Log("💡 O verifica que el jugador tenga el tag 'Player' y el objeto 'NPC_LookAt'");
            }

            Debug.Log("=== FIN VERIFICACIÓN ===");
        }
        else
        {
            Debug.LogError("No se encontró componente NPC");
        }
    }

    /// <summary>
    /// Busca automáticamente el jugador y configura el target
    /// </summary>
    [ContextMenu("Test Look At Player")]
    public void TestLookAtPlayer()
    {
        if (npcComponent == null)
        {
            npcComponent = GetComponent<NPC>();
        }

        if (npcComponent != null)
        {
            Debug.Log("=== INICIANDO TEST DE LOOK AT ===");

            // Verificar que el sistema esté listo
            if (!npcComponent.IsLookAtSystemReady())
            {
                Debug.LogError("Sistema Look At no está listo. Ejecuta 'Setup Look At System' primero.");
                return;
            }

            Debug.Log("Iniciando Look At hacia el jugador...");
            npcComponent.StartLookingAtPlayer();

            Debug.Log("=== FIN TEST DE LOOK AT ===");
        }
        else
        {
            Debug.LogError("No se encontró componente NPC");
        }
    }

    /// <summary>
    /// Detiene el Look At
    /// </summary>
    [ContextMenu("Stop Look At")]
    public void StopLookAt()
    {
        if (npcComponent == null)
        {
            npcComponent = GetComponent<NPC>();
        }

        if (npcComponent != null)
        {
            Debug.Log("Deteniendo Look At...");
            npcComponent.StopLookingAtPlayer();
        }
    }

    /// <summary>
    /// Muestra información de debug sobre el constraint
    /// </summary>
    private void LogConstraintInfo(MultiAimConstraint constraint)
    {
        Debug.Log($"=== Información del Constraint {constraint.gameObject.name} ===");
        Debug.Log($"Constrained Object: {(constraint.data.constrainedObject != null ? constraint.data.constrainedObject.name : "null")}");
        Debug.Log($"Weight: {constraint.weight}");
        Debug.Log($"Source Objects Count: {constraint.data.sourceObjects.Count}");

        for (int i = 0; i < constraint.data.sourceObjects.Count; i++)
        {
            var source = constraint.data.sourceObjects[i];
            Debug.Log($"  Source {i}: {(source.transform != null ? source.transform.name : "null")} (Weight: {source.weight})");
        }

        Debug.Log("=== Fin información del Constraint ===");
    }
}

#if UNITY_EDITOR
/// <summary>
/// Editor personalizado para facilitar la configuración
/// </summary>
[CustomEditor(typeof(NPCLookAtSetupHelper))]
public class NPCLookAtSetupHelperEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Herramientas de Configuración", EditorStyles.boldLabel);

        NPCLookAtSetupHelper helper = (NPCLookAtSetupHelper)target;

        if (GUILayout.Button("Configurar Sistema Look At"))
        {
            helper.SetupLookAtSystem();
        }

        if (GUILayout.Button("Verificar Estado"))
        {
            helper.CheckLookAtStatus();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Testing (Solo Runtime)", EditorStyles.boldLabel);

        EditorGUI.BeginDisabledGroup(!Application.isPlaying);

        if (GUILayout.Button("Test Look At Player"))
        {
            helper.TestLookAtPlayer();
        }

        if (GUILayout.Button("Stop Look At"))
        {
            helper.StopLookAt();
        }

        EditorGUI.EndDisabledGroup();
    }
}
#endif