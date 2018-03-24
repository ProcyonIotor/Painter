using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Painter
{
    public class BakeAO
    {
        public bool[] assignToColorChannel = { true, true, true, false };
        public int samples = 256;
        [Range(0, 180)]
        public float spread = 162;
        public float maxDistance = 4;
        public float intensity = 1;
        public Color minColor = Color.black;
        public Color maxColor = Color.white;
        public float bias = 0.1f;
        List<Collider> colliders = new List<Collider>();
        public enum AOBlendMode
        {
            Replace,
            Multiply
        }
        public AOBlendMode aoBlendMode = AOBlendMode.Replace;
        RaycastHit hit = new RaycastHit();

        public void Bake(VertexStream[] vertexStreams)
        {
            colliders.Clear();
            for (int i = 0; i < vertexStreams.Length; i++)
            {
                if (vertexStreams[i].GetComponent<Collider>() == null)
                {
                    Collider temporaryMeshCollider = vertexStreams[i].gameObject.AddComponent<MeshCollider>();
                    colliders.Add(temporaryMeshCollider);
                }
            }

            float radialSpread = Mathf.Deg2Rad * spread / 2;
            for (int i = 0; i < vertexStreams.Length; i++)
            {
                Vector3[] vertices = vertexStreams[i].vertexStream.vertices;
                Vector3[] normals = vertexStreams[i].meshFilter.sharedMesh.normals;
                PaintLayer activeLayer = (PaintLayer) vertexStreams[i].layerStack.layers[vertexStreams[i].layerStack.activeLayerIndex];
                Color[] color = activeLayer.GetColors();
                Debug.Log(vertexStreams[i].layerStack.activeLayerIndex);
                Color[] aoColors = new Color[vertices.Length];
                Debug.Log(color.Length);
                Debug.Log(aoColors.Length);
                for (int j = 0; j < vertices.Length; j++)
                {
                    Vector3 worldSpaceNormal = vertexStreams[i].transform.TransformDirection(normals[j]);
                    Vector3 worldSpacePosition = vertexStreams[i].transform.TransformPoint(vertices[j]);
                    Quaternion lookAt = Quaternion.LookRotation(worldSpaceNormal);
                    float rayHitSum = 0;
                    for (int k = 0; k < samples; k++)
                    {
                        float z = Random.Range(Mathf.Cos(radialSpread), 1);
                        float t = Random.Range(0, Mathf.PI * 2);
                        Vector3 randomConeDirection = new Vector3(Mathf.Sqrt(1 - z * z) * Mathf.Cos(t), Mathf.Sqrt(1 - z * z) * Mathf.Sin(t), z);
                        Vector3 randomizedNormal = lookAt * randomConeDirection;
                        Vector3 offset = Vector3.Reflect(randomizedNormal, worldSpaceNormal) * -bias;
                        if (Physics.Linecast(worldSpacePosition + offset, worldSpacePosition + randomizedNormal * maxDistance + offset, out hit))
                            rayHitSum += Mathf.Clamp01(1 - hit.distance / maxDistance);
                        else if(Physics.Linecast(worldSpacePosition + randomizedNormal * maxDistance + offset, worldSpacePosition + offset, out hit))
                            rayHitSum += Mathf.Clamp01(1 - (maxDistance-hit.distance) / maxDistance);
                    }
                    float occlusionFactor = Mathf.Clamp01((rayHitSum * intensity / samples));
                    Color occlusionColor = Color.Lerp(maxColor, minColor, occlusionFactor);
                    aoColors[j] = occlusionColor;
                    
                    if (aoBlendMode == AOBlendMode.Replace)
                        aoColors[j] = new Color(assignToColorChannel[0] ? aoColors[j].r : color[j].r, assignToColorChannel[1] ? aoColors[j].g : color[j].g, assignToColorChannel[2] ? aoColors[j].b : color[j].b, assignToColorChannel[3] ? aoColors[j].a : color[j].a);
                    else if (aoBlendMode == AOBlendMode.Multiply)
                        aoColors[j] = new Color(assignToColorChannel[0] ? aoColors[j].r * color[j].r : color[j].r, assignToColorChannel[1] ? aoColors[j].g * color[j].g : color[j].g, assignToColorChannel[2] ? aoColors[j].b * color[j].b : color[j].b, assignToColorChannel[3] ? aoColors[j].a * color[j].a : color[j].a);
                }
                float[] transparencyOverride = new float[aoColors.Length];
                for(int k = 0; k < aoColors.Length; k++)
                {
                    transparencyOverride[k] = 1f;
                }
                vertexStreams[i].layerStack.layers[vertexStreams[i].layerStack.activeLayerIndex].SetColors(aoColors);
                vertexStreams[i].layerStack.layers[vertexStreams[i].layerStack.activeLayerIndex].SetTransparency(transparencyOverride);
                vertexStreams[i].RecalculateOutputColors();
            }

            for (int i = 0; i < colliders.Count; i++)
                Object.DestroyImmediate(colliders[i]);
            colliders.Clear();
        }
    }
}