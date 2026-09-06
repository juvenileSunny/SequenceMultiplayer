using System;
using UnityEngine;

public class HandManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform handContainer;
    [SerializeField] private HandCardView handCardPrefab;

    [Header("Features")]
    [SerializeField] private bool showCardSelectionVisual = true;

    private Player player;
    private HandCardView selectedCardView;
    public event Action<Card> OnSelectedCardChanged;

    public Card SelectedCard =>
        selectedCardView != null
            ? selectedCardView.Card
            : null;

    public void Initialize(Player newPlayer)
    {
        player = newPlayer;

        RefreshHand();
    }

    public void RefreshHand()
    {
        if (player == null)
        {
            Debug.LogWarning("HandManager has no player assigned.");
            return;
        }

        ClearHand();

        foreach (Card card in player.Hand)
        {
            HandCardView cardView =
                Instantiate(
                    handCardPrefab,
                    handContainer
                );

            cardView.Initialize(
                card,
                OnCardClicked
            );
        }
    }

    private void OnCardClicked(HandCardView cardView)
    {
        // Click selected card again = deselect
        if (selectedCardView == cardView)
        {
            selectedCardView.SetSelected(false);

            selectedCardView = null;

            OnSelectedCardChanged?.Invoke(null);

            Debug.Log("Card deselected.");

            return;
        }

        // Remove previous visual
        if (selectedCardView != null)
        {
            selectedCardView.SetSelected(false);
        }

        selectedCardView = cardView;

        selectedCardView.SetSelected(
            showCardSelectionVisual
        );

        OnSelectedCardChanged?.Invoke(
            selectedCardView.Card
        );

        Debug.Log(
            $"Selected card: {cardView.Card.GetCode()}"
        );
    }

    private void ClearHand()
    {
        selectedCardView = null;

        foreach (Transform child in handContainer)
        {
            Destroy(child.gameObject);
        }
    }
}