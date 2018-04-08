using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

namespace Painter
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(VertexStream))]
    public class VertexStreamEditor : Editor
    {

        VertexStream vertexStream;
        [SerializeField]
        private ReorderableList paintLayerList;
        List<Layer> paintLayers;

        public void OnEnable()
        {
            vertexStream = (VertexStream)target;
            if (paintLayerList == null)
                paintLayers = vertexStream.layerStack.Layers;
            if (paintLayerList == null)
            {
                paintLayerList = new ReorderableList(paintLayers, typeof(Layer), true, true, true, true);
                paintLayerList.drawHeaderCallback += OnDrawHeader;
                paintLayerList.drawElementCallback += OnDrawElement;
                paintLayerList.drawElementBackgroundCallback += OnDrawElementBackground;
                paintLayerList.elementHeightCallback += OnElementHeight;
                paintLayerList.onAddCallback += OnAddItem;
                paintLayerList.onRemoveCallback += OnRemoveItem;
                paintLayerList.onSelectCallback += OnSelectItem;
                paintLayerList.onCanRemoveCallback += OnCanRemoveItem;
                paintLayerList.onChangedCallback += OnChangeItem;
                if (paintLayerList.index == 0)
                    paintLayerList.index = vertexStream.layerStack.targetLayerIndex;
            }
        }

        private void OnDrawHeader(Rect rect)
        {
            GUI.Label(rect, "Paint Layers");
        }

        private void OnChangeItem(ReorderableList list)
        {
            EditorUtility.SetDirty(target);
            EditorSceneManager.MarkSceneDirty(vertexStream.gameObject.scene);
        }

        private void OnAddItem(ReorderableList list)
        {
            vertexStream.layerStack.Add(new Layer(), vertexStream.meshFilter.sharedMesh.vertexCount);
            list.index = vertexStream.layerStack.targetLayerIndex;
            OnChangeItem(list);
        }

        private bool OnCanRemoveItem(ReorderableList list)
        {
            return (list.count > 1);
        }

        private void OnRemoveItem(ReorderableList list)
        {
            if (list.count > 1)
            {
                vertexStream.layerStack.RemoveAt(vertexStream.layerStack.targetLayerIndex);
                list.index = vertexStream.layerStack.targetLayerIndex;
                vertexStream.RecalculateOutputColors();
                EditorUtility.SetDirty(target);
                EditorSceneManager.MarkSceneDirty(vertexStream.gameObject.scene);
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            vertexStream.autoReprojection = EditorGUILayout.Toggle(new GUIContent("Automatic Reprojection", "Automatically attempt to update the vertex colors after external modifications to the source mesh based on the nearest vertices in the original source mesh."), vertexStream.autoReprojection);
            vertexStream.sourceColorMode = (VertexStream.SourceColorMode)EditorGUILayout.EnumPopup("Source Color", vertexStream.sourceColorMode);
            if (vertexStream.sourceColorMode == VertexStream.SourceColorMode.Override || vertexStream.meshFilter.sharedMesh.vertexCount != vertexStream.meshFilter.sharedMesh.colors.Length)
                vertexStream.sourceOverrideColor = EditorGUILayout.ColorField("Override Color", vertexStream.sourceOverrideColor);
            if (vertexStream.sourceColorMode == VertexStream.SourceColorMode.Import && vertexStream.meshFilter.sharedMesh.vertexCount != vertexStream.meshFilter.sharedMesh.colors.Length)
                EditorGUILayout.HelpBox("The mesh does not have vertex colors for each vertex. The override color is being used instead.", MessageType.Warning);
            if (EditorGUI.EndChangeCheck())
                vertexStream.RecalculateOutputColors();
            paintLayerList.DoLayoutList();
            if (GUILayout.Button("Open Vertex Painter"))
                PainterWindow.ShowWindow();
                //EditorWindow.GetWindow(typeof(PainterWindow));
            if (GUILayout.Button("Bake Vertex Stream"))
                BakeVertexStream();
            //if (GUILayout.Button("Transfer Cache Attributes"))
            //    vertexStream.TransferCacheAttributes();
        }

        void BakeVertexStream()
        {
            if (EditorUtility.DisplayDialog("Apply to Source Mesh", "This action will apply all changes made to the mesh to all instances of the mesh and remove the Vertex Stream component. Are you sure you wish to continue?", "Continue", "Cancel"))
            {
                vertexStream.BaseMesh = vertexStream.Stream;
                vertexStream.meshRenderer.additionalVertexStreams.Clear();
                DestroyImmediate(vertexStream);
            }
        }

        private float OnElementHeight(int index)
        {
            float height = 20;
            if (vertexStream.layerStack.targetLayerIndex == index)
                height += 40;
            return height;
        }

        private void OnSelectItem(ReorderableList list)
        {
            vertexStream.layerStack.targetLayerIndex = list.index;
        }

        private void OnDrawElementBackground(Rect rect, int index, bool active, bool focused)
        {
            if (vertexStream.layerStack.targetLayerIndex == index)
            {
                Texture2D tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, new Color(0.33f, 0.66f, 1f, 0.66f));
                tex.Apply();
                GUI.DrawTexture(rect, tex as Texture);
            }
        }

        private void OnDrawElement(Rect rect, int index, bool active, bool focused)
        {
            EditorGUI.BeginChangeCheck();
            Layer item = paintLayers[index];
            item.layerName = EditorGUI.TextField(new Rect(rect.x + 18, rect.y, item.layerName.Length * 8 + 10, 18), item.layerName, EditorStyles.label);
            item.isActive = EditorGUI.Toggle(new Rect(rect.x, rect.y, 18, 18), item.isActive);
            if (vertexStream.layerStack.targetLayerIndex == index)
            {
                Layer paintItem = item;
                EditorGUI.LabelField(new Rect(rect.x + 18, rect.y + 20, 80, 18), "Blend Mode:");
                item.blendMode = (Layer.BlendMode)EditorGUI.EnumPopup(new Rect(rect.x + 95, rect.y + 20, rect.width / 2f - 80, 18), item.blendMode);
                EditorGUI.LabelField(new Rect(rect.width / 2f + 60, rect.y + 20, 80, 18), "Opacity:");
                paintItem.opacity = EditorGUI.Slider(new Rect(rect.width / 2f + 115, rect.y + 20, rect.width / 2f - 80, 18), paintItem.opacity, 0f, 1f);
                EditorGUI.LabelField(new Rect(rect.x + 18, rect.y + 40, 80, 18), "Color Mask:");
                string[] rgbaLabel = { "R", "G", "B", "A" };
                for (int i = 0; i < 4; i++)
                {
                    EditorGUI.LabelField(new Rect(rect.x + 96 + i * 34, rect.y + 40, 80, 18), rgbaLabel[i]);
                    paintItem.isColorActive[i] = EditorGUI.Toggle(new Rect(rect.x + i * 34 + 110, rect.y + 40, 20, 20), paintItem.isColorActive[i]);
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                vertexStream.RecalculateOutputColors();
                EditorUtility.SetDirty(target);
                EditorSceneManager.MarkSceneDirty(vertexStream.gameObject.scene);
            }
        }
    }
}
