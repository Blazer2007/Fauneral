using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;

public class PlayerCosmetics : NetworkBehaviour
{
    [SerializeField] private Transform _hatPoint;
    [SerializeField] private CrateData _allHats;
    [SerializeField] private Animator _animator;
    [SerializeField] private RuntimeAnimatorController _bunnyController;
    [SerializeField] private RuntimeAnimatorController _wolfController;

    private NetworkVariable<FixedString32Bytes> _netHatId = new NetworkVariable<FixedString32Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<FixedString32Bytes> _netAnimalId = new NetworkVariable<FixedString32Bytes>("Wolf", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private GameObject _currentHatInstance;

    public override void OnNetworkSpawn()
    {
        _netHatId.OnValueChanged += (oldVal, newVal) => ApplyHat(newVal.ToString());
        _netAnimalId.OnValueChanged += (oldVal, newVal) => ApplyAnimal(newVal.ToString());

        if (IsOwner)
        {
            string hatId = "";
            string animalId = "Wolf";

            if (ProfileManager.Instance != null)
            {
                hatId = ProfileManager.Instance.equippedHatId;
                animalId = ProfileManager.Instance.selectedAnimal;
            }
            else
            {
                hatId = PlayerPrefs.GetString("Profile_Hat", "");
                animalId = PlayerPrefs.GetString("Profile_Animal", "Wolf");
            }

            UpdateCosmeticsServerRpc(hatId, animalId);
        }
        else
        {
            ApplyHat(_netHatId.Value.ToString());
            ApplyAnimal(_netAnimalId.Value.ToString());
        }
    }

    [ServerRpc]
    private void UpdateCosmeticsServerRpc(string hatId, string animalId)
    {
        _netHatId.Value = hatId;
        _netAnimalId.Value = animalId;
    }

    private void ApplyHat(string hatId)
    {
        if (_currentHatInstance != null) Destroy(_currentHatInstance);
        if (string.IsNullOrEmpty(hatId)) return;

        if (_allHats == null) return;

        HatData data = _allHats.hats.Find(h => h != null && h.hatID == hatId);
        if (data != null && data.hatPrefab != null)
        {
            _currentHatInstance = Instantiate(data.hatPrefab, _hatPoint);
            _currentHatInstance.transform.localPosition = Vector3.zero;
            _currentHatInstance.transform.localRotation = Quaternion.identity;
            _currentHatInstance.transform.localScale = Vector3.one;
        }
    }

    private void ApplyAnimal(string animalId)
    {
        if (_animator == null) return;
        if (animalId == "Wolf")
        {
            if (_wolfController != null) _animator.runtimeAnimatorController = _wolfController;
        }
        else
        {
            if (_bunnyController != null) _animator.runtimeAnimatorController = _bunnyController;
        }
    }
}