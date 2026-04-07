using Unity.Netcode;
using Unity.Netcode.Transports.UTP; // ضروري للتحكم في الـ IP
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button joinBtn;
    [SerializeField] private InputField ipInputField; // أضفنا مربع نص للـ IP

    private void Awake()
    {
        // زر الـ Host (المضيف)
        hostBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            HideUI();
        });

        // زر الـ Join (المنضم)
        joinBtn.onClick.AddListener(() => {
            // كود سحري: يخبر الهاتف بالاتصال بعنوان الـ IP المكتوب في المربع
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (ipInputField != null && !string.IsNullOrEmpty(ipInputField.text))
            {
                transport.ConnectionData.Address = ipInputField.text;
            }

            NetworkManager.Singleton.StartClient();
            HideUI();
        });
    }

    private void HideUI()
    {
        gameObject.SetActive(false);
    }
}
