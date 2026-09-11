using System;
using UnityEngine;

public class RoomSessionContext : MonoBehaviour
{
    // =========================================================
    // CURRENT ROOM
    // =========================================================

    [Header("Current Room")]
    [SerializeField] private string roomCode = "";

    // =========================================================
    // PLAYER IDENTITY
    // =========================================================

    [Header("Player Identity")]
    [SerializeField] private int localPlayerId = -1;
    [SerializeField] private int hostPlayerId = -1;

    // =========================================================
    // EVENTS
    // =========================================================

    public event Action OnSessionChanged;

    // =========================================================
    // PUBLIC DATA
    // =========================================================

    public string RoomCode =>
        roomCode;

    public int LocalPlayerId =>
        localPlayerId;

    public int HostPlayerId =>
        hostPlayerId;

    public bool HasRoom =>
        !string.IsNullOrWhiteSpace(
            roomCode
        );

    public bool HasLocalPlayer =>
        localPlayerId > 0;

    public bool HasHost =>
        hostPlayerId > 0;

    public bool IsLocalPlayerHost =>
        HasLocalPlayer &&
        HasHost &&
        localPlayerId == hostPlayerId;

    // =========================================================
    // CREATE ROOM
    // =========================================================

    public void ConfigureAsHost(
        string newRoomCode,
        int newLocalPlayerId)
    {
        roomCode =
            newRoomCode;

        localPlayerId =
            newLocalPlayerId;

        // Room creator becomes host.
        hostPlayerId =
            newLocalPlayerId;

        Debug.Log(
            $"Session configured as HOST. " +
            $"Room={roomCode}, " +
            $"LocalPlayer={localPlayerId}, " +
            $"HostPlayer={hostPlayerId}"
        );

        OnSessionChanged?.Invoke();
    }

    // =========================================================
    // JOIN ROOM
    // =========================================================

    public void ConfigureAsClient(
        string newRoomCode,
        int newLocalPlayerId,
        int newHostPlayerId)
    {
        roomCode =
            newRoomCode;

        localPlayerId =
            newLocalPlayerId;

        hostPlayerId =
            newHostPlayerId;

        Debug.Log(
            $"Session configured as CLIENT. " +
            $"Room={roomCode}, " +
            $"LocalPlayer={localPlayerId}, " +
            $"HostPlayer={hostPlayerId}"
        );

        OnSessionChanged?.Invoke();
    }

    // =========================================================
    // CLEAR SESSION
    // =========================================================

    public void ClearSession()
    {
        roomCode = "";
        localPlayerId = -1;
        hostPlayerId = -1;

        Debug.Log(
            "Room session cleared."
        );

        OnSessionChanged?.Invoke();
    }

    // =========================================================
    // DEVELOPMENT PLAYER SIMULATION
    // =========================================================

#if UNITY_EDITOR || DEVELOPMENT_BUILD

    public void DebugSetLocalPlayerId(
        int playerId)
    {
        if (playerId <= 0)
        {
            Debug.LogWarning(
                $"DEBUG: Invalid simulated PlayerId " +
                $"{playerId}."
            );

            return;
        }

        localPlayerId =
            playerId;

        Debug.LogWarning(
            $"DEBUG: This Unity instance is now " +
            $"simulating Player {localPlayerId}."
        );

        OnSessionChanged?.Invoke();
    }

#endif
}