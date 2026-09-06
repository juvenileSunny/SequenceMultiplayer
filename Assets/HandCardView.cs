using System;
using UnityEngine;
using UnityEngine.UI;

public class HandCardView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image cardImage;
    [SerializeField] private Button button;
    [SerializeField] private Outline selectionOutline;

    [Header("Selection")]
    [SerializeField] private float selectedYOffset = 15f;

    private Card card;

    private RectTransform cardImageRect;
    private Vector2 normalPosition;

    public Card Card => card;

    private void Awake()
    {
        if (cardImage != null)
        {
            cardImageRect =
                cardImage.GetComponent<RectTransform>();

            normalPosition =
                cardImageRect.anchoredPosition;
        }

        if (selectionOutline != null)
        {
            selectionOutline.enabled = false;
        }
    }

    public void Initialize(
        Card newCard,
        Action<HandCardView> onClicked)
    {
        card = newCard;

        UpdateVisual();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();

            button.onClick.AddListener(() =>
            {
                onClicked?.Invoke(this);
            });
        }

        // Always start from normal state
        SetSelected(false);
    }

    private void UpdateVisual()
    {
        if (card == null || cardImage == null)
            return;

        string cardCode = card.GetCode();

        Sprite sprite =
            Resources.Load<Sprite>(
                $"Cards/{cardCode}"
            );

        if (sprite == null)
        {
            Debug.LogWarning(
                $"Missing hand card sprite: {cardCode}"
            );

            return;
        }

        cardImage.sprite = sprite;
        cardImage.preserveAspect = true;
    }

    public void SetSelected(bool selected)
    {
        if (cardImageRect == null)
            return;

        if (selectionOutline != null)
        {
            selectionOutline.enabled = selected;
        }

        if (selected)
        {
            cardImageRect.anchoredPosition =
                normalPosition +
                new Vector2(0f, selectedYOffset);
        }
        else
        {
            cardImageRect.anchoredPosition =
                normalPosition;
        }
    }
}