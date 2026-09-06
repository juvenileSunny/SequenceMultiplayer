using System;
using UnityEngine;
using UnityEngine.UI;

public class HandManager : MonoBehaviour
{
    // =========================================================
    // UI REFERENCES
    // =========================================================

    [Header("UI")]
    [SerializeField] private Transform handContainer;
    [SerializeField] private HandCardView handCardPrefab;
    [SerializeField] private Button replaceDeadCardButton;

    // =========================================================
    // FEATURES
    // =========================================================

    [Header("Features")]
    [SerializeField] private bool showCardSelectionVisual = true;

    // =========================================================
    // EVENTS
    // =========================================================

    // Fired whenever a hand card is selected or deselected.
    // null means no card is selected.
    public event Action<Card> OnSelectedCardChanged;

    // Fired when the player presses the
    // Replace Dead Card button.
    public event Action<Card> OnDeadCardRequested;

    // =========================================================
    // DATA
    // =========================================================

    private Player player;

    private HandCardView selectedCardView;

    public Card SelectedCard =>
        selectedCardView != null
            ? selectedCardView.Card
            : null;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        SetupDeadCardButton();
    }

    // =========================================================
    // DEAD CARD BUTTON
    // =========================================================

    private void SetupDeadCardButton()
    {
        if (replaceDeadCardButton == null)
            return;

        replaceDeadCardButton.onClick.RemoveAllListeners();

        replaceDeadCardButton.onClick.AddListener(
            RequestDeadCardReplacement
        );

        // Disabled unless the selected card
        // is confirmed to be dead.
        replaceDeadCardButton.interactable = false;
    }

    private void RequestDeadCardReplacement()
    {
        if (selectedCardView == null)
            return;

        OnDeadCardRequested?.Invoke(
            selectedCardView.Card
        );
    }

    public void SetDeadCardButtonState(bool enabled)
    {
        if (replaceDeadCardButton == null)
            return;

        replaceDeadCardButton.interactable = enabled;
    }

    // =========================================================
    // INITIALIZATION
    // =========================================================

    public void Initialize(Player newPlayer)
    {
        player = newPlayer;

        RefreshHand();
    }

    // =========================================================
    // HAND UI
    // =========================================================

    public void RefreshHand()
    {
        if (player == null)
        {
            Debug.LogWarning(
                "HandManager has no player assigned."
            );

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

    // =========================================================
    // CARD SELECTION
    // =========================================================

    private void OnCardClicked(
        HandCardView cardView)
    {
        if (cardView == null)
            return;

        // -----------------------------------------------------
        // CLICK CURRENTLY SELECTED CARD AGAIN
        // -> DESELECT
        // -----------------------------------------------------

        if (selectedCardView == cardView)
        {
            selectedCardView.SetSelected(false);

            selectedCardView = null;

            // Dead-card replacement no longer applies.
            SetDeadCardButtonState(false);

            // Tell GameManager nothing is selected.
            OnSelectedCardChanged?.Invoke(null);

            Debug.Log(
                "Card deselected."
            );

            return;
        }

        // -----------------------------------------------------
        // REMOVE OLD SELECTION
        // -----------------------------------------------------

        if (selectedCardView != null)
        {
            selectedCardView.SetSelected(false);
        }

        // Dead-card status must be recalculated
        // for the newly selected card.
        SetDeadCardButtonState(false);

        // -----------------------------------------------------
        // SELECT NEW CARD
        // -----------------------------------------------------

        selectedCardView = cardView;

        selectedCardView.SetSelected(
            showCardSelectionVisual
        );

        // Tell GameManager which card was selected.
        OnSelectedCardChanged?.Invoke(
            selectedCardView.Card
        );

        Debug.Log(
            $"Selected card: " +
            $"{selectedCardView.Card.GetCode()}"
        );
    }

    // =========================================================
    // CLEAR HAND
    // =========================================================

    private void ClearHand()
    {
        // Clear selection state.
        if (selectedCardView != null)
        {
            selectedCardView.SetSelected(false);
        }

        selectedCardView = null;

        // New hand starts with no dead card selected.
        SetDeadCardButtonState(false);

        // Remove old card UI.
        foreach (Transform child in handContainer)
        {
            Destroy(child.gameObject);
        }
    }
}