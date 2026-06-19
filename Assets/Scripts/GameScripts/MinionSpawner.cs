using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Acessório da carta "Minions". Ao ser usada, spawna N minions que perseguem
/// e atacam os outros jogadores (não o dono).
///
/// O PRÓPRIO PREFAB "Minions" (ligado a accessoryPrefab no ScriptableCard) é
/// um spawner vazio que apenas instancia os minions reais e depois auto-destrói-se —
/// isto porque AccessoryBase é pensado para UM objecto por uso de carta, e aqui
/// precisamos de vários minions. Ver MinionUnit.cs para o comportamento de cada minion.
///
/// SETUP (Inspector do prefab "MinionSpawner"):
///   - NetworkObject
///   - MinionSpawner (este script)
///   - _minionPrefab → prefab MinionUnit (com NetworkObject)
///   - _minionCount → quantos minions por uso (default 2)
/// </summary>
public class MinionSpawner : AccessoryBase
{
    [SerializeField] private GameObject _minionPrefab;
    [SerializeField] private int _minionCount = 2;
    [SerializeField] private float _spawnRadius = 1.5f;

    protected override void OnInit()
    {
        if (!IsServer) return; // AccessoryBase.Init só corre no servidor (ver PlayerCardUser)

        for (int i = 0; i < _minionCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * _spawnRadius;
            Vector3 spawnPos = transform.position + new Vector3(offset.x, offset.y, 0f);

            var go = Instantiate(_minionPrefab, spawnPos, Quaternion.identity);
            var netObj = go.GetComponent<NetworkObject>();

            if (netObj == null)
            {
                Debug.LogWarning("[MinionSpawner] O prefab do minion não tem NetworkObject.");
                Destroy(go);
                continue;
            }

            netObj.Spawn();

            var minion = go.GetComponent<MinionUnit>();
            minion?.Init(OwnerId); // o minion sabe quem é o "mestre" e ataca todos os outros
        }

        // O spawner em si já cumpriu o seu papel — desaparece
        GetComponent<NetworkObject>().Despawn();
    }
}
