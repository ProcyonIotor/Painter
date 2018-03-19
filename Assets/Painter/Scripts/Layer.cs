using Painter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class Layer : ScriptableObject {
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

    public bool isActive = true;
    public string layerName = "New Layer";
    public BlendMode blendMode = BlendMode.Normal;

    public virtual Color GetOutputColor(Color inputColor)
    {
        return inputColor;
    }

    public virtual Color[] GetOutputColors(Color[] inputColors)
    {
        return inputColors;
    }

    public virtual void SetDefaultLayerProperties(int index)
    {

    }

    public virtual void SetColors(Color[] colors)
    {
        
    }

    public virtual void SetTransparency(float[] transparency)
    {

    }

    public virtual Color[] GetColors()
    {
        return null;
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
