using TMPro;
using UnityEngine;

public class NetworkTurnController : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("Network")]
    [SerializeField]
    private NetworkGameState networkGameState;

    [SerializeField]
    private RoomSessionContext roomSessionContext;

    [Header("Local Game")]
    [SerializeField]
    private GameManager gameManager;

    [Header("UI")]
    [SerializeField]
    private TMP_Text turnText;

    // =========================================================
    // UNITY
    // =========================================================

    private void OnEnable()
    {
        if (networkGameState != null)
        {
            networkGameState.OnCurrentPlayerChanged +=
                HandleCurrentPlayerChanged;
        }

        if (roomSessionContext != null)
        {
            roomSessionContext.OnSessionChanged +=
                HandleSessionChanged;
        }

        RefreshTurnState();
    }

    private void OnDisable()
    {
        if (networkGameState != null)
        {
            networkGameState.OnCurrentPlayerChanged -=
                HandleCurrentPlayerChanged;
        }

        if (roomSessionContext != null)
        {
            roomSessionContext.OnSessionChanged -=
                HandleSessionChanged;
        }
    }

    // =========================================================
    // EVENTS
    // =========================================================

    private void HandleCurrentPlayerChanged(
        int previousPlayerId,
        int newPlayerId)
    {
        RefreshTurnState();
    }

    private void HandleSessionChanged()
    {
        RefreshTurnState();
    }

    // =========================================================
    // REFRESH
    // =========================================================

    private void RefreshTurnState()
    {
        if (networkGameState == null ||
            roomSessionContext == null)
        {
            return;
        }

        int currentPlayerId =
            networkGameState.CurrentPlayerId;

        int localPlayerId =
            roomSessionContext.LocalPlayerId;

        if (currentPlayerId <= 0 ||
            localPlayerId <= 0)
        {
            if (turnText != null)
            {
                turnText.text =
                    "WAITING FOR GAME...";
            }

            if (gameManager != null)
            {
                gameManager.SetLocalTurnInputEnabled(
                    false
                );
            }

            return;
        }

        bool isMyTurn =
            currentPlayerId ==
            localPlayerId;

        // =====================================================
        // INPUT
        // =====================================================

        if (gameManager != null)
        {
            gameManager.SetLocalTurnInputEnabled(
                isMyTurn
            );
        }

        // =====================================================
        // UI
        // =====================================================

        if (turnText != null)
        {
            if (isMyTurn)
            {
                turnText.text =
                    "YOUR TURN";
            }
            else
            {
                turnText.text =
                    $"PLAYER {currentPlayerId}'S TURN";
            }
        }

        Debug.Log(
            $"NETWORK TURN VIEW: " +
            $"LocalPlayer={localPlayerId}, " +
            $"CurrentPlayer={currentPlayerId}, " +
            $"IsMyTurn={isMyTurn}"
        );
    }
}