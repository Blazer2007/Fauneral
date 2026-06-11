using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Networking
{
    public class DiscoveryManager : MonoBehaviour
    {
        public static DiscoveryManager Instance { get; private set; }

        private const string BaseUrl = "http://127.0.0.1:8080"; // Endereço do servidor Node.js

        private void Awake()
        {
            // Singleton para garantir que a comunicação com o Node.js seja centralizada
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // Solicita ao Node.js a criação de uma sala e retorna o PIN gerado
        public void CreateRoom(string relayJoinCode, string roomName, bool isPublic, int maxPlayers, Action<string> onComplete)
        {
            StartCoroutine(CreateRoomCoroutine(relayJoinCode, roomName, isPublic, maxPlayers, onComplete));
        }

        private IEnumerator CreateRoomCoroutine(string relayJoinCode, string roomName, bool isPublic, int maxPlayers, Action<string> onComplete)
        {
            // Envia o código do Relay e metadados da sala
            var roomData = new RoomData 
            { 
                ip = relayJoinCode, 
                port = 7777,
                name = roomName,
                isPublic = isPublic,
                maxPlayers = maxPlayers
            };
            string json = JsonUtility.ToJson(roomData);

            using (UnityWebRequest request = new UnityWebRequest($"{BaseUrl}/create-room", "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<CreateRoomResponse>(request.downloadHandler.text);
                    onComplete?.Invoke(response.code); // Retorna o PIN de 4 dígitos
                }
                else
                {
                    Debug.LogError($"CreateRoom failed: {request.error}\nResponse: {request.downloadHandler?.text}");
                    onComplete?.Invoke(null);
                }
            }
        }

        // Busca a lista de todas as salas marcadas como públicas no servidor Node.js
        public void GetPublicRooms(Action<RoomData[]> onComplete)
        {
            StartCoroutine(GetPublicRoomsCoroutine(onComplete));
        }

        private IEnumerator GetPublicRoomsCoroutine(Action<RoomData[]> onComplete)
        {
            using (UnityWebRequest request = UnityWebRequest.Get($"{BaseUrl}/public-rooms"))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    // Faz o parse da lista de salas (formato JSON array)
                    string json = request.downloadHandler.text;
                    if (!json.StartsWith("{")) json = "{\"rooms\":" + json + "}";
                    var response = JsonUtility.FromJson<RoomListResponse>(json);
                    onComplete?.Invoke(response.rooms);
                }
                else
                {
                    Debug.LogError($"GetPublicRooms failed: {request.error}");
                    onComplete?.Invoke(null);
                }
            }
        }

        // Busca os dados de conexão de uma sala específica usando o PIN
        public void JoinRoom(string nodeJsCode, Action<string> onComplete)
        {
            StartCoroutine(JoinRoomCoroutine(nodeJsCode, onComplete));
        }

        private IEnumerator JoinRoomCoroutine(string nodeJsCode, Action<string> onComplete)
        {
            using (UnityWebRequest request = UnityWebRequest.Get($"{BaseUrl}/join-room/{nodeJsCode}"))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<RoomData>(request.downloadHandler.text);
                    onComplete?.Invoke(response.ip); // Retorna o código do Relay
                }
                else
                {
                    Debug.LogError($"JoinRoom failed: {request.error}");
                    onComplete?.Invoke(null);
                }
            }
        }

        // Avisa o servidor Node.js que a quantidade de jogadores mudou (para atualizar na lista pública)
        public void UpdateRoom(string nodeJsCode, int currentPlayers)
        {
            StartCoroutine(UpdateRoomCoroutine(nodeJsCode, currentPlayers));
        }

        private IEnumerator UpdateRoomCoroutine(string nodeJsCode, int currentPlayers)
        {
            var roomUpdate = new RoomUpdate { currentPlayers = currentPlayers };
            string json = JsonUtility.ToJson(roomUpdate);

            using (UnityWebRequest request = new UnityWebRequest($"{BaseUrl}/update-room/{nodeJsCode}", "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"UpdateRoom failed: {request.error}");
                }
            }
        }

        // Remove a sala do catálogo global do Node.js
        public void DeleteRoom(string nodeJsCode)
        {
            StartCoroutine(DeleteRoomCoroutine(nodeJsCode));
        }

        private IEnumerator DeleteRoomCoroutine(string nodeJsCode)
        {
            using (UnityWebRequest request = UnityWebRequest.Delete($"{BaseUrl}/delete-room/{nodeJsCode}"))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"DeleteRoom failed: {request.error}");
                }
                else
                {
                    Debug.Log($"Room {nodeJsCode} deleted from discovery server.");
                }
            }
        }

        // Classes auxiliares para serialização JSON
        [Serializable]
        private class RoomUpdate
        {
            public int currentPlayers;
        }

        [Serializable]
        public class RoomData
        {
            public string ip;
            public int port;
            public string code;
            public string name;
            public bool isPublic;
            public int maxPlayers;
            public int currentPlayers;
        }

        [Serializable]
        private class CreateRoomResponse
        {
            public string code;
        }

        [Serializable]
        private class RoomListResponse
        {
            public RoomData[] rooms;
        }
    }
}
