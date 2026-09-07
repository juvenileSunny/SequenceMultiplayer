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

        Debug.LogWarning(
            "DEBUG TEST: F1 pressed."
        );

        gameManager
            .DebugCreateSequenceForCurrentTeam();
    }
}