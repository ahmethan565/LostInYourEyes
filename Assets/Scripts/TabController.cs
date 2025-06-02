using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections.Generic;

public class TabController : MonoBehaviour, IPointerClickHandler
{
    private static Dictionary<string, List<TabController>> tabGroups = new Dictionary<string, List<TabController>>();

    [Header("UI")]
    public RectTransform backgroundSelected;

    [Header("Ayarlar")]
    public string groupName;
    public float pressedY = -10f;
    public float duration = 0.25f;
    public bool selectAndAutoDeselect = false; // <--- Yeni �zellik

    private Vector2 originalPos;
    private bool isSelected;

    void Start()
    {
        if (backgroundSelected == null)
        {
            Debug.LogError("BackgroundSelected atanmad�!");
            return;
        }

        originalPos = backgroundSelected.anchoredPosition;

        if (!tabGroups.ContainsKey(groupName))
        {
            tabGroups[groupName] = new List<TabController>();
        }
        tabGroups[groupName].Add(this);

        DeselectThis(); // Varsay�lan olarak deselect
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
        if (selectAndAutoDeselect)
        {
            // Di�erlerini deselect et
            foreach (var tab in tabGroups[groupName])
            {
                if (tab != this)
                    tab.DeselectThis();
            }

            // K�sa s�reli�ine se� ve hemen geri kald�r
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
        backgroundSelected.DOAnchorPosY(originalPos.y, duration).SetEase(Ease.OutCubic);
    }
}
