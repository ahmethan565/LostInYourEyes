using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections.Generic;

public class TabController : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private static Dictionary<string, List<TabController>> tabGroups = new Dictionary<string, List<TabController>>();

    [Header("UI")]
    public RectTransform backgroundSelected;

    [Header("Ayarlar")]
    public string groupName;
    public float pressedY = -10f;
    public float duration = 0.25f;
    public bool selectAndAutoDeselect = false;
    public bool enableHoverAnimation = false; // <-- Yeni özellik

    private Vector2 originalPos;
    private bool isSelected;
    private bool isHovering;

    void Start()
    {
        if (backgroundSelected == null)
        {
            Debug.LogError("BackgroundSelected atanmadı!");
            return;
        }

        originalPos = backgroundSelected.anchoredPosition;

        if (!tabGroups.ContainsKey(groupName))
        {
            tabGroups[groupName] = new List<TabController>();
        }
        tabGroups[groupName].Add(this);

        DeselectThis();
    }

    void OnDestroy()
    {
        if (tabGroups.ContainsKey(groupName))
        {
            tabGroups[groupName].Remove(this);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (enableHoverAnimation)
            return; // Hover aktifse tıklama animasyonu oynatma

        if (selectAndAutoDeselect)
        {
            foreach (var tab in tabGroups[groupName])
            {
                if (tab != this)
                    tab.DeselectThis();
            }

            backgroundSelected.DOAnchorPosY(originalPos.y + pressedY, duration / 2f)
                .SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    backgroundSelected.DOAnchorPosY(originalPos.y, duration / 2f).SetEase(Ease.InCubic);
                });
        }
        else
        {
            if (isSelected)
            {
                DeselectThis();
            }
            else
            {
                SelectThis();
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (enableHoverAnimation && !isSelected)
        {
            isHovering = true;
            backgroundSelected.DOAnchorPosY(originalPos.y + pressedY, duration / 2f).SetEase(Ease.OutCubic);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (enableHoverAnimation && !isSelected && isHovering)
        {
            isHovering = false;
            backgroundSelected.DOAnchorPosY(originalPos.y, duration / 2f).SetEase(Ease.InCubic);
        }
    }

    void SelectThis()
    {
        isSelected = true;

        foreach (var tab in tabGroups[groupName])
        {
            if (tab != this)
                tab.DeselectThis();
        }

        backgroundSelected.DOAnchorPosY(originalPos.y + pressedY, duration).SetEase(Ease.OutCubic);
    }

    void DeselectThis()
    {
        isSelected = false;
        isHovering = false;
        backgroundSelected.DOAnchorPosY(originalPos.y, duration).SetEase(Ease.OutCubic);
    }
}
