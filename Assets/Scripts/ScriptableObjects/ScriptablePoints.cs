using System;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Points", menuName = "Scriptable Objects/Points")]

[Serializable]
public class ScriptablePoints : ScriptableObject
{
    [SerializeField] private int points;
    public event Action<int> OnPointsChanged;
    public int Points => points;


    public void AddPoints(int amount)
    {
        points += amount;
        OnPointsChanged?.Invoke(points);
    }

    public bool TrySpend(int amount)
    {
        if (points >= amount)
        {
            points -= amount;
            OnPointsChanged?.Invoke(points);
            return true;
        }
        return false;
    }
}
