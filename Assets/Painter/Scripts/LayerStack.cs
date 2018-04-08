using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LayerStack {
    [SerializeField]
    public int targetLayerIndex = 1;
    [SerializeField]
    List<Layer> _layers = new List<Layer>();
    [SerializeField]
    Color[] outputColors = new Color[] { Color.white };

    public List<Layer> Layers
    {
        get
        {
            return _layers;
        }
        set
        {
            _layers = value;
        }
    }

    public int Count
    {
        get
        {
            return _layers.Count;
        }           
    }

    public Layer Add(Layer layer, int vertexCount)
    {
        Layers.Add(layer);
        Layer newLayer = Layers[Layers.Count - 1];
        newLayer.vertexCount = vertexCount;
        int index = Layers.Count - 1;
        newLayer.layerName = "New Paint Layer " + index;
        targetLayerIndex = Count - 1; 
        return newLayer;
    }

    public bool Remove(Layer layer)
    {
        int index = Layers.IndexOf(layer);
        bool removed = Layers.Remove(layer);
        targetLayerIndex = index - 1;
        return removed;
    }

    public void RemoveAt(int index)
    {
        Layers.RemoveAt(index);
        targetLayerIndex = index - 1;
    }

    public Color[] RecalculateOutputColors(Color[] sourceColors)
    {
        outputColors = sourceColors;
        for (int i = 0; i < Layers.Count; i++)
            if (Layers[i].isActive)
                outputColors = Layers[i].GetOutputColors(outputColors);
        return outputColors;
    }
}
