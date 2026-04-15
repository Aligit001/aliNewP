using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
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

    private RectTransform miniMapCanvas;
    private float mapScale = 2.5f; 

    private static Text notificationText;

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
            CreateMiniMapUI();
            CreateNotificationUI();
            NotifyServerRpc(deviceName + " Joined the world! 🍏");
        }
    }

    void CreateAdvancedNameTag()
    {
        GameObject pivot = new GameObject("PlayerInfoPivot");
        pivot.transform.SetParent(this.transform);
        // رفعنا نقطة الارتكاز قليلاً للأعلى عن جسم اللاعب
        pivot.transform.localPosition = new Vector3(0, 0.015f, 0); 

        float fontScale = IsOwner ? 0.0035f : 0.007f;
        int fontSize = IsOwner ? 35 : 65;

        // 1. نص الـ IP (تم إنزاله للأسفل لزيادة المسافة)
        GameObject ipObj = new GameObject("IPAddress", typeof(TextMesh));
        ipObj.transform.SetParent(pivot.transform);
        ipObj.transform.localPosition = new Vector3(0, -0.004f, 0); // مسافة كافية تحت الاسم
        ipObj.transform.localScale = new Vector3(fontScale, fontScale, fontScale); 
        ipTextMesh = ipObj.GetComponent<TextMesh>();
        ipTextMesh.fontSize = fontSize;
        ipTextMesh.anchor = TextAnchor.MiddleCenter;
        ipTextMesh.color = Color.yellow; 
        ipTextMesh.text = deviceIP;

        // 2. اسم الجهاز (في المنتصف)
        GameObject nameObj = new GameObject("DeviceName", typeof(TextMesh));
        nameObj.transform.SetParent(pivot.transform);
        nameObj.transform.localPosition = Vector3.zero; 
        nameObj.transform.localScale = new Vector3(fontScale * 1.1f, fontScale * 1.1f, fontScale * 1.1f); 
        deviceNameTextMesh = nameObj.GetComponent<TextMesh>();
        deviceNameTextMesh.fontSize = fontSize + 10;
        deviceNameTextMesh.fontStyle = FontStyle.Bold;
        deviceNameTextMesh.anchor = TextAnchor.MiddleCenter;
        deviceNameTextMesh.color = IsOwner ? new Color(0, 1, 1, 0.8f) : Color.white; 
        deviceNameTextMesh.text = (IsOwner ? "🍏 [Dev] " : "") + deviceName;

        // 3. الملصق (تم رفعه للأعلى لزيادة المسافة)
        GameObject spriteObj = new GameObject("EmojiSprite", typeof(SpriteRenderer));
        spriteObj.transform.SetParent(pivot.transform);
        spriteObj.transform.localPosition = new Vector3(0, 0.008f, 0); // مسافة كافية فوق الاسم
        spriteObj.transform.localScale = new Vector3(0.06f, 0.06f, 0.06f); 
        emojiSpriteRenderer = spriteObj.GetComponent<SpriteRenderer>();
        emojiSpriteRenderer.sortingOrder = 100;

        pivot.AddComponent<FaceCamera>();
    }

    void CreateNotificationUI() {
        if (notificationText != null) return;
        Canvas canvas = GameObject.FindFirstObjectByType<Canvas>();
        GameObject notifyObj = new GameObject("NotifyText", typeof(RectTransform), typeof(Text));
        notifyObj.transform.SetParent(canvas.transform, false);
        RectTransform rt = notifyObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, -80);
        rt.sizeDelta = new Vector2(600, 50);
        notificationText = notifyObj.GetComponent<Text>();
        notificationText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        notificationText.fontSize = 22;
        notificationText.alignment = TextAnchor.MiddleCenter;
        notificationText.color = Color.white;
        notificationText.text = "";
    }

    [ServerRpc] void NotifyServerRpc(string message) { NotifyClientRpc(message); }
    [ClientRpc] void NotifyClientRpc(string message) { if(notificationText != null) StartCoroutine(ShowNotify(message)); }
    IEnumerator ShowNotify(string msg) {
        notificationText.text = msg;
        yield return new WaitForSeconds(3f);
        notificationText.text = "";
    }

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

    void CreateMiniMapUI() {
        Canvas canvas = GameObject.FindFirstObjectByType<Canvas>();
        GameObject mapBg = new GameObject("MiniMap", typeof(RectTransform), typeof(Image));
        mapBg.transform.SetParent(canvas.transform, false);
        RectTransform rt = mapBg.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(15, -15);
        rt.sizeDelta = new Vector2(120, 120);
        mapBg.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        GameObject pDot = new GameObject("PD", typeof(RectTransform), typeof(Image));
        pDot.transform.SetParent(mapBg.transform, false);
        pDot.GetComponent<RectTransform>().sizeDelta = new Vector2(8, 8);
        pDot.GetComponent<Image>().color = Color.green;
        miniMapCanvas = rt;
    }

    void Update() {
        if (!IsOwner) return;
        if (joystick != null) {
            Vector3 move = transform.right * joystick.Horizontal + transform.forward * joystick.Vertical;
            controller.Move((move * SpeedMove + transform.up * vFlyInput * FlySpeed) * Time.deltaTime);
        }
        UpdateMiniMap();
    }

    void UpdateMiniMap() {
        if (miniMapCanvas == null) return;
        PlayerMove[] players = GameObject.FindObjectsByType<PlayerMove>(FindObjectsSortMode.None);
        foreach (Transform child in miniMapCanvas) { if (child.name == "ED") Destroy(child.gameObject); }
        foreach (PlayerMove p in players) {
            if (p == this) continue;
            Vector3 diff = p.transform.position - transform.position;
            Vector2 mapPos = new Vector2(diff.x, diff.z) * mapScale;
            if (mapPos.magnitude < 55f) {
                GameObject dot = new GameObject("ED", typeof(RectTransform), typeof(Image));
                dot.transform.SetParent(miniMapCanvas.transform, false);
                RectTransform drt = dot.GetComponent<RectTransform>();
                drt.anchoredPosition = mapPos; drt.sizeDelta = new Vector2(6, 6);
                dot.GetComponent<Image>().color = Color.red;
            }
        }
    }

    void CreateHorizontalEmojiMenu() {
        Canvas canvas = GameObject.FindFirstObjectByType<Canvas>();
        string[] emojis = { "IMG_0354", "IMG_1097", "IMG_1609", "IMG_1652", "IMG_1653", "IMG_1911" }; 
        string[] labels = { "😊", "🌹", "🤔", "🇸🇾", "🫡", "👍" };
        for (int i = 0; i < emojis.Length; i++) {
            string f = emojis[i];
            float x = (i - (emojis.Length / 2.0f) + 0.5f) * 60f;
            Vector2 tc = new Vector2(0.5f, 1f);
            GameObject btn = CreateButton(canvas, "B"+i, labels[i], new Vector2(x, -30), tc, tc, tc, new Vector2(55, 55)); 
            btn.GetComponent<Button>().onClick.AddListener(() => RequestEmojiServerRpc(f));
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
            ipTextMesh.text = deviceIP; 
            deviceNameTextMesh.text = (IsOwner ? "🍏 [Dev] " : "") + deviceName;
        }
    }

    GameObject CreateButton(Canvas canvas, string name, string label, Vector2 pos, Vector2 min, Vector2 max, Vector2 piv, Vector2 size) {
        GameObject b = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        b.transform.SetParent(canvas.transform, false);
        RectTransform r = b.GetComponent<RectTransform>();
        r.anchorMin = min; r.anchorMax = max; r.pivot = piv; r.anchoredPosition = pos; r.sizeDelta = size;
        b.GetComponent<Image>().color = new Color(0,0,0,0.5f);
        GameObject tO = new GameObject("T", typeof(Text));
        tO.transform.SetParent(b.transform, false);
        Text t = tO.GetComponent<Text>();
        t.text = label; t.fontSize = 30; t.alignment = TextAnchor.MiddleCenter;
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

    string GetDeviceIP() {
        try { return Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))?.ToString() ?? "0.0.0.0"; } catch { return "0.0.0.0"; }
    }
}

public class FaceCamera : MonoBehaviour {
    private Transform cam;
    void Start() { if (Camera.main != null) cam = Camera.main.transform; }
    void LateUpdate() { if (cam != null) transform.LookAt(transform.position + cam.forward); }
}
