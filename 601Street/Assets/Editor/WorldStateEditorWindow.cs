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

        isDragging = false;
        startConnectionNode = null;
        isCreatingConnection = false;
        isPanning = false;

        // Establecer un valor de scroll inicial razonable
        scrollPosition = new Vector2(1000, 1000); // Punto de inicio más cercano al origen visual

        // Forzar actualización inmediata de la ventana
        wantsMouseMove = true;

        // IMPORTANTE: Añadir botón para centrar en nodos existentes cuando se abra el editor
        EditorApplication.delayCall += CenterOnAllNodes;
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

        // Área del grafo - obtenemos el rectángulo para el área de visualización
        Rect graphArea = GUILayoutUtility.GetRect(0, position.width * 0.7f, 0, position.height - 60);
        GUI.Box(graphArea, "Graph View", EditorStyles.helpBox);

        // ENFOQUE CRÍTICO: No usamos BeginScrollView, sino que dibujamos directamente en el área
        // y manejamos el desplazamiento nosotros mismos

        // Calcular la escala y transformación basada en scrollPosition
        float halfWidth = graphArea.width * 0.5f;
        float halfHeight = graphArea.height * 0.5f;

        // Transformar todas las coordenadas basadas en scrollPosition
        GUI.BeginClip(graphArea);

        // Dibujar el grid
        DrawCustomGrid(graphArea, 20, 0.2f, Color.gray);
        DrawCustomGrid(graphArea, 100, 0.4f, Color.gray);

        // Dibujar las conexiones primero
        DrawCustomConnections(graphArea);

        // Dibujar conexión en progreso si estamos creando una
        if (isCreatingConnection && startConnectionNode != null)
        {
            DrawCustomConnectionInProgress(graphArea);
        }

        // Dibujar los nodos
        DrawCustomNodes(graphArea);

        GUI.EndClip();

        // Manejar eventos después de dibujar todo
        HandleCustomEvents(graphArea);

        // Área de inspector para el nodo seleccionado
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.3f));
        DrawNodeInspector();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        // Botones de acción en la parte inferior
        DrawBottomButtons();

        // Mostrar información de depuración

        // Guardar cambios
        if (GUI.changed && graph != null)
        {
            EditorUtility.SetDirty(graph);
        }
    }
    private void HandleCustomEvents(Rect graphArea)
    {
        Event e = Event.current;

        // Si el ratón está fuera del área visible, solo procesar eventos Mouse Up
        if (!graphArea.Contains(e.mousePosition) && e.type != EventType.MouseUp)
            return;

        // Calcular la posición real del mouse en el espacio del mundo
        Vector2 worldMousePos = ScreenToWorldPoint(e.mousePosition, graphArea);

        // Detectar nodo bajo el cursor
        bool isOverNode = false;
        WorldStateNode nodeUnderMouse = null;
        const float nodePadding = 8f;

        if (graph != null && graph.nodes != null)
        {
            foreach (var node in graph.nodes)
            {
                // Crear un rectángulo expandido para mejor detección
                Rect expandedRect = new Rect(
                    node.position.x - nodePadding,
                    node.position.y - nodePadding,
                    node.position.width + (nodePadding * 2),
                    node.position.height + (nodePadding * 2)
                );

                if (expandedRect.Contains(worldMousePos))
                {
                    isOverNode = true;
                    nodeUnderMouse = node;
                    break;
                }
            }
        }

        // Para conexiones en progreso, actualizar posición
        if (isCreatingConnection)
        {
            currentMousePosition = worldMousePos;
            Repaint();
        }

        // Manejar eventos según su tipo
        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0) // Botón izquierdo
                {
                    if (isOverNode)
                    {
                        selectedNode = nodeUnderMouse;
                        isDragging = true;
                        GUI.FocusControl(null);
                        e.Use();

                    }
                }
                else if (e.button == 1) // Botón derecho
                {
                    if (isOverNode)
                    {
                        selectedNode = nodeUnderMouse;

                        // Mostrar menú contextual con opciones de misión
                        GenericMenu contextMenu = new GenericMenu();

                        // Opciones para misiones
                        contextMenu.AddItem(new GUIContent("Assign Mission..."), false, () => {
                            selectedNode = nodeUnderMouse;
                            ShowMissionSelector();
                        });

                        if (nodeUnderMouse.misionAsociada != null)
                        {
                            contextMenu.AddItem(new GUIContent("Clear Mission"), false, () => {
                                nodeUnderMouse.misionAsociada = null;
                                EditorUtility.SetDirty(graph);
                                Repaint();
                            });
                        }

                        // Separador entre secciones del menú
                        contextMenu.AddSeparator("");

                        // Opción para iniciar una conexión
                        contextMenu.AddItem(new GUIContent("Create Connection"), false, () => {
                            startConnectionNode = nodeUnderMouse;
                            isCreatingConnection = true;
                            currentMousePosition = worldMousePos;
                            Repaint();
                        });

                        contextMenu.ShowAsContext();
                        e.Use();
                    }
                    else
                    {
                        // Si no está sobre un nodo, iniciar una conexión desde el nodo seleccionado
                        if (selectedNode != null)
                        {
                            startConnectionNode = selectedNode;
                            isCreatingConnection = true;
                            currentMousePosition = worldMousePos;
                            e.Use();
                        }
                    }
                }
                else if (e.button == 2) // Botón central - navegación
                {
                    isPanning = true;
                    lastMousePosition = e.mousePosition;
                    e.Use();
                }
                break;

            case EventType.MouseUp:
                if (e.button == 0)
                {
                    if (isDragging)
                    {
                        isDragging = false;
                        e.Use();
                    }

                    if (isCreatingConnection && startConnectionNode != null)
                    {
                        if (isOverNode && nodeUnderMouse != startConnectionNode)
                        {
                            CreateConnection(startConnectionNode, nodeUnderMouse);
                        }
                        isCreatingConnection = false;
                        startConnectionNode = null;
                        e.Use();
                    }
                }
                else if (e.button == 2) // Finalizar navegación
                {
                    if (isPanning)
                    {
                        isPanning = false;
                        e.Use();
                    }
                }
                break;

            case EventType.MouseDrag:
                if (isDragging && selectedNode != null)
                {
                    // Arrastrar nodo - importante usar el delta directamente 
                    selectedNode.position.x += e.delta.x;
                    selectedNode.position.y += e.delta.y;
                    GUI.changed = true;
                    e.Use();
                }
                else if (isPanning)
                {
                    // Navegación - mover todo el viewport
                    scrollPosition -= e.delta;
                    GUI.changed = true;
                    e.Use();
                    Repaint();
                }
                break;

            case EventType.DragUpdated:
            case EventType.DragPerform:
                // Manejar arrastrar y soltar para misiones
                if (DragAndDrop.objectReferences.Length > 0 &&
                    DragAndDrop.objectReferences[0] is Mision)
                {
                    if (isOverNode)
                    {
                        // Permitir soltar la misión sobre el nodo
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                        if (e.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();

                            // Asignar la misión al nodo
                            nodeUnderMouse.misionAsociada = DragAndDrop.objectReferences[0] as Mision;
                            selectedNode = nodeUnderMouse; // Seleccionar el nodo
                            EditorUtility.SetDirty(graph);
                        }

                        e.Use();
                    }
                }
                break;

            case EventType.ScrollWheel:
                // Scroll vertical
                scrollPosition.y += e.delta.y * 15f;
                GUI.changed = true;
                e.Use();
                Repaint();
                break;
        }
    }
    private Vector2 WorldToScreenPoint(Vector2 worldPoint, Rect graphArea)
    {
        return new Vector2(
            worldPoint.x - scrollPosition.x,
            worldPoint.y - scrollPosition.y
        );
    }
    private Vector2 ScreenToWorldPoint(Vector2 screenPoint, Rect graphArea)
    {
        return new Vector2(
            screenPoint.x + scrollPosition.x,
            screenPoint.y + scrollPosition.y
        );
    } 
    public void CenterOnAllNodes()
    {
        if (graph == null || graph.nodes == null || graph.nodes.Count == 0)
            return;

        // Calcular el centro de todos los nodos
        Vector2 minPos = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 maxPos = new Vector2(float.MinValue, float.MinValue);

        foreach (var node in graph.nodes)
        {
            minPos.x = Mathf.Min(minPos.x, node.position.x);
            minPos.y = Mathf.Min(minPos.y, node.position.y);
            maxPos.x = Mathf.Max(maxPos.x, node.position.x + node.position.width);
            maxPos.y = Mathf.Max(maxPos.y, node.position.y + node.position.height);
        }

        // Calcular el centro de todos los nodos
        Vector2 center = (minPos + maxPos) * 0.5f;

        // Ajustar la posición de scroll para centrar todos los nodos
        scrollPosition = center - new Vector2(position.width * 0.35f, position.height * 0.5f);

        // Forzar repintado
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
    private void DrawNodeInspector()
    {
        // Lista de objetos inactivos 
        GUILayout.Space(10);
        GUILayout.Label("Inactive Objects", EditorStyles.boldLabel);
        DrawObjectList(selectedNode.inactiveObjectIDs);

        // NUEVA SECCIÓN: Misión asociada
        GUILayout.Space(10);
        GUILayout.Label("Associated Mission", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        // Mostrar la misión actualmente seleccionada
        Mision oldMission = selectedNode.misionAsociada;
        Mision newMission = (Mision)EditorGUILayout.ObjectField(
            selectedNode.misionAsociada, typeof(Mision), false);

        // Si ha cambiado la misión
        if (oldMission != newMission)
        {
            selectedNode.misionAsociada = newMission;
            EditorUtility.SetDirty(graph);
        }

        // Botón para buscar misiones
        if (GUILayout.Button("Find Mission", GUILayout.Width(100)))
        {
            ShowMissionSelector();
        }

        // Botón para eliminar la misión
        if (GUILayout.Button("Clear", GUILayout.Width(50)) && selectedNode.misionAsociada != null)
        {
            selectedNode.misionAsociada = null;
            EditorUtility.SetDirty(graph);
        }

        EditorGUILayout.EndHorizontal();

        if (selectedNode.misionAsociada != null)
        {
            EditorGUILayout.HelpBox(
                $"Mission: {selectedNode.misionAsociada.name}\nID: {selectedNode.misionAsociada.ID}",
                MessageType.Info);

            // Nuevo campo para el retraso de la misión
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Mission Delay (seconds):", GUILayout.Width(150));
            float newDelay = EditorGUILayout.FloatField(selectedNode.misionDelay, GUILayout.Width(50));

            if (newDelay != selectedNode.misionDelay)
            {
                selectedNode.misionDelay = Mathf.Max(0, newDelay); // Garantizar que no sea negativo
                EditorUtility.SetDirty(graph);
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox(
                "No mission assigned. Drag a Mission asset here or use the Find button.",
                MessageType.Info);
        }

        // Conexiones con mejoras visuales
        GUILayout.Space(10);
        GUILayout.Label("Connected To:", EditorStyles.boldLabel);

        GUILayout.Label("Node Inspector", EditorStyles.boldLabel);

        if (selectedNode == null)
        {
            EditorGUILayout.HelpBox("Select a node to edit", MessageType.Info);
            return;
        }

        // NUEVO: Campo para editar el nombre del nodo
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Name", GUILayout.Width(50));
        string oldName = selectedNode.name;
        string newName = EditorGUILayout.TextField(selectedNode.name);

        // Actualizar el nombre si ha cambiado
        if (oldName != newName)
        {
            selectedNode.name = newName;
            EditorUtility.SetDirty(graph);
        }
        EditorGUILayout.EndHorizontal();

        // Campo para editar el ID del nodo
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

        // SECCIÓN DE MANIPULACIÓN DE NODOS
        if (GUILayout.Button("Add Node", GUILayout.Width(80)))
        {
            AddNode();
        }

        if (selectedNode != null && GUILayout.Button("Delete Node", GUILayout.Width(80)))
        {
            DeleteNode(selectedNode);
        }

        GUILayout.FlexibleSpace();

        // SECCIÓN DE NAVEGACIÓN
        GUILayout.Label("Navigation:", GUILayout.Width(70));

        if (GUILayout.Button("Center All", GUILayout.Width(80)))
        {
            CenterOnAllNodes();
        }

        if (selectedNode != null && GUILayout.Button("Center Selected", GUILayout.Width(100)))
        {
            CenterOnNode(selectedNode);
        }

        if (GUILayout.Button("Reset View", GUILayout.Width(80)))
        {
            scrollPosition = new Vector2(1000, 1000);
            Repaint();
        }

        EditorGUILayout.EndHorizontal();
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

        // Crear un nuevo nodo en el centro de la vista actual
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

        // Calcular el centro del nodo
        Vector2 nodeCenter = new Vector2(
            node.position.x + node.position.width * 0.5f,
            node.position.y + node.position.height * 0.5f
        );

        // Ajustar scrollPosition para centrar el nodo en la ventana
        scrollPosition = nodeCenter - new Vector2(position.width * 0.35f, position.height * 0.5f);

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
    private void DrawCustomGrid(Rect graphArea, float gridSpacing, float gridOpacity, Color gridColor)
    {
        Handles.BeginGUI();
        Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

        // Calcular cuántas líneas necesitamos en cada dirección, basado en el área visible
        float startX = scrollPosition.x - gridSpacing;
        float startY = scrollPosition.y - gridSpacing;
        float endX = scrollPosition.x + graphArea.width + gridSpacing;
        float endY = scrollPosition.y + graphArea.height + gridSpacing;

        // Ajustar a las líneas de la cuadrícula
        startX = Mathf.Floor(startX / gridSpacing) * gridSpacing;
        startY = Mathf.Floor(startY / gridSpacing) * gridSpacing;

        // Dibujar líneas verticales
        for (float x = startX; x <= endX; x += gridSpacing)
        {
            float screenX = x - scrollPosition.x;
            Handles.DrawLine(
                new Vector3(screenX, 0, 0),
                new Vector3(screenX, graphArea.height, 0)
            );
        }

        // Dibujar líneas horizontales
        for (float y = startY; y <= endY; y += gridSpacing)
        {
            float screenY = y - scrollPosition.y;
            Handles.DrawLine(
                new Vector3(0, screenY, 0),
                new Vector3(graphArea.width, screenY, 0)
            );
        }

        Handles.EndGUI();
    }

    private void DrawCustomConnections(Rect graphArea)
    {
        if (graph == null || graph.connections == null) return;

        Handles.BeginGUI();

        foreach (var connection in graph.connections)
        {
            WorldStateNode fromNode = graph.FindNodeByID(connection.fromNodeID);
            WorldStateNode toNode = graph.FindNodeByID(connection.toNodeID);

            if (fromNode != null && toNode != null)
            {
                // Calcular puntos de conexión en el espacio de la pantalla
                Vector2 startPos = WorldToScreenPoint(new Vector2(
                    fromNode.position.x + fromNode.position.width,
                    fromNode.position.y + fromNode.position.height * 0.5f
                ), graphArea);

                Vector2 endPos = WorldToScreenPoint(new Vector2(
                    toNode.position.x,
                    toNode.position.y + toNode.position.height * 0.5f
                ), graphArea);

                // Dibujar solo si al menos un punto está en pantalla (optimización)
                Rect expandedArea = new Rect(
                    -200, -200,
                    graphArea.width + 400, graphArea.height + 400
                );

                if (expandedArea.Contains(startPos) || expandedArea.Contains(endPos))
                {
                    Handles.color = Color.white;

                    // Usar una curva Bezier para una mejor apariencia
                    Handles.DrawBezier(
                        startPos,
                        endPos,
                        startPos + Vector2.right * 50f,
                        endPos - Vector2.right * 50f,
                        Color.white,
                        null,
                        2f
                    );

                    // Dibujar flecha
                    DrawArrow(startPos, endPos, 15f);
                }
            }
        }

        Handles.EndGUI();
    }

    // Dibujar conexión en progreso
    private void DrawCustomConnectionInProgress(Rect graphArea)
    {
        if (startConnectionNode == null) return;

        Vector2 startPos = WorldToScreenPoint(new Vector2(
            startConnectionNode.position.x + startConnectionNode.position.width,
            startConnectionNode.position.y + startConnectionNode.position.height * 0.5f
        ), graphArea);

        Vector2 mousePos = Event.current.mousePosition;

        Handles.BeginGUI();
        Handles.color = Color.white;
        Handles.DrawLine(startPos, mousePos);

        // Flecha temporal
        Vector2 direction = (mousePos - startPos).normalized;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x) * 8f;

        Vector2 arrowTip = mousePos;
        Vector2 arrowLeft = arrowTip - direction * 15f + perpendicular;
        Vector2 arrowRight = arrowTip - direction * 15f - perpendicular;

        Handles.DrawLine(arrowTip, arrowLeft);
        Handles.DrawLine(arrowTip, arrowRight);
        Handles.EndGUI();
    }

    private void DrawCustomNodes(Rect graphArea)
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

        // Calcular la posición del mouse para efectos de hover
        Vector2 mousePos = Event.current.mousePosition;
        Vector2 worldMousePos = ScreenToWorldPoint(mousePos, graphArea);

        foreach (var node in graph.nodes)
        {
            // Convertir coordenadas del nodo al espacio de la pantalla
            Vector2 screenPos = WorldToScreenPoint(new Vector2(node.position.x, node.position.y), graphArea);

            // Verificar si el nodo está visible en la pantalla (o cerca)
            Rect screenRect = new Rect(screenPos.x, screenPos.y, node.position.width, node.position.height);
            Rect expandedGraphArea = new Rect(
                -node.position.width,
                -node.position.height,
                graphArea.width + node.position.width * 2,
                graphArea.height + node.position.height * 2
            );

            if (!expandedGraphArea.Overlaps(screenRect))
                continue; // Skipear nodos que no son visibles

            // Definir colores para diferentes tipos de nodos
            Color nodeColor;
            float borderWidth = 0f;

            bool isActiveInRuntime = EditorApplication.isPlaying && node.id == activeNodeID;
            bool isHovered = node.position.Contains(worldMousePos);

            // Prioridad de colores
            if (isActiveInRuntime)
            {
                nodeColor = new Color(1.0f, 0.5f, 0.0f, 0.9f);
                borderWidth = 3f;
            }
            else if (selectedNode == node)
            {
                nodeColor = new Color(0.7f, 0.7f, 0.9f, 0.9f);
            }
            else if (isHovered)
            {
                nodeColor = new Color(0.4f, 0.4f, 0.4f, 0.9f);
            }
            else if (node.isInitialNode)
            {
                nodeColor = new Color(0.5f, 0.8f, 0.5f, 0.9f);
            }
            else
            {
                nodeColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);
            }

            // Dibujar el fondo del nodo
            Rect nodeRect = new Rect(screenPos.x, screenPos.y, node.position.width, node.position.height);
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = CreateRoundedRectTexture(20, 20, nodeColor, 8);
            boxStyle.border = new RectOffset(8, 8, 8, 8);

            GUI.Box(nodeRect, "", boxStyle);

            // Si es el nodo activo en runtime, dibujar un borde destacado
            if (isActiveInRuntime && borderWidth > 0)
            {
                Handles.BeginGUI();
                Handles.color = Color.yellow;

                Rect borderRect = new Rect(
                    nodeRect.x - borderWidth,
                    nodeRect.y - borderWidth,
                    nodeRect.width + borderWidth * 2,
                    nodeRect.height + borderWidth * 2
                );

                Handles.DrawSolidRectangleWithOutline(borderRect, new Color(0, 0, 0, 0), Color.yellow);
                Handles.EndGUI();
            }

            // Dibujar contenido del nodo
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.alignment = TextAnchor.UpperCenter;
            titleStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(nodeRect.x, nodeRect.y + 5, nodeRect.width, 20), node.name, titleStyle);

            // Si es nodo activo, añadir etiqueta "ACTIVE"
            if (isActiveInRuntime)
            {
                GUIStyle activeStyle = new GUIStyle(EditorStyles.boldLabel);
                activeStyle.alignment = TextAnchor.UpperCenter;
                activeStyle.normal.textColor = Color.yellow;
                GUI.Label(new Rect(nodeRect.x, nodeRect.y + 25, nodeRect.width, 20), "ACTIVE", activeStyle);
            }

            // ID y contenido adicional
            float yPos = nodeRect.y + (isActiveInRuntime ? 45 : 30);
            string displayID = node.id.Length > 15 ? node.id.Substring(0, 15) + "..." : node.id;
            GUI.Label(new Rect(nodeRect.x + 5, yPos, nodeRect.width - 10, 20), $"ID: {displayID}");

            yPos += 20;
            GUI.Label(new Rect(nodeRect.x + 5, yPos, nodeRect.width - 10, 20),
                     $"Active Objects: {node.activeObjectIDs.Count}");

            yPos += 20;
            GUI.Label(new Rect(nodeRect.x + 5, yPos, nodeRect.width - 10, 20),
                     $"Inactive Objects: {node.inactiveObjectIDs.Count}");

            if (node.misionAsociada != null)
            {
                yPos += 20;
                GUIStyle missionStyle = new GUIStyle(EditorStyles.boldLabel);
                missionStyle.normal.textColor = new Color(1f, 0.8f, 0.2f); // Color dorado

                string missionText = $"Mission: {node.misionAsociada.name}";
                if (node.misionDelay > 0)
                {
                    missionText += $" ({node.misionDelay}s)";
                }

                GUI.Label(new Rect(nodeRect.x + 5, yPos, nodeRect.width - 10, 20),
                         missionText, missionStyle);
            }

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
    
    private void OnRunnerStateChanged(string oldState, string newState)
    {
        Repaint();
    }
    private void ShowMissionSelector()
    {
        // Crear un menú contextual con todas las misiones del proyecto
        GenericMenu menu = new GenericMenu();

        // Opción para eliminar la misión
        menu.AddItem(new GUIContent("None"), selectedNode.misionAsociada == null, () => {
            selectedNode.misionAsociada = null;
            EditorUtility.SetDirty(graph);
            Repaint();
        });

        menu.AddSeparator("");

        // Obtener todas las misiones del proyecto
        string[] guids = AssetDatabase.FindAssets("t:Mision");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Mision mission = AssetDatabase.LoadAssetAtPath<Mision>(path);

            if (mission != null)
            {
                // Construir ruta en el menú basada en la estructura de carpetas
                string menuPath = path.Replace("Assets/", "");
                menuPath = System.IO.Path.GetDirectoryName(menuPath).Replace("\\", "/");
                string displayName = mission.name;

                if (!string.IsNullOrEmpty(menuPath))
                    displayName = menuPath + "/" + displayName;

                menu.AddItem(
                    new GUIContent(displayName),
                    selectedNode.misionAsociada == mission,
                    () => {
                        selectedNode.misionAsociada = mission;
                        EditorUtility.SetDirty(graph);
                        Repaint();
                    });
            }
        }

        menu.ShowAsContext();
    }
}