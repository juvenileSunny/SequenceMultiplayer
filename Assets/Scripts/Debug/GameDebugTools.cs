using Unity.Netcode;
using UnityEngine;

public class GameDebugTools : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

        // F1:
        // Create next Sequence for current player's team.
        if (Input.GetKeyDown(
                KeyCode.F1))
        {
            TestCreateSequence();
        }

#endif
    }

    // =========================================================
    // F1 - CREATE SEQUENCE
    // =========================================================

    private void TestCreateSequence()
    {
        if (gameManager == null)
        {
            Debug.LogError(
                "DEBUG: GameManager reference is missing."
            );

            return;
        }

        NetworkManager networkManager =
            NetworkManager.Singleton;

        // F1 changes authoritative board state directly.
        // In a network game, run it from the host/server window.
        //
        // The host can still create the Sequence for Player 2
        // or Player 3's team because GameManager uses the
        // authoritative current turn, not the local host team.
        if (networkManager != null &&
            networkManager.IsListening &&
            !networkManager.IsServer)
        {
            Debug.LogWarning(
                "DEBUG F1: Use F1 in the HOST/SERVER window. " +
                "Remote clients cannot directly modify the " +
                "authoritative board."
            );

            return;
        }

        Debug.LogWarning(
            "DEBUG TEST: F1 pressed on authority."
        );

        gameManager
            .DebugCreateSequenceForCurrentTeam();
    }
}