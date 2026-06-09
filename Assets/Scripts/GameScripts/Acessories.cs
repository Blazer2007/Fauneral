using UnityEngine;
using Unity.Netcode;

public class Acessories : NetworkBehaviour
{
    //[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    //public void SpawnAcessoryServerRpc(NetworkObject player, ScriptableCard card)
    //{
    //    if(card.acessoryPrefab != null)
    //    {
    //        Instantiate(card.acessoryPrefab, player.transform.position, Quaternion.identity);
    //    }
    //}
}
