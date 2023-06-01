#if UNITY_EDITOR
using UnityEditor.UI;
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Reflection;

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

        AddStartQuad(toFill);

        for (int i = 1; i < lineCrt.points.Count - 2; i++)
        {
            AddMidQuad(toFill, i);
        }

        if (lineCrt.points.Count > 2)
        {
            Debug.LogError("AddEndQuad");
            AddEndQuad(toFill);
        }
    }

    private void AddStartQuad(VertexHelper toFill)
    {
        if (lineCrt.points.Count > 1)
        {
            PointInfo ctrPoint = lineCrt.points[0];
            PointInfo nexPoint = lineCrt.points[1];
            Vector3 pointDir = PointDir(ctrPoint.pos, nexPoint.pos);
            Vector3 offsetDir = new(-pointDir.y, pointDir.x, 0);
            float radius = ctrPoint.radius + lineCrt.roundRadius;

            AddTwoVert(toFill, ctrPoint, radius * offsetDir, ctrPoint, nexPoint);
            disLeft = disRight = -lineCrt.roundRadius;

            Vector3 offset = -pointDir * lineCrt.roundRadius;
            vertexLeftLast.position += offset;
            vertexLeftLast.uv2.x = vertexLeftLast.position.x; vertexLeftLast.uv2.y = vertexLeftLast.position.y;
            vertexRightLast.position += offset;
            vertexRightLast.uv2.x = vertexRightLast.position.x; vertexRightLast.uv2.y = vertexRightLast.position.y;
            Debug.LogError(" ---SetUIVertex--- AddEndQuad Left os " + vertexLeftLast.uv2.x + "," + vertexLeftLast.uv2.y);
            Debug.LogError(" ---SetUIVertex--- AddEndQuad Right os " + vertexRightLast.uv2.x + "," + vertexRightLast.uv2.y);
            vertexLeftLast.uv0.x = vertexRightLast.uv0.x = vertexLeftLast.uv0.x + offset.x;
            vertexLeftLast.uv0.y = vertexRightLast.uv0.y = vertexLeftLast.uv0.y + offset.y;
            //Debug.LogError(" ---SetUIVertex--- AddStartQuad abpos " + vertexRightLast.uv0.x + "," + vertexRightLast.uv0.z);
            toFill.SetUIVertex(vertexLeftLast, vertexCount - 2);
            toFill.SetUIVertex(vertexRightLast, vertexCount - 1);

            ctrPoint = lineCrt.points[1];
            if (lineCrt.points.Count > 2)
            {
                Vector3 nexPointDir = PointDir(ctrPoint.pos, lineCrt.points[2].pos);
                AddMidTwoVert(toFill, ctrPoint, pointDir, nexPointDir, lineCrt.points[0], ctrPoint);
                vertexLeftLast.uv0.x = vertexRightLast.uv0.x = vertexLeftLast.uv0.x + offset.x;
                vertexLeftLast.uv0.y = vertexRightLast.uv0.y = vertexLeftLast.uv0.y + offset.y;
                //Debug.LogError(" ---SetUIVertex--- AddStartQuad abpos " + vertexRightLast.uv0.x + "," + vertexRightLast.uv0.z);
                toFill.SetUIVertex(vertexLeftLast, vertexCount - 2);
                toFill.SetUIVertex(vertexRightLast, vertexCount - 1);
            }
            else
            {
                AddTwoVert(toFill, ctrPoint, radius * offsetDir, lineCrt.points[0], ctrPoint);
            }

            AddTwoTriangle(toFill);
        }
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

        AddTwoVert(toFill, ctrPoint, (ctrPoint.radius + lineCrt.roundRadius) * offsetDir, prePoint, ctrPoint);

        Vector3 offset = pointDir * lineCrt.roundRadius;
        vertexLeftLast.uv0.z = vertexRightLast.uv0.z = vertexLeftLast.uv0.z + offset.x; 
        vertexLeftLast.uv0.w = vertexRightLast.uv0.w = vertexLeftLast.uv0.w + offset.y;
        //Debug.LogError(" ---SetUIVertex--- AddEndQuad abpos " + vertexRightLast.uv0.x + "," + vertexRightLast.uv0.z);
        vertexLeftLast.position += offset;
        vertexLeftLast.uv2.x = vertexLeftLast.position.x; vertexLeftLast.uv2.y = vertexLeftLast.position.y;
        vertexRightLast.position += offset;
        vertexRightLast.uv2.x = vertexRightLast.position.x; vertexRightLast.uv2.y = vertexRightLast.position.y;

        toFill.SetUIVertex(vertexLeftLast, vertexCount - 2);
        toFill.SetUIVertex(vertexRightLast, vertexCount - 1);
        Debug.LogError(" ---SetUIVertex--- AddEndQuad Left os " + vertexLeftLast.uv2.x + "," + vertexLeftLast.uv2.y);
        Debug.LogError(" ---SetUIVertex--- AddEndQuad Right os " + vertexRightLast.uv2.x + "," + vertexRightLast.uv2.y);
        AddTwoTriangle(toFill);
    }

    private void ReuseTwoVert(VertexHelper toFill, PointInfo aPoint, PointInfo bPoint)
    {
        Vector3 a2bDir = PointDir(aPoint.pos, bPoint.pos);
        float cornerCutLen = 0.5f * Mathf.Abs(disLeft - disRight) + lineCrt.roundRadius;
        Vector3 cornerCut = cornerCutLen * a2bDir;

        vertexRightLast.uv0 = vertexLeftLast.uv0 = new Vector4(aPoint.pos.x + cornerCut.x, aPoint.pos.y + cornerCut.y, 
            bPoint.pos.x - cornerCut.x, bPoint.pos.y - cornerCut.y);
        //Debug.LogError(" --- ReuseTwoVert abpos " + vertexRightLast.uv0.x + "," + vertexRightLast.uv0.z);
        if (disLeft > disRight)
        {
            disLeft = disRight - disLeft;
            disRight = 0;
        }
        else
        {
            disRight = disLeft - disRight;
            disLeft = 0;
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
        AddTwoVert(toFill, ctrPoint, zoom * (ctrPoint.radius + lineCrt.roundRadius) * offsetDir, aPoint, bPoint);
    }

    private void AddTwoVert(VertexHelper toFill, PointInfo ctrPoint, Vector3 offset, PointInfo aPoint, PointInfo bPoint)
    {
        Vector3 posLeft = ctrPoint.pos + offset;
        Vector3 posRight = ctrPoint.pos - offset;

        UIVertex vertexLeft = UIVertex.simpleVert;
        vertexLeft.position = posLeft;
        vertexLeft.color = ctrPoint.color * color;

        disLeft += (vertexLeftLast.position - posLeft).magnitude;
        disRight += (vertexRightLast.position - posRight).magnitude;

        Vector3 a2bDir = PointDir(aPoint.pos, bPoint.pos);
        float cornerCutLen = 0.5f * Mathf.Abs(disLeft - disRight) + lineCrt.roundRadius;
        Vector3 cornerCut = cornerCutLen * a2bDir;

        vertexLeft.uv0 = new Vector4(aPoint.pos.x + cornerCut.x, aPoint.pos.y + cornerCut.y,
            bPoint.pos.x - cornerCut.x, bPoint.pos.y - cornerCut.y);
        //Debug.LogError("AddTwoVert abpos " + vertexLeft.uv0.x + "," + vertexLeft.uv0.z);
        vertexLeft.uv1 = new Vector4(ctrPoint.radius * 2, lineCrt.blankStart, lineCrt.blankLen, lineCrt.roundRadius);

        UIVertex vertexRight = vertexLeft;
        vertexRight.position = posRight;

        vertexLeft.uv2 = new Vector4(vertexLeft.position.x, vertexLeft.position.y, disLeft, lineCrt.fadeRadius);
        toFill.AddVert(vertexLeft);
        vertexLeftLast = vertexLeft;

        vertexRight.uv2 = new Vector4(vertexRight.position.x, vertexRight.position.y, disRight, lineCrt.fadeRadius);
        toFill.AddVert(vertexRight);
        vertexRightLast = vertexRight;
        vertexCount += 2;

        Debug.LogError(" AddTwoVert Left os " + vertexLeftLast.uv2.x + "," + vertexLeftLast.uv2.y);
        Debug.LogError(" AddTwoVert Right os " + vertexRightLast.uv2.x + "," + vertexRightLast.uv2.y);
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