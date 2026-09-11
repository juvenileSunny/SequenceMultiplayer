using System;
using UnityEngine;

public abstract class LobbyRequestGateway : MonoBehaviour
{
    // =========================================================
    // SEAT
    // =========================================================

    public abstract void RequestSeat(
        int seatIndex,
        Action<AuthorityResult> onCompleted
    );

    // =========================================================
    // READY
    // =========================================================

    public abstract void RequestReadyState(
        bool isReady,
        Action<AuthorityResult> onCompleted
    );

    // =========================================================
    // START MATCH
    // =========================================================

    public abstract void RequestStartMatch(
        Action<AuthorityResult> onCompleted
    );
}