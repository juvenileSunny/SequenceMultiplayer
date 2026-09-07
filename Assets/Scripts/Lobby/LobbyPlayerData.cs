using System;

[Serializable]
public class LobbyPlayerData
{
    // =========================================================
    // IDENTITY
    // =========================================================

    public int PlayerId { get; private set; }

    public string DisplayName { get; private set; }

    // =========================================================
    // LOBBY STATE
    // =========================================================

    public int SeatIndex { get; private set; } = -1;

    public int TeamId { get; private set; } = -1;

    public bool IsReady { get; private set; } = false;

    // =========================================================
    // HELPERS
    // =========================================================

    public bool HasSeat =>
        SeatIndex > 0;

    public bool HasTeam =>
        TeamId > 0;

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public LobbyPlayerData(
        int playerId,
        string displayName)
    {
        PlayerId =
            playerId;

        DisplayName =
            string.IsNullOrWhiteSpace(displayName)
                ? $"Player {playerId}"
                : displayName;
    }

    // =========================================================
    // SEAT
    // =========================================================

    public void AssignSeat(
        int seatIndex,
        int teamCount)
    {
        if (seatIndex <= 0)
            return;

        if (teamCount <= 0)
            return;

        SeatIndex =
            seatIndex;

        // Team is determined by seat position.
        TeamId =
            ((seatIndex - 1) %
                teamCount) + 1;

        // Changing seat invalidates Ready.
        IsReady =
            false;
    }

    // =========================================================
    // READY
    // =========================================================

    public void SetReady(
        bool ready)
    {
        if (!HasSeat ||
            !HasTeam)
        {
            IsReady =
                false;

            return;
        }

        IsReady =
            ready;
    }

    // =========================================================
    // REMOVE FROM SEAT
    // =========================================================

    public void ClearSeat()
    {
        SeatIndex =
            -1;

        TeamId =
            -1;

        IsReady =
            false;
    }

    // =========================================================
    // DISPLAY NAME
    // =========================================================

    public void SetDisplayName(
        string displayName)
    {
        if (string.IsNullOrWhiteSpace(
                displayName))
        {
            return;
        }

        DisplayName =
            displayName;
    }
}