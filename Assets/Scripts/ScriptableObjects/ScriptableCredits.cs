using System;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Credits", menuName = "Scriptable Objects/Credits")]

[Serializable]
public class ScriptableCredits : ScriptableObject
{
    [SerializeField] private int credits;
    public event Action<int> OnCreditsChanged;
    public int Credits => credits;


    public void AddCredits(int amount)
    {
        credits += amount;
        OnCreditsChanged?.Invoke(credits);
    }

    public bool TrySpend(int amount)
    {
        if (credits >= amount)
        {
            credits -= amount;
            OnCreditsChanged?.Invoke(credits);
            return true;
        }
        return false;
    }
}
