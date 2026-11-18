using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ЛАВКА v7 - фикс размера кнопки + поддержка GUI из сцены.
/// Если GUI создан редактором (PortShopUIBuilder) и ссылки назначены в Inspector -
/// используется GUI из сцены (можно крутить в Hierarchy).
/// Если ссылок нет - GUI строится в коде как раньше (fallback).
/// Кнопка "продать" - ФИКСИРОВАННЫЙ размер 160x70, не растягивается.
/// </summary>
public class PortShopUI : MonoBehaviour
{
    [Header("Ссылки (трюм)")]
    [SerializeField] private CargoHold hold;

    [Header("GUI из сцены (если создан редактором)")]
    [Tooltip("Корень панели лавки. Если пусто - GUI построится в коде")]
    [SerializeField] private GameObject panelRoot;
    [Tooltip("Родитель строк с рыбой")]
    [SerializeField] private Transform rowsParent;
    [Tooltip("Текст монет в шапке")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [Tooltip("Префаб строки рыбы (ShopRow). Если пусто - строка создаётся в коде")]
    [SerializeField] private GameObject rowPrefab;

    [Header("Цены за 1 рыбу (для теста)")]
    [SerializeField] private int defaultPrice = 10;
    [SerializeField] private int salmonPrice = 25;
    [SerializeField] private int sharkPrice = 40;

    [Header("Фикс. размер кнопки 'продать'")]
    [SerializeField] private float sellButtonWidth = 160f;
    [SerializeField] private float sellButtonHeight = 70f;

    private int money;
    private int lastHoldCount = -1;
    private float openAnim = 1f;
    private bool isOpen;
    private bool useSceneUI;

    private static readonly Color BlueBg = new Color(0.12f, 0.62f, 0.85f);
    private static readonly Color DarkTeal = new Color(0.10f, 0.42f, 0.52f);
    private static readonly Color White = Color.white;

    private void Awake()
    {
        if (hold == null) hold = FindObjectOfType<CargoHold>();

        // Если GUI уже есть в сцене (создан редактором) - не строим в коде
        useSceneUI = panelRoot != null && rowsParent != null;
        if (!useSceneUI)
        {
            BuildUI();
        }
        else
        {
            // Подключаем кнопки из сцены (выйти / продать всё), если они есть
            WireSceneButtons();
        }
        Close();
    }

    private void WireSceneButtons()
    {
        // Ищем кнопки по именам, которые создал редактор
        Transform exitT = panelRoot.transform.Find("Header/ExitButton");
        if (exitT != null)
        {
            Button b = exitT.GetComponent<Button>();
            if (b != null) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(Close); }
        }
        Transform sellAllT = panelRoot.transform.Find("SellAllButton");
        if (sellAllT != null)
        {
            Button b = sellAllT.GetComponent<Button>();
            if (b != null) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(SellAll); }
        }
        if (moneyText == null)
        {
            Transform mt = panelRoot.transform.Find("Header/MoneyText");
            if (mt != null) moneyText = mt.GetComponent<TextMeshProUGUI>();
        }
    }

    private void Update()
    {
        if (!isOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape)) Close();

        if (openAnim < 1f)
        {
            openAnim = Mathf.Min(1f, openAnim + Time.deltaTime * 5f);
            float s = Mathf.Lerp(0.85f, 1f, openAnim);
            panelRoot.transform.localScale = new Vector3(s, s, 1f);
        }

        if (hold != null && hold.CurrentFish != lastHoldCount)
        {
            lastHoldCount = hold.CurrentFish;
            Refresh();
        }
    }

    public void Open()
    {
        isOpen = true;
        openAnim = 0f;
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
            panelRoot.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
        }
        lastHoldCount = hold != null ? hold.CurrentFish : -1;
        Refresh();
    }

    public void Close()
    {
        isOpen = false;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void Refresh()
    {
        if (rowsParent == null || hold == null) return;
        foreach (Transform child in rowsParent) Destroy(child.gameObject);

        Dictionary<string, int> counts = hold.GetCountsByType();
        if (counts.Count == 0) CreateEmptyRow();
        else foreach (KeyValuePair<string, int> kv in counts) CreateRow(kv.Key, kv.Value, GetPrice(kv.Key));

        UpdateMoneyText();
        lastHoldCount = hold.CurrentFish;
    }

    private int GetPrice(string typeName)
    {
        string t = typeName.ToLower();
        if (t.Contains("лосос")) return salmonPrice;
        if (t.Contains("акул")) return sharkPrice;
        return defaultPrice;
    }

    private void Sell(string typeName)
    {
        if (hold == null) return;
        int sold = hold.SellType(typeName);
        if (sold > 0) money += sold * GetPrice(typeName);
        Refresh();
    }

    private void SellAll()
    {
        if (hold == null) return;
        Dictionary<string, int> counts = hold.GetCountsByType();
        int total = 0;
        foreach (KeyValuePair<string, int> kv in counts) total += kv.Value * GetPrice(kv.Key);
        hold.SellAll();
        money += total;
        Refresh();
    }

    private void UpdateMoneyText()
    {
        if (moneyText != null) moneyText.text = $"Монеты: {money}";
    }

    // ---------- Строки: из префаба (сцена) или в коде ----------

    private void CreateRow(string typeName, int count, int price)
    {
        if (rowPrefab != null)
        {
            CreateRowFromPrefab(typeName, count, price);
            return;
        }
        CreateRowInCode(typeName, count, price);
    }

    private void CreateRowFromPrefab(string typeName, int count, int price)
    {
        GameObject row = Instantiate(rowPrefab, rowsParent);
        row.name = $"Row_{typeName}";

        // Ищем Label и SellButton внутри префаба
        TextMeshProUGUI label = row.GetComponentInChildren<TextMeshProUGUI>(true);
        // Label - первый TMP, который не внутри кнопки (по имени)
        Transform labelT = row.transform.Find("Label");
        if (labelT != null) label = labelT.GetComponent<TextMeshProUGUI>();

        if (label != null)
            label.text = $"{typeName}  ×{count}\n{price} за шт. · Итого: {price * count}";

        Transform sellT = row.transform.Find("SellButton");
        if (sellT != null)
        {
            Button btn = sellT.GetComponent<Button>();
            if (btn != null)
            {
                string captured = typeName;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => Sell(captured));
            }
            // Фикс. размер кнопки - принудительно, даже если в префабе растянули
            LayoutElement le = sellT.GetComponent<LayoutElement>();
            if (le != null)
            {
                le.preferredWidth = sellButtonWidth;
                le.preferredHeight = sellButtonHeight;
                le.flexibleWidth = 0f;
                le.flexibleHeight = 0f;
            }
        }
    }

    private void CreateRowInCode(string typeName, int count, int price)
    {
        GameObject row = CreatePanel($"Row_{typeName}", rowsParent, DarkTeal);
        row.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 90f);
        LayoutElement le = row.AddComponent<LayoutElement>();
        le.preferredHeight = 90f;
        le.flexibleHeight = 0f;

        HorizontalLayoutGroup h = row.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 10f;
        h.padding = new RectOffset(20, 20, 10, 10);
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childForceExpandWidth = false;  // <-- ФИКС: кнопка не растягивается
        h.childForceExpandHeight = true;

        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(row.transform, false);
        LayoutElement labelLE = labelGO.AddComponent<LayoutElement>();
        labelLE.flexibleWidth = 1f; // текст занимает всё свободное место
        labelLE.flexibleHeight = 1f;
        CreateText($"{typeName}  ×{count}\n{price} за шт. · Итого: {price * count}", labelGO.transform, 22f);

        GameObject sellGO = new GameObject("SellButton");
        sellGO.transform.SetParent(row.transform, false);
        sellGO.AddComponent<Image>().color = DarkTeal;
        LayoutElement sellLE = sellGO.AddComponent<LayoutElement>();
        sellLE.preferredWidth = sellButtonWidth;   // ФИКС. ширина
        sellLE.preferredHeight = sellButtonHeight; // ФИКС. высота
        sellLE.flexibleWidth = 0f;  // не растягиваться!
        sellLE.flexibleHeight = 0f;
        Button sellBtn = sellGO.AddComponent<Button>();
        string capturedType = typeName;
        sellBtn.onClick.AddListener(() => Sell(capturedType));
        CreateText("продать", sellGO.transform, 22f);
    }

    private void CreateEmptyRow()
    {
        if (rowPrefab != null)
        {
            GameObject row = Instantiate(rowPrefab, rowsParent);
            TextMeshProUGUI label = row.GetComponentInChildren<TextMeshProUGUI>(true);
            Transform labelT = row.transform.Find("Label");
            if (labelT != null) label = labelT.GetComponent<TextMeshProUGUI>();
            if (label != null) label.text = "В трюме пока пусто";
            Transform sellT = row.transform.Find("SellButton");
            if (sellT != null) sellT.gameObject.SetActive(false);
            return;
        }
        GameObject emptyRow = CreatePanel("Row_Empty", rowsParent, DarkTeal);
        LayoutElement le = emptyRow.AddComponent<LayoutElement>();
        le.preferredHeight = 90f;
        le.flexibleHeight = 0f;
        CreateText("В трюме пусто. Поймай рыбу на спавнере (клавиша F)", emptyRow.transform, 20f);
    }

    // ---------- Fallback GUI в коде (если в сцене не создан) ----------

    private void BuildUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        panelRoot = new GameObject("PortShopPanel");
        panelRoot.transform.SetParent(canvas.transform, false);
        panelRoot.AddComponent<Image>().color = BlueBg;
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(900f, 650f);
        panelRect.anchoredPosition = Vector2.zero;

        GameObject header = CreatePanel("Header", panelRoot.transform, DarkTeal);
        RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.sizeDelta = new Vector2(0f, 90f);
        headerRect.anchoredPosition = new Vector2(0f, -45f);

        GameObject exitGO = new GameObject("ExitButton");
        exitGO.transform.SetParent(header.transform, false);
        exitGO.AddComponent<Image>().color = DarkTeal;
        RectTransform exitRect = exitGO.GetComponent<RectTransform>();
        exitRect.anchorMin = new Vector2(0f, 0f);
        exitRect.anchorMax = new Vector2(0f, 1f);
        exitRect.sizeDelta = new Vector2(140f, 0f);
        exitRect.anchoredPosition = new Vector2(70f, 0f);
        Button exitBtn = exitGO.AddComponent<Button>();
        exitBtn.onClick.AddListener(Close);
        CreateText("выйти", exitGO.transform, 22f);

        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(header.transform, false);
        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        CreateText("Портовая лавка", titleGO.transform, 28f);

        GameObject moneyGO = new GameObject("MoneyText");
        moneyGO.transform.SetParent(header.transform, false);
        RectTransform moneyRect = moneyGO.AddComponent<RectTransform>();
        moneyRect.anchorMin = new Vector2(1f, 0f);
        moneyRect.anchorMax = new Vector2(1f, 1f);
        moneyRect.sizeDelta = new Vector2(220f, 0f);
        moneyRect.anchoredPosition = new Vector2(-120f, 0f);
        moneyText = CreateText("Монеты: 0", moneyGO.transform, 22f);

        GameObject rowsGO = new GameObject("Rows");
        rowsGO.transform.SetParent(panelRoot.transform, false);
        RectTransform rowsRect = rowsGO.AddComponent<RectTransform>();
        rowsRect.anchorMin = new Vector2(0f, 0f);
        rowsRect.anchorMax = new Vector2(1f, 1f);
        rowsRect.offsetMin = new Vector2(30f, 90f);
        rowsRect.offsetMax = new Vector2(-30f, -120f);
        VerticalLayoutGroup layout = rowsGO.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 18f;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        rowsParent = rowsGO.transform;

        GameObject sellAllGO = new GameObject("SellAllButton");
        sellAllGO.transform.SetParent(panelRoot.transform, false);
        sellAllGO.AddComponent<Image>().color = DarkTeal;
        RectTransform sellAllRect = sellAllGO.GetComponent<RectTransform>();
        sellAllRect.anchorMin = new Vector2(0.5f, 0f);
        sellAllRect.anchorMax = new Vector2(0.5f, 0f);
        sellAllRect.sizeDelta = new Vector2(320f, 60f);
        sellAllRect.anchoredPosition = new Vector2(0f, 45f);
        Button sellAllBtn = sellAllGO.AddComponent<Button>();
        sellAllBtn.onClick.AddListener(SellAll);
        CreateText("Продать всё", sellAllGO.transform, 24f);
    }

    private GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color; // Image сам добавляет RectTransform
        return go;
    }

    private TextMeshProUGUI CreateText(string text, Transform parent, float fontSize)
    {
        GameObject go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = White;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }
}
