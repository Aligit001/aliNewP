using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Linq;

public class PlayerMove : NetworkBehaviour 
{
    [Header("Settings")]
    public FixedJoystick joystick;
    public float SpeedMove = 5f;
    public float FlySpeed = 8f;

    private CharacterController controller;
    private float vFlyInput = 0f;
    
    private SpriteRenderer emojiSpriteRenderer;
    private TextMesh ipTextMesh; 
    private TextMesh deviceNameTextMesh;
    private string deviceIP;
    private string deviceName;

    private RectTransform miniMapCanvas;
    private float mapScale = 2.5f; 

    private static Text notificationText;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        deviceIP = GetDeviceIP();
        deviceName = SystemInfo.deviceName;

        // --- 1. بناء واجهة البداية الاحترافية (إذا لم تكن موجودة) ---
        if (IsOwner && GameObject.Find("AliMenuCanvas") == null)
        {
            CreateAliProMenu();
        }

        // --- 2. بناء التاج فوق رأس اللاعب مع ضبط المسافات ---
        CreateAdvancedNameTag();

        if (IsOwner)
        {
            joystick = GameObject.FindFirstObjectByType<FixedJoystick>();
            CreateFlightUI();
            CreateHorizontalEmojiMenu();
            CreateMiniMapUI();
            
            // إرسال إشعار دخول للجميع
            NotifyServerRpc(deviceName + " Joined the world! 🍏");
        }
    }

    void CreateAdvancedNameTag()
    {
        GameObject pivot = new GameObject("PlayerInfoPivot");
        pivot.transform.SetParent(this.transform);
        // رفعنا نقطة الارتكاز لمستوى الرأس لضمان عدم التداخل
        pivot.transform.localPosition = new Vector3(0, 2.2f, 0); 

        float fontScale = IsOwner ? 0.08f : 0.12f;
        int fontSize = 50;

        // 1. نص الـ IP (الأسفل) - مسافة كافية
        GameObject ipObj = new GameObject("IPAddress", typeof(TextMesh));
        ipObj.transform.SetParent(pivot.transform);
        ipObj.transform.localPosition = new Vector3(0, -0.6f, 0); 
        ipObj.transform.localScale = new Vector3(fontScale * 0.8f, fontScale * 0.8f, fontScale * 0.8f); 
        ipTextMesh = ipObj.GetComponent<TextMesh>();
        ipTextMesh.fontSize = fontSize;
        ipTextMesh.anchor = TextAnchor.MiddleCenter;
        ipTextMesh.color = Color.yellow; 
        ipTextMesh.text = deviceIP;

        // 2. اسم الجهاز (المنتصف)
        GameObject nameObj = new GameObject("DeviceName", typeof(TextMesh));
        nameObj.transform.SetParent(pivot.transform);
        nameObj.transform.localPosition = Vector3.zero; 
        nameObj.transform.localScale = new Vector3(fontScale, fontScale, fontScale); 
        deviceNameTextMesh = nameObj.GetComponent<TextMesh>();
        deviceNameTextMesh.fontSize = fontSize + 10;
        deviceNameTextMesh.fontStyle = FontStyle.Bold;
        deviceNameTextMesh.anchor = TextAnchor.MiddleCenter;
        deviceNameTextMesh.color = IsOwner ? Color.cyan : Color.white; 
        deviceNameTextMesh.text = (IsOwner ? "🍏 [Dev] " : "") + deviceName;

        // 3. الملصق (الأعلى) - مسافة كافية
        GameObject spriteObj = new GameObject("EmojiSprite", typeof(SpriteRenderer));
        spriteObj.transform.SetParent(pivot.transform);
        spriteObj.transform.localPosition = new Vector3(0, 0.7f, 0); 
        spriteObj.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f); 
        emojiSpriteRenderer = spriteObj.GetComponent<SpriteRenderer>();
        emojiSpriteRenderer.sortingOrder = 100;

        pivot.AddComponent<FaceCamera>();
    }

    // --- واجهة البداية الاحترافية ---
    void CreateAliProMenu()
    {
        GameObject canvasObj = new GameObject("AliMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);

        GameObject bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(canvas.transform, false);
        RectTransform rt = bg.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.01f, 0.01f, 0.05f, 0.98f);

        CreateMenuText(bg.transform, "ALI'S UNIVERSE 🍏", new Vector2(0, 300), 90, Color.cyan);
        
        GameObject hBtn = CreateMenuBtn(bg.transform, "HOST SERVER", new Vector2(0, 60), new Color(0.1f, 0.6f, 0.2f));
        hBtn.GetComponent<Button>().onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            canvasObj.SetActive(false);
        });

        GameObject jBtn = CreateMenuBtn(bg.transform, "JOIN WORLD", new Vector2(0, -100), new Color(0.1f, 0.4f, 0.8f));
        jBtn.GetComponent<Button>().onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
            canvasObj.SetActive(false);
        });

        // إنشاء نظام الإشعارات
        CreateNotificationUI(canvasObj.transform);
    }

    // --- نظام الإشعارات ---
    void CreateNotificationUI(Transform p) {
        GameObject nO = new GameObject("NotifyText", typeof(RectTransform), typeof(Text));
        nO.transform.SetParent(p, false);
        RectTransform rt = nO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, -100);
        notificationText = nO.GetComponent<Text>();
        notificationText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        notificationText.fontSize = 35;
        notificationText.alignment = TextAnchor.MiddleCenter;
        notificationText.color = Color.white;
        nO.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 100);
    }

    [ServerRpc] void NotifyServerRpc(string m) { NotifyClientRpc(m); }
    [ClientRpc] void NotifyClientRpc(string m) { if(notificationText != null) StartCoroutine(ShowNotify(m)); }
    IEnumerator ShowNotify(string msg) {
        notificationText.text = msg;
        yield return new WaitForSeconds(3f);
        notificationText.text = "";
    }

    // --- واجهة الطيران ---
    void CreateFlightUI() {
        Canvas canvas = GameObject.FindFirstObjectByType<Canvas>();
        Vector2 br = new Vector2(1f, 0f);
        float btnS = 90f; float offsetX = -110f; 
        GameObject up = CreateButton(canvas, "Up", "↑", new Vector2(offsetX, 190), br, br, br, new Vector2(btnS, btnS));
        AddEventTrigger(up, EventTriggerType.PointerDown, () => vFlyInput = 1f);
        AddEventTrigger(up, EventTriggerType.PointerUp, () => vFlyInput = 0f);
        GameObject down = CreateButton(canvas, "Down", "↓", new Vector2(offsetX, 80), br, br, br, new Vector2(btnS, btnS));
        AddEventTrigger(down, EventTriggerType.PointerDown, () => vFlyInput = -1f);
        AddEventTrigger(down, EventTriggerType.PointerUp, () => vFlyInput = 0f);
    }

    // --- الرادار (MiniMap) ---
    void CreateMiniMapUI() {
        Canvas canvas = GameObject.FindFirstObjectByType<Canvas>();
        GameObject mapBg = new GameObject("MiniMap", typeof(RectTransform), typeof(Image));
        mapBg.transform.SetParent(canvas.transform, false);
        RectTransform rt = mapBg.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(20, -20);
        rt.sizeDelta = new Vector2(140, 140);
        mapBg.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);
        miniMapCanvas = rt;
    }

    void UpdateMiniMap() {
        if (miniMapCanvas == null) return;
        PlayerMove[] players = GameObject.FindObjectsByType<PlayerMove>(FindObjectsSortMode.None);
        foreach (Transform child in miniMapCanvas) { if (child.name == "ED") Destroy(child.gameObject); }
        foreach (PlayerMove p in players) {
            Vector3 diff = p.transform.position - transform.position;
            Vector2 mapPos = new Vector2(diff.x, diff.z) * mapScale;
            if (mapPos.magnitude < 65f) {
                GameObject dot = new GameObject("ED", typeof(RectTransform), typeof(Image));
                dot.transform.SetParent(miniMapCanvas.transform, false);
                RectTransform drt = dot.GetComponent<RectTransform>();
                drt.anchoredPosition = mapPos; drt.sizeDelta = new Vector2(8, 8);
                drt.GetComponent<Image>().color = (p == this) ? Color.green : Color.red;
            }
        }
    }

    // --- قائمة الملصقات ---
    void CreateHorizontalEmojiMenu() {
        Canvas canvas = GameObject.FindFirstObjectByType<Canvas>();
        string[] emojis = { "IMG_0354", "IMG_1097", "IMG_1609", "IMG_1652", "IMG_1653", "IMG_1911" }; 
        string[] labels = { "😊", "🌹", "🤔", "🇸🇾", "🫡", "👍" };
        for (int i = 0; i < emojis.Length; i++) {
            string f = emojis[i];
            float x = (i - (emojis.Length / 2.0f) + 0.5f) * 65f;
            Vector2 tc = new Vector2(0.5f, 1f);
            GameObject btn = CreateButton(canvas, "B"+i, labels[i], new Vector2(x, -35), tc, tc, tc, new Vector2(60, 60)); 
            btn.GetComponent<Button>().onClick.AddListener(() => RequestEmojiServerRpc(f));
        }
    }

    [ServerRpc] void RequestEmojiServerRpc(string f) { UpdateEmojiClientRpc(f); }
    [ClientRpc] void UpdateEmojiClientRpc(string f) { StopAllCoroutines(); StartCoroutine(ShowEmojiRoutine(f)); }
    IEnumerator ShowEmojiRoutine(string f) {
        Sprite pic = Resources.Load<Sprite>("Emojis/" + f);
        if (pic != null) {
            ipTextMesh.text = ""; deviceNameTextMesh.text = "";
            emojiSpriteRenderer.sprite = pic;
            yield return new WaitForSeconds(4f);
            emojiSpriteRenderer.sprite = null;
            ipTextMesh.text = deviceIP; 
            deviceNameTextMesh.text = (IsOwner ? "🍏 [Dev] " : "") + deviceName;
        }
    }

    // --- أدوات المساعدة (Helpers) ---
    GameObject CreateMenuBtn(Transform p, string txt, Vector2 pos, Color col) {
        GameObject b = new GameObject(txt, typeof(RectTransform), typeof(Image), typeof(Button));
        b.transform.SetParent(p, false);
        RectTransform rt = b.GetComponent<RectTransform>();
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(500, 120);
        b.GetComponent<Image>().color = col;
        GameObject tO = new GameObject("T", typeof(Text));
        tO.transform.SetParent(b.transform, false);
        Text t = tO.GetComponent<Text>();
        t.text = txt; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 40; t.color = Color.white; t.alignment = TextAnchor.MiddleCenter;
        tO.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 120);
        return b;
    }

    void CreateMenuText(Transform p, string c, Vector2 pos, int s, Color col) {
        GameObject tO = new GameObject("T", typeof(Text));
        tO.transform.SetParent(p, false);
        Text t = tO.GetComponent<Text>();
        t.text = c; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = s; t.color = col; t.alignment = TextAnchor.MiddleCenter;
        tO.GetComponent<RectTransform>().anchoredPosition = pos;
        tO.GetComponent<RectTransform>().sizeDelta = new Vector2(1000, 250);
    }

    GameObject CreateButton(Canvas canvas, string name, string label, Vector2 pos, Vector2 min, Vector2 max, Vector2 piv, Vector2 size) {
        GameObject b = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        b.transform.SetParent(canvas.transform, false);
        RectTransform r = b.GetComponent<RectTransform>();
        r.anchorMin = min; r.anchorMax = max; r.pivot = piv; r.anchoredPosition = pos; r.sizeDelta = size;
        b.GetComponent<Image>().color = new Color(0,0,0,0.6f);
        GameObject tO = new GameObject("T", typeof(Text));
        tO.transform.SetParent(b.transform, false);
        Text t = tO.GetComponent<Text>();
        t.text = label; t.fontSize = 35; t.alignment = TextAnchor.MiddleCenter;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.color = Color.white;
        tO.GetComponent<RectTransform>().sizeDelta = size;
        return b;
    }

    void AddEventTrigger(GameObject o, EventTriggerType t, System.Action a) {
        EventTrigger tr = o.GetComponent<EventTrigger>() ?? o.AddComponent<EventTrigger>();
        var e = new EventTrigger.Entry { eventID = t };
        e.callback.AddListener((d) => a());
        tr.triggers.Add(e);
    }

    void Update() {
        if (!IsOwner) return;
        if (joystick != null) {
            Vector3 move = transform.right * joystick.Horizontal + transform.forward * joystick.Vertical;
            controller.Move((move * SpeedMove + transform.up * vFlyInput * FlySpeed) * Time.deltaTime);
        }
        UpdateMiniMap();
    }

    string GetDeviceIP() {
        try { return Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))?.ToString() ?? "0.0.0.0"; } catch { return "0.0.0.0"; }
    }
}

public class FaceCamera : MonoBehaviour {
    private Transform cam;
    void Start() { if (Camera.main != null) cam = Camera.main.transform; }
    void LateUpdate() { if (cam != null) transform.LookAt(transform.position + cam.forward); }
}
