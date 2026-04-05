using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode; // إلزامي للأونلاين

public class CameraLook : NetworkBehaviour // غيرنا MonoBehaviour لـ NetworkBehaviour
{
    private float XMove;
    private float YMove;
    private float XRotation;
    [SerializeField] private Transform PlayerBody;
    public Vector2 LockAxis;
    public float Sensivity = 40f;

    // كاميرا اللاعب لكي نتحكم بظهورها
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();

        // إذا لم أكن صاحب هذا اللاعب، أطفئ الكاميرا لكي لا أرى شاشة غيري
        if (!IsOwner)
        {
            cam.enabled = false;
            // وأطفئ الـ AudioListener أيضاً لكي لا تسمع صوتين في نفس الوقت
            if (GetComponent<AudioListener>()) GetComponent<AudioListener>().enabled = false;
        }
    }

    void Update()
    {
        // أهم سطر: إذا لم أكن المالك، لا تسمح بتحريك الكاميرا
        if (!IsOwner) return;

        XMove = LockAxis.x * Sensivity * Time.deltaTime;
        YMove = LockAxis.y * Sensivity * Time.deltaTime;
        XRotation -= YMove;
        XRotation = Mathf.Clamp(XRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(XRotation, 0, 0);
        PlayerBody.Rotate(Vector3.up * XMove);
    }
}