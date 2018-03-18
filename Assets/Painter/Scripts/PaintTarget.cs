using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VertexPaintData
{
    public float opacity = 0f;
    public Color color = Color.white;
}

public class PaintTarget : MonoBehaviour {
	public Dictionary<PaintLayer, VertexPaintData[]> layerPaintData = new Dictionary<PaintLayer, VertexPaintData[]>();
}