using UnityEngine;

public class GameManager : MonoBehaviour
{
    private Deck deck;

    private void Start()
    {
        deck = new Deck();
        Debug.Log($"Deck created with {deck.Count} cards.");
        deck.Shuffle();
        Card card = deck.Draw();

        Debug.Log($"Drew card: {card.Rank} of {card.Suit}");
        Debug.Log($"Cards remaining: {deck.Count}");
    }
}
