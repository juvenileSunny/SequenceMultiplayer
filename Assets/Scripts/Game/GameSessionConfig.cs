using System;
using System.Collections.Generic;

[Serializable]
public class PlayerSlotData
{
    public int PlayerId;
    public int SeatIndex;
    public int TeamId;

    public PlayerSlotData(
        int playerId,
        int seatIndex,
        int teamId)
    {
        PlayerId = playerId;
        SeatIndex = seatIndex;
        TeamId = teamId;
    }
}

[Serializable]
public class GameSessionConfig
{
    // =========================================================
    // GAME SETTINGS
    // =========================================================

    public int PlayerCount;
    public int TeamCount;

    // =========================================================
    // PLAYER / SEAT / TEAM CONFIGURATION
    // =========================================================

    public List<PlayerSlotData> PlayerSlots =
        new List<PlayerSlotData>();

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public GameSessionConfig(
        int playerCount,
        int teamCount)
    {
        PlayerCount = playerCount;
        TeamCount = teamCount;
    }

    // =========================================================
    // ADD PLAYER
    // =========================================================

    public void AddPlayer(
        int playerId,
        int seatIndex,
        int teamId)
    {
        PlayerSlotData slot =
            new PlayerSlotData(
                playerId,
                seatIndex,
                teamId
            );

        PlayerSlots.Add(
            slot
        );
    }

    // =========================================================
    // FIND PLAYER
    // =========================================================

    public PlayerSlotData GetPlayer(
        int playerId)
    {
        foreach (PlayerSlotData slot
                 in PlayerSlots)
        {
            if (slot.PlayerId ==
                playerId)
            {
                return slot;
            }
        }

        return null;
    }

    // =========================================================
    // FIND SEAT
    // =========================================================

    public PlayerSlotData GetSeat(
        int seatIndex)
    {
        foreach (PlayerSlotData slot
                 in PlayerSlots)
        {
            if (slot.SeatIndex ==
                seatIndex)
            {
                return slot;
            }
        }

        return null;
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    public bool IsValid(
        out string errorMessage)
    {
        errorMessage = "";

        // -----------------------------------------------------
        // PLAYER COUNT
        // -----------------------------------------------------

        if (!IsSupportedPlayerCount(
                PlayerCount))
        {
            errorMessage =
                $"Unsupported player count: {PlayerCount}";

            return false;
        }

        // -----------------------------------------------------
        // TEAM COUNT
        // -----------------------------------------------------

        if (TeamCount != 2 &&
            TeamCount != 3)
        {
            errorMessage =
                $"Unsupported team count: {TeamCount}";

            return false;
        }

        // -----------------------------------------------------
        // EQUAL TEAM SIZE
        // -----------------------------------------------------

        if (PlayerCount % TeamCount != 0)
        {
            errorMessage =
                $"{PlayerCount} players cannot be " +
                $"divided evenly into {TeamCount} teams.";

            return false;
        }

        // -----------------------------------------------------
        // NUMBER OF PLAYER SLOTS
        // -----------------------------------------------------

        if (PlayerSlots.Count !=
            PlayerCount)
        {
            errorMessage =
                $"Expected {PlayerCount} player slots, " +
                $"but found {PlayerSlots.Count}.";

            return false;
        }

        HashSet<int> playerIds =
            new HashSet<int>();

        HashSet<int> seats =
            new HashSet<int>();

        int[] teamSizes =
            new int[TeamCount + 1];

        // -----------------------------------------------------
        // INDIVIDUAL PLAYER VALIDATION
        // -----------------------------------------------------

        foreach (PlayerSlotData slot
                 in PlayerSlots)
        {
            if (slot.PlayerId <= 0)
            {
                errorMessage =
                    "PlayerId must be greater than zero.";

                return false;
            }

            if (!playerIds.Add(
                    slot.PlayerId))
            {
                errorMessage =
                    $"Duplicate PlayerId: " +
                    $"{slot.PlayerId}";

                return false;
            }

            if (slot.SeatIndex < 1 ||
                slot.SeatIndex >
                PlayerCount)
            {
                errorMessage =
                    $"Invalid SeatIndex: " +
                    $"{slot.SeatIndex}";

                return false;
            }

            if (!seats.Add(
                    slot.SeatIndex))
            {
                errorMessage =
                    $"Seat {slot.SeatIndex} " +
                    "has already been taken.";

                return false;
            }

            if (slot.TeamId < 1 ||
                slot.TeamId >
                TeamCount)
            {
                errorMessage =
                    $"Invalid TeamId: " +
                    $"{slot.TeamId}";

                return false;
            }

            teamSizes[
                slot.TeamId
            ]++;
        }

        // -----------------------------------------------------
        // VERIFY TEAM SIZES
        // -----------------------------------------------------

        int requiredTeamSize =
            PlayerCount /
            TeamCount;

        for (int teamId = 1;
             teamId <= TeamCount;
             teamId++)
        {
            if (teamSizes[teamId] !=
                requiredTeamSize)
            {
                errorMessage =
                    $"Team {teamId} has " +
                    $"{teamSizes[teamId]} players. " +
                    $"Expected {requiredTeamSize}.";

                return false;
            }
        }

        // -----------------------------------------------------
        // VERIFY TEAM TURN ORDER
        //
        // Teams must alternate around the table.
        //
        // 2 teams:
        // T1 T2 T1 T2 ...
        //
        // 3 teams:
        // T1 T2 T3 T1 T2 T3 ...
        // -----------------------------------------------------

        for (int seatIndex = 1;
             seatIndex <= PlayerCount;
             seatIndex++)
        {
            PlayerSlotData slot =
                GetSeat(
                    seatIndex
                );

            if (slot == null)
            {
                errorMessage =
                    $"Seat {seatIndex} is empty.";

                return false;
            }

            int expectedTeamId =
                ((seatIndex - 1) %
                    TeamCount) + 1;

            if (slot.TeamId !=
                expectedTeamId)
            {
                errorMessage =
                    $"Seat {seatIndex} must belong " +
                    $"to Team {expectedTeamId}, " +
                    $"but Player {slot.PlayerId} " +
                    $"is assigned to Team " +
                    $"{slot.TeamId}.";

                return false;
            }
        }

        return true;
    }

    // =========================================================
    // SUPPORTED PLAYER COUNTS
    // =========================================================

    private bool IsSupportedPlayerCount(
        int count)
    {
        return
            count == 2 ||
            count == 3 ||
            count == 4 ||
            count == 6 ||
            count == 8 ||
            count == 9 ||
            count == 10 ||
            count == 12;
    }
}