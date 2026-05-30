using UnityEngine;
using UnityEngine.EventSystems;

public class CardInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Vector3 _originalScale;
    private Vector3 _recentScale;

    void Start()
    {
            _originalScale = transform.localScale;
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Aumenta o tamanho da carta para dar feedback visual
        transform.localScale = _originalScale * 1.5f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Restaura o tamanho original da carta
        transform.localScale = _originalScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Lógica para quando a carta é clicada
        Debug.Log("Carta clicada!");
    }
}
