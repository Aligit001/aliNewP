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

        // إنشاء اسم اللاعب والملصق
        CreateAdvancedNameTag();

        if (IsOwner)
        {
            joystick = GameObject.FindFirstObjectByType<FixedJoystick>();
            CreateFlightUI();
            CreateHorizontalEmojiMenu(); // الأزرار الأفقية
        }
    }

    void CreateAdvancedNameTag()
    {
        GameObject pivot = new GameObject("PlayerInfoPivot");
        pivot.transform.SetParent(this.transform);
        pivot.transform.localPosition = new Vector3(0, 2.0f, 0); // رفعناه قليلاً ليكون فوق الرأس

        // إعداد نص الـ IP برمجياً ليكون واضحاً
        GameObject textObj = new GameObject("IPAddress", typeof(TextMesh));
        textObj.transform.SetParent(pivot.transform);
        textObj.transform.localPosition = Vector3.zero;
        
        // تصغير الحجم جداً برمجياً لأن النصوص ثلاثية الأبعاد تكون ضخمة
        textObj.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f); 
        
        nameTextMesh = textObj.GetComponent<TextMesh>();
        nameTextMesh.characterSize = 1f;
        nameTextMesh.fontSize = 100; // خط كبير مع Scale صغير يعطي دقة عالية
        nameTextMesh.anchor = TextAnchor.MiddleCenter;
        nameTextMesh.alignment = TextAlignment.Center;
        nameTextMesh.text = deviceIP;

        // إعداد الملصق (Emoji)
        GameObject spriteObj = new GameObject("EmojiSprite", typeof(SpriteRenderer));
        spriteObj.transform.SetParent(pivot.transform);
        spriteObj.transform.localPosition = new Vector3(0, 1.0f, 0); // فوق النص
        spriteObj.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f); 
        
        emojiSpriteRenderer = spriteObj.GetComponent<SpriteRenderer>();
        emojiSpriteRenderer.sortingOrder = 100; // لضمان ظهوره فوق كل شيء

        // إضافة سكربت مواجهة الكاميرا
        pivot.AddComponent<FaceCamera>();
    }

    // دالة جديدة لترتيب الأزرار أفقياً في أعلى الشاشة
    void CreateHorizontalEmojiMenu()
    {
        Canvas canvas = GameObject.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;
        
        string[] myEmojis = { "IMG_0354", "IMG_1097", "IMG_1609", "IMG_1652", "IMG_1653", "IMG_1911" }; 
        string[] emojiLabels = { "😊", "🌹", "🤔", "🇸🇾", "🫡", "👍" };
        
        float buttonSize = 70f; // حجم الزر
        float spacing = 80f;    // المسافة بين الأزرار
        
        for (int i = 0; i < myEmojis.Length; i++) {
            string fileName = myEmojis[i];
            
            // حساب الموقع ليكونوا في المنتصف أفقياً
            float xPos = (i - (myEmojis.Length / 2.0f) + 0.5f) * spacing;

            GameObject btn = CreateButton(canvas, "Btn_" + i, emojiLabels[i], new Vector2(xPos, -40)); // -40 ليكون أسفل الحافة العلوية قليلاً
            
            RectTransform rt = btn.GetComponent<RectTransform>();
            // تثبيت الأزرار في أعلى المنتصف
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(buttonSize, buttonSize);

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
        // محاولة تحميل الصورة كـ Sprite أولاً
        Sprite pic = Resources.Load<Sprite>("Emojis/" + fileName);
        
        // إذا فشل (بسبب إعدادات الإمبورت في السيرفر)، نحملها كـ Texture ونحولها
        if (pic == null) {
            Texture2D tex = Resources.Load<Texture2D>("Emojis/" + fileName);
            if (tex != null) {
                pic = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }

        if (pic != null) {
            nameTextMesh.text = ""; 
            emojiSpriteRenderer.sprite = pic;
            yield return new WaitForSeconds(5f); 
            emojiSpriteRenderer.sprite = null; 
            nameTextMesh.text = deviceIP; 
        } else {
            // كود لاختبار إذا كانت الصورة غير موجودة نهائياً
            nameTextMesh.text = "الصورة مفقودة!";
            yield return new WaitForSeconds(2f);
            nameTextMesh.text = deviceIP;
        }
    }

    void CreateFlightUI() {
        Canvas canvas = GameObject.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        // وضعنا أزرار الطيران على اليمين لكي لا تتداخل مع الـ Joystick أو الملصقات
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
        rt.anchorMin = new Vector2(1, 0); // الافتراضي أسفل اليمين (لأزرار الطيران)
        rt.anchorMax = new Vector2(1, 0); 
        rt.anchoredPosition = pos; 
        rt.sizeDelta = new Vector2(100, 100);
        
        btnObj.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);
        
        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txtObj.transform.SetParent(btnObj.transform, false);
        
        Text t = txtObj.GetComponent<Text>();
        t.text = label; 
        t.fontSize = 35; 
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.color = Color.white; 
        
        txtObj.GetComponent<RectTransform>().sizeDelta = rt.sizeDelta;
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

// السكربت المساعد لالتفاف الـ IP نحو الكاميرا
public class FaceCamera : MonoBehaviour
{
    private Transform cam;
    void Start() { 
        if (Camera.main != null) cam = Camera.main.transform; 
    }
    void LateUpdate() {
        if (cam != null) {
            transform.LookAt(transform.position + cam.forward);
        }
    }
}
