using UnityEngine;

public class NetworkHandPresenter : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("Network")]
    [SerializeField]
    private NetworkHandState networkHandState;

    [SerializeField]
    private RoomSessionContext roomSessionContext;

    [Header("UI")]
    [SerializeField]
    private HandManager handManager;

    // =========================================================
    // LOCAL CLIENT-SIDE PLAYER COPY
    //
    // IMPORTANT:
    // This is NOT authoritative game state.
    //
    // It exists only so HandManager can render this
    // client's private cards.
    // =========================================================

    private Player localHandPlayer;

    // =========================================================
    // UNITY
    // =========================================================

    private void OnEnable()
    {
        if (networkHandState != null)
        {
            networkHandState.OnPrivateHandReceived +=
                HandlePrivateHandReceived;
        }
    }

    private void OnDisable()
    {
        if (networkHandState != null)
        {
            networkHandState.OnPrivateHandReceived -=
                HandlePrivateHandReceived;
        }
    }

    // =========================================================
    // PRIVATE HAND RECEIVED
    // =========================================================

    private void HandlePrivateHandReceived(
        int playerId,
        string[] cardCodes)
    {
        if (roomSessionContext == null)
        {
            Debug.LogWarning(
                "NetworkHandPresenter: " +
                "RoomSessionContext is missing."
            );

            return;
        }

        if (handManager == null)
        {
            Debug.LogWarning(
                "NetworkHandPresenter: " +
                "HandManager is missing."
            );

            return;
        }

        // -----------------------------------------------------
        // SECURITY / IDENTITY CHECK
        //
        // Even though the RPC is already targeted,
        // we additionally confirm that this hand belongs
        // to this local player.
        // -----------------------------------------------------

        if (!roomSessionContext.HasLocalPlayer)
            return;

        if (playerId !=
            roomSessionContext.LocalPlayerId)
        {
            Debug.LogWarning(
                $"Ignoring hand for Player {playerId}. " +
                $"This client owns Player " +
                $"{roomSessionContext.LocalPlayerId}."
            );

            return;
        }

        // -----------------------------------------------------
        // CREATE CLIENT-SIDE DISPLAY PLAYER
        // -----------------------------------------------------

        localHandPlayer =
            new Player(
                playerId
            );

        // -----------------------------------------------------
        // CONVERT NETWORK CODES BACK INTO CARDS
        // -----------------------------------------------------

        if (cardCodes != null)
        {
            foreach (string code in cardCodes)
            {
                if (Card.TryFromCode(
                        code,
                        out Card card))
                {
                    localHandPlayer.AddCard(
                        card
                    );
                }
                else
                {
                    Debug.LogWarning(
                        $"Could not parse network card: {code}"
                    );
                }
            }
        }

        // -----------------------------------------------------
        // DISPLAY THIS CLIENT'S PRIVATE HAND
        // -----------------------------------------------------

        handManager.Initialize(
            localHandPlayer
        );

        Debug.LogWarning(
            $"PRIVATE HAND rendered for " +
            $"local Player {playerId}: " +
            $"{localHandPlayer.Hand.Count} cards."
        );
    }
}