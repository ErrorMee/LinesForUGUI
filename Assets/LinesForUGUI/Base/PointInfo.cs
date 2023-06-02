using System;
using UnityEngine;

[Serializable]
public class PointInfo
{
    public Vector3 pos = Vector3.zero;

    public Color color = Color.white;
    [Range(0, 128)]
    public float radius = 0;
}