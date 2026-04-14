using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlayerMove : NetworkBehaviour 
{
    public FixedJoystick joystick;
    public float SpeedMove = 5f;
    public float FlySpeed = 8f;

    private CharacterController controller;
    private float vFlyInput = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (IsOwner)
        {
            joystick = GameObject.FindObjectOfType<FixedJoystick>();
            
            // استدعاء دالة إنشاء الأزرار تلقائياً
            CreateFlightUI();
        }
    }

    // --- دالة إنشاء الأزرار برمجياً ---
    void CreateFlightUI()
    {
        Canvas canvas = GameObject.FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // إنشاء زر الصعود (↑)
        GameObject upBtn = CreateButton(canvas, "FlyUp", "↑", new Vector2(-100, 250));
        AddEventTrigger(upBtn, EventTriggerType.PointerDown, () => vFlyInput = 1f);
        AddEventTrigger(upBtn, EventTriggerType.PointerUp, () => vFlyInput = 0f);

        // إنشاء زر النزول (↓)
        GameObject downBtn = CreateButton(canvas, "FlyDown", "↓", new Vector2(-100, 100));
        AddEventTrigger(downBtn, EventTriggerType.PointerDown, () => vFlyInput = -1f);
        AddEventTrigger(downBtn, EventTriggerType.PointerUp, () => vFlyInput = 0f);
    }

    GameObject CreateButton(Canvas canvas, string name, string label, Vector2 pos)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(canvas.transform, false);
        
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1, 0); // تثبيت أسفل يمين الشاشة
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(100, 100);

        btnObj.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f); // لون أسود شفاف

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txtObj.transform.SetParent(btnObj.transform, false);
        Text t = txtObj.GetComponent<Text>();
        t.text = label;
        t.fontSize = 50;
        t.alignment = TextAnchor.MiddleCenter;
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

        // الحركة العادية + حركة الطيران
        Vector3 moveDirection = transform.right * joystick.Horizontal + transform.forward * joystick.Vertical;
        Vector3 flyDirection = transform.up * vFlyInput;

        Vector3 finalMove = moveDirection * SpeedMove + flyDirection * FlySpeed;
        controller.Move(finalMove * Time.deltaTime);
    }
}
