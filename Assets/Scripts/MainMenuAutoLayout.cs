using UnityEngine;
using UnityEngine.UI;

public class MainMenuAutoLayout : MonoBehaviour
{
    void Start()
    {
        // 1. البحث عن الـ Canvas في المشهد
        Canvas canvas = GameObject.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("MainMenu: لم يتم العثور على Canvas في المشهد.");
            return;
        }

        // 2. تعديل الـ Canvas Scaler ليتناسب مع حجم الشاشة تلقائياً
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); // دقة مرجعية قياسية

        // 3. ترتيب جميع الأزرار والنصوص داخل الـ Canvas برمجياً
        Transform[] allTransforms = canvas.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in allTransforms)
        {
            // تجاهل الـ Canvas نفسه
            if (child == canvas.transform) continue;

            RectTransform rt = child.GetComponent<RectTransform>();
            if (rt == null) continue;

            // تثبيت الكائنات في منتصف الشاشة (Centering Anchors)
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            // تكبير الأزرار وتوزيعها عمودياً لمنع التداخل
            if (child.GetComponent<Button>())
            {
                rt.sizeDelta = new Vector2(400, 100); // حجم الزر (عرض، ارتفاع)
                if (child.name.Contains("Host") || child.name.Contains("إنشاء")) // مثال لاسم زر
                {
                    rt.anchoredPosition = new Vector2(0, 120);
                }
                else if (child.name.Contains("Join") || child.name.Contains("دخول")) // مثال لاسم زر
                {
                    rt.anchoredPosition = new Vector2(0, 0);
                }
            }
            // تموضع الـ Input Field الخاص بالـ IP
            else if (child.GetComponent<InputField>())
            {
                rt.sizeDelta = new Vector2(400, 70);
                rt.anchoredPosition = new Vector2(0, -120);
            }
            // تموضع النص العنواني (Made by Ali...)
            else if (child.GetComponent<Text>())
            {
                // النص الذي في الأعلى
                if (rt.anchoredPosition.y > 0)
                {
                    rt.anchoredPosition = new Vector2(0, 250);
                }
            }
        }
    }
}
