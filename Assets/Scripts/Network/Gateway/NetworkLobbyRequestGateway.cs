using System;
using UnityEngine;

public class NetworkLobbyRequestGateway :
    LobbyRequestGateway
{
    [Header("Network Bridge")]
    [SerializeField]
    private NetworkLobbyBridge networkLobbyBridge;

    // =========================================================
    // SEAT
    // =========================================================

    public override void RequestSeat(
        int seatIndex,
        Action<AuthorityResult> onCompleted)
    {
        if (networkLobbyBridge == null)
        {
            onCompleted?.Invoke(
                AuthorityResult.Rejected(
                    AuthorityResultCode.LobbyNotAvailable,
                    "Network lobby is unavailable."
                )
            );

            return;
        }

        networkLobbyBridge.RequestSeat(
            seatIndex,
            onCompleted
        );
    }

    // =========================================================
    // READY
    // =========================================================

    public override void RequestReadyState(
        bool isReady,
        Action<AuthorityResult> onCompleted)
    {
        if (networkLobbyBridge == null)
        {
            onCompleted?.Invoke(
                AuthorityResult.Rejected(
                    AuthorityResultCode.LobbyNotAvailable,
                    "Network lobby is unavailable."
                )
            );

            return;
        }

        networkLobbyBridge.RequestReadyState(
            isReady,
            onCompleted
        );
    }

    // =========================================================
    // START MATCH
    // =========================================================

    public override void RequestStartMatch(
        Action<AuthorityResult> onCompleted)
    {
        if (networkLobbyBridge == null)
        {
            onCompleted?.Invoke(
                AuthorityResult.Rejected(
                    AuthorityResultCode.LobbyNotAvailable,
                    "Network lobby is unavailable."
                )
            );

            return;
        }

        networkLobbyBridge.RequestStartMatch(
            onCompleted
        );
    }
}