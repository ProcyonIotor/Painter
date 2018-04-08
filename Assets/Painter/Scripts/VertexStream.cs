using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Painter
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [ExecuteInEditMode]
    [CanEditMultipleObjects]
    public class VertexStream : MonoBehaviour
    {
        [SerializeField] public bool autoReprojection = true;
        [SerializeField] public MeshFilter meshFilter;
        [SerializeField] public MeshRenderer meshRenderer;
        [SerializeField] private Mesh _baseMesh;
        [SerializeField] private Mesh _vertexStream;
        [HideInInspector]
        [SerializeField] private int instanceID = 0;
        int cacheVertexCount = 0;
        int sourceVertexCount = 0;
        [HideInInspector] public bool isPaintEnabled = true;
        [SerializeField] public LayerStack layerStack;

        public enum SourceColorMode
        {
            Import,
            Override
        }

        [SerializeField]
        public SourceColorMode sourceColorMode = SourceColorMode.Import;
        [SerializeField]
        private Color[] _sourceColors;
        [SerializeField]
        public Color sourceOverrideColor = Color.white;

        public Color[] SourceColors
        {
            get
            {
                switch (sourceColorMode)// TODO: Optimize by only updating the source colors array if the source color mode changed
                {
                    case SourceColorMode.Import:
                        Color[] importSourceColors = new Color[meshFilter.sharedMesh.vertexCount];
                        if (meshFilter.sharedMesh.colors.Length == meshFilter.sharedMesh.vertexCount)
                            importSourceColors = meshFilter.sharedMesh.colors;
                        else                        
                            for (int i = 0; i < importSourceColors.Length; i++)
                            importSourceColors[i] = sourceOverrideColor;
                        return importSourceColors;
                    default:
                    case SourceColorMode.Override:
                        Color[] overrideSourceColors = new Color[meshFilter.sharedMesh.vertexCount];
                        for (int i = 0; i < overrideSourceColors.Length; i++)
                            overrideSourceColors[i] = sourceOverrideColor;
                        return overrideSourceColors;
                }
            }
        }

        public Layer TargetLayer
        {
            get
            {
                return layerStack.Layers[layerStack.targetLayerIndex];
            }
        } 

        public Mesh BaseMesh
        {
            get
            {
                return _baseMesh;
            }
            set
            {
                _baseMesh.vertices = value.vertices;
                _baseMesh.colors = value.colors;
            }
        }

        public Mesh Stream
        {
            get
            {
                return _vertexStream;
            }
            set
            {
                _vertexStream.vertices = value.vertices;
                _vertexStream.colors = value.colors;
                _vertexStream.name = value.name;
            }
        }

        public Color[] RecalculateOutputColors()
        {
            Stream.colors = layerStack.RecalculateOutputColors(SourceColors);
            return Stream.colors;
        }

        void Awake()
        {
            if (meshFilter == null)
                meshFilter = GetComponent<MeshFilter>();
            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();
            if (_baseMesh == null)
                _baseMesh = meshFilter.sharedMesh;

            // If a duplicate of the mesh is made give it a unique vertex stream based on the existing vertex stream or add a new one.
            if (instanceID != GetInstanceID())
            {
                Mesh duplicateStreamMesh = new Mesh();
                if (instanceID == 0)
                    duplicateStreamMesh = CopyMeshProperties(BaseMesh);
                else
                    duplicateStreamMesh = CopyMeshProperties(Stream);
                _vertexStream = new Mesh();
                Stream = duplicateStreamMesh;
                Stream.name = BaseMesh.name + " (Vertex Stream)";
                instanceID = GetInstanceID();
            }

            if (layerStack == null)
            {
                layerStack = new LayerStack();
                if (layerStack.Count == 0)
                {
                    int vertexCount = meshFilter.sharedMesh.vertexCount;
                    layerStack.Add(new Layer(), vertexCount);
                }
            }           

            
            // If the base mesh has no vertex colors, then add some.
            if (Stream.colors.Length != Stream.vertexCount)
            {
                Color[] baseColors = new Color[Stream.vertexCount];
                for (int i = 0; i < Stream.vertexCount; i++)
                    baseColors[i] = sourceOverrideColor;
                BaseMesh.colors = baseColors;
                Stream.colors = baseColors;
                BaseMesh.UploadMeshData(false);
                Stream.UploadMeshData(false);
                meshRenderer.additionalVertexStreams = Stream;
            }
            
        }

        // If the Mesh Stream component is deleted, remove the additional vertex stream data.
        void OnDestroy()
        {
            meshRenderer.additionalVertexStreams = null;
        }

        // If the Mesh Stream component is disabled, do not use the additional vertex stream data.
        void OnDisable()
        {
            meshRenderer.additionalVertexStreams = null;
        }

        public Mesh CopyMeshProperties(Mesh source)
        {
            Mesh duplicate = new Mesh();
            duplicate.vertices = source.vertices;
            duplicate.colors = source.colors;
            return duplicate;
        }

        /*
        public void TransferCacheAttributes()
        {
            Mesh streamCache = new Mesh();
            streamCache = CopyMeshProperties(_vertexStream);
            Mesh targetStream = new Mesh();
            targetStream = CopyMeshProperties(_baseMesh);

            Color[] targetStreamColors = new Color[targetStream.vertexCount];

            // TRANSFER COLOR, NORMAL
            for (int i = 0; i < targetStream.vertices.Length; i++)
            {
                bool isVertexAssigned = false;
                int nearestVertexID = -1;
                float nearestVertexDistance = -1;
                for (int j = 0; j < streamCache.vertexCount; j++)
                {
                    float vertexDistance = Vector3.Distance(targetStream.vertices[i], streamCache.vertices[j]);
                    if (!isVertexAssigned)
                    {
                        nearestVertexID = j;
                        nearestVertexDistance = vertexDistance;
                        isVertexAssigned = true;
                    }
                    else
                    {
                        if (vertexDistance < nearestVertexDistance)
                        {
                            nearestVertexID = j;
                            nearestVertexDistance = vertexDistance;
                        }
                    }
                }
                targetStreamColors[i] = streamCache.colors[nearestVertexID];
            }
            targetStream.colors = targetStreamColors;

            Stream.Clear();
            Stream = CopyMeshProperties(BaseMesh);
            Stream.name = BaseMesh.name + " (Vertex Stream)";
            Stream.colors = targetStream.colors;
            Stream.UploadMeshData(false);
            meshRenderer.additionalVertexStreams = Stream;
        }*/

        void ReprojectMesh()
        {
            Vector3[] sourceVertices = Stream.vertices;
            Vector3[] targetVertices = meshFilter.sharedMesh.vertices;
            int[] nearestSourceVertexIndex = new int[targetVertices.Length];
            for(int i = 0; i < targetVertices.Length; i++)
            {
                float minDistance = Mathf.Infinity;
                for(int j = 0; j < sourceVertices.Length; j++)
                {
                    float distance = Vector3.Distance(targetVertices[i], sourceVertices[j]);
                    if(distance < minDistance)
                    {
                        minDistance = distance;
                        nearestSourceVertexIndex[i] = j;
                    }
                }
            }

            List<Layer> layers = layerStack.Layers;
            for(int i = 0; i < layers.Count; i++)
            {
                Color[] sourceColors = layers[i].Colors;
                float[] sourceTransparency = layers[i].Transparency;
                layers[i].Colors = new Color[targetVertices.Length];
                layers[i].Transparency = new float[targetVertices.Length];
                for(int j = 0; j < targetVertices.Length; j++)
                {
                    layers[i].Colors[j] = sourceColors[nearestSourceVertexIndex[j]];
                    layers[i].Transparency[j] = sourceTransparency[nearestSourceVertexIndex[j]];
                }
            }

            Stream.vertices = meshFilter.sharedMesh.vertices;
            RecalculateOutputColors();
        }

        // Update to keep the vertex color upon reloading a scene
#if UNITY_EDITOR
        void Update()
        {
            cacheVertexCount = Stream.vertexCount;
            sourceVertexCount = meshFilter.sharedMesh.vertexCount;
            if (sourceVertexCount != cacheVertexCount)
                if(autoReprojection)
                    ReprojectMesh();
            if (meshRenderer != null)
                meshRenderer.additionalVertexStreams = Stream;
        }
#endif
    }
}