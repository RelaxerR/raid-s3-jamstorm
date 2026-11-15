#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OceanSystem))]
public class OceanSystemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space(8);
        var ocean = (OceanSystem)target;

        if (GUILayout.Button("Перестроить океан по настройкам"))
        {
            Undo.RecordObject(ocean, "Rebuild ocean");
            ocean.BuildOcean();
            EditorUtility.SetDirty(ocean);
        }

        if (GUILayout.Button("Создать тестовый плавающий куб"))
            CreateTestCube(ocean);

        EditorGUILayout.HelpBox("Океан — один объект с одной сеткой и материалом. Для тёмного градиента и пены нужна Depth Texture камеры; в URP включите Depth Texture в URP Asset или на Camera.", MessageType.Info);
    }

    private static void CreateTestCube(OceanSystem ocean)
    {
        Vector3 position = ocean.transform.position + Vector3.up * 5f;
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Floating Test Cube";
        Undo.RegisterCreatedObjectUndo(cube, "Create floating test cube");
        cube.transform.position = position;
        cube.transform.localScale = new Vector3(1.4f, 1.0f, 1.4f);

        Rigidbody body = Undo.AddComponent<Rigidbody>(cube);
        body.mass = 1.5f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.useGravity = true;

        OceanBuoyancy buoyancy = Undo.AddComponent<OceanBuoyancy>(cube);
        Undo.RecordObject(buoyancy, "Assign ocean to test cube");
        buoyancy.SetOcean(ocean);
        EditorUtility.SetDirty(buoyancy);
        Selection.activeGameObject = cube;
    }

    [MenuItem("GameObject/OceanPro/Create Ocean System", false, 10)]
    private static void CreateOceanObject(MenuCommand command)
    {
        GameObject go = new GameObject("Ocean System");
        Undo.RegisterCreatedObjectUndo(go, "Create Ocean System");
        GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
        go.AddComponent<OceanSystem>();
        Selection.activeGameObject = go;
    }
}
#endif
