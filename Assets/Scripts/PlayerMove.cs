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

        // إنشاء نظام المعلومات فوق الرأس
        CreateAdvancedNameTag();

        if (IsOwner)
        {
            joystick = GameObject.FindFirstObjectByType<FixedJoystick>();
            CreateFlightUI();
            CreateHorizontalEmojiMenu(); 
        }
    }

    void CreateAdvancedNameTag()
    {
        // هذا الكائن هو "الأب" لكل شيء فوق رأس اللاعب
        GameObject pivot = new GameObject("PlayerInfoPivot");
        pivot.transform.SetParent(this.transform);
        
        // تعديل الارتفاع: غيرناه من 2.0 إلى 0.15 ليكون قريباً جداً من الرأس
        pivot.transform.localPosition = new Vector3(0, 0.15f, 0); 

        // إعداد نص الـ IP
        GameObject textObj = new GameObject("IPAddress", typeof(TextMesh));
        textObj.transform.SetParent(pivot.transform);
        textObj.transform.localPosition = Vector3.zero;
        
        // تقليل الـ Scale أكثر ليناسب الحجم الجديد للاعب
        textObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f); 
        
        nameTextMesh = textObj.GetComponent<TextMesh>();
        nameTextMesh.characterSize = 1f;
        nameTextMesh.fontSize = 80; 
        nameTextMesh.anchor = TextAnchor.MiddleCenter;
        nameTextMesh.alignment = TextAlignment.Center;
        nameTextMesh.color = Color.yellow; // لون مميز ليكون أوضح
        nameTextMesh.text = deviceIP;

        // إعداد الملصق (Emoji)
        GameObject spriteObj = new GameObject("EmojiSprite", typeof(SpriteRenderer));
        spriteObj.transform.SetParent(pivot.transform);
        
        // المسافة بين الملصق والنص (جعلناها 0.08 بدلاً من 1.0)
        spriteObj.transform.localPosition = new Vector3(0, 0.08f, 0); 
        
        // تصغير حجم الملصق ليتناسب مع اللاعب
        spriteObj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f); 
        
        emojiSpriteRenderer = spriteObj.GetComponent<SpriteRenderer>();
        emojiSpriteRenderer.sortingOrder = 100;

        // إضافة سكربت مواجهة الكاميرا ليبقى النص والملصق أمامك دائماً
        pivot.AddComponent<FaceCamera>();
    }

    void CreateHorizontalEmojiMenu()
    {
        Canvas canvas = GameObject.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;
        
        string[] myEmojis = { "IMG_0354", "IMG_1097", "IMG_1609", "IMG_1652", "IMG_1653", "IMG_1911" }; 
        string[] emojiLabels = { "😊", "🌹", "🤔", "🇸🇾", "🫡", "👍" };
        
        float buttonSize = 50f; // صغرنا حجم الزر قليلاً
        float spacing = 55f;    // المسافة بين الأزرار
        
        for (int i = 0; i < myEmojis.Length; i++) {
            string fileName = myEmojis[i];
            float xPos = (i - (myEmojis.Length / 2.0f) + 0.5f) * spacing;

            // وضع الأزرار في أعلى الشاشة (Y = -30 ليكون بعيداً عن حافة الهاتف)
            GameObject btn = CreateButton(canvas, "Btn_" + i, emojiLabels[i], new Vector2(xPos, -30)); 
            
            RectTransform rt = btn.GetComponent<RectTransform>();
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
        Sprite pic = Resources.Load<Sprite>("Emojis/" + fileName);
        if (pic == null) {
            Texture2D tex = Resources.Load<Texture2D>("Emojis/" + fileName);
            if (tex != null) pic = Sprite.Create(tex, new Rect(0,0,tex.width,tex.height), new Vector2(0.5f,0.5f));
        }

        if (pic != null) {
            nameTextMesh.text = ""; 
            emojiSpriteRenderer.sprite = pic;
            yield return new WaitForSeconds(4f); 
            emojiSpriteRenderer.sprite = null; 
            nameTextMesh.text = deviceIP; 
        }
    }

    void CreateFlightUI() {
        Canvas canvas = GameObject.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        // أزرار الطيران أسفل اليمين لكي لا تزحم الشاشة
        GameObject upBtn = CreateButton(canvas, "FlyUp", "↑", new Vector2(-60, 180));
        AddEventTrigger(upBtn, EventTriggerType.PointerDown, () => vFlyInput = 1f);
        AddEventTrigger(upBtn, EventTriggerType.PointerUp, () => vFlyInput = 0f);
        
        GameObject downBtn = CreateButton(canvas, "FlyDown", "↓", new Vector2(-60, 70));
        AddEventTrigger(downBtn, EventTriggerType.PointerDown, () => vFlyInput = -1f);
        AddEventTrigger(downBtn, EventTriggerType.PointerUp, () => vFlyInput = 0f);
    }

    GameObject CreateButton(Canvas canvas, string name, string label, Vector2 pos) {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(canvas.transform, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos; 
        rt.sizeDelta = new Vector2(80, 80);
        btnObj.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        
        GameObject txtObj = new GameObject("Text", typeof(Text));
        txtObj.transform.SetParent(btnObj.transform, false);
        Text t = txtObj.GetComponent<Text>();
        t.text = label; t.fontSize = 25; t.alignment = TextAnchor.MiddleCenter;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.color = Color.white;
        txtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 80);
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

public class FaceCamera : MonoBehaviour {
    private Transform cam;
    void Start() { if (Camera.main != null) cam = Camera.main.transform; }
    void LateUpdate() { if (cam != null) transform.LookAt(transform.position + cam.forward); }
}
