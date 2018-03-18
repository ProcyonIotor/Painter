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

        string[] toolbarNames = new string[] { "Scene", "Paint", "Bake" };
        string[] paintModeNames = new string[] { "Paint", "Erase", "Blend" };

        VertexPainterUtility vertexPainterUtility = VertexPainterUtility.Paint;

        private MeshFilter[] paintSelection;

        BakeAO bakeAO = new BakeAO();

        // VERTEX PAINTER TOOL SETTINGS
        private const float MAX_RADIUS = 5f;
        private const float MIN_RADIUS = 0f;
        private const float BRUSH_RESCALE_STEP = 0.05f;

        [MenuItem("Window/Painter")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(PainterWindow), false, "Painter", true);
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
                    DrawSceneWindow();
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
        void DrawSceneWindow()
        {
            EditorGUILayout.LabelField("Paint Targets", EditorStyles.boldLabel);
            if (reorderableVertexStreamList != null)
                reorderableVertexStreamList.DoLayoutList();
            EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            if (selectedVertexStream == null)
                GUI.enabled = false;
            if (GUILayout.Button("Select Object"))
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
            GUI.enabled = true;
            GUILayout.EndHorizontal();
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
            if (GUILayout.Button("Remove Selection"))
            {
                GameObject[] objSelection = Selection.gameObjects;
                for (int i = 0; i < objSelection.Length; i++)
                    if (objSelection[i].GetComponent<VertexStream>() != null)
                        DestroyImmediate(objSelection[i].GetComponent<VertexStream>());
                UpdateVertexStreamList();
                UpdatePaintTargetList();
                OnSelectionChanged();
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
                for(int j = 0; j < childVertexStreams.Length; j++)
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

        void CreatePaintSelection()
        {
            GameObject[] objSelection = Selection.gameObjects;
            List<MeshFilter> mfSelection = new List<MeshFilter>();
            for (int i = 0; i < objSelection.Length; i++)
                if (objSelection[i].GetComponent<MeshFilter>())
                {
                    mfSelection.Add(objSelection[i].GetComponent<MeshFilter>());
                    if (objSelection[i].GetComponent<VertexStream>() == null)
                        objSelection[i].AddComponent<VertexStream>();
                }
            paintSelection = mfSelection.ToArray();
        }

        void ClearPaintSelection()
        {
            paintSelection = null;
        }

        void OnInspectorUpdate()
        {
            Repaint();
        }

        void UpdateBrushPreview() {

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
                Undo.RegisterCompleteObjectUndo(brushStroke.vertexStreamMeshes, "Vertex Painting");
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
                Undo.RecordObjects(brushStroke.vertexStreamMeshes, "Vertex Painting");
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

            // Remove and re-add the sceneGUI delegate
            SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
            SceneView.onSceneGUIDelegate += this.OnSceneGUI;
        }

        // SAVE EDITOR SETTINGS
        void OnLostFocus()
        {
            EditorPrefs.SetBool("PaintVertexColors", brushSettings.isColorActive);
            EditorPrefs.SetFloat("Radius", brushSettings.radius);
            EditorPrefs.SetFloat("Strength", brushSettings.strength);
            EditorPrefs.SetFloat("Falloff", brushSettings.falloff);
            EditorPrefs.SetInt("PaintMode", (int)brushSettings.paintMode);
        }

        void OnDestroy()
        {
            SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
            Undo.undoRedoPerformed -= OnUndo;
        }

        void OnUndo()
        {
            Debug.Log("Undo");
        }
    }
}