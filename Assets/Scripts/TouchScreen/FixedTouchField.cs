using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;


public class FixedTouchField : MonoBehaviour , IPointerDownHandler, IPointerUpHandler
{
    [HideInInspector]
    public Vector2 TouchDist;
    [HideInInspector]
    public Vector2 PointerOld;
    [HideInInspector]
    protected int PointerId;
    [HideInInspector]
    public bool Pressed;

    // Use this for initialization
    void Start()
    {

    }

// Update is called once per frame
    void Update()
    {
        if (Pressed)
        {
            // التحقق من اللمس عبر الشاشة
            if (Touchscreen.current != null && PointerId >= 0 && PointerId < Touchscreen.current.touches.Count)
            {
                TouchDist = Touchscreen.current.touches[PointerId].position.ReadValue() - PointerOld;
                PointerOld = Touchscreen.current.touches[PointerId].position.ReadValue();
            }
            // إذا لم يكن هناك لمس، استخدم الماوس للتحكم (مفيد للتجربة داخل محرر Unity)
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