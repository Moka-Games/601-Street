// WorldStateEditorWindow.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class WorldStateEditorWindow : EditorWindow
{
    private WorldStateGraph graph;
    private Vector2 scrollPosition;
    private WorldStateNode selectedNode;
    private bool isDragging;
    private WorldStateNode startConnectionNode;
    private bool isCreatingConnection = false;
    private Vector2 currentMousePosition;

    // Constantes para el diseño visual
    private const float NODE_WIDTH = 200f;
    private const float NODE_HEIGHT = 150f;

    [MenuItem("Window/World State Editor")]
    public static void ShowWindow()
    {
        GetWindow<WorldStateEditorWindow>("World State Editor");
    }

    private void OnEnable()
    {
        // Asegurarse de que el cursor no se quede "pegado" al reiniciar
        isDragging = false;
        startConnectionNode = null;
        isCreatingConnection = false;

        // Establece el callback para dibujo continuo cuando se crea una conexión
        wantsMouseMove = true;
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (graph == null)
        {
            EditorGUILayout.HelpBox("Select or create a World State Graph asset", MessageType.Info);
            return;
        }

        // Divide la ventana en área de grafo y área de inspección
        EditorGUILayout.BeginHorizontal();

        // Área del grafo
        Rect graphArea = GUILayoutUtility.GetRect(0, position.width * 0.7f, 0, position.height - 40);
        GUI.Box(graphArea, "Graph View");

        // Vista de desplazamiento para el grafo
        scrollPosition = GUI.BeginScrollView(graphArea, scrollPosition, new Rect(0, 0, 2000, 2000));

        // Dibuja conexiones y nodos
        DrawConnections();

        // Dibuja la conexión en progreso si estamos creando una
        if (isCreatingConnection && startConnectionNode != null)
        {
            Vector2 startPos = new Vector2(
                startConnectionNode.position.x + startConnectionNode.position.width,
                startConnectionNode.position.y + startConnectionNode.position.height * 0.5f);

            Handles.BeginGUI();
            Handles.color = Color.gray;
            Handles.DrawLine(startPos, currentMousePosition);
            DrawArrow(startPos, currentMousePosition, 10f);
            Handles.EndGUI();
        }

        DrawNodes();

        // Maneja eventos de ratón
        HandleEvents(graphArea);

        GUI.EndScrollView();

        // Área de inspector para el nodo seleccionado
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.3f));
        DrawNodeInspector();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        // Botones de acción en la parte inferior
        DrawBottomButtons();

        // Guarda los cambios
        if (GUI.changed && graph != null)
        {
            EditorUtility.SetDirty(graph);
        }

        // Forzar repintado continuo cuando estamos creando una conexión
        if (isCreatingConnection)
            Repaint();
    }

    private void DrawToolbar()
    {
        // Título de la ventana
        GUILayout.Label("World State Editor", EditorStyles.boldLabel);

        // Selección del grafo o creación de uno nuevo
        EditorGUILayout.BeginHorizontal();
        WorldStateGraph newGraph = (WorldStateGraph)EditorGUILayout.ObjectField("Graph", graph, typeof(WorldStateGraph), false);

        if (newGraph != graph)
        {
            graph = newGraph;
            selectedNode = null;
        }

        if (GUILayout.Button("Create New"))
        {
            CreateNewGraph();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawNodes()
    {
        if (graph == null || graph.nodes == null) return;

        foreach (var node in graph.nodes)
        {
            // Define el estilo del nodo (inicial, seleccionado, etc.)
            GUIStyle nodeStyle = new GUIStyle(GUI.skin.window);

            // Color del nodo según su estado
            Color nodeColor = Color.gray;
            if (node.isInitialNode)
                nodeColor = new Color(0.5f, 0.8f, 0.5f);
            if (selectedNode == node)
                nodeColor = new Color(0.7f, 0.7f, 0.9f);

            nodeStyle.normal.background = MakeColoredTexture(2, 2, nodeColor);

            // Dibuja el nodo
            GUI.Box(node.position, node.name, nodeStyle);

            // Información dentro del nodo
            GUILayout.BeginArea(new Rect(
                node.position.x + 5,
                node.position.y + 20,
                node.position.width - 10,
                node.position.height - 25));

            GUILayout.Label($"ID: {node.id.Substring(0, Mathf.Min(8, node.id.Length))}...");
            GUILayout.Label($"Active Objects: {node.activeObjectIDs.Count}");
            GUILayout.Label($"Inactive Objects: {node.inactiveObjectIDs.Count}");

            if (node.isInitialNode)
                GUILayout.Label("Initial Node", EditorStyles.boldLabel);

            GUILayout.EndArea();
        }
    }

    private void DrawConnections()
    {
        if (graph == null || graph.connections == null) return;

        foreach (var connection in graph.connections)
        {
            WorldStateNode fromNode = graph.FindNodeByID(connection.fromNodeID);
            WorldStateNode toNode = graph.FindNodeByID(connection.toNodeID);

            if (fromNode != null && toNode != null)
            {
                // Puntos de inicio y fin
                Vector2 startPos = new Vector2(
                    fromNode.position.x + fromNode.position.width,
                    fromNode.position.y + fromNode.position.height * 0.5f);
                Vector2 endPos = new Vector2(
                    toNode.position.x,
                    toNode.position.y + toNode.position.height * 0.5f);

                // Dibuja la línea con una flecha
                Handles.BeginGUI();
                Handles.color = Color.black;
                Handles.DrawLine(startPos, endPos);
                DrawArrow(startPos, endPos, 10f);
                Handles.EndGUI();
            }
        }
    }

    private void DrawNodeInspector()
    {
        GUILayout.Label("Node Inspector", EditorStyles.boldLabel);

        if (selectedNode == null)
        {
            EditorGUILayout.HelpBox("Select a node to edit", MessageType.Info);
            return;
        }

        // Nombre del nodo
        EditorGUI.BeginChangeCheck();
        selectedNode.name = EditorGUILayout.TextField("Name", selectedNode.name);

        // Opción de nodo inicial
        bool wasInitialNode = selectedNode.isInitialNode;
        selectedNode.isInitialNode = EditorGUILayout.Toggle("Is Initial Node", selectedNode.isInitialNode);

        // Si este nodo se marca como inicial, actualizar el grafo
        if (selectedNode.isInitialNode && !wasInitialNode)
        {
            // Desmarcar cualquier otro nodo inicial
            foreach (var node in graph.nodes)
            {
                if (node != selectedNode)
                    node.isInitialNode = false;
            }

            graph.initialNodeID = selectedNode.id;
        }

        // Mostrar ID
        EditorGUILayout.LabelField("ID", selectedNode.id);

        // Lista de objetos activos
        GUILayout.Label("Active Objects", EditorStyles.boldLabel);
        DrawObjectList(selectedNode.activeObjectIDs);

        // Lista de objetos inactivos
        GUILayout.Label("Inactive Objects", EditorStyles.boldLabel);
        DrawObjectList(selectedNode.inactiveObjectIDs);

        // Conexiones
        GUILayout.Label("Connected To:", EditorStyles.boldLabel);
        foreach (var connectionID in selectedNode.connectedNodeIDs)
        {
            WorldStateNode targetNode = graph.FindNodeByID(connectionID);
            if (targetNode != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(targetNode.name);

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    RemoveConnection(selectedNode.id, connectionID);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(graph);
        }
    }

    private void DrawObjectList(List<string> objectIDs)
    {
        int removeIndex = -1;

        for (int i = 0; i < objectIDs.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            objectIDs[i] = EditorGUILayout.TextField(objectIDs[i]);

            if (GUILayout.Button("Find", GUILayout.Width(50)))
            {
                // Abre selector de objetos con UniqueID
                ShowObjectSelector(selectedID => {
                    objectIDs[i] = selectedID;
                    Repaint();
                });
            }

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                removeIndex = i;
            }

            EditorGUILayout.EndHorizontal();
        }

        // Elimina el objeto marcado
        if (removeIndex >= 0)
            objectIDs.RemoveAt(removeIndex);

        // Añade nuevo espacio
        if (GUILayout.Button("Add Object"))
            objectIDs.Add("");
    }

    private void DrawBottomButtons()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Node"))
        {
            AddNode();
        }

        if (selectedNode != null && GUILayout.Button("Delete Node"))
        {
            DeleteNode(selectedNode);
        }

        if (GUILayout.Button("Save Graph"))
        {
            AssetDatabase.SaveAssets();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void HandleEvents(Rect graphArea)
    {
        Event e = Event.current;

        // Actualizar posición del mouse para la conexión en progreso
        if (isCreatingConnection)
        {
            currentMousePosition = e.mousePosition + scrollPosition;
            Repaint();
        }

        switch (e.type)
        {
            case EventType.MouseDown:
                if (graphArea.Contains(e.mousePosition))
                {
                    // Verifica si se hizo clic en un nodo
                    WorldStateNode clickedNode = GetNodeAtPosition(e.mousePosition + scrollPosition);

                    if (clickedNode != null)
                    {
                        // Selecciona el nodo
                        selectedNode = clickedNode;

                        // Si es botón derecho, inicia una conexión
                        if (e.button == 1)
                        {
                            startConnectionNode = clickedNode;
                            isCreatingConnection = true;
                            currentMousePosition = e.mousePosition + scrollPosition;
                        }
                        // Si es botón izquierdo, inicia arrastre
                        else if (e.button == 0)
                        {
                            isDragging = true;
                        }
                    }
                    else
                    {
                        // Clic en espacio vacío
                        selectedNode = null;
                    }

                    e.Use();
                }
                break;

            case EventType.MouseUp:
                if (isCreatingConnection && startConnectionNode != null)
                {
                    // Finaliza la creación de conexión
                    WorldStateNode endNode = GetNodeAtPosition(e.mousePosition + scrollPosition);
                    if (endNode != null && endNode != startConnectionNode)
                    {
                        CreateConnection(startConnectionNode, endNode);
                    }

                    isCreatingConnection = false;
                    startConnectionNode = null;
                    e.Use();
                }

                isDragging = false;
                break;

            case EventType.MouseDrag:
                if (isDragging && selectedNode != null)
                {
                    // Arrastra el nodo
                    selectedNode.position.x += e.delta.x;
                    selectedNode.position.y += e.delta.y;
                    e.Use();
                }
                break;
        }
    }

    private void CreateNewGraph()
    {
        // Crear un cuadro de diálogo para guardar
        string path = EditorUtility.SaveFilePanelInProject(
            "Save World State Graph",
            "NewWorldStateGraph",
            "asset",
            "Please enter a name for the new World State Graph"
        );

        if (string.IsNullOrEmpty(path)) return;

        // Crear el ScriptableObject
        WorldStateGraph newGraph = CreateInstance<WorldStateGraph>();
        AssetDatabase.CreateAsset(newGraph, path);
        AssetDatabase.SaveAssets();

        // Asignar como grafo actual
        graph = newGraph;
        selectedNode = null;
    }

    private void AddNode()
    {
        if (graph == null) return;

        // Crear un nuevo nodo en el centro de la vista
        Vector2 center = scrollPosition + new Vector2(position.width * 0.35f, position.height * 0.5f);
        WorldStateNode newNode = new WorldStateNode("New Node", center);

        // Si es el primer nodo, hacerlo inicial
        if (graph.nodes.Count == 0)
        {
            newNode.isInitialNode = true;
            graph.initialNodeID = newNode.id;
        }

        // Añadir al grafo
        graph.nodes.Add(newNode);
        selectedNode = newNode;

        EditorUtility.SetDirty(graph);
    }

    private void DeleteNode(WorldStateNode node)
    {
        if (graph == null || node == null) return;

        // Eliminar conexiones asociadas
        graph.connections.RemoveAll(c => c.fromNodeID == node.id || c.toNodeID == node.id);

        // Eliminar referencias en otros nodos
        foreach (var otherNode in graph.nodes)
        {
            otherNode.connectedNodeIDs.RemoveAll(id => id == node.id);
        }

        // Eliminar el nodo
        graph.nodes.Remove(node);

        // Si era el nodo inicial, actualizar
        if (node.isInitialNode && graph.nodes.Count > 0)
        {
            graph.nodes[0].isInitialNode = true;
            graph.initialNodeID = graph.nodes[0].id;
        }

        selectedNode = null;

        EditorUtility.SetDirty(graph);
    }

    private void CreateConnection(WorldStateNode from, WorldStateNode to)
    {
        if (graph == null || from == null || to == null) return;

        // Evitar conexiones duplicadas
        if (from.connectedNodeIDs.Contains(to.id))
            return;

        // Crear la conexión
        WorldStateConnection newConnection = new WorldStateConnection(from.id, to.id);
        graph.connections.Add(newConnection);

        // Actualizar el nodo de origen
        from.connectedNodeIDs.Add(to.id);

        EditorUtility.SetDirty(graph);
    }

    private void RemoveConnection(string fromNodeID, string toNodeID)
    {
        if (graph == null) return;

        // Eliminar la conexión
        graph.connections.RemoveAll(c => c.fromNodeID == fromNodeID && c.toNodeID == toNodeID);

        // Actualizar el nodo de origen
        WorldStateNode fromNode = graph.FindNodeByID(fromNodeID);
        if (fromNode != null)
        {
            fromNode.connectedNodeIDs.Remove(toNodeID);
        }

        EditorUtility.SetDirty(graph);
    }

    private WorldStateNode GetNodeAtPosition(Vector2 position)
    {
        if (graph == null || graph.nodes == null) return null;

        // Buscar de atrás hacia adelante para seleccionar el nodo superior en caso de solapamiento
        for (int i = graph.nodes.Count - 1; i >= 0; i--)
        {
            if (graph.nodes[i].position.Contains(position))
                return graph.nodes[i];
        }

        return null;
    }

    private Texture2D MakeColoredTexture(int width, int height, Color color)
    {
        // Crear textura y asignar color
        Texture2D texture = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;

        texture.SetPixels(pixels);
        texture.Apply();

        return texture;
    }

    private void DrawArrow(Vector2 start, Vector2 end, float size)
    {
        Vector2 direction = (end - start).normalized;
        Vector2 right = new Vector2(-direction.y, direction.x) * size * 0.5f;

        Vector2 arrowStart = end - direction * size;
        Vector2 arrowEnd1 = arrowStart + right;
        Vector2 arrowEnd2 = arrowStart - right;

        // Dibujar la flecha
        Handles.DrawLine(end, arrowEnd1);
        Handles.DrawLine(end, arrowEnd2);
    }

    private void ShowObjectSelector(System.Action<string> onSelected)
    {
        // Encontrar todos los objetos con UniqueID en la escena
        UniqueID[] allIDs = GameObject.FindObjectsByType<UniqueID>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        // Crear ventana para mostrar y seleccionar
        GenericMenu menu = new GenericMenu();

        foreach (var id in allIDs)
        {
            string objectName = id.gameObject.name;
            string idValue = id.GetID();

            menu.AddItem(new GUIContent(objectName), false, () => {
                onSelected?.Invoke(idValue);
            });
        }

        menu.ShowAsContext();
    }
}