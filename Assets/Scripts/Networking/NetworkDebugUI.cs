using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Networking
{
    public class NetworkDebugUI : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _codeInput;
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _joinButton;
        [SerializeField] private TMP_Text _statusText;

        private void Start()
        {
            if (_hostButton != null) _hostButton.onClick.AddListener(OnHostClicked);
            if (_joinButton != null) _joinButton.onClick.AddListener(OnJoinClicked);
        }

        private async void OnHostClicked()
        {
            _statusText.text = "Creating room...";
            string roomCode = await MatchmakingController.Instance.StartHostOnline(4);
            if (!string.IsNullOrEmpty(roomCode))
            {
                _statusText.text = $"Room created! Code: {roomCode}";
            }
            else
            {
                _statusText.text = "Failed to create room.";
            }
        }

        private async void OnJoinClicked()
        {
            string code = _codeInput.text;
            if (string.IsNullOrEmpty(code))
            {
                _statusText.text = "Please enter a code.";
                return;
            }

            _statusText.text = "Joining room...";
            bool success = await MatchmakingController.Instance.StartClientOnline(code);
            if (success)
            {
                _statusText.text = "Joined successfully!";
            }
            else
            {
                _statusText.text = "Failed to join room.";
            }
        }
    }
}
