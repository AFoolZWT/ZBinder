using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(RectTransform))]
public class CustomRectTransformEditor : DecoratorEditor
{
    public static ZBinder zBinder;
    public CustomRectTransformEditor() : base("RectTransformEditor") { }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        RectTransform transform = target as RectTransform;
        if (zBinder == null)
            return;
        if (zBinder.transform == transform)
            return;
       
        zBinder.editorToggle.TryGetValue(transform, out bool value);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.Toggle("Binding Lua", value);
        if (EditorGUI.EndChangeCheck())
        {
            zBinder.BindTo(transform.gameObject);
            EditorUtility.SetDirty(target);
        }
    }
}


[CustomEditor(typeof(ZBinder))]
public class ZBinderEditor : Editor
{
    public override void OnInspectorGUI()
    {

        DrawDefaultInspector();

        ZBinder binder = (ZBinder)target;

        if (GUILayout.Button("自动绑定"))
        {
            ZBinder.BindTo(binder);
        }
    }

    public override VisualElement CreateInspectorGUI()
    {
        ZBinder binder = (ZBinder)target;
        if (CustomRectTransformEditor.zBinder != binder)
        {
            binder.FormatEditorToggle();
            CustomRectTransformEditor.zBinder = binder;
        }
        return base.CreateInspectorGUI();
    }
}