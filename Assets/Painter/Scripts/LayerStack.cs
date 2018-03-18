using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LayerStack {
    [SerializeField]
    public int activeLayerIndex = 1;
    [SerializeField]
    List<Layer> _layers = new List<Layer>();
    [SerializeField]
    Color[] outputColors = new Color[] { Color.white };

    public List<Layer> layers
    {
        get
        {
            return _layers;
        }
    }

    public int layerCount
    {
        get
        {
            return _layers.Count;
        }           
    }

    public PaintLayer Add(PaintLayer layer, int vertexCount)
    {
        layers.Add(layer);
        PaintLayer newLayer = layers[layers.Count - 1] as PaintLayer;
        newLayer.vertexCount = vertexCount;
        newLayer.SetDefaultLayerProperties(layers.Count - 1);
        activeLayerIndex = layerCount - 1; 
        return newLayer;
    }

    public bool Remove(Layer layer)
    {
        int index = layers.IndexOf(layer);
        bool removed = layers.Remove(layer);
        activeLayerIndex = index - 1;
        return removed;
    }

    public void RemoveAt(int index)
    {
        layers.RemoveAt(index);
        activeLayerIndex = index - 1;
    }

    public Color[] RecalculateOutputColors(Color[] sourceColors)
    {
        outputColors = sourceColors;
        for (int i = 0; i < layers.Count; i++)
            if (layers[i] is PaintLayer && layers[i].isActive)
                outputColors = layers[i].GetOutputColors(outputColors);
        return outputColors;
    }
}
