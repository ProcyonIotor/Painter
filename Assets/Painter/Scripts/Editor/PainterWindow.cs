using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEditorInternal;
using System.Linq;

namespace Painter
{
    public class PainterWindow : EditorWindow
    {
        [SerializeField]
        BrushSettings brushSettings = new BrushSettings();
        BrushStroke brushStroke;
        bool isPainting;
        bool isPaintFocus;

        enum VertexPainterUtility
        {
            Scene,
            Paint,
            Bake
        }

        string[] toolbarNames = new string[] { "Selection", "Paint", "Bake" };
        string[] paintModeNames = new string[] { "Paint", "Erase", "Blend" };

        VertexPainterUtility vertexPainterUtility = VertexPainterUtility.Paint;

        private List<Color> colorSwatches = new List<Color>();

        BakeAO bakeAO = new BakeAO();

        // VERTEX PAINTER TOOL SETTINGS
        private const float MAX_RADIUS = 5f;
        private const float MIN_RADIUS = 0f;
        private const float BRUSH_RESCALE_STEP = 0.05f;

        [MenuItem("Window/Painter")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(PainterWindow), true, "Painter", true);
        }

        Vector2 scrollPosition;

        void OnGUI()
        {
            EditorGUILayout.Separator();
            vertexPainterUtility = (VertexPainterUtility)GUILayout.Toolbar((int)vertexPainterUtility, toolbarNames);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);//Separator line
            scrollPosition =
                EditorGUILayout.BeginScrollView(scrollPosition);
            switch (vertexPainterUtility)
            {
                case VertexPainterUtility.Scene:
                    isPaintFocus = false;
                    DrawSelectionWindow();
                    break;
                case VertexPainterUtility.Paint:
                    DrawPaintWindow();
                    break;
                case VertexPainterUtility.Bake:
                    isPaintFocus = false;
                    DrawBakeWindow();
                    break;
            }
            EditorGUILayout.EndScrollView();
        }

        List<VertexStream> sceneVertexStreamList = new List<VertexStream>();
        private ReorderableList reorderableVertexStreamList;
        VertexStream selectedVertexStream;
        private string selectByLayerNameString = "Layer Name";

        void DrawSelectionWindow()
        {
            Tools.hidden = false;
            EditorGUILayout.LabelField("Paint Targets", EditorStyles.boldLabel);
            if (reorderableVertexStreamList != null)
                reorderableVertexStreamList.DoLayoutList();
            EditorGUILayout.LabelField("Selection Tools", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            if (selectedVertexStream == null)
                GUI.enabled = false;
            if (GUILayout.Button("Find in Scene"))
                Selection.activeGameObject = selectedVertexStream.gameObject;
            GUI.enabled = true;
            if (sceneVertexStreamList.Count == 0)
                GUI.enabled = false;
            if (GUILayout.Button("Select All"))
            {
                List<GameObject> vertexStreamObjectList = new List<GameObject>();
                for (int i = 0; i < sceneVertexStreamList.Count; i++)
                    vertexStreamObjectList.Add(sceneVertexStreamList[i].gameObject);
                GameObject[] vertexStreamObjectArray = vertexStreamObjectList.ToArray();
                Selection.objects = vertexStreamObjectArray;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Filter By Layer Name"))
                Selection.objects = SelectByLayerName();
            selectByLayerNameString = GUILayout.TextField(selectByLayerNameString);
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Selection"))
            {
                GameObject[] objSelection = Selection.gameObjects;
                for (int i = 0; i < objSelection.Length; i++)
                {
                    if (objSelection[i].GetComponent<MeshFilter>() != null)
                    {
                        if (objSelection[i].GetComponent<VertexStream>())
                        {
                            if (!sceneVertexStreamList.Contains(objSelection[i].GetComponent<VertexStream>()))
                                sceneVertexStreamList.Add(objSelection[i].GetComponent<VertexStream>());
                        }
                        else
                        {
                            objSelection[i].AddComponent<VertexStream>();
                            sceneVertexStreamList.Add(objSelection[i].GetComponent<VertexStream>());
                        }
                    }
                }
                UpdatePaintTargetList();
            }
            GUILayout.EndHorizontal();
        }

        void UpdateVertexStreamList()
        {
            VertexStream[] vertexStreamArray = (VertexStream[])FindObjectsOfType(typeof(VertexStream));
            sceneVertexStreamList = vertexStreamArray.ToList();
        }

        void OnEnable()
        {
            UpdateVertexStreamList();
            UpdatePaintTargetList();
            GetPaintSelection();
            LoadEditorPrefs();
            EditorApplication.hierarchyWindowChanged += OnHierarchyChanged;
            Selection.selectionChanged += OnSelectionChanged;
        }

        void OnHierarchyChanged()
        {
            if (vertexPainterUtility == VertexPainterUtility.Scene)
            {
                UpdateVertexStreamList();
                UpdatePaintTargetList();
            }
        }

        List<VertexStream> paintSelectionList = new List<VertexStream>();

        void OnSelectionChanged()
        {
            GetPaintSelection();
        }

        // Get all vertex streams in the selected objects and their child objects
        public VertexStream[] GetPaintSelection()
        {
            GameObject[] objSelection = Selection.gameObjects;
            List<VertexStream> mfSelection = new List<VertexStream>();
            for (int i = 0; i < objSelection.Length; i++)
            {
                VertexStream[] childVertexStreams = objSelection[i].GetComponentsInChildren<VertexStream>();
                for (int j = 0; j < childVertexStreams.Length; j++)
                    if (!mfSelection.Contains(childVertexStreams[j]))
                        mfSelection.Add(childVertexStreams[j]);
            }
            paintSelectionList.Clear();
            paintSelectionList = mfSelection;
            return paintSelectionList.ToArray();
        }

        void UpdatePaintTargetList()
        {
            reorderableVertexStreamList = new ReorderableList(sceneVertexStreamList, typeof(VertexStream), false, false, false, false);
            reorderableVertexStreamList.drawElementCallback += DrawElement;
            reorderableVertexStreamList.onSelectCallback += OnListSelect;
        }

        void OnListSelect(ReorderableList list)
        {
            selectedVertexStream = sceneVertexStreamList[list.index];
        }

        void DrawHeader(Rect rect)
        {
            GUI.Label(rect, "");
        }

        void DrawElement(Rect rect, int index, bool active, bool focused)
        {
            VertexStream item = sceneVertexStreamList[index];
            EditorGUI.BeginChangeCheck();
            EditorGUI.LabelField(new Rect(rect.x, rect.y, 90, rect.height), item.name);
            item.isPaintEnabled = EditorGUI.Toggle(new Rect(rect.x + 90, rect.y, 18, rect.height), item.isPaintEnabled);
            EditorGUI.EndChangeCheck();
        }

        void ApplyColorFill()
        {
            GameObject[] selectedObjects = Selection.gameObjects;
            List<VertexStream> selectedVertexStreamList = new List<VertexStream>();
            for (int i = 0; i < selectedObjects.Length; i++)
            {
                VertexStream stream = selectedObjects[i].GetComponent<VertexStream>();
                if (stream != null)
                    selectedVertexStreamList.Add(stream);
            }

            for (int i = 0; i < selectedVertexStreamList.Count; i++)
            {
                Layer targetLayer = selectedVertexStreamList[i].TargetLayer;
                Color[] fillColors = new Color[selectedVertexStreamList[i].Stream.vertexCount];
                float[] fillTransparency = new float[selectedVertexStreamList[i].Stream.vertexCount];

                for (int j = 0; j < fillColors.Length; j++)
                {
                    fillColors[j] = brushSettings.color;
                    fillTransparency[j] = 1.0f;
                }
                targetLayer.Colors = fillColors;
                targetLayer.Transparency = fillTransparency;
                selectedVertexStreamList[i].RecalculateOutputColors();
            }
        }

        Texture2D CreateColorSwatchPreview(Color color)
        {
            Color opaqueColor = color;
            opaqueColor.a = 1f;
            Texture2D texture = new Texture2D(30, 30);
            Color[] colors = new Color[30 * 30];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = opaqueColor;
            texture.SetPixels(colors);
            texture.Apply();
            return texture;
        }

        List<Texture2D> colorSwatchPreviews = new List<Texture2D>();
        int activeColorSwatch = 0;

        void DrawPaintWindow()
        {
            if (isPaintFocus)
            {
                Tools.hidden = true;
                brushSettings.paintMode = (BrushSettings.PaintMode)GUILayout.Toolbar((int)brushSettings.paintMode, paintModeNames);
                brushSettings.radius = EditorGUILayout.Slider("Radius", brushSettings.radius, MIN_RADIUS, MAX_RADIUS);
                brushSettings.strength = EditorGUILayout.Slider("Strength", brushSettings.strength, 0.0f, 1.0f);
                brushSettings.falloff = EditorGUILayout.Slider("Falloff", brushSettings.falloff, 0.0f, 1.0f);
                brushSettings.ignoreBackfacing = EditorGUILayout.Toggle("Ignore Back-Facing", brushSettings.ignoreBackfacing);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);//Separator line
                if (brushSettings.isColorActive)
                {
                    brushSettings.color = EditorGUILayout.ColorField("Color", brushSettings.color);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorGUI.BeginChangeCheck();
                    activeColorSwatch = GUILayout.Toolbar(activeColorSwatch, colorSwatchPreviews.ToArray());
                    if (EditorGUI.EndChangeCheck())
                        brushSettings.color = colorSwatches[activeColorSwatch];
                    if (GUILayout.Button("+", GUILayout.Width(36), GUILayout.Height(36)))
                    {
                        colorSwatches.Add(brushSettings.color);
                        colorSwatchPreviews.Add(CreateColorSwatchPreview(brushSettings.color));
                        EditorPrefs.SetInt("Swatch_Count", colorSwatches.Count);
                        EditorPrefs.SetFloat("Swatch_" + (colorSwatches.Count - 1) + "_R", brushSettings.color.r);
                        EditorPrefs.SetFloat("Swatch_" + (colorSwatches.Count - 1) + "_G", brushSettings.color.g);
                        EditorPrefs.SetFloat("Swatch_" + (colorSwatches.Count - 1) + "_B", brushSettings.color.b);
                        EditorPrefs.SetFloat("Swatch_" + (colorSwatches.Count - 1) + "_A", brushSettings.color.a);
                    }
                    if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Trash"), GUILayout.Width(36), GUILayout.Height(36)))
                    {
                        if(EditorUtility.DisplayDialog("Clear Swatches",
                "Are you sure you want to remove all " + colorSwatches.Count
                + " swatches?", "Clear", "Cancel"))
                        {
                            for (int i = colorSwatches.Count - 1; i >= 0; i--)
                            {
                                EditorPrefs.DeleteKey("Swatch_" + i + "_R");
                                EditorPrefs.DeleteKey("Swatch_" + i + "_G");
                                EditorPrefs.DeleteKey("Swatch_" + i + "_B");
                                EditorPrefs.DeleteKey("Swatch_" + i + "_A");
                            }
                            colorSwatches.Clear();
                            colorSwatchPreviews.Clear();
                            EditorPrefs.SetInt("Swatch_Count", colorSwatches.Count);
                        }                       
                    }

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    brushSettings.isRGBActive[0] = EditorGUILayout.Toggle("R", brushSettings.isRGBActive[0]);
                    if (!brushSettings.isRGBActive[0])
                        GUI.enabled = false;
                    brushSettings.color.r = EditorGUILayout.Slider(brushSettings.color.r, 0, 1);
                    if (!brushSettings.isRGBActive[0])
                        GUI.enabled = true;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    brushSettings.isRGBActive[1] = EditorGUILayout.Toggle("G", brushSettings.isRGBActive[1]);
                    if (!brushSettings.isRGBActive[1])
                        GUI.enabled = false;
                    brushSettings.color.g = EditorGUILayout.Slider(brushSettings.color.g, 0, 1);
                    if (!brushSettings.isRGBActive[1])
                        GUI.enabled = true;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    brushSettings.isRGBActive[2] = EditorGUILayout.Toggle("B", brushSettings.isRGBActive[2]);
                    if (!brushSettings.isRGBActive[2])
                        GUI.enabled = false;
                    brushSettings.color.b = EditorGUILayout.Slider(brushSettings.color.b, 0, 1);
                    if (!brushSettings.isRGBActive[2])
                        GUI.enabled = true;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    brushSettings.isRGBActive[3] = EditorGUILayout.Toggle("A", brushSettings.isRGBActive[3]);
                    if (!brushSettings.isRGBActive[3])
                        GUI.enabled = false;
                    brushSettings.color.a = EditorGUILayout.Slider(brushSettings.color.a, 0, 1);
                    if (!brushSettings.isRGBActive[3])
                        GUI.enabled = true;
                    EditorGUILayout.EndHorizontal();
                    if (GUILayout.Button("Color Fill"))
                        ApplyColorFill();
                }
                GUI.enabled = true;
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);//Separator line
            }
            else
                Tools.hidden = false;
            if (isPaintFocus == false)
            {
                if (paintSelectionList.Count > 0)
                    GUI.enabled = true;
                else
                    GUI.enabled = false;
                if (GUILayout.Button("Paint Selection"))
                    isPaintFocus = true;
            }

            if (isPaintFocus == true)
            {
                if (GUILayout.Button("Stop Painting"))
                    isPaintFocus = false;
            }
            DrawSelectionList();
        }

        void DrawSelectionList()
        {
            if (paintSelectionList.Count > 0)
            {
                GUILayout.Label("Selected Vertex Streams:", EditorStyles.centeredGreyMiniLabel);
                for (int i = 0; i < paintSelectionList.Count; i++)
                    GUILayout.Label(paintSelectionList[i].name, EditorStyles.centeredGreyMiniLabel);
            }
        }

        void DrawBakeWindow()
        {
            Tools.hidden = false;
            EditorGUILayout.LabelField("AO Bake Settings", EditorStyles.boldLabel);
            bakeAO.samples = EditorGUILayout.IntField("Samples", Mathf.Max(1, bakeAO.samples));
            bakeAO.maxDistance = EditorGUILayout.FloatField("Max Sample Distance", Mathf.Max(0, bakeAO.maxDistance));
            bakeAO.spread = EditorGUILayout.Slider("Spread", bakeAO.spread, 0, 180);
            bakeAO.intensity = EditorGUILayout.FloatField("Intensity", Mathf.Max(0, bakeAO.intensity));
            bakeAO.bias = EditorGUILayout.FloatField("Bias", Mathf.Max(0.01f, bakeAO.bias));
            EditorGUILayout.LabelField("Ambient Color", EditorStyles.boldLabel);
            bakeAO.minColor = EditorGUILayout.ColorField("Min Color", bakeAO.minColor);
            bakeAO.maxColor = EditorGUILayout.ColorField("Max Color", bakeAO.maxColor);
            EditorGUILayout.LabelField("Apply Color Channel", EditorStyles.boldLabel);
            bakeAO.assignToColorChannel[0] = EditorGUILayout.Toggle("R", bakeAO.assignToColorChannel[0]);
            bakeAO.assignToColorChannel[1] = EditorGUILayout.Toggle("G", bakeAO.assignToColorChannel[1]);
            bakeAO.assignToColorChannel[2] = EditorGUILayout.Toggle("B", bakeAO.assignToColorChannel[2]);
            bakeAO.assignToColorChannel[3] = EditorGUILayout.Toggle("A", bakeAO.assignToColorChannel[3]);
            EditorGUILayout.LabelField("Blend Mode", EditorStyles.boldLabel);
            bakeAO.aoBlendMode = (BakeAO.AOBlendMode)GUILayout.Toolbar((int)bakeAO.aoBlendMode, new string[] { "Replace", "Multiply" });
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);//Separator line
            if (GUILayout.Button("Bake AO for Selection"))
                bakeAO.Bake(GetPaintSelection());

            if (GUILayout.Button("Restore Default Settings"))
            {
                bakeAO.minColor = Color.black;
                bakeAO.maxColor = Color.white;
                bakeAO.samples = 256;
                bakeAO.maxDistance = 4;
                bakeAO.spread = 162;
                bakeAO.intensity = 1;
                bakeAO.bias = 0.1f;
            }
            DrawSelectionList();
        }

        void OnInspectorUpdate()
        {
            Repaint();
        }

        void OnSceneGUI(SceneView sceneView)
        {
            if (isPaintFocus && vertexPainterUtility == VertexPainterUtility.Paint)
            {
                Event currentEvent = Event.current;
                Painting(currentEvent, sceneView);

                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive)); // Stop selecting objects in the scene view

                // rescale the brush
                if (currentEvent.keyCode == (KeyCode.LeftBracket))
                {
                    if (brushSettings.radius > MIN_RADIUS + BRUSH_RESCALE_STEP)
                        brushSettings.radius -= BRUSH_RESCALE_STEP;
                    else
                        brushSettings.radius = MIN_RADIUS;
                    EditorPrefs.SetFloat("PaintRadius", brushSettings.radius);
                    GetWindow(typeof(PainterWindow)).Repaint();
                    sceneView.Focus();
                }
                else if (currentEvent.keyCode == (KeyCode.RightBracket))
                {
                    if (brushSettings.radius < MAX_RADIUS - BRUSH_RESCALE_STEP)
                        brushSettings.radius += BRUSH_RESCALE_STEP;
                    else
                        brushSettings.radius = MAX_RADIUS;
                    EditorPrefs.SetFloat("PaintRadius", brushSettings.radius);
                    GetWindow(typeof(PainterWindow)).Repaint();
                    sceneView.Focus();
                }
            }
        }

        void Painting(Event currentEvent, SceneView sceneView)
        {
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && !currentEvent.alt)
            {
                isPainting = true;
                brushStroke = new BrushStroke();
                Undo.RegisterCompleteObjectUndo(brushStroke.VertexStreamMeshes, "Vertex Painting");
                GetWindow(typeof(PainterWindow)).Repaint();
                sceneView.Focus();
            }
            else if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0)
            {
                isPainting = false;
                GetWindow(typeof(PainterWindow)).Repaint();
                sceneView.Focus();
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            if (isPainting)
            {
                Undo.RecordObjects(brushStroke.VertexStreamMeshes, "Vertex Painting");
                RaycastHit hit;
                Vector3 centerMousePosition = currentEvent.mousePosition;
                //Adjust for screen
                float mult = EditorGUIUtility.pixelsPerPoint;
                centerMousePosition.y = sceneView.camera.pixelHeight - centerMousePosition.y * mult;
                centerMousePosition.x *= mult;

                Ray ray = sceneView.camera.ScreenPointToRay(centerMousePosition);
                if (Physics.Raycast(ray, out hit))
                {
                    Vector3 from = Mathf.Abs(hit.normal.x) != 1 ? Vector3.Cross(hit.normal, Vector3.right) : Vector3.Cross(hit.normal, Vector3.forward);
                    Color handleColor = new Color(1f - brushSettings.color.r, 1f - brushSettings.color.g, 1f - brushSettings.color.b, 1);
                    Handles.color = handleColor;
                    Handles.DrawWireArc(hit.point, hit.normal, from, 360, brushSettings.radius); //Full radius
                    Handles.DrawWireArc(hit.point, hit.normal, from, 360, brushSettings.radius - brushSettings.radius * brushSettings.falloff); // Falloff
                    Handles.DrawLine(hit.point, hit.point + hit.normal * brushSettings.radius);
                    if (hit.collider.GetComponent<VertexStream>() && paintSelectionList.Contains(hit.collider.GetComponent<VertexStream>()))
                        brushStroke.AddMeshStream(hit.collider.GetComponent<VertexStream>());
                    brushStroke.UpdateStroke(hit.point, brushSettings);
                }
            }
        }

        GameObject[] SelectByLayerName()
        {
            VertexStream[] vertexStreamArray = (VertexStream[])FindObjectsOfType<VertexStream>();
            List<GameObject> filteredStreamList = new List<GameObject>();
            for (int i = 0; i < vertexStreamArray.Length; i++)
            {
                List<Layer> layers = vertexStreamArray[i].layerStack.Layers;
                for (int j = 0; j < layers.Count; j++)
                {
                    if (layers[j].layerName == selectByLayerNameString)
                    {
                        filteredStreamList.Add(vertexStreamArray[i].gameObject);
                        vertexStreamArray[i].layerStack.targetLayerIndex = j;
                        break;
                    }
                }
            }
            GameObject[] filteredStreamArray = filteredStreamList.ToArray();
            return filteredStreamArray;
        }

        // LOAD EDITOR SETTINGS
        void OnFocus()
        {
            if (EditorPrefs.HasKey("Radius"))
                brushSettings.radius = EditorPrefs.GetFloat("Radius");
            if (EditorPrefs.HasKey("Strength"))
                brushSettings.strength = EditorPrefs.GetFloat("Strength");
            if (EditorPrefs.HasKey("Falloff"))
                brushSettings.falloff = EditorPrefs.GetFloat("Falloff");
            if (EditorPrefs.HasKey("PaintVertexColors"))
                brushSettings.isColorActive = EditorPrefs.GetBool("PaintVertexColors");
            if (EditorPrefs.HasKey("PaintMode"))
                brushSettings.paintMode = (BrushSettings.PaintMode)EditorPrefs.GetInt("PaintMode");

            if (EditorPrefs.HasKey("Swatch_Count"))
            {
                colorSwatches = new List<Color>();
                colorSwatchPreviews = new List<Texture2D>();
                int swatchCount = EditorPrefs.GetInt("Swatch_Count");
                for (int i = 0; i < swatchCount; i++)
                {
                    colorSwatches.Add(new Color(EditorPrefs.GetFloat("Swatch_" + i + "_R", brushSettings.color.r), EditorPrefs.GetFloat("Swatch_" + i + "_G"), EditorPrefs.GetFloat("Swatch_" + i + "_B"), EditorPrefs.GetFloat("Swatch_" + i + "_A")));
                    colorSwatchPreviews.Add(CreateColorSwatchPreview(colorSwatches[i]));
                }
            }
            // Remove and re-add the sceneGUI delegate
            SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
            SceneView.onSceneGUIDelegate += this.OnSceneGUI;
        }

        void LoadEditorPrefs()
        {
            if (EditorPrefs.HasKey("Radius"))
                brushSettings.radius = EditorPrefs.GetFloat("Radius");
            if (EditorPrefs.HasKey("Strength"))
                brushSettings.strength = EditorPrefs.GetFloat("Strength");
            if (EditorPrefs.HasKey("Falloff"))
                brushSettings.falloff = EditorPrefs.GetFloat("Falloff");
            if (EditorPrefs.HasKey("PaintVertexColors"))
                brushSettings.isColorActive = EditorPrefs.GetBool("PaintVertexColors");
            if (EditorPrefs.HasKey("PaintMode"))
                brushSettings.paintMode = (BrushSettings.PaintMode)EditorPrefs.GetInt("PaintMode");
            if (EditorPrefs.HasKey("BrushColor_R"))
                brushSettings.color = new Color(EditorPrefs.GetFloat("BrushColor_R"), EditorPrefs.GetFloat("BrushColor_G"), EditorPrefs.GetFloat("BrushColor_B"), EditorPrefs.GetFloat("BrushColor_A"));

            if (EditorPrefs.HasKey("Swatch_Count"))
            {
                colorSwatches = new List<Color>();
                colorSwatchPreviews = new List<Texture2D>();
                int swatchCount = EditorPrefs.GetInt("Swatch_Count");
                for (int i = 0; i < swatchCount; i++)
                {
                    colorSwatches.Add(new Color(EditorPrefs.GetFloat("Swatch_" + i + "_R", brushSettings.color.r), EditorPrefs.GetFloat("Swatch_" + i + "_G"), EditorPrefs.GetFloat("Swatch_" + i + "_B"), EditorPrefs.GetFloat("Swatch_" + i + "_A")));
                    colorSwatchPreviews.Add(CreateColorSwatchPreview(colorSwatches[i]));
                }
            }
        }

        // SAVE EDITOR SETTINGS
        void OnLostFocus()
        {
            EditorPrefs.SetBool("PaintVertexColors", brushSettings.isColorActive);
            EditorPrefs.SetFloat("Radius", brushSettings.radius);
            EditorPrefs.SetFloat("Strength", brushSettings.strength);
            EditorPrefs.SetFloat("Falloff", brushSettings.falloff);
            EditorPrefs.SetInt("PaintMode", (int)brushSettings.paintMode);
            EditorPrefs.SetFloat("BrushColor_R", brushSettings.color.r);
            EditorPrefs.SetFloat("BrushColor_G", brushSettings.color.g);
            EditorPrefs.SetFloat("BrushColor_B", brushSettings.color.b);
            EditorPrefs.SetFloat("BrushColor_A", brushSettings.color.a);
        }

        void OnDestroy()
        {
            Tools.hidden = false;
            SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
            Undo.undoRedoPerformed -= OnUndo;
        }

        void OnUndo()
        {
            Debug.Log("Undo");
        }
    }
}