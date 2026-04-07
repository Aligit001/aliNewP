using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class FixedTouchField : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [HideInInspector] public Vector2 TouchDist;
    [HideInInspector] public Vector2 PointerOld;
    [HideInInspector] protected int PointerId;
    [HideInInspector] public bool Pressed;

    void Update()
    {
        if (Pressed)
        {
            // أولاً: فحص اللمس (للأيفون) - الطريقة الأضمن
            if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
            {
                bool foundTouch = false;
                foreach (var touch in Touchscreen.current.touches)
                {
                    if (touch.touchId.ReadValue() == PointerId)
                    {
                        TouchDist = touch.position.ReadValue() - PointerOld;
                        PointerOld = touch.position.ReadValue();
                        foundTouch = true;
                        break;
                    }
                }
                
                // إذا لم نجد الـ ID (أحياناً يحدث في الأيفون)، نأخذ أول لمسة كاحتياط
                if (!foundTouch)
                {
                    TouchDist = Touchscreen.current.touches[0].position.ReadValue() - PointerOld;
                    PointerOld = Touchscreen.current.touches[0].position.ReadValue();
                }
            }
            // ثانياً: فحص الماوس (للتجربة داخل Unity)
            else if (Mouse.current != null)
            {
                TouchDist = Mouse.current.position.ReadValue() - PointerOld;
                PointerOld = Mouse.current.position.ReadValue();
            }
        }
        else
        {
            TouchDist = new Vector2();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Pressed = true;
        PointerId = eventData.pointerId;
        PointerOld = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Pressed = false;
    }
}
