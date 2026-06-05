using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkRoundStarter : MonoBehaviour
{
    public void StartServer() 
    {
        NetworkManager.Singleton.StartServer();
    }
    public void StartClient() 
    {
        NetworkManager.Singleton.StartClient();
    }
}