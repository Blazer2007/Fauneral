using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class TesteHTTP : MonoBehaviour
{
    IEnumerator Start()
    {
        Debug.Log("TESTANDO A CONEXÃO...");
        using var r = UnityWebRequest.Get("https://node-server-4eg2.onrender.com/public-rooms");
        r.timeout = 0;
        yield return r.SendWebRequest();
        Debug.Log($"Resultado: {r.result} | Erro: {r.error} | Resposta: {r.downloadHandler.text}");
    }
}
