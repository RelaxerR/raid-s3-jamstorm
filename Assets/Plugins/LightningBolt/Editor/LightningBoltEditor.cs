#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LightningBolt))]
public class LightningBoltEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Утилиты редактора", EditorStyles.boldLabel);

        if (GUILayout.Button("Создать недостающие точки молнии"))
            CreateMissingPoints();

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Запустить молнию сейчас (Play Mode)"))
                ((LightningBolt)target).Strike();
        }

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Тестовый запуск доступен во время Play Mode. Убедитесь, что установлен пакет Input System и Active Input Handling настроен на Input System Package (New) или Both.", MessageType.Info);
    }

    private void CreateMissingPoints()
    {
        var bolt = (LightningBolt)target;
        var so = new SerializedObject(bolt);
        so.Update();

        CreatePointIfMissing(so, bolt.transform, "startPoint", "Lightning Start", Vector3.left);
        CreatePointIfMissing(so, bolt.transform, "endPoint", "Lightning End", Vector3.right);

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(bolt);
    }

    private static void CreatePointIfMissing(SerializedObject so, Transform parent, string propertyName, string objectName, Vector3 offset)
    {
        var property = so.FindProperty(propertyName);
        if (property == null || property.objectReferenceValue != null)
            return;

        var point = new GameObject(objectName);
        Undo.RegisterCreatedObjectUndo(point, "Create lightning point");
        point.transform.SetParent(parent, false);
        point.transform.localPosition = offset;
        property.objectReferenceValue = point.transform;
    }
}
#endif
