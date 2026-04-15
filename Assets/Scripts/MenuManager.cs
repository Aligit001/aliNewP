using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    private Canvas canvas;
    private GameObject menuPanel;

    void Start()
    {
        CreateModernMenu();
    }

    void CreateModernMenu()
    {
        // 1. إنشاء الكانفاس الأساسي
        GameObject canvasObj = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        // 2. إنشاء الخلفية (لوحة شفافة أنيقة)
        menuPanel = new GameObject("MenuPanel", typeof(RectTransform), typeof(Image));
        menuPanel.transform.SetParent(canvas.transform, false);
        RectTransform panelRt = menuPanel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero; panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero; panelRt.offsetMax = Vector2.zero;
        
        // لون خلفية غامق وشبه شفاف (Dark Overlay)
        menuPanel.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.9f);

        // 3. عنوان اللعبة (Title)
        CreateText(menuPanel.transform, "GameTitle", "ALI'S UNIVERSE", new Vector2(0, 200), 80, Color.cyan, FontStyle.Bold);

        // 4. أزرار التحكم (Host / Join)
        float btnWidth = 350f;
        float btnHeight = 80f;

        GameObject hostBtn = CreateModernButton(menuPanel.transform, "HostBtn", "START SERVER (HOST)", new Vector2(0, 40), new Vector2(btnWidth, btnHeight), new Color(0.1f, 0.6f, 0.1f));
        hostBtn.GetComponent<Button>().onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            menuPanel.SetActive(false);
        });

        GameObject joinBtn = CreateModernButton(menuPanel.transform, "JoinBtn", "JOIN WORLD (CLIENT)", new Vector2(0, -60), new Vector2(btnWidth, btnHeight), new Color(0.1f, 0.4f, 0.8f));
        joinBtn.GetComponent<Button>().onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
            menuPanel.SetActive(false);
        });

        // 5. معلومة إصدار اللعبة واسم الجهاز في الزاوية
        CreateText(menuPanel.transform, "DeviceInfo", "Device: " + SystemInfo.deviceName + "\nVer: 1.0.4-🍏", new Vector2(20, 20), 20, new Color(0.7f, 0.7f, 0.7f), FontStyle.Normal, TextAnchor.LowerLeft);
    }

    GameObject CreateModernButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color btnColor)
    {
        // إنشاء جسم الزر مع زوايا ناعمة (باستخدام Image بسيطة)
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        // تصميم الزر
        Image img = btnObj.GetComponent<Image>();
        img.color = btnColor;
        
        // إضافة نص داخل الزر
        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txtObj.transform.SetParent(btnObj.transform, false);
        Text t = txtObj.GetComponent<Text>();
        t.text = label;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 28;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;

        return btnObj;
    }

    void CreateText(Transform parent, string name, string content, Vector2 pos, int size, Color color, FontStyle style, TextAnchor anchor = TextAnchor.MiddleCenter)
    {
        GameObject txtObj = new GameObject(name, typeof(RectTransform), typeof(Text));
        txtObj.transform.SetParent(parent, false);
        Text t = txtObj.GetComponent<Text>();
        t.text = content;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.color = color;
        t.fontStyle = style;
        t.alignment = anchor;

        RectTransform rt = txtObj.GetComponent<RectTransform>();
        if (anchor == TextAnchor.MiddleCenter) {
            rt.anchoredPosition = pos;
        } else {
            // للتعامل مع الـ UI في الزوايا
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = pos;
        }
        rt.sizeDelta = new Vector2(800, 200);
    }
}
