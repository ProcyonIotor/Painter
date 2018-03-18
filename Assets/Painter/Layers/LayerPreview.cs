using Painter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class LayerPreview : MonoBehaviour {
    [SerializeField]
    private Color[] sourceColor;
    [SerializeField]
    public List<PaintLayer> layers;
    public string paintGroupId = "Paint Group";

    [HideInInspector]
    public int activePaintGroup;

    public VertexStream[] vertexStreams;

    public PaintLayer AddLayer()
    {
        PaintLayer layer = new PaintLayer();
        layer.paintLayerGroup = layers;
        for (int i = 0; i < layer.colors.Length; i++)
            layer.colors[i] = new Color(1, 1, 1, 0);
        layers.Add(layer);
        return layer;
    }

    void Update()
    {
        /*
        for (int i = 0; i < layers.Count; i++)
        {
            Color[] inputColor = i == 0 ? sourceColor : layers[i - 1].outputColor;
            for(int j=0; j < sourceColor.Length; j++)
            {
                layers[i].outputColor[j] = layers[i].GetBlendTargetColor(inputColor[j], layers[i].layerColors[j], layers[i].opacity);
            }
        }
        */
    }
}
