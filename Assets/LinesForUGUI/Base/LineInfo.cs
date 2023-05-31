using System;
using System.Collections.Generic;

[Serializable]
public class LineInfo
{
    public float fadeRadius = 0;

    public float radius = 0;

    public float blankStart = 0;

    public float blankLen = 0;

    public List<PointInfo> points = new();
}