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
                _text.text = "Meu IP: " + GetBestLocalIP();
            }
        }

        private string GetBestLocalIP()
        {
            return NetworkIPUtils.GetBestLocalIP();
        }
}
}
