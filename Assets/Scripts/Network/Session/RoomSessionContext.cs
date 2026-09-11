using UnityEngine;

public class RoomSessionContext : MonoBehaviour
{
    [Header("Current Room")]
    [SerializeField] private string roomCode = "";

    [Header("Player Identity")]
    [SerializeField] private int localPlayerId = -1;
    [SerializeField] private int hostPlayerId = -1;

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
        !string.IsNullOrWhiteSpace(roomCode);

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

        // The player who creates the room
        // becomes the room host.
        hostPlayerId =
            newLocalPlayerId;

        Debug.Log(
            $"Session configured as HOST. " +
            $"Room={roomCode}, " +
            $"LocalPlayer={localPlayerId}, " +
            $"HostPlayer={hostPlayerId}"
        );
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
    }

    // =========================================================
    // CLEAR
    // =========================================================

    public void ClearSession()
    {
        roomCode = "";
        localPlayerId = -1;
        hostPlayerId = -1;

        Debug.Log(
            "Room session cleared."
        );
    }
}