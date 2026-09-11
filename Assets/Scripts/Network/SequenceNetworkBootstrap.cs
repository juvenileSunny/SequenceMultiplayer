using Unity.Netcode;
using UnityEngine;

public class SequenceNetworkBootstrap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NetworkManager networkManager;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (networkManager == null)
        {
            networkManager = NetworkManager.Singleton;
        }
    }

    // =========================================================
    // START HOST
    // =========================================================

    public bool StartHost()
    {
        if (!ValidateNetworkManager())
            return false;

        if (networkManager.IsListening)
        {
            Debug.LogWarning(
                "Network is already running."
            );

            return false;
        }

        bool started =
            networkManager.StartHost();

        if (started)
        {
            Debug.Log(
                "Sequence network HOST started."
            );
        }
        else
        {
            Debug.LogError(
                "Failed to start Sequence network host."
            );
        }

        return started;
    }

    // =========================================================
    // START CLIENT
    // =========================================================

    public bool StartClient()
    {
        if (!ValidateNetworkManager())
            return false;

        if (networkManager.IsListening)
        {
            Debug.LogWarning(
                "Network is already running."
            );

            return false;
        }

        bool started =
            networkManager.StartClient();

        if (started)
        {
            Debug.Log(
                "Sequence network CLIENT started."
            );
        }
        else
        {
            Debug.LogError(
                "Failed to start Sequence network client."
            );
        }

        return started;
    }

    // =========================================================
    // SHUTDOWN
    // =========================================================

    public void Shutdown()
    {
        if (networkManager == null)
            return;

        if (!networkManager.IsListening)
            return;

        networkManager.Shutdown();

        Debug.Log(
            "Sequence network stopped."
        );
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    private bool ValidateNetworkManager()
    {
        if (networkManager == null)
        {
            Debug.LogError(
                "SequenceNetworkBootstrap: " +
                "NetworkManager is missing."
            );

            return false;
        }

        return true;
    }
}