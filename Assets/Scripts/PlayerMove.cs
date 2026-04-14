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
    private TextMesh nameTextMesh; 
    private string deviceIP;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        deviceIP = GetDeviceIP();

        // إنشاء الـ NameTag بارتفاع منخفض جداً (1.5) ليكون فوق الرأس مباشرة
        CreateAdvancedNameTag();

        if (IsOwner)
        {
            joystick = GameObject.FindObjectOfType<FixedJoystick>();
            CreateFlightUI();
            CreateEmojiMenu(); 
        }
    }

    void CreateAdvancedNameTag()
    {
        // تم خفض الارتفاع من 2.7 إلى 1.5 ليكون ملتصقاً بالرأس
        GameObject pivot = new GameObject("PlayerInfoPivot");
        pivot.transform.SetParent(this.transform);
        pivot.transform.localPosition = new Vector3(0, 1.5f, 0); 

        // النص (IP)
        GameObject textObj = new GameObject("IPAddress", typeof(TextMesh));
        textObj.transform.SetParent(pivot.transform);
        textObj.transform.localPosition = Vector3.zero;
        nameTextMesh = textObj.GetComponent<TextMesh>();
        nameTextMesh.characterSize = 0.07f; // تصغير الخط قليلاً
        nameTextMesh.fontSize = 50;
        nameTextMesh.anchor = TextAnchor.MiddleCenter;
        nameTextMesh.text = deviceIP;

        // الملصق (Sprite) - حجم صغير ومناسب جداً
        GameObject spriteObj = new GameObject("EmojiSprite", typeof(SpriteRenderer));
        spriteObj.transform.SetParent(pivot.transform);
        spriteObj.transform.localPosition = new Vector3(0, 0.4f, 0); 
        // تصغير الحجم ليكون متناسقاً مع الشخصية
        spriteObj.transform.localScale = new Vector3(0.1f, 0.1f, 1f); 
        emojiSpriteRenderer = spriteObj.GetComponent<SpriteRenderer>();

        pivot.AddComponent<FaceCamera>();
    }

    void CreateEmojiMenu()
    {
        Canvas canvas = GameObject.FindObjectOfType<Canvas>();
        
        // هذه الأسماء مطابقة تماماً لما في صورتك على GitHub (image_14.png)
        // ملاحظة: يونيتي لا يحتاج لكتابة .jpeg في الكود عند التحميل من Resources
        string[] myEmojis = { "IMG_0354", "IMG_1097", "IMG_1609", "IMG_1652", "IMG_1653", "IMG_1911" }; 
        string[] emojiLabels = { "🦶", "🖕", "🤔", "🗿", "😮", "😡" }; // مسميات للأزرار
        
        for (int i = 0; i < myEmojis.Length; i++) {
            string fileName = myEmojis[i];
            string label = emojiLabels[i];

            GameObject btn = CreateButton(canvas, "Btn_" + i, label, new Vector2(100, 100 + (i * 110)));
            RectTransform rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 0); // أسفل اليسار
            rt.anchoredPosition = new Vector2(80, 80 + (i * 105));
            rt.sizeDelta = new Vector2(85, 85);

            btn.GetComponent<Button>().onClick.AddListener(() => {
                RequestEmojiServerRpc(fileName);
            });
        }
    }

    [ServerRpc]
    void RequestEmojiServerRpc(string fileName) { UpdateEmojiClientRpc(fileName); }

    [ClientRpc]
    void UpdateEmojiClientRpc(string fileName) {
        StopAllCoroutines();
        StartCoroutine(ShowEmojiRoutine(fileName));
    }

    IEnumerator ShowEmojiRoutine(string fileName) {
        // تحميل الصورة من Resources/Emojis/
        Sprite pic = Resources.Load<Sprite>("Emojis/" + fileName);
        if (pic != null) {
            nameTextMesh.text = ""; 
            emojiSpriteRenderer.sprite = pic;
            yield return new WaitForSeconds(5f); 
            emojiSpriteRenderer.sprite = null; 
            nameTextMesh.text = deviceIP; 
        }
    }

    // --- الحركة والأزرار الجانبية (بقية الكود) ---
    void CreateFlightUI() {
        Canvas canvas = GameObject.FindObjectOfType<Canvas>();
        GameObject upBtn = CreateButton(canvas, "FlyUp", "↑", new Vector2(-100, 250));
        AddEventTrigger(upBtn, EventTriggerType.PointerDown, () => vFlyInput = 1f);
        AddEventTrigger(upBtn, EventTriggerType.PointerUp, () => vFlyInput = 0f);
        GameObject downBtn = CreateButton(canvas, "FlyDown", "↓", new Vector2(-100, 100));
        AddEventTrigger(downBtn, EventTriggerType.PointerDown, () => vFlyInput = -1f);
        AddEventTrigger(downBtn, EventTriggerType.PointerUp, () => vFlyInput = 0f);
    }

    GameObject CreateButton(Canvas canvas, string name, string label, Vector2 pos) {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(canvas.transform, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1, 0); 
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(100, 100);
        btnObj.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);
        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txtObj.transform.SetParent(btnObj.transform, false);
        Text t = txtObj.GetComponent<Text>();
        t.text = label; t.fontSize = 35; t.alignment = TextAnchor.MiddleCenter;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.color = Color.white; txtObj.GetComponent<RectTransform>().sizeDelta = rt.sizeDelta;
        return btnObj;
    }

    void AddEventTrigger(GameObject obj, EventTriggerType type, System.Action action) {
        EventTrigger trigger = obj.GetComponent<EventTrigger>() ?? obj.AddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener((data) => action());
        trigger.triggers.Add(entry);
    }

    void Update() {
        if (!IsOwner || joystick == null) return;
        Vector3 move = transform.right * joystick.Horizontal + transform.forward * joystick.Vertical;
        controller.Move((move * SpeedMove + transform.up * vFlyInput * FlySpeed) * Time.deltaTime);
    }

    string GetDeviceIP() {
        try { return Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))?.ToString() ?? "Player"; } catch { return "Player"; }
    }
}
