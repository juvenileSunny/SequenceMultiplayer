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

    [Header("Rejoin Identity")]
    [SerializeField] private string rejoinToken = "";

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

    public string RejoinToken =>
        rejoinToken;

    public bool HasRoom =>
        !string.IsNullOrWhiteSpace(
            roomCode
        );

    public bool HasLocalPlayer =>
        localPlayerId > 0;

    public bool HasHost =>
        hostPlayerId > 0;

    public bool HasRejoinToken =>
        !string.IsNullOrWhiteSpace(
            rejoinToken
        );

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
            NormalizeRoomCode(
                newRoomCode
            );

        localPlayerId =
            newLocalPlayerId;

        // Room creator becomes host.
        hostPlayerId =
            newLocalPlayerId;

        // Host migration/rejoin is not supported yet.
        // Remote-client rejoin uses this token system.
        rejoinToken =
            "";

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
        int newHostPlayerId,
        string newRejoinToken = null)
    {
        roomCode =
            NormalizeRoomCode(
                newRoomCode
            );

        localPlayerId =
            newLocalPlayerId;

        hostPlayerId =
            newHostPlayerId;

        if (!string.IsNullOrWhiteSpace(
                newRejoinToken))
        {
            rejoinToken =
                newRejoinToken.Trim();

            SaveRejoinToken(
                roomCode,
                rejoinToken
            );
        }
        else
        {
            rejoinToken =
                GetOrCreateRejoinToken(
                    roomCode
                );
        }

        Debug.Log(
            $"Session configured as CLIENT. " +
            $"Room={roomCode}, " +
            $"LocalPlayer={localPlayerId}, " +
            $"HostPlayer={hostPlayerId}, " +
            $"RejoinIdentityReady={HasRejoinToken}"
        );

        OnSessionChanged?.Invoke();
    }

    // =========================================================
    // REJOIN TOKEN
    // =========================================================

    public string GetOrCreateRejoinToken(
        string targetRoomCode)
    {
        string normalizedRoomCode =
            NormalizeRoomCode(
                targetRoomCode
            );

        if (string.IsNullOrWhiteSpace(
                normalizedRoomCode))
        {
            Debug.LogWarning(
                "Cannot create a rejoin token without a room code."
            );

            return "";
        }

        string key =
            GetRejoinTokenKey(
                normalizedRoomCode
            );

        if (PlayerPrefs.HasKey(
                key))
        {
            string savedToken =
                PlayerPrefs.GetString(
                    key,
                    ""
                );

            if (!string.IsNullOrWhiteSpace(
                    savedToken))
            {
                rejoinToken =
                    savedToken;

                return rejoinToken;
            }
        }

        rejoinToken =
            Guid.NewGuid()
                .ToString("N");

        SaveRejoinToken(
            normalizedRoomCode,
            rejoinToken
        );

        Debug.Log(
            $"Created persistent rejoin identity " +
            $"for Room {normalizedRoomCode}."
        );

        return rejoinToken;
    }

    public bool TryLoadRejoinToken(
        string targetRoomCode,
        out string token)
    {
        token = "";

        string normalizedRoomCode =
            NormalizeRoomCode(
                targetRoomCode
            );

        if (string.IsNullOrWhiteSpace(
                normalizedRoomCode))
        {
            return false;
        }

        string key =
            GetRejoinTokenKey(
                normalizedRoomCode
            );

        if (!PlayerPrefs.HasKey(
                key))
        {
            return false;
        }

        token =
            PlayerPrefs.GetString(
                key,
                ""
            );

        if (string.IsNullOrWhiteSpace(
                token))
        {
            token = "";
            return false;
        }

        rejoinToken =
            token;

        return true;
    }

    public void ForgetRejoinIdentity(
        string targetRoomCode)
    {
        string normalizedRoomCode =
            NormalizeRoomCode(
                targetRoomCode
            );

        if (string.IsNullOrWhiteSpace(
                normalizedRoomCode))
        {
            return;
        }

        string key =
            GetRejoinTokenKey(
                normalizedRoomCode
            );

        if (PlayerPrefs.HasKey(
                key))
        {
            PlayerPrefs.DeleteKey(
                key
            );

            PlayerPrefs.Save();
        }

        if (roomCode ==
            normalizedRoomCode)
        {
            rejoinToken =
                "";
        }

        Debug.Log(
            $"Forgot rejoin identity for " +
            $"Room {normalizedRoomCode}."
        );
    }

    private void SaveRejoinToken(
        string targetRoomCode,
        string token)
    {
        if (string.IsNullOrWhiteSpace(
                targetRoomCode))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(
                token))
        {
            return;
        }

        PlayerPrefs.SetString(
            GetRejoinTokenKey(
                targetRoomCode
            ),
            token
        );

        PlayerPrefs.Save();
    }

    private string GetRejoinTokenKey(
        string targetRoomCode)
    {
        return
            "SequenceGame.RejoinToken." +
            NormalizeRoomCode(
                targetRoomCode
            );
    }

    private string NormalizeRoomCode(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return "";
        }

        return value
            .Trim()
            .ToUpperInvariant();
    }

    // =========================================================
    // CLEAR SESSION
    // =========================================================

    public void ClearSession(
        bool forgetRejoinIdentity = false)
    {
        string previousRoomCode =
            roomCode;

        if (forgetRejoinIdentity &&
            !string.IsNullOrWhiteSpace(
                previousRoomCode))
        {
            ForgetRejoinIdentity(
                previousRoomCode
            );
        }

        roomCode = "";
        localPlayerId = -1;
        hostPlayerId = -1;

        // Keep the persisted token by default so an
        // accidental disconnect/app restart can rejoin.
        rejoinToken = "";

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
