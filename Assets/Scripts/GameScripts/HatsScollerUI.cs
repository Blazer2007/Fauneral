//using System.Collections.Generic;
//using Unity.Netcode;
//using UnityEngine;
//using TMPro;


//public class HatsScrollerUI : MonoBehaviour
//{
//    [SerializeField] private Transform _content;          // o Content do ScrollRect
//    [SerializeField] private GameObject _hatCardPrefab;
//    [SerializeField] private TMP_Text _selectedLabel;
//    [SerializeField] private TMP_Text _counterText;

//    private HatCardUI _currentSelected;
//    private string _pendingHatId;

//    void Start()
//    {
//        // Dados v�m do PlayerProfileManager � por agora, dados hardcoded de teste
//        PopulateGrid(GetTestData(), new[] { "hat_crown", "hat_tophat", "hat_helmet", "hat_cap" });
//    }

//    void PopulateGrid(HatData[] allHats, string[] unlockedIds)
//    {
//        var unlockedSet = new HashSet<string>(unlockedIds);
//        _counterText.text = $"{unlockedIds.Length} de {allHats.Length} desbloqueados";

//        foreach (var hat in allHats)
//        {
//            var go = Instantiate(_hatCardPrefab, _content);
//            var card = go.GetComponent<HatCardUI>();
//            card.Setup(hat, unlockedSet.Contains(hat.hatId), this);
//        }
//    }

//    public void OnCardSelected(HatCardUI card, string hatId)
//    {
//        _currentSelected?.SetSelected(false);
//        _currentSelected = card;
//        _currentSelected.SetSelected(true);
//        _pendingHatId = hatId;
//        _selectedLabel.text = $"Seleccionado: {card.GetDisplayName()}";
//    }

//    public void OnConfirmButton()
//    {
//        if (_pendingHatId == null) return;
//        // PlayerProfileManager.Instance.SetEquippedHat(_pendingHatId);
//        Debug.Log($"Hat confirmado: {_pendingHatId}");
//    }
//}
