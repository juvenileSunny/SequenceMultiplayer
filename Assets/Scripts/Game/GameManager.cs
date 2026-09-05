using UnityEngine;

public class GameManager : MonoBehaviour
{
    private Deck deck;

    public Deck Deck => deck;

    private void Start()
    {
        InitializeGame();
    }

    private void InitializeGame()
    {
        deck = new Deck();

        deck.Shuffle();

        Debug.Log(
            $"Game initialized. Deck contains {deck.Count} cards."
        );
    }
}