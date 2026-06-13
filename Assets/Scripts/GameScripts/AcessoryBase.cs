using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Classe base para todos os acessórios spawnaveis durante uma ronda.
/// Herda desta classe para criar acessórios concretos (ex: BoogieBomb).
///
/// O PlayerCardUser chama Init() após o spawn para passar o contexto.
///
/// EXEMPLO DE ACESSÓRIO CONCRETO:
///
///   public class BoogieBomb : AccessoryBase
///   {
///       [SerializeField] private float _radius   = 5f;
///       [SerializeField] private float _fuseTime = 3f;
///       [SerializeField] private float _danceDuration = 4f;
///
///       protected override void OnInit()
///       {
///           StartCoroutine(Fuse());
///       }
///
///       private IEnumerator Fuse()
///       {
///           yield return new WaitForSeconds(_fuseTime);
///           Explode();
///       }
///
///       private void Explode()
///       {
///           if (!IsServer) return;
///           var hits = Physics2D.OverlapCircleAll(transform.position, _radius);
///           foreach (var hit in hits)
///               hit.GetComponent<PlayerAbilityHandler>()?.ApplyAbility("dance", _danceDuration);
///           GetComponent<NetworkObject>().Despawn();
///       }
///   }
/// </summary>
public abstract class AccessoryBase : NetworkBehaviour
{
    // ClientId do jogador que usou a carta
    public ulong OwnerId { get; private set; }

    // Carta que originou este acessório
    public ScriptableCard Card { get; private set; }

    /// <summary>
    /// Chamado pelo PlayerCardUser imediatamente após o spawn.
    /// </summary>
    public void Init(ulong ownerId, ScriptableCard card)
    {
        OwnerId = ownerId;
        Card = card;
        OnInit();
    }

    /// <summary>
    /// Override para implementar a lógica do acessório concreto.
    /// Já está no servidor quando é chamado.
    /// </summary>
    protected virtual void OnInit() { }
}