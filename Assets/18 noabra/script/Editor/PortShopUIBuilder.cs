#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Создаёт редактируемый GUI лавки. Меню: 18 Noabra/Создать GUI Портовой лавки.</summary>
public static class PortShopUIBuilder
{
    private static readonly Color BlueBg = new Color(0.12f, 0.62f, 0.85f);
    private static readonly Color DarkTeal = new Color(0.10f, 0.42f, 0.52f);

    [MenuItem("18 Noabra/Создать GUI Портовой лавки")]
    public static void CreateShopGUI()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
        }

        GameObject panel = new GameObject("PortShopPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        panel.GetComponent<Image>().color = BlueBg;
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(900, 650);
        Undo.RegisterCreatedObjectUndo(panel, "Create PortShopPanel");

        GameObject header = CreatePanel("Header", panel.transform, DarkTeal);
        RectTransform hr = header.GetComponent<RectTransform>();
        hr.anchorMin = new Vector2(0, 1); hr.anchorMax = new Vector2(1, 1);
        hr.sizeDelta = new Vector2(0, 90); hr.anchoredPosition = new Vector2(0, -45);

        GameObject exit = CreateButton("ExitButton", header.transform, "выйти", 140, 60, 22);
        RectTransform er = exit.GetComponent<RectTransform>();
        er.anchorMin = new Vector2(0, 0.5f); er.anchorMax = new Vector2(0, 0.5f);
        er.sizeDelta = new Vector2(140, 60); er.anchoredPosition = new Vector2(80, 0);

        GameObject title = new GameObject("Title", typeof(RectTransform));
        title.transform.SetParent(header.transform, false);
        RectTransform tr = title.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0.25f, 0); tr.anchorMax = new Vector2(0.75f, 1);
        tr.offsetMin = tr.offsetMax = Vector2.zero;
        CreateText("Портовая лавка", title.transform, 28, TextAlignmentOptions.Center);

        GameObject moneyGO = new GameObject("MoneyText", typeof(RectTransform));
        moneyGO.transform.SetParent(header.transform, false);
        RectTransform mr = moneyGO.GetComponent<RectTransform>();
        mr.anchorMin = new Vector2(1, 0); mr.anchorMax = new Vector2(1, 1);
        mr.sizeDelta = new Vector2(230, 0); mr.anchoredPosition = new Vector2(-125, 0);
        TextMeshProUGUI moneyText = CreateText("Монеты: 0", moneyGO.transform, 22, TextAlignmentOptions.Center);

        GameObject rowsGO = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup));
        rowsGO.transform.SetParent(panel.transform, false);
        RectTransform rowsRect = rowsGO.GetComponent<RectTransform>();
        rowsRect.anchorMin = Vector2.zero; rowsRect.anchorMax = Vector2.one;
        rowsRect.offsetMin = new Vector2(30, 95); rowsRect.offsetMax = new Vector2(-30, -110);
        VerticalLayoutGroup rowsLayout = rowsGO.GetComponent<VerticalLayoutGroup>();
        rowsLayout.spacing = 12;
        rowsLayout.padding = new RectOffset(10, 10, 10, 10);
        rowsLayout.childAlignment = TextAnchor.UpperCenter;
        rowsLayout.childControlWidth = true;
        rowsLayout.childForceExpandWidth = true;
        rowsLayout.childControlHeight = true;
        rowsLayout.childForceExpandHeight = false;

        GameObject sellAll = CreateButton("SellAllButton", panel.transform, "Продать всё", 320, 60, 24);
        RectTransform sar = sellAll.GetComponent<RectTransform>();
        sar.anchorMin = sar.anchorMax = new Vector2(0.5f, 0);
        sar.sizeDelta = new Vector2(320, 60); sar.anchoredPosition = new Vector2(0, 35);

        GameObject rowPrefab = CreateRowPrefab();
        PortShopUI shopUI = Object.FindObjectOfType<PortShopUI>();
        GameObject shopGO;
        if (shopUI == null)
        {
            shopGO = new GameObject("PortShopUI");
            shopUI = shopGO.AddComponent<PortShopUI>();
            Undo.RegisterCreatedObjectUndo(shopGO, "Create PortShopUI");
        }
        else shopGO = shopUI.gameObject;

        SerializedObject so = new SerializedObject(shopUI);
        so.FindProperty("panelRoot").objectReferenceValue = panel;
        so.FindProperty("rowsParent").objectReferenceValue = rowsGO.transform;
        so.FindProperty("moneyText").objectReferenceValue = moneyText;
        so.FindProperty("rowPrefab").objectReferenceValue = rowPrefab;
        so.ApplyModifiedProperties();

        panel.SetActive(false);
        Selection.activeGameObject = shopGO;
        Debug.Log("GUI создан. Строки лавки теперь на всю ширину; текст не залезает на кнопку.");
    }

    private static GameObject CreateRowPrefab()
    {
        GameObject row = new GameObject("ShopRow", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        row.GetComponent<Image>().color = DarkTeal;
        LayoutElement rowLayout = row.GetComponent<LayoutElement>();
        rowLayout.minHeight = 100; rowLayout.preferredHeight = 100; rowLayout.flexibleHeight = 0; rowLayout.flexibleWidth = 1;

        // Label занимает всю строку слева, оставляя кнопку зарезервированной областью справа.
        GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(row.transform, false);
        RectTransform lr = labelGO.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
        lr.offsetMin = new Vector2(22, 8); lr.offsetMax = new Vector2(-190, -8);
        TextMeshProUGUI label = labelGO.GetComponent<TextMeshProUGUI>();
        label.text = "Дикий лосось  ×1\n25 за шт. · Итого: 25";
        label.fontSize = 24; label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.enableWordWrapping = true; label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;

        GameObject sell = CreateButton("SellButton", row.transform, "продать", 160, 70, 22);
        RectTransform sr = sell.GetComponent<RectTransform>();
        sr.anchorMin = sr.anchorMax = new Vector2(1, 0.5f);
        sr.sizeDelta = new Vector2(160, 70); sr.anchoredPosition = new Vector2(-90, 0);

        string folder = "Assets/18 noabra/prefab";
        if (!AssetDatabase.IsValidFolder("Assets/18 noabra")) AssetDatabase.CreateFolder("Assets", "18 noabra");
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/18 noabra", "prefab");
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(row, folder + "/ShopRow.prefab");
        Object.DestroyImmediate(row);
        return prefab;
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static GameObject CreateButton(string name, Transform parent, string caption, float width, float height, float fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = DarkTeal;
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth = width; le.preferredHeight = height;
        le.flexibleWidth = 0; le.flexibleHeight = 0;
        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(go.transform, false);
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        TextMeshProUGUI text = textGO.GetComponent<TextMeshProUGUI>();
        text.text = caption; text.fontSize = fontSize; text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
        return go;
    }

    private static TextMeshProUGUI CreateText(string value, Transform parent, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = value; tmp.fontSize = fontSize; tmp.color = Color.white;
        tmp.alignment = alignment; tmp.raycastTarget = false;
        return tmp;
    }
}
#endif
