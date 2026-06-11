using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Componente que controla cada linha (sala) na tabela de salas públicas.
/// </summary>
public class LobbyListItem : MonoBehaviour
{
    [SerializeField] private TMP_Text _roomNameText;
    [SerializeField] private TMP_Text _playerCountText;
    [SerializeField] private Button _joinButton;

    private string _pin; // PIN associado a esta linha da tabela

    // Configura os textos e o PIN da sala
    public void Setup(string pin, string roomName, int current, int max)
    {
        _pin = pin;

        if (_roomNameText != null)
            _roomNameText.text = roomName;

        if (_playerCountText != null)
            _playerCountText.text = $"{current}/{max}";

        if (_joinButton != null)
        {
            // Remove ouvintes antigos para evitar cliques duplicados
            _joinButton.onClick.RemoveAllListeners();
            _joinButton.onClick.AddListener(OnJoinClick);
        }
    }

    // Chamado ao clicar no botão "Entrar" da linha
    private void OnJoinClick()
    {
        // Dispara o processo global de entrada usando o PIN desta sala
        JoinRoomUI.Instance?.JoinRoom(_pin);
    }
}