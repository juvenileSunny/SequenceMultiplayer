using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbySeatTileUI : MonoBehaviour
{
    [Header("Seat Visuals")]
    [SerializeField] private Image backgroundImage;

    [SerializeField] private Image teamRingImage;
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text avatarInitialText;

    [SerializeField] private TMP_Text playerLabelText;
    [SerializeField] private TMP_Text seatNumberText;

    [SerializeField] private Image readyIndicator;

    [Header("Team Colors")]
    [SerializeField] private Color team1Color =
        new Color(0.85f, 0.20f, 0.20f);

    [SerializeField] private Color team2Color =
        new Color(0.20f, 0.40f, 0.90f);

    [SerializeField] private Color team3Color =
        new Color(0.20f, 0.70f, 0.30f);

    [Header("Seat Colors")]
    [SerializeField] private Color emptySeatColor =
        new Color(1f, 1f, 1f, 0.15f);

    [SerializeField] private Color avatarColor =
        new Color(0.92f, 0.92f, 0.92f, 1f);

    [Header("Ready Colors")]
    [SerializeField] private Color readyColor =
        new Color(0.20f, 0.85f, 0.30f);

    [SerializeField] private Color notReadyColor =
        new Color(0.90f, 0.65f, 0.15f);

    private int seatIndex;

    // =========================================================
    // INITIALIZE
    // =========================================================

    public void Initialize(int newSeatIndex)
    {
        seatIndex = newSeatIndex;

        UpdateSeatNumber();

        ShowEmpty();
    }

    // =========================================================
    // REFRESH
    // =========================================================

    public void Refresh(LobbyPlayerData player)
    {
        UpdateSeatNumber();

        if (player == null)
        {
            ShowEmpty();
            return;
        }

        ShowPlayer(player);
    }

    // =========================================================
    // OCCUPIED
    // =========================================================

    private void ShowPlayer(LobbyPlayerData player)
    {
        Color teamColor =
            GetTeamColor(player.TeamId);

        // Root background is not needed while occupied.
        if (backgroundImage != null)
        {
            backgroundImage.color =
                new Color(1f, 1f, 1f, 0f);
        }

        // Team colored outer ring.
        if (teamRingImage != null)
        {
            teamRingImage.gameObject.SetActive(true);
            teamRingImage.color = teamColor;
        }

        // Inner avatar.
        if (avatarImage != null)
        {
            avatarImage.gameObject.SetActive(true);
            avatarImage.color = avatarColor;
        }

        // P1, P2, P3, etc.
        if (avatarInitialText != null)
        {
            avatarInitialText.gameObject.SetActive(true);
            avatarInitialText.text =
                $"P{player.PlayerId}";
        }

        // Player name underneath.
        if (playerLabelText != null)
        {
            playerLabelText.gameObject.SetActive(true);

            if (!string.IsNullOrWhiteSpace(
                    player.DisplayName))
            {
                playerLabelText.text =
                    player.DisplayName;
            }
            else
            {
                playerLabelText.text =
                    $"Player {player.PlayerId}";
            }
        }

        // Ready indicator.
        if (readyIndicator != null)
        {
            readyIndicator.gameObject.SetActive(true);

            readyIndicator.color =
                player.IsReady
                    ? readyColor
                    : notReadyColor;
        }
    }

    // =========================================================
    // EMPTY
    // =========================================================

    private void ShowEmpty()
    {
        // Only a neutral/colorless seat circle.
        if (backgroundImage != null)
        {
            backgroundImage.color =
                emptySeatColor;
        }

        if (teamRingImage != null)
        {
            teamRingImage.gameObject.SetActive(false);
        }

        if (avatarImage != null)
        {
            avatarImage.gameObject.SetActive(false);
        }

        if (avatarInitialText != null)
        {
            avatarInitialText.gameObject.SetActive(false);
        }

        if (playerLabelText != null)
        {
            playerLabelText.gameObject.SetActive(false);
        }

        if (readyIndicator != null)
        {
            readyIndicator.gameObject.SetActive(false);
        }
    }

    // =========================================================
    // SEAT NUMBER
    // =========================================================

    private void UpdateSeatNumber()
    {
        if (seatNumberText != null)
        {
            seatNumberText.text =
                $"SEAT {seatIndex}";
        }
    }

    // =========================================================
    // TEAM COLOR
    // =========================================================

    private Color GetTeamColor(int teamId)
    {
        switch (teamId)
        {
            case 1:
                return team1Color;

            case 2:
                return team2Color;

            case 3:
                return team3Color;

            default:
                return emptySeatColor;
        }
    }
}