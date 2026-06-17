using UnityEngine;
using TMPro;
using System.Net;
using System.Net.Sockets;

namespace Networking
{
    public class LocalIPDisplay : MonoBehaviour
    {
        private TextMeshProUGUI _text;

        void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
            UpdateDisplay();
        }

        void OnEnable()
        {
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            if (_text != null)
            {
                _text.text = "Meu IP: " + GetLocalIPAddress();
            }
        }

        private string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (ip.ToString() == "127.0.0.1") continue;
                        return ip.ToString();
                    }
                }
            }
            catch { }
            return "127.0.0.1";
        }
    }
}
