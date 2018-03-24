using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VertexPaintData
{
    public float opacity = 0f;
    public Color color = Color.white;
}

public class PaintTarget : MonoBehaviour {
	public Dictionary<Layer, VertexPaintData[]> layerPaintData = new Dictionary<Layer, VertexPaintData[]>();
}