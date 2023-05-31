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

    UIVertex vertexLeftLast; UIVertex vertexRightLast;
    float disLeft = 0;
    float disRight = 0;

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
        lineCrt = lineInfo;
        disLeft = disRight = 0;

        if (lineCrt.points.Count > 1)
        {
            AddStartQuad(toFill);
        }

        for (int i = 1; i < lineCrt.points.Count - 2; i++)
        {
            AddMidQuad(toFill, i);
        }

        if (lineCrt.points.Count > 2)
        {
            AddEndQuad(toFill);
        }
    }

    private void AddStartQuad(VertexHelper toFill)
    {
        PointInfo ctrPoint = lineCrt.points[0];
        PointInfo nexPoint = lineCrt.points[1];
        Vector3 pointDir = PointDir(ctrPoint.pos, nexPoint.pos);
        Vector3 offsetDir = new(-pointDir.y, pointDir.x, 0);

        AddTwoVert(toFill, ctrPoint, ctrPoint.radius * offsetDir, ctrPoint, nexPoint, false);

        ctrPoint = lineCrt.points[1];
        if (lineCrt.points.Count > 2)
        {
            Vector3 nexPointDir = PointDir(ctrPoint.pos, lineCrt.points[2].pos);
            AddMidTwoVert(toFill, ctrPoint, pointDir, nexPointDir, lineCrt.points[0], ctrPoint);
        }
        else
        {
            AddTwoVert(toFill, ctrPoint, ctrPoint.radius * offsetDir, lineCrt.points[0], ctrPoint);
        }

        AddTwoTriangle(toFill);
    }

    private void AddMidQuad(VertexHelper toFill, int index)
    {
        PointInfo ctrPoint = lineCrt.points[index];
        PointInfo nexPoint = lineCrt.points[index + 1];
        PointInfo nex2Point = lineCrt.points[index + 2];

        ReuseTwoVert(toFill, ctrPoint, nexPoint);

        Vector3 pointDir = PointDir(ctrPoint.pos, nexPoint.pos);
        Vector3 nexPointDir = PointDir(nexPoint.pos, nex2Point.pos);
        AddMidTwoVert(toFill, nexPoint, pointDir, nexPointDir, ctrPoint, nexPoint);

        AddTwoTriangle(toFill);
    }

    private void AddEndQuad(VertexHelper toFill)
    {
        PointInfo ctrPoint = lineCrt.points[^1];
        PointInfo prePoint = lineCrt.points[^2];

        ReuseTwoVert(toFill, prePoint, ctrPoint);

        Vector3 pointDir = PointDir(prePoint.pos, ctrPoint.pos);
        Vector3 offsetDir = new(-pointDir.y, pointDir.x, 0);

        AddTwoVert(toFill, ctrPoint, ctrPoint.radius * offsetDir, prePoint, ctrPoint);

        AddTwoTriangle(toFill);
    }

    private void ReuseTwoVert(VertexHelper toFill, PointInfo aPoint, PointInfo bPoint)
    {
        vertexRightLast.uv0 = vertexLeftLast.uv0 = new Vector4(aPoint.pos.x, aPoint.pos.y, bPoint.pos.x, bPoint.pos.y);
        //Debug.LogError("abPos " + vertexRightLast.uv0);

        if (disLeft < disRight)
        {
            disRight = disLeft - (disRight - disLeft);
        }
        else
        {
            disLeft = disRight - (disLeft - disRight);
        }

        vertexLeftLast.uv2.z = disLeft;
        vertexRightLast.uv2.z = disRight;

        toFill.AddVert(vertexLeftLast);
        toFill.AddVert(vertexRightLast);
        vertexCount += 2;
    }

    private void AddMidTwoVert(VertexHelper toFill, PointInfo ctrPoint, Vector3 pointDir, Vector3 nexPointDir,
        PointInfo aPoint, PointInfo bPoint)
    {
        Vector3 dirAverage = (pointDir + nexPointDir) * 0.5f;
        Vector3 offsetDir = new(-dirAverage.y, dirAverage.x, 0);
        float cos = dirAverage.x * pointDir.x + dirAverage.y * pointDir.y;
        float zoom = Mathf.Min(1.0f / cos, 99999);
        AddTwoVert(toFill, ctrPoint, zoom * ctrPoint.radius * offsetDir, aPoint, bPoint);
    }

    private void AddTwoVert(VertexHelper toFill, PointInfo ctrPoint, Vector3 offset,
        PointInfo aPoint, PointInfo bPoint, bool addDis = true)
    {
        UIVertex vertexLeft = UIVertex.simpleVert;
        vertexLeft.position = ctrPoint.pos + offset;
        vertexLeft.color = ctrPoint.color * color;
        vertexLeft.uv0 = new Vector4(aPoint.pos.x, aPoint.pos.y, bPoint.pos.x, bPoint.pos.y); 
        //Debug.LogError("abPos " + vertexLeft.uv0);
        vertexLeft.uv1 = new Vector4(ctrPoint.radius, lineCrt.blankStart, lineCrt.blankLen);

        UIVertex vertexRight = vertexLeft;
        vertexRight.position = ctrPoint.pos - offset;

        if (addDis)
        {
            disLeft += (vertexLeftLast.position - vertexLeft.position).magnitude;
            disRight += (vertexRightLast.position - vertexRight.position).magnitude;
        }
        
        vertexLeft.uv2 = new Vector4(vertexLeft.position.x, vertexLeft.position.y, disLeft);
        toFill.AddVert(vertexLeft);
        vertexLeftLast = vertexLeft;

        vertexRight.uv2 = new Vector4(vertexRight.position.x, vertexRight.position.y, disRight);
        toFill.AddVert(vertexRight);
        vertexRightLast = vertexRight;
        Debug.LogError("disLeft " + disLeft + " disRight " + disRight);
        vertexCount += 2;
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