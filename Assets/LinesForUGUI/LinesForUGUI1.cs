#if UNITY_EDITOR
using UnityEditor.UI;
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LinesForUGUI1 : Image
{
    [SerializeField] List<LineInfo> lines;

    int vertexCount = 0; LineInfo lineCrt;
    Vector3 lastCtrPos; float lineDis = 0;

    public void Draw(List<LineInfo> lines)
    {
        this.lines = lines;
    }

    protected override void OnPopulateMesh(VertexHelper toFill)
    {
        toFill.Clear();
        vertexCount = 0;
        for (int i = 0; i < lines.Count; i++)
        {
            Draw(toFill, lines[i]);
        }
    }

    private void Draw(VertexHelper toFill, LineInfo lineInfo)
    {
        if (lineInfo.points.Count < 1)
        {
            return;
        }
        lineCrt = lineInfo; lineDis = 0;
        AddStartVert(toFill);

        for (int i = 0; i < lineInfo.points.Count - 1; i++)
        {
            AddMidRect(toFill, lineInfo.points[i], lineInfo.points[i + 1].pos);
        }
        if (lineInfo.points.Count > 1)
        {
            PointInfo ctrPoint = lineInfo.points[^1];
            Vector3 dirPreToCrt = PointDir(lineInfo.points[^2].pos, ctrPoint.pos);
            AddMidRect(toFill, ctrPoint, ctrPoint.pos + dirPreToCrt);
        }
        AddEndRect(toFill);
    }

    private Vector3 PointDir(Vector3 fromPos, Vector3 toPos)
    {
        Vector3 pointDir = (toPos - fromPos).normalized;
        if (pointDir == Vector3.zero)
        {
            pointDir = Vector3.right;
        }
        return pointDir;
    }

    private void AddStartVert(VertexHelper toFill)
    {
        PointInfo ctrPoint = lineCrt.points[0];
        Vector3 pointDir = Vector3.left;
        if (lineCrt.points.Count > 1)
        {
            pointDir = PointDir(lineCrt.points[1].pos, ctrPoint.pos);
        }
        Vector3 wideDir = new(-pointDir.y, pointDir.x, 0);
        AddTwoVert(toFill, ctrPoint, pointDir, wideDir);
    }

    private void AddEndRect(VertexHelper toFill)
    {
        PointInfo ctrPoint = lineCrt.points[^1];
        Vector3 pointDir = Vector3.right;
        if (lineCrt.points.Count > 1)
        {
            pointDir = PointDir(lineCrt.points[^2].pos, ctrPoint.pos);
        }
        Vector3 wideDir = new(-pointDir.y, pointDir.x, 0);
        AddTwoVert(toFill, ctrPoint, pointDir, -wideDir);
        AddTwoTriangle(toFill);
    }

    private void AddTwoVert(VertexHelper toFill, PointInfo ctrPoint, Vector3 pointDir, Vector3 wideDir)
    {
        float radius = ctrPoint.radius + lineCrt.radius;
        Vector3 wideOffset = wideDir * radius;
        Vector3 posOffset = pointDir * radius;

        UIVertex vertexLeft = UIVertex.simpleVert;
        vertexLeft.position = ctrPoint.pos + posOffset - wideOffset;
        vertexLeft.color = ctrPoint.color * color;
        // ctrPos fadeRadius
        vertexLeft.uv0 = new Vector4(ctrPoint.pos.x, ctrPoint.pos.y, lineCrt.fadeRadius);
        // radius, blankStart, blankLen, lineDis

        vertexLeft.uv1 = new Vector4(radius, lineCrt.blankStart, lineCrt.blankLen, lineDis);
        // os
        vertexLeft.uv2 = new Vector4(vertexLeft.position.x, vertexLeft.position.y);
        toFill.AddVert(vertexLeft);

        UIVertex vertexRight = vertexLeft;
        vertexRight.position = ctrPoint.pos + posOffset + wideOffset;
        vertexRight.uv2 = new Vector4(vertexRight.position.x, vertexRight.position.y);
        toFill.AddVert(vertexRight);

        lastCtrPos = (vertexLeft.position + vertexRight.position) * 0.5f;
        vertexCount += 2;
    }

    private void AddMidRect(VertexHelper toFill, PointInfo ctrPoint, Vector3 nexCtrPos)
    {
        Vector3 dirPreToCrt = PointDir(lastCtrPos, ctrPoint.pos);
        Vector3 dirCrtToNex = PointDir(ctrPoint.pos, nexCtrPos);
        Vector3 dirAverage = (dirPreToCrt + dirCrtToNex) * 0.5f;

        Vector3 wideDir = new(-dirAverage.y, dirAverage.x, 0);

        float cos = dirAverage.x * dirPreToCrt.x + dirAverage.y * dirPreToCrt.y;
        float zoom = Mathf.Min(1.0f / cos, 99999);
        float radius = lineCrt.radius + ctrPoint.radius;
        Vector3 wideOffset = zoom * radius * wideDir;

        UIVertex vertexLeft = UIVertex.simpleVert;
        vertexLeft.position = ctrPoint.pos + wideOffset;
        vertexLeft.color = ctrPoint.color * color;
        // ctrPos fadeRadius
        vertexLeft.uv0 = new Vector4(ctrPoint.pos.x, ctrPoint.pos.y, lineCrt.fadeRadius);
        // radius, blankStart, blankLen, lineDis
        lineDis += (lastCtrPos - ctrPoint.pos).magnitude;
        vertexLeft.uv1 = new Vector4(Mathf.Sqrt(zoom) * radius, lineCrt.blankStart, lineCrt.blankLen, lineDis);
        // os
        vertexLeft.uv2 = new Vector4(vertexLeft.position.x, vertexLeft.position.y);
        toFill.AddVert(vertexLeft);
        //Debug.LogError("vertexLeft " + vertexLeft.position + " lineDis " + lineDis);

        UIVertex vertexRight = vertexLeft;
        vertexRight.position = ctrPoint.pos - wideOffset;
        vertexRight.uv2 = new Vector4(vertexRight.position.x, vertexRight.position.y);
        toFill.AddVert(vertexRight);

        lastCtrPos = ctrPoint.pos;
        vertexCount += 2;

        AddTwoTriangle(toFill);
    }

    private void AddTwoTriangle(VertexHelper toFill)
    {
        toFill.AddTriangle(vertexCount - 4, vertexCount - 2, vertexCount - 3);
        toFill.AddTriangle(vertexCount - 2, vertexCount - 1, vertexCount - 3);
    }
}