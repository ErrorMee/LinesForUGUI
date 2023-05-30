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