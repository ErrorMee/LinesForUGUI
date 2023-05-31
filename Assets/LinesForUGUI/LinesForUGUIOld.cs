#if UNITY_EDITOR
using UnityEditor.UI;
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LinesForUGUIOld : Image
{
    [SerializeField] List<LineInfo> lines;

    int vertexCount = 0; LineInfo lineCrt;
    UIVertex vertexLastLeft; UIVertex vertexLastRight;
    float offsetStartLeft = 0; float offsetStartRight = 0;

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
        lineCrt = lineInfo; offsetStartLeft = offsetStartRight = - lineCrt.radius;

        PointInfo pointCrt = lineInfo.points[0];
        Vector3 dirCrtToNex = Vector3.right;
        if (lineInfo.points.Count > 1)
        {
            dirCrtToNex = (lineInfo.points[1].pos - pointCrt.pos).normalized;
            if (dirCrtToNex == Vector3.zero)
            {
                dirCrtToNex = Vector3.right;
            }
        }
        Vector3 wideDir = new(-dirCrtToNex.y, dirCrtToNex.x, 0);
        Vector3 wideOffset = wideDir * (pointCrt.radius + lineInfo.radius);
        Vector3 roundOffset = lineInfo.radius * dirCrtToNex;
        AddStartRect(toFill, pointCrt.pos - roundOffset, wideOffset, pointCrt, pointCrt.pos + roundOffset);

        for (int i = 1; i < lineInfo.points.Count - 1; i++)
        {
            pointCrt = lineInfo.points[i];
            PointInfo pointPre = lineInfo.points[i - 1];
            Vector3 dirPreToCrt = (pointCrt.pos - pointPre.pos).normalized;
            if (dirPreToCrt == Vector3.zero)
            {
                dirPreToCrt = Vector3.right;
            }
            PointInfo pointNex = lineInfo.points[i + 1];
            dirCrtToNex = (pointNex.pos - pointCrt.pos).normalized;

            Vector3 dirAverage = (dirPreToCrt + dirCrtToNex) * 0.5f;

            wideDir = new(-dirAverage.y, dirAverage.x, 0);

            float cos = dirAverage.x * dirPreToCrt.x + dirAverage.y * dirPreToCrt.y;
            float dCos = Mathf.Min(1.0f / cos, 99999);
            wideOffset = dCos * (pointCrt.radius + lineInfo.radius) * wideDir;

            AddMidRect(toFill, wideOffset, pointCrt, pointPre.pos);
        }

        if (lineInfo.points.Count > 1)
        {
            pointCrt = lineInfo.points[^1];
            PointInfo pointPre = lineInfo.points[^2];
            Vector3 dirPreToCrt = (pointCrt.pos - pointPre.pos).normalized;

            if (dirPreToCrt == Vector3.zero)
            {
                dirPreToCrt = Vector3.right;
            }

            wideDir = new(-dirPreToCrt.y, dirPreToCrt.x, 0);
            wideOffset = wideDir * (pointCrt.radius + lineInfo.radius);
            AddMidRect(toFill, wideOffset, pointCrt, pointPre.pos);
            AddEndRect(toFill, pointCrt.pos + lineInfo.radius * dirPreToCrt, wideOffset, pointCrt, pointPre.pos);
        }
        else
        {
            wideOffset = Vector3.up * (pointCrt.radius + lineInfo.radius);
            roundOffset = lineInfo.radius * lineInfo.radius * Vector3.right;
            PointInfo pointPre = lineInfo.points[0];
            AddEndRect(toFill, pointPre.pos + roundOffset, wideOffset, pointCrt, pointPre.pos - roundOffset);
        }
    }

    private void AddStartRect(VertexHelper toFill, Vector3 posFrom, Vector3 wideOffset, PointInfo pointCrt, Vector3 posTo)
    {
        vertexLastLeft = UIVertex.simpleVert;
        vertexLastLeft.position = posFrom + wideOffset;
        vertexLastRight = UIVertex.simpleVert;
        vertexLastRight.position = posFrom - wideOffset;

        NewTwoVert(toFill, posFrom, wideOffset, pointCrt, pointCrt.pos, posTo);
        NewTwoVert(toFill, pointCrt.pos, wideOffset, pointCrt, pointCrt.pos, posTo);
        AddTwoTriangle(toFill);
    }

    private void AddMidRect(VertexHelper toFill, Vector3 wideOffset, PointInfo pointB, Vector3 posA)
    {
        ReuseTwoVert(toFill, pointB, posA);
        NewTwoVert(toFill, pointB.pos, wideOffset, pointB, posA, pointB.pos);
        AddTwoTriangle(toFill);
    }

    private void AddEndRect(VertexHelper toFill, Vector3 posB, Vector3 wideOffset, PointInfo pointB, Vector3 posA)
    {
        ReuseTwoVert(toFill, pointB, posA);
        NewTwoVert(toFill, posB, wideOffset, pointB, posA, pointB.pos);
        AddTwoTriangle(toFill);
    }

    private void ReuseTwoVert(VertexHelper toFill, PointInfo pointB, Vector3 posA)
    {
        float leftGapRight = (offsetStartLeft - offsetStartRight) * 0.5f;
        offsetStartLeft = -leftGapRight;
        offsetStartRight = leftGapRight;

        vertexLastLeft.uv2.w = offsetStartLeft;
        vertexLastRight.uv2.w = offsetStartRight;

        UIVertex vertexLeft = vertexLastLeft;
        vertexLeft.uv0 = new Vector4(posA.x, posA.y, pointB.pos.x, pointB.pos.y);
        toFill.AddVert(vertexLeft);
        Debug.LogError(" offsetStart: " + vertexLeft.uv2.w + "  --- L");
        UIVertex vertexRight = vertexLastRight;
        vertexRight.uv0 = vertexLeft.uv0;
        toFill.AddVert(vertexRight);
        Debug.LogError(" offsetStart: " + vertexRight.uv2.w + "  --- R");
        vertexCount += 2;
    }

    private void NewTwoVert(VertexHelper toFill, Vector3 midPos, Vector3 wideOffset, 
        PointInfo pointCrt, Vector3 posA, Vector3 posB)
    {
        UIVertex vertexLeft = UIVertex.simpleVert;
        vertexLeft.color = pointCrt.color * color;
        // sdOrientedBox: abPos
        vertexLeft.uv0 = new Vector4(posA.x, posA.y, posB.x, posB.y);
        // sdOrientedBox: thickness, round, blank
        vertexLeft.uv1 = new Vector4(pointCrt.radius * 2, lineCrt.radius, lineCrt.blankStart, lineCrt.blankLen);

        vertexLeft.position = midPos + wideOffset;
        // os, fadeRadius, offsetStart
        offsetStartLeft += (vertexLeft.position - vertexLastLeft.position).magnitude;
        vertexLeft.uv2 = new Vector4(vertexLeft.position.x, vertexLeft.position.y, lineCrt.fadeRadius, offsetStartLeft);
        toFill.AddVert(vertexLeft);
        Debug.LogError(" offsetStart: " + vertexLeft.uv2.w + "  +++ L");
        vertexLastLeft = vertexLeft;

        UIVertex vertexRight = vertexLeft;
        vertexRight.position = midPos - wideOffset;
        offsetStartRight += (vertexRight.position - vertexLastRight.position).magnitude;
        vertexRight.uv2 = new Vector4(vertexRight.position.x, vertexRight.position.y, lineCrt.fadeRadius, offsetStartRight);
        toFill.AddVert(vertexRight);
        Debug.LogError(" offsetStart: " + vertexRight.uv2.w + "  +++ R");
        vertexLastRight = vertexRight;

        vertexCount += 2;
    }

    private void AddTwoTriangle(VertexHelper toFill)
    {
        toFill.AddTriangle(vertexCount - 4, vertexCount - 2, vertexCount - 3);
        toFill.AddTriangle(vertexCount - 2, vertexCount - 1, vertexCount - 3);
    }
}