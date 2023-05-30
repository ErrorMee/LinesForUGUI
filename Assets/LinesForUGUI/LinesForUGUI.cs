#if UNITY_EDITOR
using UnityEditor.UI;
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LinesForUGUI : Image
{
    [SerializeField] List<LineInfo> lines;

    int vertexCount = 0; LineInfo lineCrt;
    UIVertex vertexLastLeft; UIVertex vertexLastRight;

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

    private Vector3 PointDir(Vector3 fromPos, Vector3 toPos)
    {
        Vector3 pointDir = (fromPos - toPos).normalized;
        if (pointDir == Vector3.zero)
        {
            pointDir = Vector3.right;
        }
        return pointDir;
    }

    private void AddTwoHeadVert(VertexHelper toFill, PointInfo ctrPoint)
    {
        Vector3 pointDir = Vector3.left;
        if (lineCrt.points.Count > 1)
        {
            pointDir = PointDir(lineCrt.points[1].pos, ctrPoint.pos);
        }
        Vector3 wideDir = new(-pointDir.y, pointDir.x, 0);
        Vector3 wideOffset = wideDir * (ctrPoint.radius + lineCrt.roundRadius);
        Vector3 posOffset = lineCrt.roundRadius * pointDir;

        UIVertex vertexLeft = UIVertex.simpleVert;
        vertexLeft.position = ctrPoint.pos + posOffset + wideOffset;
        vertexLeft.color = ctrPoint.color * color;
        // ctrPos fadeRadius
        vertexLeft.uv0 = new Vector4(ctrPoint.pos.x, ctrPoint.pos.y, lineCrt.fadeRadius);
        // sdOrientedBox: thickness, round, blank
        vertexLeft.uv1 = new Vector4(ctrPoint.radius * 2, lineCrt.roundRadius, lineCrt.blankStart, lineCrt.blankLen);
        // os
        vertexLeft.uv2 = new Vector4(vertexLeft.position.x, vertexLeft.position.y);
        toFill.AddVert(vertexLeft);
        vertexLastLeft = vertexLeft;

        UIVertex vertexRight = vertexLeft;
        vertexRight.position = ctrPoint.pos + posOffset - wideOffset;
        vertexRight.uv2 = new Vector4(vertexRight.position.x, vertexRight.position.y);
        toFill.AddVert(vertexRight);
        vertexLastRight = vertexRight;
    }

    private void Draw(VertexHelper toFill, LineInfo lineInfo)
    {
        if (lineInfo.points.Count < 1)
        {
            return;
        }
        lineCrt = lineInfo; 

        PointInfo ctrPoint = lineInfo.points[0];
        AddTwoHeadVert(toFill, ctrPoint);


        for (int i = 1; i < lineInfo.points.Count - 1; i++)
        {
            ctrPoint = lineInfo.points[i];
            PointInfo pointPre = lineInfo.points[i - 1];
            Vector3 dirPreToCrt = (ctrPoint.pos - pointPre.pos).normalized;
            if (dirPreToCrt == Vector3.zero)
            {
                dirPreToCrt = Vector3.right;
            }
            PointInfo pointNex = lineInfo.points[i + 1];
            pointDir = (pointNex.pos - ctrPoint.pos).normalized;

            Vector3 dirAverage = (dirPreToCrt + pointDir) * 0.5f;

            wideDir = new(-dirAverage.y, dirAverage.x, 0);

            float cos = dirAverage.x * dirPreToCrt.x + dirAverage.y * dirPreToCrt.y;
            float dCos = Mathf.Min(1.0f / cos, 99999);
            wideOffset = dCos * (ctrPoint.radius + lineInfo.roundRadius) * wideDir;

            AddMidRect(toFill, wideOffset, ctrPoint, pointPre.pos);
        }

        if (lineInfo.points.Count > 1)
        {
            ctrPoint = lineInfo.points[^1];
            PointInfo pointPre = lineInfo.points[^2];
            Vector3 dirPreToCrt = (ctrPoint.pos - pointPre.pos).normalized;

            if (dirPreToCrt == Vector3.zero)
            {
                dirPreToCrt = Vector3.right;
            }

            wideDir = new(-dirPreToCrt.y, dirPreToCrt.x, 0);
            wideOffset = wideDir * (ctrPoint.radius + lineInfo.roundRadius);
            AddMidRect(toFill, wideOffset, ctrPoint, pointPre.pos);
            //AddEndRect(toFill, pointCrt.pos + lineInfo.roundRadius * dirPreToCrt, wideOffset, pointCrt, pointPre.pos);
        }
        else
        {
            wideOffset = Vector3.up * (ctrPoint.radius + lineInfo.roundRadius);
            posOffset = lineInfo.roundRadius * lineInfo.roundRadius * Vector3.right;
            PointInfo pointPre = lineInfo.points[0];
            AddEndRect(toFill, pointPre.pos + posOffset, wideOffset, ctrPoint, pointPre.pos - posOffset);
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
        vertexLeft.uv1 = new Vector4(pointCrt.radius * 2, lineCrt.roundRadius, lineCrt.blankStart, lineCrt.blankLen);

        vertexLeft.position = midPos + wideOffset;
        // os, fadeRadius, offsetStart0
        vertexLeft.uv2 = new Vector4(vertexLeft.position.x, vertexLeft.position.y, lineCrt.fadeRadius, 0);
        toFill.AddVert(vertexLeft);
        Debug.LogError(" offsetStart: " + vertexLeft.uv2.w + "  +++ L");
        vertexLastLeft = vertexLeft;

        UIVertex vertexRight = vertexLeft;
        vertexRight.position = midPos - wideOffset;
        vertexRight.uv2 = new Vector4(vertexRight.position.x, vertexRight.position.y, lineCrt.fadeRadius, 0);
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