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
    Vector3 lastCtrPos;

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
            pointDir = PointDir(ctrPoint.pos, lineCrt.points[1].pos);
        }
        Vector3 wideDir = new(-pointDir.y, pointDir.x, 0);
        Vector3 wideOffset = wideDir * (ctrPoint.radius + lineCrt.roundRadius);
        Vector3 posOffset = lineCrt.roundRadius * pointDir;

        UIVertex vertexLeft = UIVertex.simpleVert;
        vertexLeft.position = ctrPoint.pos + posOffset - wideOffset;
        vertexLeft.color = ctrPoint.color * color;
        // ctrPos fadeRadius
        vertexLeft.uv0 = new Vector4(ctrPoint.pos.x, ctrPoint.pos.y, lineCrt.fadeRadius);
        // sdOrientedBox: thickness, round, blank
        vertexLeft.uv1 = new Vector4(ctrPoint.radius * 2, lineCrt.roundRadius, lineCrt.blankStart, lineCrt.blankLen);
        // os
        vertexLeft.uv2 = new Vector4(vertexLeft.position.x, vertexLeft.position.y);
        toFill.AddVert(vertexLeft);
        Debug.LogError("vertexLeft " + vertexLeft.position);
        vertexLastLeft = vertexLeft;

        UIVertex vertexRight = vertexLeft;
        vertexRight.position = ctrPoint.pos + posOffset + wideOffset;
        vertexRight.uv2 = new Vector4(vertexRight.position.x, vertexRight.position.y);
        toFill.AddVert(vertexRight);
        Debug.LogError("vertexRight " + vertexRight.position);
        vertexLastRight = vertexRight;

        lastCtrPos = (vertexLastLeft.position + vertexLastRight.position) * 0.5f;
        vertexCount += 2;
    }

    private void AddMidRect(VertexHelper toFill, PointInfo ctrPoint, Vector3 nexCtrPos)
    {
        Vector3 dirPreToCrt = PointDir(ctrPoint.pos, lastCtrPos);
        Vector3 dirCrtToNex = PointDir(nexCtrPos, ctrPoint.pos);
        Vector3 dirAverage = (dirPreToCrt + dirCrtToNex) * 0.5f;

        Vector3 wideDir = new(-dirAverage.y, dirAverage.x, 0);

        float cos = dirAverage.x * dirPreToCrt.x + dirAverage.y * dirPreToCrt.y;
        float dCos = Mathf.Min(1.0f / cos, 99999);
        Vector3 wideOffset = dCos * (ctrPoint.radius + lineCrt.roundRadius) * wideDir;

        UIVertex vertexLeft = UIVertex.simpleVert;
        vertexLeft.position = ctrPoint.pos + wideOffset;
        vertexLeft.color = ctrPoint.color * color;
        // ctrPos fadeRadius
        vertexLeft.uv0 = new Vector4(ctrPoint.pos.x, ctrPoint.pos.y, lineCrt.fadeRadius);
        // sdOrientedBox: thickness, round, blank
        vertexLeft.uv1 = new Vector4(ctrPoint.radius * 2, lineCrt.roundRadius, lineCrt.blankStart, lineCrt.blankLen);
        // os
        vertexLeft.uv2 = new Vector4(vertexLeft.position.x, vertexLeft.position.y);
        toFill.AddVert(vertexLeft);
        Debug.LogError("vertexLeft " + vertexLeft.position);
        vertexLastLeft = vertexLeft;

        UIVertex vertexRight = vertexLeft;
        vertexRight.position = ctrPoint.pos - wideOffset;
        vertexRight.uv2 = new Vector4(vertexRight.position.x, vertexRight.position.y);
        toFill.AddVert(vertexRight);
        Debug.LogError("vertexRight " + vertexRight.position);
        vertexLastRight = vertexRight;

        lastCtrPos = ctrPoint.pos;
        vertexCount += 2;

        AddTwoTriangle(toFill);
    }

    private void Draw(VertexHelper toFill, LineInfo lineInfo)
    {
        if (lineInfo.points.Count < 1)
        {
            return;
        }
        lineCrt = lineInfo; 

        AddTwoHeadVert(toFill, lineCrt.points[0]);

        for (int i = 0; i < lineInfo.points.Count - 1; i++)
        {
            AddMidRect(toFill, lineInfo.points[i], lineInfo.points[i + 1].pos);
        }
        if (lineInfo.points.Count > 1)
        {
            PointInfo ctrPoint = lineInfo.points[^1];
            Vector3 dirPreToCrt = PointDir(ctrPoint.pos, lineInfo.points[^2].pos);
            AddMidRect(toFill, ctrPoint, ctrPoint.pos + dirPreToCrt);
        }
        
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