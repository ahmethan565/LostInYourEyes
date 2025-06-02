using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class SimpleButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private RectTransform rectTransform;
    private Vector3 originalScale;

    [Header("Hover Ayarlarý")]
    public float hoverScale = 1.05f;
    public float hoverDuration = 0.15f;

    [Header("Click Ayarlarý")]
    public float clickScale = 0.9f;
    public float clickDuration = 0.1f;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        rectTransform
            .DOScale(originalScale * hoverScale, hoverDuration)
            .SetEase(Ease.OutQuad);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        rectTransform
            .DOScale(originalScale, hoverDuration)
            .SetEase(Ease.OutQuad);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Sequence clickSeq = DOTween.Sequence();
        clickSeq.Append(rectTransform
            .DOScale(originalScale * clickScale, clickDuration)
            .SetEase(Ease.OutQuad));
        clickSeq.Append(rectTransform
            .DOScale(originalScale * hoverScale, hoverDuration)
            .SetEase(Ease.OutQuad));
    }
}
