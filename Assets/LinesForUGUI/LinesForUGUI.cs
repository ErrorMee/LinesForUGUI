#if UNITY_EDITOR
using UnityEditor.UI;
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class LinesForUGUI : Image
{
    [SerializeField] List<LineInfo> lines;
    VertexHelper toFill;
    int vertexCount = 0; LineInfo lineCrt;
    UIVertex lastVertLeft; UIVertex lastVertRight;
    float dashLen; float lastSegmentLen; float lastOffsetA;
    float solidLen = 0;
    int lackStart; int lackEnd = 0;

    [SerializeField][Range(0, 128)] float fadeRadius;
    static readonly int FadeRadiusKey = Shader.PropertyToID("_FadeRadius");

    public void Draw(List<LineInfo> lines)
    {
        this.lines = lines;
    }

    protected override void OnPopulateMesh(VertexHelper toFill)
    {
        toFill.Clear(); this.toFill = toFill;
        vertexCount = 0;
        material.SetFloat(FadeRadiusKey, fadeRadius);
        for (int i = 0; i < lines.Count; i++)
        {
            Draw(lines[i]);
        }
    }

    private void Draw(LineInfo lineInfo)
    {
        if (lineInfo.points.Count < 1)
        {
            return;
        }
        lineCrt = lineInfo; lastSegmentLen = 0; lastOffsetA = 0; solidLen = lineCrt.blankStart;
        dashLen = solidLen + lineCrt.roundRadius * 2 + lineCrt.blankLen;
        
        if (lineInfo.points.Count == 1)
        {
            solidLen = 0.01f;
            PointInfo ctrPoint = lineCrt.points[0];
            Vector3 hOffset = Vector3.left * lineCrt.roundRadius;
            Vector3 VOffset = Vector3.up * (ctrPoint.radius + lineCrt.roundRadius);
            AddValidRect(ctrPoint.pos + hOffset, ctrPoint.pos - hOffset,  VOffset, VOffset, ctrPoint, ctrPoint);
        }

        for (int i = 1; i < lineCrt.points.Count; i++)
        {
            DrawSegment(i);
        }
    }

    private void DrawSegment(int index)
    {
        PointInfo prePoint = lineCrt.points[index - 1];
        PointInfo ctrPoint = lineCrt.points[index];
        Vector3 pointDir = PointDir(prePoint.pos, ctrPoint.pos);
        Vector3 thicknessDir = new(-pointDir.y, pointDir.x, 0);
        Vector3 thicknessOffset = thicknessDir * (ctrPoint.radius + lineCrt.roundRadius);
        lackStart = lackEnd = 0;
        if (index == 1)
        {
            if (lineCrt.points.Count > 2)
            {
                Vector3 startPos = prePoint.pos - pointDir * lineCrt.roundRadius;
                Vector3 endPos = CalcEndPos(index, thicknessOffset);
                AddValidRect(startPos, endPos, thicknessDir * (prePoint.radius + lineCrt.roundRadius), thicknessOffset, prePoint, ctrPoint);
            }
            else
            {
                Vector3 startPos = prePoint.pos - pointDir * lineCrt.roundRadius;
                Vector3 endPos = ctrPoint.pos + pointDir * lineCrt.roundRadius;
                AddValidRect(startPos, endPos, thicknessDir * (prePoint.radius + lineCrt.roundRadius), thicknessOffset, prePoint, ctrPoint);
            }
            return;
        }

        if (index == lineCrt.points.Count - 1)
        {
            Vector3 startPos = CalcStartPos(index, thicknessOffset);
            Vector3 endPos = ctrPoint.pos + pointDir * lineCrt.roundRadius;
            AddValidRect(startPos, endPos, thicknessDir * (prePoint.radius + lineCrt.roundRadius), thicknessOffset, prePoint, ctrPoint);
            return;
        }

        AddValidRect(CalcStartPos(index, thicknessOffset), CalcEndPos(index, thicknessOffset),
            thicknessDir * (prePoint.radius + lineCrt.roundRadius), thicknessOffset, prePoint, ctrPoint);
    }

    private Vector3 CalcStartPos(int index, Vector3 thicknessOffset)
    {
        PointInfo prePoint = lineCrt.points[index - 1];
        PointInfo ctrPoint = lineCrt.points[index];
        Vector3 pointDir = PointDir(prePoint.pos, ctrPoint.pos);

        PointInfo pre2Point = lineCrt.points[index - 2];
        Vector3 pointDirPre = PointDir(pre2Point.pos, prePoint.pos);
        Vector3 pointDirStart = (pointDir + pointDirPre).normalized;
        Vector3 thicknessDirStart = new(-pointDirStart.y, pointDirStart.x, 0);

        float cosStart = pointDirStart.x * pointDirPre.x + pointDirStart.y * pointDirPre.y;
        float zoomStart = Mathf.Min(1.0f / cosStart, 999);

        float radius = ctrPoint.radius + lineCrt.roundRadius;
        Vector3 offsetStart = zoomStart * radius * thicknessDirStart;
        Vector3 posLeftStart = prePoint.pos + offsetStart;
        Vector3 posRightStart = prePoint.pos - offsetStart;

        Vector3 posLeftEnd = ctrPoint.pos + thicknessOffset;
        Vector3 posRightEnd = ctrPoint.pos - thicknessOffset;
        
        float disLeft = (posLeftStart - posLeftEnd).magnitude;
        float disRight = (posRightStart - posRightEnd).magnitude;

        float minDis = Mathf.Min(disLeft, disRight);
        lackStart = Mathf.RoundToInt(Mathf.Abs(disLeft - disRight) + lineCrt.roundRadius);
        Vector3 startPos = ctrPoint.pos - pointDir * minDis;

        if (disLeft > disRight)
        {
            Vector3 leftPos = startPos + thicknessOffset;
            Vector3 rightPos = startPos - thicknessOffset;
            AddFillRect(rightPos, lastVertLeft.position, prePoint.pos + prePoint.pos - rightPos, leftPos);
        }
        else
        {
            Vector3 leftPos = startPos + thicknessOffset;
            Vector3 rightPos = startPos - thicknessOffset;
            AddFillRect(prePoint.pos + prePoint.pos - leftPos, lastVertRight.position, leftPos, rightPos);
        }

        return startPos;
    }

    private Vector3 CalcEndPos(int index, Vector3 thicknessOffset)
    {
        PointInfo prePoint = lineCrt.points[index - 1];
        PointInfo ctrPoint = lineCrt.points[index];
        Vector3 pointDir = PointDir(prePoint.pos, ctrPoint.pos);

        PointInfo nexPoint = lineCrt.points[index + 1];
        Vector3 pointDirNex = PointDir(ctrPoint.pos, nexPoint.pos);
        Vector3 pointDirEnd = (pointDir + pointDirNex).normalized;
        Vector3 thicknessDirEnd = new(-pointDirEnd.y, pointDirEnd.x, 0);

        float cosEnd = pointDirEnd.x * pointDir.x + pointDirEnd.y * pointDir.y;
        float zoomEnd = Mathf.Min(1.0f / cosEnd, 999);

        Vector3 offsetEnd = zoomEnd * (ctrPoint.radius + lineCrt.roundRadius) * thicknessDirEnd;
        Vector3 posLeftEnd = ctrPoint.pos + offsetEnd;
        Vector3 posRightEnd = ctrPoint.pos - offsetEnd;

        Vector3 posLeftStart = prePoint.pos + thicknessOffset;
        Vector3 posRightStart = prePoint.pos - thicknessOffset;

        float disLeft = (posLeftStart - posLeftEnd).magnitude;
        float disRight = (posRightStart - posRightEnd).magnitude;
        float minDis = Mathf.Min(disLeft, disRight);
        lackEnd = Mathf.RoundToInt(Mathf.Abs(disLeft - disRight) + lineCrt.roundRadius);
        Vector3 endPos = prePoint.pos + pointDir * minDis;
        return endPos;
    }

    private void AddFillRect(Vector3 pos0, Vector3 pos1, Vector3 pos2, Vector3 pos3)
    {
        UIVertex vertex = lastVertLeft;
        vertex.position = pos0; vertex.uv2.x = vertex.position.x; vertex.uv2.y = vertex.position.y;
        toFill.AddVert(vertex);

        vertex.position = pos1; vertex.uv2.x = vertex.position.x; vertex.uv2.y = vertex.position.y;
        toFill.AddVert(vertex);

        vertex.position = pos2; vertex.uv2.x = vertex.position.x; vertex.uv2.y = vertex.position.y;
        toFill.AddVert(vertex);

        vertex.position = pos3; vertex.uv2.x = vertex.position.x; vertex.uv2.y = vertex.position.y;
        toFill.AddVert(vertex); 

        vertexCount += 4;
        toFill.AddTriangle(vertexCount - 4, vertexCount - 3, vertexCount - 2);
        toFill.AddTriangle(vertexCount - 4, vertexCount - 2, vertexCount - 1);
    }

    private void AddValidRect(Vector3 start, Vector3 end, Vector3 offsetStart, Vector3 offsetEnd, PointInfo prePoint, PointInfo ctrPoint)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = prePoint.color * color;
        vertex.uv0 = new Vector4(start.x, start.y, end.x, end.y);
        vertex.uv1 = new Vector4(prePoint.radius * 2 + lackStart * 10000, solidLen + lackEnd * 1000, lineCrt.blankLen, lineCrt.roundRadius);
        vertex.uv2.w = (-lastSegmentLen + lastOffsetA) % dashLen;
        lastOffsetA = vertex.uv2.w;

        vertex.position = start + offsetStart; vertex.uv2.x = vertex.position.x; vertex.uv2.y = vertex.position.y; vertex.uv2.z = 0;
        toFill.AddVert(vertex); 

        vertex.position = start - offsetStart; vertex.uv2.x = vertex.position.x; vertex.uv2.y = vertex.position.y;
        toFill.AddVert(vertex);

        vertex.color = ctrPoint.color * color;
        vertex.uv1.x = ctrPoint.radius * 2 + lackStart * 10000;
        lastSegmentLen = (start - end).magnitude;
        vertex.position = end + offsetEnd; vertex.uv2.x = vertex.position.x; vertex.uv2.y = vertex.position.y; vertex.uv2.z = lastSegmentLen;
        toFill.AddVert(vertex); lastVertLeft = vertex; 

        vertex.position = end - offsetEnd; vertex.uv2.x = vertex.position.x; vertex.uv2.y = vertex.position.y; 
        toFill.AddVert(vertex); lastVertRight = vertex; 

        vertexCount += 4;
        toFill.AddTriangle(vertexCount - 4, vertexCount - 2, vertexCount - 3);
        toFill.AddTriangle(vertexCount - 2, vertexCount - 1, vertexCount - 3);
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
}

#if UNITY_EDITOR
[CustomEditor(typeof(LinesForUGUI), true)]
public class LinesForUGUIEditor : GraphicEditor
{
    [MenuItem("GameObject/Roarbro/LinesForUGUI", false, 1)]
    public static void CreateLinesForUGUI()
    {
        var goRoot = Selection.activeGameObject;
        if (goRoot == null)
            return;
        var gameObject = new GameObject("LinesForUGUI");
        LinesForUGUI component = gameObject.AddComponent<LinesForUGUI>();
        component.raycastTarget = false;
        gameObject.transform.SetParent(goRoot.transform, false);
        gameObject.transform.SetAsLastSibling();
        Undo.RegisterCreatedObjectUndo(gameObject, "Created " + gameObject.name);
    }

    [MenuItem("CONTEXT/Graphic/Convert To LinesForUGUI", true)]
    static bool CheckConvertToLinesForUGUI(MenuCommand command)
    {
        return ComponentConverter.CanConvertTo<LinesForUGUI>(command.context);
    }

    [MenuItem("CONTEXT/Graphic/Convert To LinesForUGUI", false)]
    static void ConvertToLinesForUGUI(MenuCommand command)
    {
        ComponentConverter.ConvertTo<LinesForUGUI>(command.context);
    }

    SerializedProperty fadeRadius;
    SerializedProperty lines;

    protected override void OnEnable()
    {
        base.OnEnable();
        fadeRadius = serializedObject.FindProperty("fadeRadius");
        lines = serializedObject.FindProperty("lines");
    }

    protected override void OnDisable() { }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        AppearanceControlsGUI();
        CustomGUI();
        RaycastControlsGUI();
        serializedObject.ApplyModifiedProperties();
    }

    protected void CustomGUI()
    {
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(fadeRadius);
        EditorGUILayout.PropertyField(lines);
        EditorGUI.EndChangeCheck();
    }
}
#endif