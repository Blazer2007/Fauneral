using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Controla o arranque da rede.
/// </summary>
public class NetworkManagerUI : MonoBehaviour
{
    [Header("Auto-iniciar como servidor (para a build de servidor)")]
    [Tooltip("Se true, arranca automaticamente como servidor dedicado ao iniciar")]
    [SerializeField] private bool _autoStartAsServer = false;

    private void Awake()
    {
        if (_autoStartAsServer)
            StartDedicatedServer();
    }

    /// <summary>
    /// Arranca como servidor dedicado (sem cliente local).
    /// Chama este método na build do servidor.
    /// </summary>
    public void StartDedicatedServer()
    {
        NetworkManager.Singleton.StartServer();
        Debug.Log("[NetworkManagerUI] Servidor dedicado iniciado.");
    }

    /// <summary>
    /// Arranca como cliente e liga ao servidor.
    /// Chama este método nas builds dos jogadores.
    /// </summary>
    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        Debug.Log("[NetworkManagerUI] Cliente a ligar...");
    }

    // Mantido para compatibilidade, mas não é usado nesta arquitectura
    public void StartHost()
    {
        Debug.LogWarning("[NetworkManagerUI] Host não é suportado nesta arquitectura. Usa StartDedicatedServer + StartClient.");
    }

    public void StartServer() => StartDedicatedServer();

    /// <summary>
    /// Desliga da rede (cliente ou servidor).
    /// </summary>
    public void Disconnect()
    {
        NetworkManager.Singleton.Shutdown();
        Debug.Log("[NetworkManagerUI] Desligado da rede.");
    }
}