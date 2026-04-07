using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode; // ضروري جداً

public class CameraLook : NetworkBehaviour 
{
    [Header("Settings")]
    public float Sensivity = 40f;
    [SerializeField] private Transform PlayerBody;

    private float XMove;
    private float YMove;
    private float XRotation;

    [HideInInspector] public Vector2 LockAxis; // هذا المتغير سيستقبل البيانات من اللمس
    private Camera cam;
    private FixedTouchField touchField;

    public override void OnNetworkSpawn()
    {
        cam = GetComponent<Camera>();

        // إذا لم أكن أنا صاحب اللاعب، أطفئ الكاميرا والسمع
        if (!IsOwner)
        {
            if (cam != null) cam.enabled = false;
            if (GetComponent<AudioListener>()) GetComponent<AudioListener>().enabled = false;
            return;
        }

        // الربط التلقائي: يبحث عن منطقة اللمس في الـ UI برمجياً
        GameObject touchObj = GameObject.Find("TouchScreen");
        if (touchObj != null)
        {
            touchField = touchObj.GetComponent<FixedTouchField>();
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        // تحديث قيمة LockAxis من الـ touchField تلقائياً
        if (touchField != null)
        {
            LockAxis = touchField.TouchDist;
        }

        // الحسابات الأساسية لتدوير الكاميرا
        XMove = LockAxis.x * Sensivity * Time.deltaTime;
        YMove = LockAxis.y * Sensivity * Time.deltaTime;

        XRotation -= YMove;
        XRotation = Mathf.Clamp(XRotation, -90f, 90f);

        // تدوير الكاميرا (فوق وتحت)
        transform.localRotation = Quaternion.Euler(XRotation, 0, 0);
        
        // تدوير جسم اللاعب (يمين ويسار)
        if (PlayerBody != null)
        {
            PlayerBody.Rotate(Vector3.up * XMove);
        }
    }
}
