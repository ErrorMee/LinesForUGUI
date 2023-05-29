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
    float travelLenLeft = 0; float travelLenRight = 0;

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
        lineCrt = lineInfo; travelLenLeft = travelLenRight = 0;

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
        Vector3 wideOffset = wideDir * (pointCrt.radius + lineInfo.roundRadius);
        Vector3 roundOffset = lineInfo.roundRadius * dirCrtToNex;
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
            float dCos = Mathf.Min(1.0f / cos, 32);
            wideOffset = dCos * (pointCrt.radius + lineInfo.roundRadius) * wideDir;

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
            wideOffset = wideDir * (pointCrt.radius + lineInfo.roundRadius);
            AddMidRect(toFill, wideOffset, pointCrt, pointPre.pos);
            AddEndRect(toFill, pointCrt.pos + lineInfo.roundRadius * dirPreToCrt, wideOffset, pointCrt, pointPre.pos);
        }
        else
        {
            wideOffset = Vector3.up * (pointCrt.radius + lineInfo.roundRadius);
            roundOffset = lineInfo.roundRadius * lineInfo.roundRadius * Vector3.right;
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
        travelLenLeft = travelLenRight * 2 - travelLenLeft;
        vertexLastLeft.uv2.w = travelLenLeft;

        UIVertex vertexLeft = vertexLastLeft;
        vertexLeft.uv0 = new Vector4(posA.x, posA.y, pointB.pos.x, pointB.pos.y);
        toFill.AddVert(vertexLeft);
        Debug.LogError(" ab " + vertexLeft.uv0 + " dis " + vertexLeft.uv2.w + " --- Reuse vertexLeft ");
        UIVertex vertexRight = vertexLastRight;
        vertexRight.uv0 = vertexLeft.uv0;
        toFill.AddVert(vertexRight);
        Debug.LogError(" ab " + vertexRight.uv0 + " dis " + vertexRight.uv2.w + " --- Reuse vertexRight ");
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
        // os, fadeRadius, travel len
        travelLenLeft += (vertexLeft.position - vertexLastLeft.position).magnitude;
        vertexLeft.uv2 = new Vector4(vertexLeft.position.x, vertexLeft.position.y, lineCrt.fadeRadius, travelLenLeft);
        toFill.AddVert(vertexLeft);
        Debug.LogError(" ab " + vertexLeft.uv0 + " dis " + vertexLeft.uv2.w + " +++ New vertexLeft ");
        vertexLastLeft = vertexLeft;

        UIVertex vertexRight = vertexLeft;
        vertexRight.position = midPos - wideOffset;
        travelLenRight += (vertexRight.position - vertexLastRight.position).magnitude;
        vertexRight.uv2 = new Vector4(vertexRight.position.x, vertexRight.position.y, lineCrt.fadeRadius, travelLenRight);
        toFill.AddVert(vertexRight);
        Debug.LogError(" ab " + vertexRight.uv0 + " dis " + vertexRight.uv2.w + " +++ New vertexRight ");
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