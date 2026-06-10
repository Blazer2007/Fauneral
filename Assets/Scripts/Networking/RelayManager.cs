using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace Networking
{
    public class RelayManager : MonoBehaviour
    {
        public static RelayManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public async Task<string> CreateRelay(int maxPlayers)
        {
            try
            {
                if (NetworkManager.Singleton == null)
                {
                    Debug.LogError("NetworkManager.Singleton is null. Is there a NetworkManager in the scene?");
                    return null;
                }

                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                if (transport == null)
                {
                    Debug.LogError("UnityTransport component not found on NetworkManager!");
                    return null;
                }

                transport.SetRelayServerData(
                    allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port,
                    allocation.AllocationIdBytes,
                    allocation.Key,
                    allocation.ConnectionData
                );

                return joinCode;
            }
            catch (RelayServiceException e)
            {
                Debug.LogError($"Relay Create Error: {e.Message}");
                return null;
            }
        }

        public async Task<bool> JoinRelay(string joinCode)
        {
            try
            {
                if (NetworkManager.Singleton == null)
                {
                    Debug.LogError("NetworkManager.Singleton is null.");
                    return false;
                }

                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

                UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                if (transport == null)
                {
                    Debug.LogError("UnityTransport component not found on NetworkManager!");
                    return false;
                }

                transport.SetRelayServerData(
                    joinAllocation.RelayServer.IpV4,
                    (ushort)joinAllocation.RelayServer.Port,
                    joinAllocation.AllocationIdBytes,
                    joinAllocation.Key,
                    joinAllocation.ConnectionData,
                    joinAllocation.HostConnectionData
                );

                return true;
            }
            catch (RelayServiceException e)
            {
                Debug.LogError($"Relay Join Error: {e.Message}");
                return false;
            }
        }
    }
}
