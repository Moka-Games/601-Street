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

    private bool isPanning = false; // Para detectar cuando estamos moviendo la vista
    private Vector2 lastMousePosition; // Para calcular el desplazamiento

    private string graphAssetGUID;
    private double lastRepaintTime;



    [MenuItem("Window/World State Editor")]
    public static void ShowWindow()
    {
        GetWindow<WorldStateEditorWindow>("World State Editor");
    }

    private void Update()
    {
        // Solo actualizar en modo Play
        if (EditorApplication.isPlaying)
        {
            double currentTime = EditorApplication.timeSinceStartup;

            // Repintar cada 0.5 segundos
            if (currentTime > lastRepaintTime + 0.5)
            {
                Repaint();
                lastRepaintTime = currentTime;
            }
        }
    }
    private void OnEnable()
    {
        // Asegurarse de que el cursor no se quede "pegado" al reiniciar
        isDragging = false;
        startConnectionNode = null;
        isCreatingConnection = false;

        // Establece el callback para dibujo continuo cuando se crea una conexión
        wantsMouseMove = true;
        
        graphAssetGUID = EditorPrefs.GetString("WorldStateEditorGraphGUID", "");

        if (!string.IsNullOrEmpty(graphAssetGUID))
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(graphAssetGUID);
            if (!string.IsNullOrEmpty(assetPath))
            {
                graph = AssetDatabase.LoadAssetAtPath<WorldStateGraph>(assetPath);
            }
        }

        // Suscribirse al cambio de estado de Play Mode para mantener la referencia
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        
        if (EditorApplication.isPlaying)
        {
            SubscribeToRunnerEvents();
        }
    }
    private void OnDisable()
    {
        // Desuscribirse del evento al cerrar la ventana
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // Guardar la referencia del grafo antes de entrar a Play Mode
        if (state == PlayModeStateChange.EnteredEditMode ||
            state == PlayModeStateChange.EnteredPlayMode)
        {
            // Recargar la referencia al grafo si es necesario
            if (graph == null && !string.IsNullOrEmpty(graphAssetGUID))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(graphAssetGUID);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    graph = AssetDatabase.LoadAssetAtPath<WorldStateGraph>(assetPath);
                    Repaint();
                }
            }
        }

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            SubscribeToRunnerEvents();
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            // Desuscribirse al salir del Play Mode
            WorldStateGraphRunner runner = FindFirstObjectByType<WorldStateGraphRunner>();
            if (runner != null)
            {
                runner.OnStateChanged -= OnRunnerStateChanged;
            }
        }
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

        scrollPosition = GUI.BeginScrollView(
            graphArea,
            scrollPosition,
            new Rect(0, 0, 3000, 3000),
            false, // Ocultar barra horizontal
            false  // Ocultar barra vertical
        );

        DrawGrid(20, 0.2f, Color.gray); // Cuadrícula fina
        DrawGrid(100, 0.4f, Color.gray);

        // PRIMERO dibujamos las conexiones
        DrawConnections();

        // DESPUÉS dibujamos la conexión en progreso si estamos creando una
        if (isCreatingConnection && startConnectionNode != null)
        {
            Vector2 startPos = new Vector2(
                startConnectionNode.position.x + startConnectionNode.position.width,
                startConnectionNode.position.y + startConnectionNode.position.height * 0.5f);

            Handles.BeginGUI();
            Handles.color = Color.white;
            Handles.DrawLine(startPos, currentMousePosition);

            // Flecha temporal
            Vector2 direction = (currentMousePosition - startPos).normalized;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x) * 8f;

            Vector2 arrowTip = currentMousePosition;
            Vector2 arrowLeft = arrowTip - direction * 15f + perpendicular;
            Vector2 arrowRight = arrowTip - direction * 15f - perpendicular;

            Handles.DrawLine(arrowTip, arrowLeft);
            Handles.DrawLine(arrowTip, arrowRight);
            Handles.EndGUI();
        }


        // FINALMENTE dibujamos los nodos (para que estén encima de las conexiones)
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
    }
    private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
    {
        int widthDivs = Mathf.CeilToInt(5000f / gridSpacing);
        int heightDivs = Mathf.CeilToInt(5000f / gridSpacing);

        Handles.BeginGUI();
        Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

        // Líneas verticales
        Vector3 newOffset = new Vector3(scrollPosition.x % gridSpacing, 0, 0);
        for (int i = 0; i < widthDivs; i++)
        {
            Vector3 lineStart = new Vector3(gridSpacing * i, -gridSpacing, 0) - newOffset;
            Vector3 lineEnd = new Vector3(gridSpacing * i, 5000, 0) - newOffset;
            Handles.DrawLine(lineStart, lineEnd);
        }

        // Líneas horizontales
        newOffset = new Vector3(0, scrollPosition.y % gridSpacing, 0);
        for (int j = 0; j < heightDivs; j++)
        {
            Vector3 lineStart = new Vector3(-gridSpacing, gridSpacing * j, 0) - newOffset;
            Vector3 lineEnd = new Vector3(5000, gridSpacing * j, 0) - newOffset;
            Handles.DrawLine(lineStart, lineEnd);
        }

        Handles.EndGUI();
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

            // Guardar el GUID del grafo para persistencia
            if (graph != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(graph);
                graphAssetGUID = AssetDatabase.AssetPathToGUID(assetPath);
                EditorPrefs.SetString("WorldStateEditorGraphGUID", graphAssetGUID);
            }
            else
            {
                graphAssetGUID = "";
                EditorPrefs.SetString("WorldStateEditorGraphGUID", "");
            }
        }

        if (GUILayout.Button("Create New"))
        {
            CreateNewGraph();
        }
        EditorGUILayout.EndHorizontal();
    }
    // Modifica el método DrawNodes() en WorldStateEditorWindow.cs

    private void DrawNodes()
    {
        if (graph == null || graph.nodes == null) return;

        // Obtener el ID del nodo activo durante el runtime
        string activeNodeID = "";
        if (EditorApplication.isPlaying)
        {
            WorldStateGraphRunner runner = FindFirstObjectByType<WorldStateGraphRunner>();
            if (runner != null)
            {
                activeNodeID = runner.GetCurrentStateID();
            }
        }

        foreach (var node in graph.nodes)
        {
            // Definir colores para diferentes tipos de nodos
            Color nodeColor;
            float borderWidth = 0f;

            bool isActiveInRuntime = EditorApplication.isPlaying && node.id == activeNodeID;

            // Prioridad de colores: Activo en runtime > Seleccionado > Inicial > Normal
            if (isActiveInRuntime)
            {
                // Naranja brillante para nodo activo en runtime
                nodeColor = new Color(1.0f, 0.5f, 0.0f, 0.9f);
                borderWidth = 3f; // Borde más grueso para destacar
            }
            else if (selectedNode == node)
            {
                // Azulado para nodo seleccionado
                nodeColor = new Color(0.7f, 0.7f, 0.9f, 0.9f);
            }
            else if (node.isInitialNode)
            {
                // Verde para nodo inicial
                nodeColor = new Color(0.5f, 0.8f, 0.5f, 0.9f);
            }
            else
            {
                // Gris oscuro para nodos normales
                nodeColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);
            }

            // IMPORTANTE: Usamos GUI.Box que crea un área clickeable completa
            Rect nodeRect = node.position;
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = CreateRoundedRectTexture(20, 20, nodeColor, 8);
            boxStyle.border = new RectOffset(8, 8, 8, 8);

            // Dibujar el fondo del nodo
            GUI.Box(nodeRect, "", boxStyle);

            // Si es el nodo activo en runtime, dibujar un borde destacado
            if (isActiveInRuntime && borderWidth > 0)
            {
                // Dibujar un borde alrededor del nodo para destacarlo más
                Color borderColor = Color.yellow;
                Handles.BeginGUI();
                Handles.color = borderColor;

                // Dibujar el borde con un rectángulo expandido
                Rect borderRect = new Rect(
                    nodeRect.x - borderWidth,
                    nodeRect.y - borderWidth,
                    nodeRect.width + borderWidth * 2,
                    nodeRect.height + borderWidth * 2
                );

                // Usar líneas gruesas para el borde
                Handles.DrawSolidRectangleWithOutline(borderRect, new Color(0, 0, 0, 0), borderColor);
                Handles.EndGUI();
            }

            // Ahora dibujamos cada elemento del nodo (igual que antes)
            // Título
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.alignment = TextAnchor.UpperCenter;
            titleStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(nodeRect.x, nodeRect.y + 5, nodeRect.width, 20), node.name, titleStyle);

            // Si es nodo activo, añadir una etiqueta "ACTIVE"
            if (isActiveInRuntime)
            {
                GUIStyle activeStyle = new GUIStyle(EditorStyles.boldLabel);
                activeStyle.alignment = TextAnchor.UpperCenter;
                activeStyle.normal.textColor = Color.yellow;
                GUI.Label(new Rect(nodeRect.x, nodeRect.y + 25, nodeRect.width, 20), "ACTIVE", activeStyle);
            }

            // ID
            float yPos = nodeRect.y + (isActiveInRuntime ? 45 : 30);
            string displayID = node.id.Length > 15 ? node.id.Substring(0, 15) + "..." : node.id;
            GUI.Label(new Rect(nodeRect.x + 5, yPos, nodeRect.width - 10, 20), $"ID: {displayID}");

            // Contadores
            yPos += 20;
            GUI.Label(new Rect(nodeRect.x + 5, yPos, nodeRect.width - 10, 20),
                     $"Active Objects: {node.activeObjectIDs.Count}");

            yPos += 20;
            GUI.Label(new Rect(nodeRect.x + 5, yPos, nodeRect.width - 10, 20),
                     $"Inactive Objects: {node.inactiveObjectIDs.Count}");

            // Estado inicial
            if (node.isInitialNode)
            {
                yPos += 20;
                GUIStyle initialStyle = new GUIStyle(EditorStyles.boldLabel);
                initialStyle.normal.textColor = Color.green;
                GUI.Label(new Rect(nodeRect.x + 5, yPos, nodeRect.width - 10, 20),
                         "Initial Node", initialStyle);
            }
        }
    }
    private Texture2D CreateRoundedRectTexture(int width, int height, Color color, int radius)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];

        // Rellenar la textura con transparencia
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color(0, 0, 0, 0);

        // Rellenar el área central
        for (int y = radius; y < height - radius; y++)
        {
            for (int x = 0; x < width; x++)
            {
                pixels[y * width + x] = color;
            }
        }

        // Rellenar las áreas laterales (excluyendo esquinas)
        for (int y = 0; y < height; y++)
        {
            for (int x = radius; x < width - radius; x++)
            {
                pixels[y * width + x] = color;
            }
        }

        // Rellenar las esquinas con un degradado para simular redondez
        for (int y = 0; y < radius; y++)
        {
            for (int x = 0; x < radius; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
                if (distance <= radius)
                {
                    // Suavizar el borde
                    float alpha = distance < radius - 1 ? 1.0f : 1.0f - (distance - (radius - 1));
                    Color pixelColor = color;
                    pixelColor.a *= alpha;
                    pixels[y * width + x] = pixelColor;
                    pixels[y * width + (width - 1 - x)] = pixelColor;
                    pixels[(height - 1 - y) * width + x] = pixelColor;
                    pixels[(height - 1 - y) * width + (width - 1 - x)] = pixelColor;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return texture;
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
                // Calcular puntos de conexión (ajustados para quedar en el borde)
                Vector2 startPos = new Vector2(
                    fromNode.position.x + fromNode.position.width,
                    fromNode.position.y + fromNode.position.height * 0.5f);

                // Ajustamos la posición final para que quede exactamente en el borde izquierdo del nodo
                Vector2 endPos = new Vector2(
                    toNode.position.x,
                    toNode.position.y + toNode.position.height * 0.5f);

                // Dibujar la línea y flecha
                Handles.BeginGUI();
                Handles.color = Color.white;

                // Usar una curva Bezier para una mejor apariencia
                Handles.DrawBezier(
                    startPos,
                    endPos,
                    startPos + Vector2.right * 50f,
                    endPos - Vector2.right * 50f,
                    Color.white,
                    null,
                    2f);

                // Dibujar flecha sin que entre en el nodo destino
                DrawArrow(startPos, endPos, 15f);
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
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("ID", GUILayout.Width(30));
        string oldID = selectedNode.id;
        string newID = EditorGUILayout.TextField(selectedNode.id);

        if (GUILayout.Button("Generate Friendly ID", GUILayout.Width(120)))
        {
            newID = GenerateFriendlyID(selectedNode.name);
        }
        EditorGUILayout.EndHorizontal();

        // Si el ID ha cambiado, actualizar todas las referencias
        if (oldID != newID)
        {
            UpdateNodeID(oldID, newID);
        }


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

        // Lista de objetos activos con mejoras visuales
        GUILayout.Space(10);
        GUILayout.Label("Active Objects", EditorStyles.boldLabel);
        DrawObjectList(selectedNode.activeObjectIDs);

        // Lista de objetos inactivos 
        GUILayout.Space(10);
        GUILayout.Label("Inactive Objects", EditorStyles.boldLabel);
        DrawObjectList(selectedNode.inactiveObjectIDs);

        // Conexiones con mejoras visuales
        GUILayout.Space(10);
        GUILayout.Label("Connected To:", EditorStyles.boldLabel);

        // Si no hay conexiones, mostrar mensaje
        if (selectedNode.connectedNodeIDs.Count == 0)
        {
            EditorGUILayout.HelpBox("No connections. Right-click and drag to a node to create a connection.", MessageType.Info);
        }

        foreach (var connectionID in selectedNode.connectedNodeIDs)
        {
            WorldStateNode targetNode = graph.FindNodeByID(connectionID);
            if (targetNode != null)
            {
                EditorGUILayout.BeginHorizontal();
                GUIStyle nodeStyle = new GUIStyle(EditorStyles.label);
                nodeStyle.normal.textColor = Color.cyan; // Color distintivo para los nombres

                EditorGUILayout.LabelField(targetNode.name, nodeStyle);

                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    // Seleccionar el nodo conectado
                    selectedNode = targetNode;
                }

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

    private string GenerateFriendlyID(string nodeName)
    {
        // Limpiar el nombre (eliminar espacios, caracteres especiales, etc.)
        string cleanName = System.Text.RegularExpressions.Regex.Replace(nodeName, "[^a-zA-Z0-9]", "");

        if (string.IsNullOrEmpty(cleanName))
            cleanName = "Node";

        // Añadir timestamp para garantizar unicidad
        string uniqueID = $"{cleanName}_{System.DateTime.Now.Ticks % 10000}";

        return uniqueID;
    }
    // Modifica en WorldStateEditorWindow.cs la función DrawObjectList, alrededor de la línea 447
    private void DrawObjectList(List<string> objectIDs)
    {
        int removeIndex = -1;

        for (int i = 0; i < objectIDs.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            objectIDs[i] = EditorGUILayout.TextField(objectIDs[i]);

            if (GUILayout.Button("Find", GUILayout.Width(50)))
            {
                int currentIndex = i; // Guarda el índice actual en una variable local
                ShowObjectSelector(selectedID => {
                    if (currentIndex < objectIDs.Count)
                    { // Verificar que el índice sigue siendo válido
                        objectIDs[currentIndex] = selectedID;
                        Repaint();
                    }
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

        // Botones existentes...
        if (GUILayout.Button("Add Node"))
        {
            AddNode();
        }

        if (selectedNode != null && GUILayout.Button("Delete Node"))
        {
            DeleteNode(selectedNode);
        }

        // Botones de navegación
        EditorGUILayout.Space();

        if (GUILayout.Button("Center on Selected", GUILayout.Width(120)))
        {
            if (selectedNode != null)
            {
                CenterOnNode(selectedNode);
            }
        }

        if (GUILayout.Button("Reset View", GUILayout.Width(80)))
        {
            scrollPosition = Vector2.zero;
            Repaint();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void HandleEvents(Rect graphArea)
    {
        Event e = Event.current;
        Vector2 mousePos = e.mousePosition;
        Vector2 scrolledMousePos = mousePos + scrollPosition;

        // Verificar si el ratón está sobre algún nodo
        bool isOverNode = false;
        WorldStateNode nodeUnderMouse = null;

        for (int i = graph.nodes.Count - 1; i >= 0; i--)
        {
            if (graph.nodes[i].position.Contains(scrolledMousePos))
            {
                isOverNode = true;
                nodeUnderMouse = graph.nodes[i];
                break;
            }
        }

        // Actualizar la posición para la conexión en progreso
        if (isCreatingConnection)
        {
            currentMousePosition = scrolledMousePos;
            Repaint();
        }

        switch (e.type)
        {
            case EventType.MouseDown:
                if (graphArea.Contains(mousePos))
                {
                    if (e.button == 0) // Botón izquierdo
                    {
                        if (isOverNode)
                        {
                            // Seleccionar y preparar para arrastrar nodo
                            selectedNode = nodeUnderMouse;
                            isDragging = true;
                        }
                        else
                        {
                            // Click en el fondo - iniciar movimiento de la vista
                            isPanning = true;
                            lastMousePosition = mousePos;
                        }
                    }
                    else if (e.button == 1) // Botón derecho
                    {
                        if (isOverNode)
                        {
                            // Iniciar conexión desde este nodo
                            selectedNode = nodeUnderMouse;
                            startConnectionNode = nodeUnderMouse;
                            isCreatingConnection = true;
                            currentMousePosition = scrolledMousePos;
                        }
                    }
                    else if (e.button == 2) // Click con la rueda del ratón
                    {
                        // Iniciar movimiento de vista con la rueda
                        isPanning = true;
                        lastMousePosition = mousePos;
                    }

                    e.Use(); // Importante: consumir el evento
                }
                break;

            case EventType.MouseUp:
                if (e.button == 0 || e.button == 2) // Izquierdo o rueda
                {
                    if (isCreatingConnection && startConnectionNode != null)
                    {
                        // Finalizar conexión
                        if (isOverNode && nodeUnderMouse != startConnectionNode)
                        {
                            CreateConnection(startConnectionNode, nodeUnderMouse);
                        }

                        isCreatingConnection = false;
                        startConnectionNode = null;
                    }

                    // Detener arrastre de nodo o movimiento de vista
                    isDragging = false;
                    isPanning = false;

                    e.Use();
                }
                break;

            case EventType.MouseDrag:
                if (isDragging && selectedNode != null)
                {
                    // Arrastrar nodo
                    selectedNode.position.x += e.delta.x;
                    selectedNode.position.y += e.delta.y;
                    e.Use();
                }
                else if (isPanning)
                {
                    // Mover la vista (esto es lo clave)
                    scrollPosition -= e.delta;
                    e.Use();
                }

                Repaint(); // Forzar repintado para actualizar la vista
                break;

            case EventType.ScrollWheel:
                if (graphArea.Contains(mousePos))
                {
                    // Desplazamiento vertical con la rueda
                    scrollPosition.y += e.delta.y * 15; // Multiplicador para ajustar velocidad
                    e.Use();
                    Repaint();
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

    private void DrawArrow(Vector2 start, Vector2 end, float size)
    {
        Vector2 direction = (end - start).normalized;
        Vector2 right = new Vector2(-direction.y, direction.x) * size * 0.5f;

        // Ajustar el punto final para que la flecha quede justo en el borde
        Vector2 arrowTip = end - direction * 2f; // Retroceder 2 píxeles para no entrar en el nodo

        // Calcular los puntos de la flecha
        Vector2 arrowBase = arrowTip - direction * size;
        Vector2 arrowEnd1 = arrowBase + right;
        Vector2 arrowEnd2 = arrowBase - right;

        // Dibujar la flecha con líneas más gruesas
        Handles.color = Color.white;
        Handles.DrawAAPolyLine(3f, arrowTip, arrowEnd1);
        Handles.DrawAAPolyLine(3f, arrowTip, arrowEnd2);
    }

    // En WorldStateEditorWindow.cs, modifica ShowObjectSelector:
    private void ShowObjectSelector(System.Action<string> onSelected)
    {
        UniqueID[] allIDs = GameObject.FindObjectsByType<UniqueID>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (allIDs.Length == 0)
        {
            EditorUtility.DisplayDialog("No UniqueID Objects",
                "No objects with UniqueID component found in the scene. Add UniqueID components to objects first.",
                "OK");
            return;
        }

        // Crear menú para mostrar y seleccionar
        GenericMenu menu = new GenericMenu();

        // Agrupar por jerarquía para mejor organización
        Dictionary<string, List<UniqueID>> groupedIDs = new Dictionary<string, List<UniqueID>>();

        foreach (var id in allIDs)
        {
            if (id == null || id.gameObject == null) continue; // Evitar errores con objetos destruidos

            string path = GetGameObjectPath(id.gameObject);
            string group = path.Split('/')[0]; // Usar el primer nivel como grupo

            if (!groupedIDs.ContainsKey(group))
                groupedIDs[group] = new List<UniqueID>();

            groupedIDs[group].Add(id);
        }

        // Añadir elementos al menú, agrupados
        foreach (var group in groupedIDs.Keys)
        {
            foreach (var id in groupedIDs[group])
            {
                if (id == null || id.gameObject == null) continue; // Verificación adicional

                string objectName = id.gameObject.name;
                string idValue = id.GetID();
                string fullPath = GetGameObjectPath(id.gameObject);

                // Usar closure para capturar el idValue actual
                string capturedID = idValue;
                menu.AddItem(new GUIContent($"{group}/{fullPath}"), false, () => {
                    onSelected?.Invoke(capturedID);
                });
            }
        }

        menu.ShowAsContext();
    }
    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }
    private void UpdateNodeID(string oldID, string newID)
    {
        // Actualizar el ID en el nodo seleccionado
        selectedNode.id = newID;

        // Actualizar referencia en initialNodeID si es necesario
        if (graph.initialNodeID == oldID)
        {
            graph.initialNodeID = newID;
        }

        // Actualizar referencias en las conexiones
        foreach (var connection in graph.connections)
        {
            if (connection.fromNodeID == oldID)
                connection.fromNodeID = newID;

            if (connection.toNodeID == oldID)
                connection.toNodeID = newID;
        }

        // Actualizar referencias en las listas connectedNodeIDs de otros nodos
        foreach (var node in graph.nodes)
        {
            for (int i = 0; i < node.connectedNodeIDs.Count; i++)
            {
                if (node.connectedNodeIDs[i] == oldID)
                    node.connectedNodeIDs[i] = newID;
            }
        }

        EditorUtility.SetDirty(graph);
    }

    // Función para centrar la vista en un nodo seleccionado
    private void CenterOnNode(WorldStateNode node)
    {
        if (node == null) return;

        Rect graphArea = GUILayoutUtility.GetRect(0, position.width * 0.7f, 0, position.height - 40);

        // Calcular el centro del área visible
        Vector2 viewCenter = new Vector2(graphArea.width * 0.5f, graphArea.height * 0.5f);

        // Calcular el centro del nodo
        Vector2 nodeCenter = new Vector2(
            node.position.x + node.position.width * 0.5f,
            node.position.y + node.position.height * 0.5f
        );

        // Ajustar scrollPosition para centrar el nodo
        scrollPosition = nodeCenter - viewCenter;

        Repaint();
    }
    private void SubscribeToRunnerEvents()
    {
        if (EditorApplication.isPlaying)
        {
            WorldStateGraphRunner runner = FindFirstObjectByType<WorldStateGraphRunner>();
            if (runner != null)
            {
                // Desuscribirse primero para evitar duplicados
                runner.OnStateChanged -= OnRunnerStateChanged;
                runner.OnStateChanged += OnRunnerStateChanged;
            }
        }
    }

    private void OnRunnerStateChanged(string oldState, string newState)
    {
        // Repintar inmediatamente cuando cambia el estado
        Repaint();
    }

}