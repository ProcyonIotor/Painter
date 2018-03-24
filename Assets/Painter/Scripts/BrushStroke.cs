using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace Painter
{
    public class BrushSettings
    {
        public float radius = 1;
        public float falloff = 1;
        public float strength = 0.1f;
        public bool ignoreBackfacing = false;
        public Color color = Color.white;

        public enum PaintMode
        {
            Paint = 0,
            Erase = 1,
            Blend = 2
        }

        public PaintMode paintMode;
        public bool isColorActive = true;
        public bool[] isRGBActive = new bool[] { true, true, true, false };
        public bool isFlowActive = false;
    }

    public class BrushStroke
    {
        /// <summary>
        /// Keep track of each affected mesh and the changes made to that mesh by the brush stroke.
        /// If the stroke is finished apply the stroke to the actual MeshStream objects.
        /// </summary>
        public Dictionary<VertexStream, Mesh> previewMeshStreamDict = new Dictionary<VertexStream, Mesh>();

        // Add a new mesh stream to the list of mesh streams currently being affected by the brush stroke.
        public void AddMeshStream(VertexStream meshStream)
        {
            if (!previewMeshStreamDict.ContainsKey(meshStream))
            {
                Mesh previewMeshStream = new Mesh();
                previewMeshStream = meshStream.vertexStream;
                previewMeshStreamDict.Add(meshStream, previewMeshStream);
            }
        }

        // Get an array the edited meshes for undoing.
        public Mesh[] vertexStreamMeshes
        {
            get
            {
                Mesh[] vertexStreamArray = new Mesh[previewMeshStreamDict.Count];
                previewMeshStreamDict.Values.CopyTo(vertexStreamArray, 0);
                return vertexStreamArray;
            }
        }

        public VertexStream[] vertexStreamComponents
        {
            get
            {
                VertexStream[] vertexStreamArray = new VertexStream[previewMeshStreamDict.Count];
                previewMeshStreamDict.Keys.CopyTo(vertexStreamArray, 0);
                return vertexStreamArray;
            }
        }

        public void UpdateStroke(Vector3 position, BrushSettings settings)
        {
            VertexStream[] affectedMeshStreams = GetMeshStreamsInRadius(position, settings.radius);
            // UPDATE COPY OF STREAM
            for (int i = 0; i < affectedMeshStreams.Length; i++)
            {
                if (affectedMeshStreams[i].layerStack.layers[affectedMeshStreams[i].layerStack.activeLayerIndex].isActive)
                {
                    Mesh targetStream = affectedMeshStreams[i].vertexStream;
                    targetStream.MarkDynamic();

                    // COLOR
                    Color[] targetColors = new Color[targetStream.vertexCount];
                    Layer paintLayer = affectedMeshStreams[i].layerStack.layers[affectedMeshStreams[i].layerStack.activeLayerIndex];
                    targetColors = paintLayer.Colors;

                    Color sumBlendColor = new Color();
                    Color blendColor = new Color();

                    // CALCULATE BLEND TARGET VALUES
                    if (settings.paintMode == BrushSettings.PaintMode.Blend)
                    {
                        List<Vector3> affectedBlendVertices = new List<Vector3>();
                        for (int k = 0; k < affectedMeshStreams.Length; k++)
                        {
                            for (int j = 0; j < affectedMeshStreams[k].vertexStream.vertexCount; j++)
                            {

                                Vector3 vertexWP = affectedMeshStreams[k].transform.TransformPoint(affectedMeshStreams[k].vertexStream.vertices[j]);
                                float distance = Vector3.Distance(position, vertexWP);
                                if (distance < settings.radius)
                                {
                                    affectedBlendVertices.Add(affectedMeshStreams[k].vertexStream.vertices[j]);
                                    if (settings.isColorActive)
                                        sumBlendColor += affectedMeshStreams[k].vertexStream.colors[j];
                                }
                            }
                        }
                        blendColor = sumBlendColor / affectedBlendVertices.Count;
                    }

                    for (int j = 0; j < targetStream.vertexCount; j++)
                    {
                        Vector3 vertexWP = affectedMeshStreams[i].transform.TransformPoint(targetStream.vertices[j]);
                        float distance = Vector3.Distance(position, vertexWP);

                        if (!settings.ignoreBackfacing && distance < settings.radius || settings.ignoreBackfacing && distance < settings.radius)
                        {
                            float influence = GetBrushInfluence(distance, settings.strength, settings.radius, settings.falloff);
                            // COLOR
                            if (settings.isColorActive)
                            {
                                switch (settings.paintMode)
                                {
                                    case BrushSettings.PaintMode.Paint:
                                        float r = settings.isRGBActive[0] ? settings.color.r : targetStream.colors[j].r;
                                        float g = settings.isRGBActive[1] ? settings.color.g : targetStream.colors[j].g;
                                        float b = settings.isRGBActive[2] ? settings.color.b : targetStream.colors[j].b;
                                        float a = settings.isRGBActive[3] ? settings.color.a : targetStream.colors[j].a;
                                        Color targetColor = new Color(r, g, b, a);
                                        if (paintLayer.Transparency[j] == 0)
                                        {
                                            targetColors[j] = targetColor;
                                            //paintLayer.transparency[j] = 1;
                                        }
                                        else
                                            targetColors[j] = Color.Lerp(paintLayer.Colors[j], targetColor, influence);
                                        if (paintLayer.Transparency[j] < 1.0f)
                                            paintLayer.Transparency[j] = Mathf.Lerp(paintLayer.Transparency[j], 1.0f, influence);
                                        break;
                                    case BrushSettings.PaintMode.Erase:
                                        if (paintLayer.Transparency[j] > 0.0f)
                                            paintLayer.Transparency[j] = Mathf.Lerp(paintLayer.Transparency[j], 0.0f, influence);
                                        break;
                                    case BrushSettings.PaintMode.Blend:
                                        targetColors[j] = Color.Lerp(paintLayer.Colors[j], blendColor, influence);
                                        break;
                                }
                            }
                        }
                    }
                    targetStream.colors = targetColors;

                    affectedMeshStreams[i].vertexStream = targetStream;
                    // COLOR
                    affectedMeshStreams[i].layerStack.layers[affectedMeshStreams[i].layerStack.activeLayerIndex].Colors = targetStream.colors;
                    affectedMeshStreams[i].RecalculateOutputColors();
                }               
            }
        }

        VertexStream[] GetMeshStreamsInRadius(Vector3 position, float radius)
        {
            Collider[] hitCollider = Physics.OverlapSphere(position, radius);
            List<VertexStream> hitMeshStream = new List<VertexStream>();
            for (int i = 0; i < hitCollider.Length; i++)
                if (hitCollider[i].GetComponent<VertexStream>() != null && previewMeshStreamDict.ContainsKey(hitCollider[i].GetComponent<VertexStream>()))
                    hitMeshStream.Add(hitCollider[i].GetComponent<VertexStream>());
            return hitMeshStream.ToArray();
        }

        float GetBrushInfluence(float distance, float strength, float radius, float falloff)
        {
            float offset = (distance - radius * (1 - falloff));
            float falloffDistance = radius - radius * (1 - falloff);
            float strengthPow = Mathf.Pow(strength, 3); // limit the strength near a value of 0.5, but use full strength at 1.0.
            float influence = (1 - (offset / falloffDistance)) * strengthPow;
            return influence;
        }
    }
}
