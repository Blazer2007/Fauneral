using System;
using System.Runtime.CompilerServices;
using UnityEngine;

[CreateAssetMenu(fileName = "ScriptableFloat", menuName = "Scriptable Objects/ScriptableFloat")]
public class ScriptableFloat : ScriptableObject
{
    [SerializeField] public float Value; // The float value that can be set in the unity inspector
    [SerializeField] public float InitialValue;

    public Action _onValueChange;

    public float _value
    {
        get { return Value; }
        set 
        { 
            Value = value;
            _onValueChange?.Invoke();
        }
    }
    public void ResetValue()
    {
        Value = InitialValue;
    }
    public void OnDisable()
    {
        ResetValue();
    }
}
