using Unity.Netcode; // ضروري للأونلاين
using UnityEngine;

public class TouchController : NetworkBehaviour // غيرنا لـ NetworkBehaviour
{
    public FixedTouchField _FixedTouchField;
    public CameraLook _CameraLook;

    void Start()
    {
        // إذا كنت أنا صاحب هذا اللاعب (Owner)
        if (IsOwner)
        {
            // 1. ابحث عن منطقة اللمس في الشاشة تلقائياً لأننا لا نستطيع سحبها يدوياً
            _FixedTouchField = GameObject.FindObjectOfType<FixedTouchField>();
            
            // 2. ابحث عن سكربت الكاميرا الموجود في رأس اللاعب
            _CameraLook = GetComponentInChildren<CameraLook>();
        }
    }

    void Update()
    {
        // أهم سطر: إذا لم أكن المالك، أو لم نجد منطقة اللمس، اخرج فوراً
        if (!IsOwner || _FixedTouchField == null || _CameraLook == null) return;

        // إرسال بيانات اللمس من الشاشة إلى الكاميرا
        _CameraLook.LockAxis = _FixedTouchField.TouchDist;
    }
}
