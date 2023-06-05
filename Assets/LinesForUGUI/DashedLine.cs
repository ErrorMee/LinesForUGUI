#if UNITY_EDITOR
using UnityEditor.UI;
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DashedLine : Image
{
    [SerializeField] List<LineInfo> lines;
    VertexHelper toFill;
    int vertexCount = 0; LineInfo lineCrt;
    UIVertex lastVertLeft; UIVertex lastVertRight;

    public void Draw(List<LineInfo> lines)
    {
        this.lines = lines;
    }

    protected override void OnPopulateMesh(VertexHelper toFill)
    {
        toFill.Clear(); this.toFill = toFill;
        vertexCount = 0;
        for (int i = 0; i < lines.Count; i++)
        {
            Draw(lines[i]);
        }
    }

    private void Draw(LineInfo lineInfo)
    {
        if (lineInfo.points.Count < 2)
        {
            return;
        }
        lineCrt = lineInfo;

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

        if (index == 1)
        {
            if (lineCrt.points.Count > 2)
            {
                AddRect(prePoint.pos, CalcEndPos(index, thicknessOffset), thicknessOffset);
            }
            else
            {
                AddRect(prePoint.pos, ctrPoint.pos, thicknessOffset);
            }
            return;
        }

        if (index == lineCrt.points.Count - 1)
        {
            AddRect(CalcStartPos(index, thicknessOffset), ctrPoint.pos, thicknessOffset);
            return;
        }

        AddRect(CalcStartPos(index, thicknessOffset), CalcEndPos(index, thicknessOffset), thicknessOffset);
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

        float cos¦ÈStart = pointDirStart.x * pointDirPre.x + pointDirStart.y * pointDirPre.y;
        float zoomStart = Mathf.Min(1.0f / cos¦ÈStart, 999);

        Vector3 offsetStart = zoomStart * (ctrPoint.radius + lineCrt.roundRadius) * thicknessDirStart;
        Vector3 posLeftStart = prePoint.pos + offsetStart;
        Vector3 posRightStart = prePoint.pos - offsetStart;

        Vector3 posLeftEnd = ctrPoint.pos + thicknessOffset;
        Vector3 posRightEnd = ctrPoint.pos - thicknessOffset;
        
        float disLeft = (posLeftStart - posLeftEnd).magnitude;
        float disRight = (posRightStart - posRightEnd).magnitude;

        Vector3 startPos = ctrPoint.pos - pointDir * Mathf.Min(disLeft, disRight);
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

        float cos¦ÈEnd = pointDirEnd.x * pointDir.x + pointDirEnd.y * pointDir.y;
        float zoomEnd = Mathf.Min(1.0f / cos¦ÈEnd, 999);

        Vector3 offsetEnd = zoomEnd * (ctrPoint.radius + lineCrt.roundRadius) * thicknessDirEnd;
        Vector3 posLeftEnd = ctrPoint.pos + offsetEnd;
        Vector3 posRightEnd = ctrPoint.pos - offsetEnd;

        Vector3 posLeftStart = prePoint.pos + thicknessOffset;
        Vector3 posRightStart = prePoint.pos - thicknessOffset;

        float disLeft = (posLeftStart - posLeftEnd).magnitude;
        float disRight = (posRightStart - posRightEnd).magnitude;

        Vector3 endPos = prePoint.pos + pointDir * Mathf.Min(disLeft, disRight);
        return endPos;
    }

    private void FillGap(Vector3 pos0, Vector3 pos1, Vector3 pos2, Vector3 pos3)
    {
        UIVertex vert = UIVertex.simpleVert;
        vert.position = pos0;
        toFill.AddVert(vert);
        vert.position = pos1;
        toFill.AddVert(vert);
        vert.position = pos2;
        toFill.AddVert(vert);
        vert.position = pos3;
        toFill.AddVert(vert);

        vertexCount += 4;
        toFill.AddTriangle(vertexCount - 4, vertexCount - 2, vertexCount - 3);
        toFill.AddTriangle(vertexCount - 2, vertexCount - 1, vertexCount - 3);
    }

    private void AddRect(Vector3 start, Vector3 end, Vector3 offset)
    {
        UIVertex vert = UIVertex.simpleVert;
        vert.position = start + offset;
        toFill.AddVert(vert);
        vert.position = start - offset;
        toFill.AddVert(vert);

        vert.position = end + offset;
        toFill.AddVert(vert);
        lastVertLeft = vert;

        vert.position = end - offset;
        toFill.AddVert(vert);
        lastVertRight = vert;

        vertexCount += 4;

        toFill.AddTriangle(vertexCount - 4, vertexCount - 2, vertexCount - 3);
        toFill.AddTriangle(vertexCount - 2, vertexCount - 1, vertexCount - 3);
    }

    private void DebugVert(string tag, UIVertex vertexLeft, UIVertex vertexRight)
    {
        //Debug.LogError(tag + " ab " + vertexLeft.uv0 + " Dis L " + vertexLeft.uv2.w + " R " + vertexRight.uv2.w);
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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        for (int i = 0; i < lines.Count; i++)
        {
            LineInfo lineInfo = lines[i];
            for (int j = 0; j < lineInfo.points.Count; j++)
            {
                PointInfo pointInfo = lineInfo.points[j];
                Gizmos.DrawSphere(pointInfo.pos + transform.position, 2);
                if (j > 0)
                {
                    Gizmos.DrawLine(lineInfo.points[j - 1].pos + transform.position, pointInfo.pos + transform.position);
                }
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(DashedLine), true)]
public class DashedLineEditor : GraphicEditor
{
    [MenuItem("GameObject/Roarbro/DashedLine", false, 1)]
    public static void CreateDashedLine()
    {
        var goRoot = Selection.activeGameObject;
        if (goRoot == null)
            return;
        var gameObject = new GameObject("DashedLine");
        DashedLine component = gameObject.AddComponent<DashedLine>();
        component.raycastTarget = false;
        gameObject.transform.SetParent(goRoot.transform, false);
        gameObject.transform.SetAsLastSibling();
        Undo.RegisterCreatedObjectUndo(gameObject, "Created " + gameObject.name);
    }

    [MenuItem("CONTEXT/Graphic/Convert To DashedLine", true)]
    static bool CheckConvertToLinesForUGUI(MenuCommand command)
    {
        return ComponentConverter.CanConvertTo<DashedLine>(command.context);
    }

    [MenuItem("CONTEXT/Graphic/Convert To DashedLine", false)]
    static void ConvertToLinesForUGUI(MenuCommand command)
    {
        ComponentConverter.ConvertTo<DashedLine>(command.context);
    }

    SerializedProperty lines;

    protected override void OnEnable()
    {
        base.OnEnable();
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
        EditorGUILayout.PropertyField(lines);
        EditorGUI.EndChangeCheck();
    }
}
#endif