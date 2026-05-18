using UnityEngine;
using System;

[Serializable]
public class PlayerData
{
    [Serializable]
    public class PlayerDataInfo_Array { 
        public PlayerDataInfo[] _playerDataInfoArray;
    }
    [Serializable]
    public class PlayerDataInfo 
    {
        public int id = 1;
        public string name="PEDRO";
        public int rounds_won = 0;
    }

    public static PlayerDataInfo_Array CreateClassFromJson(string json)
    {
        return JsonUtility.FromJson<PlayerDataInfo_Array>(json);
    }   
    public static string CreateJsonFromClass(PlayerDataInfo playerDataInfo)
    {
        return JsonUtility.ToJson(playerDataInfo);
    }
}
