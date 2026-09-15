using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GamePlayerStatusSlotUI : MonoBehaviour
{
    [Header("Avatar")]
    [SerializeField]
    private Image avatarBackground;

    [SerializeField]
    private Image teamRing;

    [SerializeField]
    private TMP_Text avatarText;

    [Header("Identity")]
    [SerializeField]
    private TMP_Text playerNameText;

    [SerializeField]
    private TMP_Text teamText;

    [Header("Presence")]
    [SerializeField]
    private Image presenceDot;

    [SerializeField]
    private TMP_Text presenceText;

    [Header("Turn")]
    [SerializeField]
    private GameObject turnHighlight;

    [SerializeField]
    private TMP_Text turnLabel;

    [SerializeField]
    private GameObject youLabel;

    [Header("Team Colors")]
    [SerializeField]
    private Color team1Color =
        new Color(0.85f, 0.2f, 0.2f);

    [SerializeField]
    private Color team2Color =
        new Color(0.2f, 0.45f, 0.95f);

    [SerializeField]
    private Color team3Color =
        new Color(0.2f, 0.75f, 0.35f);

    [Header("Presence Colors")]
    [SerializeField]
    private Color onlineColor =
        new Color(0.2f, 0.8f, 0.35f);

    [SerializeField]
    private Color offlineColor =
        new Color(0.45f, 0.45f, 0.45f);

    public void Configure(
        int playerId,
        string displayName,
        int seatIndex,
        int teamId,
        bool isConnected,
        bool isCurrentTurn,
        bool isLocalPlayer)
    {
        string safeName =
            string.IsNullOrWhiteSpace(
                displayName)
                ? $"Player {playerId}"
                : displayName.Trim();

        if (playerNameText != null)
        {
            playerNameText.text =
                safeName;
        }

        if (avatarText != null)
        {
            avatarText.text =
                GetAvatarInitials(
                    safeName,
                    playerId
                );
        }

        if (teamText != null)
        {
            teamText.text =
                GetTeamName(
                    teamId
                );
        }

        if (teamRing != null)
        {
            teamRing.color =
                GetTeamColor(
                    teamId
                );
        }

        if (presenceDot != null)
        {
            presenceDot.color =
                isConnected
                    ? onlineColor
                    : offlineColor;
        }

        if (presenceText != null)
        {
            presenceText.text =
                isConnected
                    ? "Online"
                    : "Offline";
        }

        if (turnHighlight != null)
        {
            turnHighlight.SetActive(
                isCurrentTurn
            );
        }

        if (turnLabel != null)
        {
            turnLabel.gameObject.SetActive(
                isCurrentTurn
            );

            if (isCurrentTurn)
            {
                turnLabel.text =
                    isLocalPlayer
                        ? "YOUR TURN"
                        : "TURN";
            }
        }

        if (youLabel != null)
        {
            youLabel.SetActive(
                isLocalPlayer
            );
        }

        gameObject.name =
            $"PlayerStatus_{playerId}_Seat_{seatIndex}";
    }

    private string GetAvatarInitials(
        string displayName,
        int playerId)
    {
        if (string.IsNullOrWhiteSpace(
                displayName))
        {
            return $"P{playerId}";
        }

        string[] pieces =
            displayName.Split(
                ' ',
                System.StringSplitOptions.RemoveEmptyEntries
            );

        if (pieces.Length == 0)
        {
            return $"P{playerId}";
        }

        if (pieces.Length == 1)
        {
            return pieces[0]
                .Substring(0, 1)
                .ToUpperInvariant();
        }

        string first =
            pieces[0]
                .Substring(0, 1);

        string last =
            pieces[pieces.Length - 1]
                .Substring(0, 1);

        return
            (first + last)
                .ToUpperInvariant();
    }

    private string GetTeamName(
        int teamId)
    {
        switch (teamId)
        {
            case 1:
                return "Team Red";

            case 2:
                return "Team Blue";

            case 3:
                return "Team Green";

            default:
                return
                    teamId > 0
                        ? $"Team {teamId}"
                        : "No Team";
        }
    }

    private Color GetTeamColor(
        int teamId)
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
                return Color.white;
        }
    }
}
