using System;
using System.Runtime.CompilerServices;
using UnityEngine;

[CreateAssetMenu(fileName = "ScriptableFloat", menuName = "Scriptable Objects/ScriptableFloat")]
public class ScriptableFloat : ScriptableObject
{
    public float Value; // The float value that can be set in the unity inspector
    public float InitialValue;

    public Action _onValueChange;

    public float _value // the float
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
    public void AddEventListener(Action Listener) 
    {
        _onValueChange += Listener;
    }
    public void RemoveEventListener(Action Listener) 
    {
        _onValueChange -= Listener;
    }
    public void OnDisable()
    {
        ResetValue();
    }
}
