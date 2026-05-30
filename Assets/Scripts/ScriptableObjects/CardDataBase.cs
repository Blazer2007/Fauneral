using UnityEngine;

/// <summary>
/// ScriptableObject que serve de registo de todas as cartas do jogo.
/// O servidor usa-o para fazer lookup de uma carta por índice sem
/// depender de instâncias de CardDisplay.
///
/// Setup:
///   1. Right Click > Create > Fauneral > Card Database
///   2. Arrasta todos os ScriptableCard para o array Cards[]
///   3. O cardId enviado pelo cliente é o índice neste array
///   4. Arrasta este asset para o campo CardDatabase do CardEffectManager
/// </summary>
[CreateAssetMenu(fileName = "CardDatabase", menuName = "Fauneral/Card Database")]
public class CardDatabase : ScriptableObject
{
    [Tooltip("Todas as cartas do jogo por ordem. O índice é o cardId.")]
    public ScriptableCard[] Cards;

    /// <summary>
    /// Devolve a carta pelo índice ou null se inválido.
    /// </summary>
    public ScriptableCard Get(int cardId)
    {
        if (cardId < 0 || cardId >= Cards.Length) return null;
        return Cards[cardId];
    }
}