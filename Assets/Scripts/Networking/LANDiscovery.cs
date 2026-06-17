using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;

namespace Networking
{
    public class LANDiscovery : MonoBehaviour
    {
        public static LANDiscovery Instance { get; private set; }

        [Header("LAN Settings")]
        [SerializeField] private int _port = 47777;
        [SerializeField] private string _broadcastAddress = "255.255.255.255";
        
        private UdpClient _udpClient;
        private bool _isSearching;
        private bool _isBroadcasting;

        // Event for when a server is found: (IP, RoomName, PIN)
        public event Action<string, string, string> OnServerFound;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void StartBroadcasting(string roomName, string pin)
        {
            StopAll();
            _isBroadcasting = true;
            try {
                _udpClient = new UdpClient();
                _udpClient.EnableBroadcast = true;
                
                Task.Run(async () =>
                {
                    byte[] data = Encoding.UTF8.GetBytes($"FAUNERAL_LAN|{roomName}|{pin}");
                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(_broadcastAddress), _port);

                    while (_isBroadcasting)
                    {
                        try {
                            await _udpClient.SendAsync(data, data.Length, endPoint);
                        } catch (Exception ex) {
                            Debug.LogError($"[LAN] Broadcast Send Error: {ex.Message}");
                        }
                        await Task.Delay(2000);
                    }
                });
                Debug.Log($"[LAN] Started Broadcasting: {roomName} ({pin})");
            } catch (Exception ex) {
                Debug.LogError($"[LAN] StartBroadcasting Error: {ex.Message}");
            }
        }

        public void StartSearching()
        {
            StopAll();
            _isSearching = true;
            try {
                _udpClient = new UdpClient(_port);
                
                Task.Run(async () =>
                {
                    while (_isSearching)
                    {
                        try {
                            var result = await _udpClient.ReceiveAsync();
                            string message = Encoding.UTF8.GetString(result.Buffer);
                            
                            if (message.StartsWith("FAUNERAL_LAN|"))
                            {
                                string[] parts = message.Split('|');
                                string roomName = parts.Length > 1 ? parts[1] : "Local Game";
                                string pin = parts.Length > 2 ? parts[2] : "0000";
                                string ip = result.RemoteEndPoint.Address.ToString();
                                
                                // Server sends its own IP, but we get the IP from the packet origin
                                Debug.Log($"[LAN] Server found: {roomName} at {ip}");
                                // We don't have a MainThreadDispatcher yet, so we'll just queue it for next update or use a simple list
                                lock (_discoveredServers)
                                {
                                    _discoveredServers.Enqueue((ip, roomName, pin));
                                }
                            }
                        } catch { /* Ignore socket closed errors */ }
                    }
                });
                Debug.Log("[LAN] Started Searching...");
            } catch (Exception ex) {
                Debug.LogError($"[LAN] StartSearching Error: {ex.Message}");
            }
        }

        private Queue<(string ip, string name, string pin)> _discoveredServers = new Queue<(string, string, string)>();

        private void Update()
        {
            while (_discoveredServers.Count > 0)
            {
                (string ip, string name, string pin) server;
                lock (_discoveredServers)
                {
                    server = _discoveredServers.Dequeue();
                }
                OnServerFound?.Invoke(server.ip, server.name, server.pin);
            }
        }

        public void StopAll()
        {
            _isBroadcasting = false;
            _isSearching = false;
            _udpClient?.Close();
            _udpClient = null;
        }

        private void OnDestroy() => StopAll();
    }
}