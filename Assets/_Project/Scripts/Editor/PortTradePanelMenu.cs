using SecondCatch.Application;
using SecondCatch.UI;
using UnityEditor;
using UnityEngine;

namespace SecondCatch.EditorTools
{
    public static class PortTradePanelMenu
    {
        private const string MenuPath = "Second Catch/Create Port Trade Panel";
        private const string PrefabPath = "Assets/_Project/Prefabs/UI/PortTradePanel.prefab";

        [MenuItem(MenuPath, true)]
        private static bool CanCreatePanel() => Selection.activeGameObject != null
            && !EditorUtility.IsPersistent(Selection.activeGameObject)
            && Selection.activeGameObject.TryGetComponent<HomePortController>(out var port)
            && port.GetComponentInChildren<PortTradePanel>(true) == null;

        [MenuItem(MenuPath)]
        private static void CreatePanel()
        {
            var port = Selection.activeGameObject.GetComponent<HomePortController>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, port.transform);
            using var panel = new SerializedObject(instance.GetComponent<PortTradePanel>());
            panel.FindProperty("_port").objectReferenceValue = port;
            panel.ApplyModifiedPropertiesWithoutUndo();
            Undo.RegisterCreatedObjectUndo(instance, "Create Port Trade Panel");
            Selection.activeGameObject = instance;
        }
    }
}
