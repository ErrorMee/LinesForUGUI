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

        AddStartQuad(toFill);

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
        if (lineCrt.points.Count > 1)
        {
            PointInfo ctrPoint = lineCrt.points[0];
            PointInfo nexPoint = lineCrt.points[1];
            Vector3 pointDir = PointDir(ctrPoint.pos, nexPoint.pos);
            Vector3 offsetDir = new(-pointDir.y, pointDir.x, 0);
            float radius = ctrPoint.radius + lineCrt.roundRadius;

            AddTwoVert(toFill, ctrPoint, radius * offsetDir, ctrPoint, nexPoint);
            
            Vector3 offset = -pointDir * lineCrt.roundRadius;
            vertexLeftLast.position += offset;
            vertexLeftLast.uv2.x = vertexLeftLast.position.x; vertexLeftLast.uv2.y = vertexLeftLast.position.y;
            vertexRightLast.position += offset;
            vertexRightLast.uv2.x = vertexRightLast.position.x; vertexRightLast.uv2.y = vertexRightLast.position.y;
            //Debug.LogError(" ---AddStartQuad--- os " + vertexLeftLast.uv2 + "  -  " + vertexRightLast.uv2);

            vertexLeftLast.uv2.z = vertexRightLast.uv2.z = disLeft = disRight = 0;
            //Debug.LogError(" ---AddStartQuad--- disLeft " + disLeft);

            vertexLeftLast.uv0.x = vertexRightLast.uv0.x = vertexLeftLast.uv0.x + offset.x;
            vertexLeftLast.uv0.y = vertexRightLast.uv0.y = vertexLeftLast.uv0.y + offset.y;
            Debug.LogError(" ---AddStartQuad--- ab " + vertexRightLast.uv0.x + "," + vertexRightLast.uv0.z);
            toFill.SetUIVertex(vertexLeftLast, vertexCount - 2);
            toFill.SetUIVertex(vertexRightLast, vertexCount - 1);

            ctrPoint = lineCrt.points[1];
            if (lineCrt.points.Count > 2)
            {
                Vector3 nexPointDir = PointDir(ctrPoint.pos, lineCrt.points[2].pos);
                AddMidTwoVert(toFill, ctrPoint, pointDir, nexPointDir, lineCrt.points[0], ctrPoint);
                vertexLeftLast.uv0.x = vertexRightLast.uv0.x = vertexLeftLast.uv0.x + offset.x;
                vertexLeftLast.uv0.y = vertexRightLast.uv0.y = vertexLeftLast.uv0.y + offset.y;
                Debug.LogError(" ---AddStartQuad--- ab " + vertexRightLast.uv0.x + "," + vertexRightLast.uv0.z);
                toFill.SetUIVertex(vertexLeftLast, vertexCount - 2);
                toFill.SetUIVertex(vertexRightLast, vertexCount - 1);

                AddTwoTriangle(toFill);
            }
            else
            {
                AddTwoVert(toFill, ctrPoint, radius * offsetDir, lineCrt.points[0], ctrPoint);

                offset = -offset;

                OffsetTwoVert(toFill, offset, false);

                AddTwoTriangle(toFill);
            }
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

        Vector3 pointDir = PointDir(prePoint.pos, ctrPoint.pos);
        Vector3 offsetDir = new(-pointDir.y, pointDir.x, 0);

        ReuseTwoVert(toFill, prePoint, ctrPoint);

        Vector3 offset = pointDir * lineCrt.roundRadius;
        vertexLeftLast.uv0.z = vertexRightLast.uv0.z = vertexLeftLast.uv0.z + offset.x;
        vertexLeftLast.uv0.w = vertexRightLast.uv0.w = vertexLeftLast.uv0.w + offset.y;
        Debug.LogError(" ---AddEndQuad--- ab " + vertexLeftLast.uv0.x + "," + vertexRightLast.uv0.z);
        toFill.SetUIVertex(vertexLeftLast, vertexCount - 2);
        toFill.SetUIVertex(vertexRightLast, vertexCount - 1);

        AddTwoVert(toFill, ctrPoint, (ctrPoint.radius + lineCrt.roundRadius) * offsetDir, prePoint, ctrPoint);

        OffsetTwoVert(toFill, offset, true);

        AddTwoTriangle(toFill);
    }

    private void OffsetTwoVert(VertexHelper toFill, Vector3 offset, bool isEnd)
    {
        if (isEnd)
        {
            vertexLeftLast.uv0.z = vertexRightLast.uv0.z = vertexLeftLast.uv0.z + offset.x;
            vertexLeftLast.uv0.w = vertexRightLast.uv0.w = vertexLeftLast.uv0.w + offset.y;
        } else
        {
            vertexLeftLast.uv0.x = vertexRightLast.uv0.x = vertexLeftLast.uv0.x - offset.x;
            vertexLeftLast.uv0.y = vertexRightLast.uv0.y = vertexLeftLast.uv0.y - offset.y;
        }
        
        Debug.LogError("---OffsetTwoVert--- ab " + vertexLeftLast.uv0.x + "," + vertexRightLast.uv0.z);
        vertexLeftLast.position += offset;
        vertexLeftLast.uv2.x = vertexLeftLast.position.x; vertexLeftLast.uv2.y = vertexLeftLast.position.y;
        vertexRightLast.position += offset;
        vertexRightLast.uv2.x = vertexRightLast.position.x; vertexRightLast.uv2.y = vertexRightLast.position.y;
        //Debug.LogError("---OffsetTwoVert--- os " + vertexLeftLast.uv2 + "  -  " + vertexRightLast.uv2);
        vertexLeftLast.uv2.z += lineCrt.roundRadius; vertexRightLast.uv2.z += lineCrt.roundRadius;
        //Debug.LogError("---OffsetTwoVert--- disLeft " + vertexLeftLast.uv2.z + " disRight " + vertexRightLast.uv2.z);
        toFill.SetUIVertex(vertexLeftLast, vertexCount - 2);
        toFill.SetUIVertex(vertexRightLast, vertexCount - 1);
    }

    private void ReuseTwoVert(VertexHelper toFill, PointInfo aPoint, PointInfo bPoint)
    {
        Vector3 a2bDir = PointDir(aPoint.pos, bPoint.pos);
        float cornerCutLen = 0.5f * Mathf.Abs(disLeft - disRight) + lineCrt.roundRadius;
        Vector3 cornerCut = cornerCutLen * a2bDir;

        vertexRightLast.uv0 = vertexLeftLast.uv0 = new Vector4(aPoint.pos.x + cornerCut.x, aPoint.pos.y + cornerCut.y, 
            bPoint.pos.x - cornerCut.x, bPoint.pos.y - cornerCut.y);
        Debug.LogError("--- ReuseTwoVert ab " + vertexLeftLast.uv0.x + "," + vertexRightLast.uv0.z);
        if (disLeft > disRight)
        {
            disLeft = disRight - disLeft;
            disRight = -lineCrt.roundRadius;
        }
        else
        {
            disRight = disLeft - disRight;
            disLeft = 0;
        }

        vertexLeftLast.uv2.z = disLeft;
        vertexRightLast.uv2.z = disRight;
        //Debug.LogError("Reuse disLeft " + disLeft + " disRight " + disRight);
        //Debug.LogError(" ---ReuseTwoVert--- os " + vertexLeftLast.uv2 + "  -  " + vertexRightLast.uv2);
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
        Debug.LogError("AddTwoVert ab " + vertexLeft.uv0.x + "," + vertexLeft.uv0.z);
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
        //Debug.LogError(" AddTwoVert os " + vertexLeft.uv2 + "  -  " + vertexRight.uv2);
        //Debug.LogError("Add disLeft " + disLeft + " disRight " + disRight);
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