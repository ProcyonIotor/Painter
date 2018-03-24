using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PaintLayer : Layer {
    [SerializeField] public List<PaintLayer> paintLayerGroup;
    [SerializeField] Color[] _colors;
    [SerializeField] float[] _transparency; // The per-vertex influence on the vertex colors, where 0 is no influence and 1 is full influence. This is 0 by default.
    [SerializeField] public float opacity = 1f;
    [SerializeField] public bool isLocked = false;
    [SerializeField] public bool[] isColorActive = { true, true, true, true };

    public Color[] colors
    {
        get
        {
            if (_colors == null || _colors.Length == 0)
            {
                _colors = new Color[vertexCount];
            }
            Debug.Log("Returning " + _colors.Length);
            return _colors;
        }
        set
        {
            _colors = value;
        }
    }

    public float[] transparency
    {
        get
        {
            if (_transparency == null || _transparency.Length == 0)
                _transparency = new float[vertexCount];
            return _transparency;
        }
        set
        {
            _transparency = value;
        }
    }

    public override void SetColors(Color[] colors)
    {
        _colors = colors;
    }

    public override void SetTransparency(float[] transparency)
    {
        _transparency = transparency;
    }

    public override Color[] GetColors()
    {
        return _colors;
    }

    public override Color[] GetOutputColors(Color[] inputColors)
    {
        Color[] outputColors = new Color[inputColors.Length];
        for (int i = 0; i < inputColors.Length; i++)
        {
            if (opacity * transparency[i] > 0f)
                outputColors[i] = GetBlendTargetColor(inputColors[i], colors[i], opacity * transparency[i], isColorActive);
            else
                outputColors[i] = inputColors[i];
        }
        return outputColors;
    }

    public override void SetDefaultLayerProperties(int index)
    {
        base.SetDefaultLayerProperties(index);
        layerName = "New Paint Layer " + index;
    }
}