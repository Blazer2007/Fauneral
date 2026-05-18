using System;
using System.Collections;
using System.Net;
using UnityEngine;
using UnityEngine.Networking;

public class WebRequestManager : MonoBehaviour
{
    private string _serverPath = "http://127.0.0.1:8080";
    private string _getURL = "/get-data";
    private string _getDBURL = "/get-data-db";
    private string _postURL = "/post-data";
    
    [SerializeField]ButtonManager buttonManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    public void ButtonClicked(string buttonType)
    {
        Debug.Log($"O botão clicado foi:{buttonType}");
        
        if (buttonType == "get")
        {
            StartCoroutine(GetRequest(_serverPath+_getURL));

        }
        else if (buttonType == "post")
        {
            StartCoroutine(PostRequest());
        }
        else if(buttonType == "getDB")
        {
             StartCoroutine(GetRequest(_serverPath+_getDBURL));
        }
    }
    
    public IEnumerator GetRequest(string url)
    {
        UnityWebRequest webRequest = UnityWebRequest.Get(url);
        webRequest.timeout = 5;

        yield return webRequest.SendWebRequest();
        Debug.Log("Response after send request");
        // Check for errors
        if (ValidateResponse(webRequest))
        {
            ProcessRequestGet(webRequest);
        }
        
    }

    public IEnumerator PostRequest()
    {   // Create an instance of the PlayerDataInfo class and populate it with data
        PlayerData.PlayerDataInfo playerDataInfo = new PlayerData.PlayerDataInfo{ id = 4, name = "Pedro", rounds_won = 3 };
        string playerInfoJson = PlayerData.CreateJsonFromClass(playerDataInfo);     
        UnityWebRequest webRequest = UnityWebRequest.Post(_serverPath + _postURL, playerInfoJson, "application/json");
        // Set a timeout for the request (in seconds)
        webRequest.timeout = 5;
        // Set the request body 
        yield return webRequest.SendWebRequest();
        // Check for errors
        if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log($"Error: {webRequest.error}");
        }
        //if the request is successful, process the response
        else
        {
            Debug.Log($"Response: {webRequest.downloadHandler.text}");
        }
    }

    private bool ValidateResponse (UnityWebRequest webRequest)
    {
        if(webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError|| webRequest.result == UnityWebRequest.Result.DataProcessingError)
        {
            Debug.Log($"Error: {webRequest.error}"); 
            return false;
        }
        return true;
        
    }
    private void ProcessRequestDebug(UnityWebRequest webRequest) {
        Debug.Log($"Received: {webRequest.downloadHandler.text}");
    }
    private void ProcessRequestGet(UnityWebRequest webRequest) {
        

        string responseText = webRequest.downloadHandler.text;
        //PlayerData.CreateClassFromJson(responseText);
        PlayerData.PlayerDataInfo_Array playerDataInfoArray = PlayerData.CreateClassFromJson(responseText);
        for (int i = 0; i < playerDataInfoArray._playerDataInfoArray.Length; i++)
        {
            PlayerData.PlayerDataInfo playerDataInfo = playerDataInfoArray._playerDataInfoArray[i];
            Debug.Log($"Player Name: {playerDataInfo.name}, id: {playerDataInfo.id}, Rounds Won: {playerDataInfo.rounds_won}");
        }
    }


}
