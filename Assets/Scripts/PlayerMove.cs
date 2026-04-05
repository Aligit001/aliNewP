using Unity.Netcode; // إلزامي للأونلاين
using UnityEngine;

public class PlayerMove : NetworkBehaviour // تأكد أنها NetworkBehaviour
{
    public FixedJoystick joystick;
    public float SpeedMove = 5f;
    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // إذا كنت أنا صاحب هذا اللاعب (Owner)
        if (IsOwner)
        {
            // البحث عن الجويستيك في الـ UI تلقائياً
            joystick = GameObject.FindObjectOfType<FixedJoystick>();

            if (joystick == null)
            {
                Debug.LogError("لم يتم العثور على FixedJoystick في الشاشة! تأكد من وجوده داخل الـ Canvas.");
            }
        }
    }

    void Update()
    {
        // إذا لم أكن المالك، أو لم نجد الجويستيك، توقف عن التنفيذ
        if (!IsOwner || joystick == null) return;

        // كود الحركة الخاص بك
        Vector3 Move = transform.right * joystick.Horizontal + transform.forward * joystick.Vertical;
        controller.Move(Move * SpeedMove * Time.deltaTime);
    }
}
