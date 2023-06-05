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
    VertexHelper toFill;
    int vertexCount = 0; LineInfo lineCrt;
    UIVertex vertexLeftLast; UIVertex vertexRightLast;
    float disLeft = 0; float disRight = 0;

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
        if (lineInfo.points.Count < 1)
        {
            return;
        }
        lineCrt = lineInfo;
        
        AddStartQuad();

        for (int i = 1; i < lineCrt.points.Count - 2; i++)
        {
            AddMidQuad(i);
        }

        if (lineCrt.points.Count > 2)
        {
            AddEndQuad();
        }
    }

    private void CreateStartVertex()
    {
        disLeft = disRight = -lineCrt.roundRadius;
        PointInfo ctrPoint = lineCrt.points[0];
        Vector3 pointDir = Vector3.right;
        if (lineCrt.points.Count > 1)
        {
            pointDir = PointDir(ctrPoint.pos, lineCrt.points[1].pos);
        }
        Vector3 thicknessDir = new(-pointDir.y, pointDir.x, 0);
        Vector3 thicknessOffset = thicknessDir * (ctrPoint.radius + lineCrt.roundRadius);
        Vector3 roundOffset = -pointDir * lineCrt.roundRadius;

        UIVertex vertexLeft = UIVertex.simpleVert;
        vertexLeft.position = ctrPoint.pos + thicknessOffset + roundOffset;
        UIVertex vertexRight = vertexLeft;
        vertexRight.position = ctrPoint.pos - thicknessOffset + roundOffset;
        vertexLeftLast = vertexLeft;
        vertexRightLast = vertexRight;
    }

    private void AddStartQuad()
    {
        CreateStartVertex();
        if (lineCrt.points.Count > 1)
        {
            PointInfo ctrPoint = lineCrt.points[0];
            PointInfo nexPoint = lineCrt.points[1];
            Vector3 pointDir = PointDir(ctrPoint.pos, nexPoint.pos);
            Vector3 thicknessDir = new(-pointDir.y, pointDir.x, 0);
            float radius = ctrPoint.radius + lineCrt.roundRadius;

            AddTwoVert(ctrPoint, radius * thicknessDir, ctrPoint, nexPoint);

            Vector3 offset = -pointDir * lineCrt.roundRadius;
            vertexLeftLast.position += offset;
            vertexLeftLast.uv2.x = vertexLeftLast.position.x; vertexLeftLast.uv2.y = vertexLeftLast.position.y;
            vertexRightLast.position += offset;
            vertexRightLast.uv2.x = vertexRightLast.position.x; vertexRightLast.uv2.y = vertexRightLast.position.y;
            vertexLeftLast.uv2.w = vertexRightLast.uv2.w = disLeft = disRight = 0;

            if (lineCrt.points.Count > 2)
            {
                AdjustStart(ctrPoint, nexPoint, offset);

                ctrPoint = lineCrt.points[1];
                Vector3 nexPointDir = PointDir(ctrPoint.pos, lineCrt.points[2].pos);
                AddMidTwoVert(ctrPoint, pointDir, nexPointDir, lineCrt.points[0], ctrPoint);

                AdjustStart(lineCrt.points[0], ctrPoint, offset);

                AddTwoTriangle();
            }
            else
            {
                AdjustStart(ctrPoint, nexPoint, Vector3.zero);

                ctrPoint = lineCrt.points[1];
                AddTwoVert(ctrPoint, radius * thicknessDir, lineCrt.points[0], ctrPoint);

                offset = -offset;

                OffsetTwoVert(offset, false);

                AddTwoTriangle();
            }
        }
        else
        {
            PointInfo ctrPoint = lineCrt.points[0];
            float radius = ctrPoint.radius + lineCrt.roundRadius;
            AddTwoVert(ctrPoint, radius * Vector3.up, ctrPoint, ctrPoint);

            Vector3 offset = Vector3.left * lineCrt.roundRadius;
            vertexLeftLast.position += offset;
            vertexRightLast.position += offset;
            vertexLeftLast.uv0 = vertexRightLast.uv0 = new Vector4(ctrPoint.pos.x - 0.01f, ctrPoint.pos.y, ctrPoint.pos.x, ctrPoint.pos.y);
            vertexLeftLast.uv2.x = vertexLeftLast.position.x; vertexRightLast.uv2.x = vertexRightLast.position.x;
            vertexLeftLast.uv2.w = vertexRightLast.uv2.w = 0;
            UpdateLastTwo();

            AddTwoVert(ctrPoint, radius * Vector3.up, ctrPoint, ctrPoint);
            vertexLeftLast.position -= offset;
            vertexRightLast.position -= offset;
            vertexLeftLast.uv0 = vertexRightLast.uv0 = new Vector4(ctrPoint.pos.x - 0.01f, ctrPoint.pos.y, ctrPoint.pos.x, ctrPoint.pos.y);
            vertexLeftLast.uv2.x = vertexLeftLast.position.x; vertexRightLast.uv2.x = vertexRightLast.position.x;
            vertexLeftLast.uv2.w = vertexRightLast.uv2.w = 0;
            UpdateLastTwo();

            AddTwoTriangle();
        }
    }

    private void AdjustStart(PointInfo ctrPoint, PointInfo nexPoint, Vector3 bOffSet)
    {
        vertexLeftLast.uv0.x = vertexRightLast.uv0.x = ctrPoint.pos.x;
        vertexLeftLast.uv0.y = vertexRightLast.uv0.y = ctrPoint.pos.y;
        float disGap = vertexLeftLast.uv2.w - vertexRightLast.uv2.w;
        Vector3 gapOffset = 0.5f * disGap * bOffSet.normalized;
        if (Mathf.Min(lineCrt.blankStart, lineCrt.blankLen) <= 0)
        {
            gapOffset = -gapOffset;
        }
        vertexLeftLast.uv0.z = vertexRightLast.uv0.z = nexPoint.pos.x + bOffSet.x + gapOffset.x;
        vertexLeftLast.uv0.w = vertexRightLast.uv0.w = nexPoint.pos.y + bOffSet.y + gapOffset.y;
        UpdateLastTwo();
    }

    private void AddMidQuad(int index)
    {
        PointInfo ctrPoint = lineCrt.points[index];
        PointInfo nexPoint = lineCrt.points[index + 1];
        PointInfo nex2Point = lineCrt.points[index + 2];

        ReuseTwoVert(ctrPoint, nexPoint);

        Vector3 pointDir = PointDir(ctrPoint.pos, nexPoint.pos);
        Vector3 nexPointDir = PointDir(nexPoint.pos, nex2Point.pos);
        AddMidTwoVert(nexPoint, pointDir, nexPointDir, ctrPoint, nexPoint);

        AddTwoTriangle();
    }

    private void AddEndQuad()
    {
        PointInfo ctrPoint = lineCrt.points[^1];
        PointInfo prePoint = lineCrt.points[^2];

        Vector3 pointDir = PointDir(prePoint.pos, ctrPoint.pos);
        Vector3 offsetDir = new(-pointDir.y, pointDir.x, 0);

        ReuseTwoVert(prePoint, ctrPoint);

        Vector3 offset = pointDir * lineCrt.roundRadius;
        vertexLeftLast.uv0.z = vertexRightLast.uv0.z = vertexLeftLast.uv0.z + offset.x;
        vertexLeftLast.uv0.w = vertexRightLast.uv0.w = vertexLeftLast.uv0.w + offset.y;
        UpdateLastTwo();

        AddTwoVert(ctrPoint, (ctrPoint.radius + lineCrt.roundRadius) * offsetDir, prePoint, ctrPoint);

        OffsetTwoVert(offset, true);

        AddTwoTriangle();
    }

    private void OffsetTwoVert(Vector3 offset, bool isEnd)
    {
        if (!isEnd)
        {
            vertexLeftLast.uv0.x = vertexRightLast.uv0.x = vertexLeftLast.uv0.x - offset.x;
            vertexLeftLast.uv0.y = vertexRightLast.uv0.y = vertexLeftLast.uv0.y - offset.y;
        }
        vertexLeftLast.uv0.z = vertexRightLast.uv0.z = vertexLeftLast.uv0.z + offset.x;
        vertexLeftLast.uv0.w = vertexRightLast.uv0.w = vertexLeftLast.uv0.w + offset.y;

        vertexLeftLast.position += offset;
        vertexLeftLast.uv2.x = vertexLeftLast.position.x; vertexLeftLast.uv2.y = vertexLeftLast.position.y;
        vertexRightLast.position += offset;
        vertexRightLast.uv2.x = vertexRightLast.position.x; vertexRightLast.uv2.y = vertexRightLast.position.y;
        vertexLeftLast.uv2.w += lineCrt.roundRadius; vertexRightLast.uv2.w += lineCrt.roundRadius;
        UpdateLastTwo();
    }

    private void UpdateLastTwo()
    {
        toFill.SetUIVertex(vertexLeftLast, vertexCount - 2);
        toFill.SetUIVertex(vertexRightLast, vertexCount - 1); DebugVert("+++ UpdateLastTwo", vertexLeftLast, vertexRightLast);
    }

    private void ReuseTwoVert(PointInfo aPoint, PointInfo bPoint)
    {
        Vector3 a2bDir = PointDir(aPoint.pos, bPoint.pos);
        float cornerCutLen = 0.5f * Mathf.Abs(disLeft - disRight) + lineCrt.roundRadius;
        Vector3 cornerCut = cornerCutLen * a2bDir;

        vertexRightLast.uv0 = vertexLeftLast.uv0 = new Vector4(aPoint.pos.x + cornerCut.x, aPoint.pos.y + cornerCut.y, 
            bPoint.pos.x - cornerCut.x, bPoint.pos.y - cornerCut.y);
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

        vertexLeftLast.uv2.w = disLeft;
        vertexRightLast.uv2.w = disRight;
        toFill.AddVert(vertexLeftLast); 
        toFill.AddVert(vertexRightLast); DebugVert("Reuse", vertexLeftLast, vertexRightLast);
        vertexCount += 2;
    }

    private void AddMidTwoVert(PointInfo ctrPoint, Vector3 pointDir, Vector3 nexPointDir,
        PointInfo aPoint, PointInfo bPoint)
    {
        Vector3 pointDirAve = (pointDir + nexPointDir) * 0.5f;
        Vector3 pointDirAve90 = new(-pointDirAve.y, pointDirAve.x, 0);
        float cos = pointDirAve.x * pointDir.x + pointDirAve.y * pointDir.y;
        float zoom = Mathf.Min(1.0f / cos, 99999);
        AddTwoVert(ctrPoint, zoom * (ctrPoint.radius + lineCrt.roundRadius) * pointDirAve90, aPoint, bPoint);
    }

    private void AddTwoVert(PointInfo ctrPoint, Vector3 offset, PointInfo aPoint, PointInfo bPoint)
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
        //ab
        vertexLeft.uv0 = new Vector4(aPoint.pos.x + cornerCut.x, aPoint.pos.y + cornerCut.y,
            bPoint.pos.x - cornerCut.x, bPoint.pos.y - cornerCut.y);
        vertexLeft.uv1 = new Vector4(ctrPoint.radius * 2, lineCrt.blankStart, lineCrt.blankLen, lineCrt.roundRadius);

        UIVertex vertexRight = vertexLeft;
        vertexRight.position = posRight;

        vertexLeft.uv2 = new Vector4(vertexLeft.position.x, vertexLeft.position.y, lineCrt.fadeRadius, disLeft);
        toFill.AddVert(vertexLeft); 
        vertexLeftLast = vertexLeft;

        vertexRight.uv2 = new Vector4(vertexRight.position.x, vertexRight.position.y, lineCrt.fadeRadius, disRight);
        toFill.AddVert(vertexRight); DebugVert("Add", vertexLeft, vertexRight);
        vertexRightLast = vertexRight;
        vertexCount += 2;
    }

    private void DebugVert(string tag, UIVertex vertexLeft, UIVertex vertexRight)
    {
        //Debug.LogError(tag + " ab " + vertexLeft.uv0 + " Dis L " + vertexLeft.uv2.w + " R " + vertexRight.uv2.w);
        //Debug.LogError(tag + " os L( " + vertexLeft.uv0.x + "," + vertexLeft.uv0.y + ") R(" + vertexRight.uv0.x + "," + vertexRight.uv0.y + ")");
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

    private void AddTwoTriangle()
    {
        toFill.AddTriangle(vertexCount - 4, vertexCount - 2, vertexCount - 3);
        toFill.AddTriangle(vertexCount - 2, vertexCount - 1, vertexCount - 3);
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