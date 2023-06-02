using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LineInfo
{
    [Range(0, 128)]
    public float fadeRadius = 0;
    [Range(0, 128)]
    public float roundRadius = 0;
    [Range(0, 128)]
    public float blankStart = 0;
    [Range(0, 128)]
    public float blankLen = 0;

    public List<PointInfo> points = new();
}