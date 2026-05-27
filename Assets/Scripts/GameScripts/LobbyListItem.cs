using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Componente de um item na lista de salas públicas.
/// 
/// Setup no Prefab (LobbyItemPrefab):
///   RoomNameText    → TMP_Text com o nome da sala
///   PlayerCountText → TMP_Text com "X/Y jogadores"
///   JoinButton      → Button "Entrar"
/// </summary>
public class LobbyListItem : MonoBehaviour
{
    [SerializeField] private TMP_Text _roomNameText;
    [SerializeField] private TMP_Text _playerCountText;
    [SerializeField] private Button _joinButton;

    private string _pin;

    public void Setup(string pin, string roomName, int current, int max)
    {
        _pin = pin;

        if (_roomNameText != null)
            _roomNameText.text = roomName;

        if (_playerCountText != null)
            _playerCountText.text = $"{current}/{max}";

        if (_joinButton != null)
            _joinButton.onClick.AddListener(OnJoinClick);
    }

    public void OnJoinClick()
    {
        LobbyClientManager.Instance?.JoinLobby(_pin);
    }
}