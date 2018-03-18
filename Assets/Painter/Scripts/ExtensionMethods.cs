using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ExtensionMethods{

    public static Color Blend(this Color a, Color b, bool[] blendColor)
    {
        for (int i = 0; i < 4; i++)
            if (blendColor[i])
                a[i] = b[i];
        return a;
    }

    public static Color Add(this Color a, Color b, bool[] blendColor)
    {
        for (int i = 0; i < 4; i++)
            if(blendColor[i])
                a[i] += b[i];
        return a;
    }

    public static Color Subtract(this Color a, Color b, bool[] blendColor)
    {
        for (int i = 0; i < 4; i++)
            if (blendColor[i])
                a[i] -= b[i];
        return a;
    }

    public static Color Multiply(this Color a, Color b, bool[] blendColor)
    {
        for (int i = 0; i < 4; i++)
            if (blendColor[i])
                a[i] *= b[i];
        return a;
    }

    public static Color Divide(this Color a, Color b, bool[] blendColor)
    {
        for (int i = 0; i < 4; i++)
            if (blendColor[i])
                a[i] /= b[i];
        return a;
    }

    public static Color Screen(this Color a, Color b, bool[] blendColor)
    {
        for (int i = 0; i < 4; i++)
            if (blendColor[i])
                a[i] = 1-(1-a[i])*(1-b[i]);
        return a;
    }

    public static Color Overlay(this Color a, Color b, bool[] blendColor)
    {
        for (int i = 0; i < 4; i++)
            if (blendColor[i])
                if (a[i] < 0.5f)
                    a[i] = 2 * a[i] * b[i];
                else
                a[i] = 1 - 2 * (1 - a[i]) * (1 - b[i]);
        return a;
    }

    public static Color HardLight(this Color a, Color b, bool[] blendColor)
    {
        for (int i = 0; i < 4; i++)
            if (blendColor[i])
                if (b[i] < 0.5f)
                    a[i] = 2 * a[i] * b[i];
                else
                    a[i] = 1 - 2 * (1 - a[i]) * (1 - b[i]);
        return a;
    }

    public static Color SoftLight(this Color a, Color b, bool[] blendColor)
    {
        for (int i = 0; i < 4; i++)
            if (blendColor[i])
                if (b[i] < 0.5f)
                    a[i] = 2 * a[i] * b[i] + Mathf.Pow(a[i], 2) * (1 - 2 * b[i]);
                else
                    a[i] = Mathf.Sqrt(a[i]) * (2 * b[i] - 1) + (2 * a[i]) * (1 - b[i]);
        return a;
    }

    public static Color Dodge(this Color a, Color b, bool[] blendColor)
    {
        for (int i = 0; i < 4; i++)
            if (blendColor[i])
                a[i] = a[i]/(1-b[i]);
        return a;
    }

    public static Color Burn(this Color a, Color b, bool[] blendColor)
    {
        for (int i = 0; i < 4; i++)
            if (blendColor[i])
                a[i] = 1 - (1 - a[i])/b[i];
        return a;
    }

    public static Color Difference(this Color a, Color b, bool[] blendColor)
    {
        for (int i = 0; i < 4; i++)
            if (blendColor[i])
                a[i] = Mathf.Abs(a[i] - b[i]);
        return a;
    }

    public static Color DarkerColor(this Color a, Color b, bool[] blendColor)
    {
        for (int i = 0; i < 4; i++)
            if (blendColor[i])
                a[i] = a[i] < b[i] ? a[i] : b[i];
        return a;
    }

    public static Color LighterColor(this Color a, Color b, bool[] blendColor)
    {
        for (int i = 0; i < 4; i++)
            if (blendColor[i])
                a[i] = a[i] > b[i] ? a[i] : b[i];
        return a;
    }
}
