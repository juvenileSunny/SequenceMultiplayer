using UnityEngine;

public class LocalLobbyAuthority : MonoBehaviour
{
    [Header("Authoritative Lobby")]
    [SerializeField] private LobbyManager lobbyManager;

    // =========================================================
    // REQUEST SEAT
    // =========================================================

    public AuthorityResult HandleSeatRequest(
        int senderPlayerId,
        RequestSeatRequest request)
    {
        // -----------------------------------------------------
        // REQUEST VALIDATION
        // -----------------------------------------------------

        if (request == null)
        {
            return AuthorityResult.Rejected(
                AuthorityResultCode.InvalidRequest,
                "Seat request was invalid."
            );
        }

        if (lobbyManager == null)
        {
            return AuthorityResult.Rejected(
                AuthorityResultCode.LobbyNotAvailable,
                "Lobby authority is unavailable."
            );
        }

        // -----------------------------------------------------
        // IDENTIFY SENDER
        // -----------------------------------------------------

        LobbyPlayerData player =
            lobbyManager.GetPlayer(
                senderPlayerId
            );

        if (player == null)
        {
            return AuthorityResult.Rejected(
                AuthorityResultCode.InvalidPlayer,
                "Player does not exist in this lobby."
            );
        }

        // -----------------------------------------------------
        // LEAVE CURRENT SEAT
        // -----------------------------------------------------

        if (request.SeatIndex <= 0)
        {
            lobbyManager.ClearPlayerSeat(
                senderPlayerId
            );

            return AuthorityResult.Accepted(
                "Seat cleared."
            );
        }

        // -----------------------------------------------------
        // SEAT RANGE
        // -----------------------------------------------------

        if (request.SeatIndex >
            lobbyManager.PlayerCount)
        {
            return AuthorityResult.Rejected(
                AuthorityResultCode.InvalidSeat,
                $"Seat {request.SeatIndex} does not exist."
            );
        }

        // -----------------------------------------------------
        // OCCUPANCY CHECK
        // -----------------------------------------------------

        LobbyPlayerData occupant =
            lobbyManager.GetPlayerInSeat(
                request.SeatIndex
            );

        if (occupant != null &&
            occupant.PlayerId != senderPlayerId)
        {
            return AuthorityResult.Rejected(
                AuthorityResultCode.SeatTaken,
                $"Seat {request.SeatIndex} is already occupied."
            );
        }

        // -----------------------------------------------------
        // AUTHORITATIVE ASSIGNMENT
        // -----------------------------------------------------

        bool assigned =
            lobbyManager.TryAssignSeat(
                senderPlayerId,
                request.SeatIndex
            );

        if (!assigned)
        {
            return AuthorityResult.Rejected(
                AuthorityResultCode.RequestRejected,
                $"Seat {request.SeatIndex} could not be assigned."
            );
        }

        // -----------------------------------------------------
        // SUCCESS
        // -----------------------------------------------------

        LobbyPlayerData updatedPlayer =
            lobbyManager.GetPlayer(
                senderPlayerId
            );

        string teamName =
            GetTeamName(
                updatedPlayer != null
                    ? updatedPlayer.TeamId
                    : -1
            );

        return AuthorityResult.Accepted(
            $"Seat {request.SeatIndex} selected. " +
            $"Assigned to {teamName}."
        );
    }

    // =========================================================
    // READY REQUEST
    // =========================================================

    public AuthorityResult HandleReadyRequest(
        int senderPlayerId,
        SetReadyRequest request)
    {
        if (request == null)
        {
            return AuthorityResult.Rejected(
                AuthorityResultCode.InvalidRequest,
                "Ready request was invalid."
            );
        }

        if (lobbyManager == null)
        {
            return AuthorityResult.Rejected(
                AuthorityResultCode.LobbyNotAvailable,
                "Lobby authority is unavailable."
            );
        }

        LobbyPlayerData player =
            lobbyManager.GetPlayer(
                senderPlayerId
            );

        if (player == null)
        {
            return AuthorityResult.Rejected(
                AuthorityResultCode.InvalidPlayer,
                "Player does not exist in this lobby."
            );
        }

        if (player.SeatIndex <= 0 ||
            player.TeamId <= 0)
        {
            return AuthorityResult.Rejected(
                AuthorityResultCode.PlayerNotSeated,
                "Select a seat before marking yourself ready."
            );
        }

        bool success =
            lobbyManager.SetPlayerReady(
                senderPlayerId,
                request.IsReady
            );

        if (!success)
        {
            return AuthorityResult.Rejected(
                AuthorityResultCode.RequestRejected,
                "Ready state could not be changed."
            );
        }

        return AuthorityResult.Accepted(
            request.IsReady
                ? "Player is ready."
                : "Player is no longer ready."
        );
    }

    // =========================================================
    // TEAM NAME
    // =========================================================

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
                return "No Team";
        }
    }
}