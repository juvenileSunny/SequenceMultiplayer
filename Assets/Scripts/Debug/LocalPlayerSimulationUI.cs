using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LocalPlayerSimulationUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RoomSessionContext roomSessionContext;
    [SerializeField] private LobbyManager lobbyManager;
    [SerializeField] private TMP_Dropdown simulatedPlayerDropdown;

    private bool suppressCallback = false;

    // =========================================================
    // UNITY
    // =========================================================

    private void OnEnable()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

        if (simulatedPlayerDropdown != null)
        {
            simulatedPlayerDropdown.onValueChanged.AddListener(
                HandlePlayerChanged
            );
        }

        if (lobbyManager != null)
        {
            lobbyManager.OnLobbyChanged +=
                HandleLobbyChanged;
        }

        if (roomSessionContext != null)
        {
            roomSessionContext.OnSessionChanged +=
                HandleSessionChanged;
        }

        RefreshDropdown();

#else

        // Never show this debug tool in a normal release build.
        gameObject.SetActive(false);

#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

        if (simulatedPlayerDropdown != null)
        {
            simulatedPlayerDropdown.onValueChanged.RemoveListener(
                HandlePlayerChanged
            );
        }

        if (lobbyManager != null)
        {
            lobbyManager.OnLobbyChanged -=
                HandleLobbyChanged;
        }

        if (roomSessionContext != null)
        {
            roomSessionContext.OnSessionChanged -=
                HandleSessionChanged;
        }

#endif
    }

    // =========================================================
    // LOBBY CHANGED
    // =========================================================

    private void HandleLobbyChanged()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        RefreshDropdown();
#endif
    }

    // =========================================================
    // SESSION CHANGED
    // =========================================================

    private void HandleSessionChanged()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        RefreshDropdown();
#endif
    }

    // =========================================================
    // BUILD DROPDOWN
    // =========================================================

    private void RefreshDropdown()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

        if (simulatedPlayerDropdown == null ||
            lobbyManager == null ||
            roomSessionContext == null)
        {
            return;
        }

        int playerCount =
            lobbyManager.PlayerCount;

        if (playerCount <= 0)
            return;

        suppressCallback = true;

        List<string> options =
            new List<string>();

        for (int playerId = 1;
             playerId <= playerCount;
             playerId++)
        {
            options.Add(
                $"PLAYER {playerId}"
            );
        }

        simulatedPlayerDropdown.ClearOptions();
        simulatedPlayerDropdown.AddOptions(
            options
        );

        int localPlayerId =
            roomSessionContext.LocalPlayerId;

        int selectedIndex =
            Mathf.Clamp(
                localPlayerId - 1,
                0,
                playerCount - 1
            );

        simulatedPlayerDropdown.SetValueWithoutNotify(
            selectedIndex
        );

        simulatedPlayerDropdown.RefreshShownValue();

        suppressCallback = false;

#endif
    }

    // =========================================================
    // SWITCH SIMULATED PLAYER
    // =========================================================

    private void HandlePlayerChanged(
        int dropdownIndex)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

        if (suppressCallback)
            return;

        if (roomSessionContext == null)
            return;

        int simulatedPlayerId =
            dropdownIndex + 1;

        roomSessionContext.DebugSetLocalPlayerId(
            simulatedPlayerId
        );
        
        bool isHost =
            roomSessionContext.IsLocalPlayerHost;

        Debug.LogWarning(
            $"DEBUG SIMULATION: Now controlling " +
            $"Player {simulatedPlayerId}. " +
            $"Role = {(isHost ? "HOST" : "CLIENT")}. " +
            $"HostPlayerId = {roomSessionContext.HostPlayerId}"
        );
#endif
    }
}