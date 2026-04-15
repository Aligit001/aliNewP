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

    // متغيرات الخريطة المصغرة
    private RectTransform miniMapPoint; 
    private RectTransform miniMapCanvas;
    private float mapScale = 2f; // زوم الخريطة

    void Start()
    {
        controller = GetComponent<CharacterController>();
        deviceIP = GetDeviceIP();
        deviceName = SystemInfo.deviceName;

        CreateAdvancedNameTag();

        if (IsOwner)
        {
            joystick = GameObject.FindFirstObjectByType<FixedJoystick>();
            CreateFlightUI();
            CreateHorizontalEmojiMenu();
            CreateMiniMapUI(); // إنشاء الخريطة المصغرة
        }
    }

    void CreateAdvancedNameTag()
    {
        GameObject pivot = new GameObject("PlayerInfoPivot");
        pivot.transform.SetParent(this.transform);
        pivot.transform.localPosition = new Vector3(0, 0.015f, 0); 

        // نص الـ IP
        GameObject ipObj = new GameObject("IPAddress", typeof(TextMesh));
        ipObj.transform.SetParent(pivot.transform);
        ipObj.transform.localPosition = Vector3.zero;
        ipObj.transform.localScale = new Vector3(0.008f, 0.008f, 0.008f); 
        ipTextMesh = ipObj.GetComponent<TextMesh>();
        ipTextMesh.fontSize = 60;
        ipTextMesh.anchor = TextAnchor.MiddleCenter;
        ipTextMesh.color = Color.yellow; 
        ipTextMesh.text = deviceIP;

        // اسم الجهاز + رتبة المطور
        GameObject nameObj = new GameObject("DeviceName", typeof(TextMesh));
        nameObj.transform.SetParent(pivot.transform);
        nameObj.transform.localPosition = new Vector3(0, 0.006f, 0); 
        nameObj.transform.localScale = new Vector3(0.009f, 0.009f, 0.009f); 
        deviceNameTextMesh = nameObj.GetComponent<TextMesh>();
        deviceNameTextMesh.fontSize = 70;
        deviceNameTextMesh.fontStyle = FontStyle.Bold;
        deviceNameTextMesh.anchor = TextAnchor.MiddleCenter;
        deviceNameTextMesh.color = IsOwner ? Color.cyan : Color.white; 
        deviceNameTextMesh.text = (IsOwner ? "[Dev] " : "") + deviceName;

        // الملصق
        GameObject spriteObj = new GameObject("EmojiSprite", typeof(SpriteRenderer));
        spriteObj.transform.SetParent(pivot.transform);
        spriteObj.transform.localPosition = new Vector3(0, 0.014f, 0); 
        spriteObj.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f); 
        emojiSpriteRenderer = spriteObj.GetComponent<SpriteRenderer>();
        emojiSpriteRenderer.sortingOrder = 100;

        pivot.AddComponent<FaceCamera>();
    }

    void CreateMiniMapUI()
    {
        Canvas canvas = GameObject.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        // إطار الخريطة الخلفي
        GameObject mapBg = new GameObject("MiniMap", typeof(RectTransform), typeof(Image));
        mapBg.transform.SetParent(canvas.transform, false);
        RectTransform rt = mapBg.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(20, -20);
        rt.sizeDelta = new Vector2(150, 150);
        mapBg.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);

        // نقطة اللاعب (أنت)
        GameObject playerDot = new GameObject("PlayerDot", typeof(RectTransform), typeof(Image));
        playerDot.transform.SetParent(mapBg.transform, false);
        miniMapPoint = playerDot.GetComponent<RectTransform>();
        miniMapPoint.sizeDelta = new Vector2(10, 10);
        playerDot.GetComponent<Image>().color = Color.green;
        miniMapCanvas = rt;
    }

    void Update()
    {
        if (!IsOwner) return;

        // حركة اللاعب
        if (joystick != null)
        {
            Vector3 move = transform.right * joystick.Horizontal + transform.forward * joystick.Vertical;
            controller.Move((move * SpeedMove + transform.up * vFlyInput * FlySpeed) * Time.deltaTime);
        }

        // تحديث الخريطة المصغرة برمجياً
        UpdateMiniMap();
    }

    void UpdateMiniMap()
    {
        if (miniMapCanvas == null) return;

        // الحصول على كل اللاعبين في اللعبة
        PlayerMove[] allPlayers = GameObject.FindObjectsByType<PlayerMove>(FindObjectsSortMode.None);
        
        // تنظيف النقاط القديمة (باستثناء نقطة اللاعب صاحب الجهاز)
        foreach (Transform child in miniMapCanvas) {
            if (child.name == "EnemyDot") Destroy(child.gameObject);
        }

        foreach (PlayerMove p in allPlayers)
        {
            if (p == this) continue; // تخطي نفسك لأن نقطتك ثابتة في المركز

            // حساب المسافة بينك وبين اللاعب الآخر
            Vector3 diff = p.transform.position - transform.position;
            Vector2 mapPos = new Vector2(diff.x, diff.z) * mapScale;

            // إذا كان اللاعب داخل نطاق الخريطة، ارسمه
            if (mapPos.magnitude < 70f)
            {
                GameObject dot = new GameObject("EnemyDot", typeof(RectTransform), typeof(Image));
                dot.transform.SetParent(miniMapCanvas.transform, false);
                RectTransform dotRt = dot.GetComponent<RectTransform>();
                dotRt.anchoredPosition = mapPos;
                dotRt.sizeDelta = new Vector2(8, 8);
                dot.GetComponent<Image>().color = Color.red;
            }
        }
    }

    // --- بقية الدوال (Emoji, Flight UI, Buttons) تبقى كما هي مع تحديث بسيط للأماكن ---
    
    void CreateHorizontalEmojiMenu()
    {
        Canvas canvas = GameObject.FindFirstObjectByType<Canvas>();
        string[] myEmojis = { "IMG_0354", "IMG_1097", "IMG_1609", "IMG_1652", "IMG_1653", "IMG_1911" }; 
        string[] emojiLabels = { "😊", "🌹", "🤔", "🇸🇾", "🫡", "👍" };
        for (int i = 0; i < myEmojis.Length; i++) {
            string fileName = myEmojis[i];
            float xPos = (i - (myEmojis.Length / 2.0f) + 0.5f) * 55f;
            Vector2 topCenter = new Vector2(0.5f, 1f);
            GameObject btn = CreateButton(canvas, "Btn_" + i, emojiLabels[i], new Vector2(xPos, -25), topCenter, topCenter, topCenter, new Vector2(50, 50)); 
            btn.GetComponent<Button>().onClick.AddListener(() => RequestEmojiServerRpc(fileName));
        }
    }

    [ServerRpc] void RequestEmojiServerRpc(string f) { UpdateEmojiClientRpc(f); }
    [ClientRpc] void UpdateEmojiClientRpc(string f) { StopAllCoroutines(); StartCoroutine(ShowEmojiRoutine(f)); }

    IEnumerator ShowEmojiRoutine(string f) {
        Sprite pic = Resources.Load<Sprite>("Emojis/" + f);
        if (pic == null) {
            Texture2D tex = Resources.Load<Texture2D>("Emojis/" + f);
            if (tex != null) pic = Sprite.Create(tex, new Rect(0,0,tex.width,tex.height), new Vector2(0.5f,0.5f));
        }
        if (pic != null) {
            ipTextMesh.text = ""; deviceNameTextMesh.text = "";
            emojiSpriteRenderer.sprite = pic;
            yield return new WaitForSeconds(4f);
            emojiSpriteRenderer.sprite = null;
            ipTextMesh.text = deviceIP; deviceNameTextMesh.text = (IsOwner ? "[Dev] " : "") + deviceName;
        }
    }

    void CreateFlightUI() {
        Canvas canvas = GameObject.FindFirstObjectByType<Canvas>();
        Vector2 br = new Vector2(1f, 0f);
        GameObject up = CreateButton(canvas, "Up", "↑", new Vector2(-25, 120), br, br, br, new Vector2(70, 70));
        AddEventTrigger(up, EventTriggerType.PointerDown, () => vFlyInput = 1f);
        AddEventTrigger(up, EventTriggerType.PointerUp, () => vFlyInput = 0f);
        GameObject down = CreateButton(canvas, "Down", "↓", new Vector2(-25, 40), br, br, br, new Vector2(70, 70));
        AddEventTrigger(down, EventTriggerType.PointerDown, () => vFlyInput = -1f);
        AddEventTrigger(down, EventTriggerType.PointerUp, () => vFlyInput = 0f);
    }

    GameObject CreateButton(Canvas canvas, string name, string label, Vector2 pos, Vector2 min, Vector2 max, Vector2 piv, Vector2 size) {
        GameObject b = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        b.transform.SetParent(canvas.transform, false);
        RectTransform r = b.GetComponent<RectTransform>();
        r.anchorMin = min; r.anchorMax = max; r.pivot = piv; r.anchoredPosition = pos; r.sizeDelta = size;
        b.GetComponent<Image>().color = new Color(0,0,0,0.5f);
        GameObject tObj = new GameObject("T", typeof(Text));
        tObj.transform.SetParent(b.transform, false);
        Text t = tObj.GetComponent<Text>();
        t.text = label; t.fontSize = 25; t.alignment = TextAnchor.MiddleCenter;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.color = Color.white;
        tObj.GetComponent<RectTransform>().sizeDelta = size;
        return b;
    }

    void AddEventTrigger(GameObject o, EventTriggerType t, System.Action a) {
        EventTrigger tr = o.GetComponent<EventTrigger>() ?? o.AddComponent<EventTrigger>();
        var e = new EventTrigger.Entry { eventID = t };
        e.callback.AddListener((d) => a());
        tr.triggers.Add(e);
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
