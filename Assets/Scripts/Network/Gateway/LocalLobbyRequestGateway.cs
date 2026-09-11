using System;
using UnityEngine;

public class LocalLobbyRequestGateway
    : LobbyRequestGateway
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("Local Authority")]
    [SerializeField]
    private LocalLobbyAuthority lobbyAuthority;

    [Header("Session")]
    [SerializeField]
    private RoomSessionContext roomSessionContext;

    // =========================================================
    // SEAT REQUEST
    // =========================================================

    public override void RequestSeat(
        int seatIndex,
        Action<AuthorityResult> onCompleted)
    {
        if (!CanSendRequest(
                out AuthorityResult failure))
        {
            onCompleted?.Invoke(
                failure
            );

            return;
        }

        RequestSeatRequest request =
            new RequestSeatRequest(
                seatIndex
            );

        AuthorityResult result =
            lobbyAuthority.HandleSeatRequest(
                roomSessionContext.LocalPlayerId,
                request
            );

        onCompleted?.Invoke(
            result
        );
    }

    // =========================================================
    // READY REQUEST
    // =========================================================

    public override void RequestReadyState(
        bool isReady,
        Action<AuthorityResult> onCompleted)
    {
        if (!CanSendRequest(
                out AuthorityResult failure))
        {
            onCompleted?.Invoke(
                failure
            );

            return;
        }

        SetReadyRequest request =
            new SetReadyRequest(
                isReady
            );

        AuthorityResult result =
            lobbyAuthority.HandleReadyRequest(
                roomSessionContext.LocalPlayerId,
                request
            );

        onCompleted?.Invoke(
            result
        );
    }

    // =========================================================
    // START MATCH REQUEST
    // =========================================================

    public override void RequestStartMatch(
        Action<AuthorityResult> onCompleted)
    {
        if (!CanSendRequest(
                out AuthorityResult failure))
        {
            onCompleted?.Invoke(
                failure
            );

            return;
        }

        StartMatchRequest request =
            new StartMatchRequest();

        AuthorityResult result =
            lobbyAuthority.HandleStartMatchRequest(
                roomSessionContext.LocalPlayerId,
                request
            );

        onCompleted?.Invoke(
            result
        );
    }

    // =========================================================
    // LOCAL REQUEST VALIDATION
    // =========================================================

    private bool CanSendRequest(
        out AuthorityResult failure)
    {
        failure = null;

        if (lobbyAuthority == null)
        {
            failure =
                AuthorityResult.Rejected(
                    AuthorityResultCode.LobbyNotAvailable,
                    "Lobby authority is unavailable."
                );

            return false;
        }

        if (roomSessionContext == null)
        {
            failure =
                AuthorityResult.Rejected(
                    AuthorityResultCode.RequestRejected,
                    "Room session is unavailable."
                );

            return false;
        }

        if (!roomSessionContext.HasLocalPlayer)
        {
            failure =
                AuthorityResult.Rejected(
                    AuthorityResultCode.InvalidPlayer,
                    "Local player identity is unavailable."
                );

            return false;
        }

        return true;
    }
}