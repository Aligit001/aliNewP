using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button joinBtn;

    private void Awake()
    {
        // ربط الأزرار برمجياً لسهولة العمل
        hostBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            HideUI(); // إخفاء الأزرار بعد البدء
        });

        joinBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
            HideUI();
        });
    }

    private void HideUI()
    {
        // إخفاء الـ Canvas بالكامل لكي لا يغطي شاشة اللعب
        gameObject.SetActive(false);
    }
}