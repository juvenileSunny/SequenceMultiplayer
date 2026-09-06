using System;
using UnityEngine;
using UnityEngine.UI;

public class HandCardView : MonoBehaviour
{
    [SerializeField] private Image cardImage;
    [SerializeField] private Button button;

    private Card card;

    public Card Card => card;

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
}