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

        private const string BaseUrl = "http://127.0.0.1:8080";

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

        public void CreateRoom(string relayJoinCode, Action<string> onComplete)
        {
            StartCoroutine(CreateRoomCoroutine(relayJoinCode, onComplete));
        }

        private IEnumerator CreateRoomCoroutine(string relayJoinCode, Action<string> onComplete)
        {
            // The Node.js server validates if port is truthy. Sending 0 causes a 400 error.
            // Since we use Relay, we use a dummy non-zero port.
            var roomData = new RoomData { ip = relayJoinCode, port = 7777 };
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
                    onComplete?.Invoke(response.code);
                }
                else
                {
                    Debug.LogError($"CreateRoom failed: {request.error}\nResponse: {request.downloadHandler?.text}");
                    onComplete?.Invoke(null);
                }
            }
        }

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
                    onComplete?.Invoke(response.ip);
                }
                else
                {
                    Debug.LogError($"JoinRoom failed: {request.error}");
                    onComplete?.Invoke(null);
                }
            }
        }

        [Serializable]
        private class RoomData
        {
            public string ip;
            public int port;
        }

        [Serializable]
        private class CreateRoomResponse
        {
            public string code;
        }
    }
}
