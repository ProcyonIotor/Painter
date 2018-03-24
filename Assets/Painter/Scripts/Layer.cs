using Painter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Layer : ScriptableObject {
    public int vertexCount;
    public enum BlendMode
    {
        Normal,
        Multiply,
        Screen,
        Overlay,
        HardLight,
        SoftLight,
        Dodge,
        Burn,
        Divide,
        Addition,
        Subtract,
        Difference,
        DarkerColor,
        LighterColor,
    }

    [SerializeField] Color[] _colors;
    [SerializeField] float[] _transparency; // The per-vertex influence on the vertex colors, where 0 is no influence and 1 is full influence. This is 0 by default.
    public bool isActive = true;
    [SerializeField] public float opacity = 1f;
    [SerializeField] public bool isLocked = false;
    [SerializeField] public bool[] isColorActive = { true, true, true, true };
    public string layerName = "New Layer";
    public BlendMode blendMode = BlendMode.Normal;

    public Color[] Colors
    {
        get
        {
            if (_colors == null || _colors.Length == 0)
                _colors = new Color[vertexCount];
            return _colors;
        }
        set
        {
            _colors = value;
        }
    }

    public float[] Transparency
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

    public Color[] GetOutputColors(Color[] inputColors)
    {
        Color[] outputColors = new Color[inputColors.Length];
        for (int i = 0; i < inputColors.Length; i++)
        {
            if (opacity * Transparency[i] > 0f)
                outputColors[i] = GetBlendTargetColor(inputColors[i], Colors[i], opacity * Transparency[i], isColorActive);
            else
                outputColors[i] = inputColors[i];
        }
        return outputColors;
    }

    public Color GetBlendTargetColor(Color sourceColor, Color blendColor, float opacity, bool[] isColorActive)
    {
        Color targetColor = sourceColor;
        switch (blendMode)
        {
            case BlendMode.Normal:
                targetColor = targetColor.Blend(blendColor, isColorActive);
                break;
            case BlendMode.Multiply:
                targetColor = targetColor.Multiply(blendColor, isColorActive);
                break;
            case BlendMode.Screen:
                targetColor = targetColor.Screen(blendColor, isColorActive);
                break;
            case BlendMode.Overlay:
                targetColor = targetColor.Overlay(blendColor, isColorActive);
                break;
            case BlendMode.HardLight:
                targetColor = targetColor.HardLight(blendColor, isColorActive);
                break;
            case BlendMode.SoftLight:
                targetColor = targetColor.SoftLight(blendColor, isColorActive);
                break;
            case BlendMode.Dodge:
                targetColor = targetColor.Dodge(blendColor, isColorActive);
                break;
            case BlendMode.Burn:
                targetColor = targetColor.Burn(blendColor, isColorActive);
                break;
            case BlendMode.Divide:
                targetColor = targetColor.Divide(blendColor, isColorActive);
                break;
            case BlendMode.Addition:
                targetColor = targetColor.Add(blendColor, isColorActive);
                break;
            case BlendMode.Subtract:
                targetColor = targetColor.Subtract(blendColor, isColorActive);
                break;
            case BlendMode.Difference:
                targetColor = targetColor.Difference(blendColor, isColorActive);
                break;
            case BlendMode.DarkerColor:
                targetColor = targetColor.DarkerColor(blendColor, isColorActive);
                break;
            case BlendMode.LighterColor:
                targetColor = targetColor.LighterColor(blendColor, isColorActive);
                break;
        }
        targetColor = Color.Lerp(sourceColor, targetColor, opacity);
        for(int i = 0;i<4;i++)
            targetColor[i] = Mathf.Clamp01(targetColor[i]);
        return targetColor;
    }
}
