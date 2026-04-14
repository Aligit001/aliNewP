using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Net; // للحصول على الـ IP
using System.Linq; // لتصفية الـ IP

public class PlayerMove : NetworkBehaviour 
{
    public FixedJoystick joystick;
    public float SpeedMove = 5f;
    public float FlySpeed = 8f;

    private CharacterController controller;
    private float vFlyInput = 0f;

    // متغير لاسم اللاعب
    private GameObject nameTagObject;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // 1. إنشاء الاسم فوق اللاعب برمجياً (لجميع اللاعبين)
        CreateNameTag();

        if (IsOwner)
        {
            joystick = GameObject.FindObjectOfType<FixedJoystick>();
            // إنشاء أزرار الطيران (شغالة وتمام كما في المحادثة السابقة)
            CreateFlightUI();
        }
    }

    // --- دالة إنشاء الاسم فوق اللاعب برمجياً ---
    void CreateNameTag()
    {
        // إنشاء كائن النص
        nameTagObject = new GameObject("PlayerNameTag", typeof(TextMesh));
        nameTagObject.transform.SetParent(this.transform);
        // تموضع الاسم فوق رأس اللاعب
        nameTagObject.transform.localPosition = new Vector3(0, 2.5f, 0);

        TextMesh textMesh = nameTagObject.GetComponent<TextMesh>();
        textMesh.characterSize = 0.1f;
        textMesh.fontSize = 50;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.white;

        // الحصول على اسم الجهاز من الـ IP (مطلوب لـ Tailscale)
        string deviceName = GetDeviceIP();
        textMesh.text = string.IsNullOrEmpty(deviceName) ? "Connecting..." : deviceName;

        // دالة لجعل الاسم يتجه دائماً نحو الكاميرا
        nameTagObject.AddComponent<FaceCamera>();
    }

    // دالة مساعدة للحصول على الـ IP الخاص بالجهاز
    string GetDeviceIP()
    {
        try
        {
            // هذا سيعيد أول IP Address عام غير Loopback
            return Dns.GetHostEntry(Dns.GetHostName())
                .AddressList
                .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                ?.ToString() ?? Dns.GetHostName(); // fallback لاسم المضيف إذا لم يجد IP
        }
        catch
        {
            return "Unknown Player";
        }
    }

    // --- بقية الكود (أزرار وحركة) - لم يتغير ---
    // (تم دمج بقية الكود هنا من المحادثة السابقة لضمان عمل الملف بالكامل عند النسخ)
    
    void CreateFlightUI()
    {
        Canvas canvas = GameObject.FindObjectOfType<Canvas>();
        if (canvas == null) return;
        GameObject upBtn = CreateButton(canvas, "FlyUp", "↑", new Vector2(-100, 250));
        AddEventTrigger(upBtn, EventTriggerType.PointerDown, () => vFlyInput = 1f);
        AddEventTrigger(upBtn, EventTriggerType.PointerUp, () => vFlyInput = 0f);
        GameObject downBtn = CreateButton(canvas, "FlyDown", "↓", new Vector2(-100, 100));
        AddEventTrigger(downBtn, EventTriggerType.PointerDown, () => vFlyInput = -1f);
        AddEventTrigger(downBtn, EventTriggerType.PointerUp, () => vFlyInput = 0f);
    }

    GameObject CreateButton(Canvas canvas, string name, string label, Vector2 pos)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(canvas.transform, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1, 0);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(100, 100);
        btnObj.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txtObj.transform.SetParent(btnObj.transform, false);
        Text t = txtObj.GetComponent<Text>();
        t.text = label; t.fontSize = 50; t.alignment = TextAnchor.MiddleCenter;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.color = Color.white;
        txtObj.GetComponent<RectTransform>().sizeDelta = rt.sizeDelta;
        return btnObj;
    }

    void AddEventTrigger(GameObject obj, EventTriggerType type, System.Action action)
    {
        EventTrigger trigger = obj.GetComponent<EventTrigger>() ?? obj.AddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener((eventData) => action());
        trigger.triggers.Add(entry);
    }

    void Update()
    {
        if (!IsOwner || joystick == null) return;
        Vector3 moveDirection = transform.right * joystick.Horizontal + transform.forward * joystick.Vertical;
        Vector3 flyDirection = transform.up * vFlyInput;
        Vector3 finalMove = moveDirection * SpeedMove + flyDirection * FlySpeed;
        controller.Move(finalMove * Time.deltaTime);
    }
}

// كلاس مساعد لجعل الاسم يتجه للكاميرا دائماً
public class FaceCamera : MonoBehaviour
{
    private Camera mainCamera;
    void Start() { mainCamera = Camera.main; }
    void LateUpdate() { if (mainCamera != null) transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward, mainCamera.transform.rotation * Vector3.up); }
}
