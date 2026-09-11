using System;
using Unity.Netcode;
using UnityEngine;

public class SequenceNetworkBootstrap : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private NetworkManager networkManager;

    private bool waitingForClientConnection = false;

    private Action clientConnectedCallback;
    private Action<string> clientConnectionFailedCallback;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (networkManager == null)
        {
            networkManager =
                NetworkManager.Singleton;
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

            Debug.Log(
                $"NGO Host LocalClientId = " +
                $"{networkManager.LocalClientId}"
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

    public bool StartClient(
        Action onConnected,
        Action<string> onFailed)
    {
        if (!ValidateNetworkManager())
        {
            onFailed?.Invoke(
                "NetworkManager is unavailable."
            );

            return false;
        }

        if (networkManager.IsListening)
        {
            Debug.LogWarning(
                "Network is already running."
            );

            onFailed?.Invoke(
                "Networking is already running."
            );

            return false;
        }

        // -----------------------------------------------------
        // Store callbacks supplied by RoomEntryUI.
        // -----------------------------------------------------

        clientConnectedCallback =
            onConnected;

        clientConnectionFailedCallback =
            onFailed;

        waitingForClientConnection =
            true;

        // -----------------------------------------------------
        // Listen for NGO connection events BEFORE starting
        // the client.
        // -----------------------------------------------------

        networkManager.OnClientConnectedCallback +=
            HandleClientConnected;

        networkManager.OnClientDisconnectCallback +=
            HandleClientDisconnected;

        // -----------------------------------------------------
        // Start attempting to connect.
        //
        // IMPORTANT:
        // true here means:
        //
        // "The connection attempt started."
        //
        // It does NOT mean:
        //
        // "We are connected."
        // -----------------------------------------------------

        bool started =
            networkManager.StartClient();

        if (!started)
        {
            waitingForClientConnection =
                false;

            CleanupClientCallbacks();

            Debug.LogError(
                "Failed to begin Sequence client connection."
            );

            clientConnectionFailedCallback?.Invoke(
                "Could not begin connection."
            );

            ClearPendingCallbacks();

            return false;
        }

        Debug.Log(
            "Sequence network CLIENT connection attempt started."
        );

        return true;
    }

    // =========================================================
    // CLIENT CONNECTED
    // =========================================================

    private void HandleClientConnected(
        ulong clientId)
    {
        if (!waitingForClientConnection)
            return;

        if (networkManager == null)
            return;

        // We only care about THIS client's connection.
        if (clientId != networkManager.LocalClientId)
            return;

        waitingForClientConnection =
            false;

        Debug.Log(
            $"Sequence network CLIENT connected. " +
            $"NGO ClientId = {clientId}"
        );

        CleanupClientCallbacks();

        Action callback =
            clientConnectedCallback;

        ClearPendingCallbacks();

        callback?.Invoke();
    }

    // =========================================================
    // CLIENT CONNECTION FAILED / DISCONNECTED
    // =========================================================

    private void HandleClientDisconnected(
        ulong clientId)
    {
        if (!waitingForClientConnection)
            return;

        if (networkManager == null)
            return;

        if (clientId != networkManager.LocalClientId)
            return;

        waitingForClientConnection =
            false;

        Debug.LogWarning(
            "Sequence client failed to connect " +
            "or disconnected before joining."
        );

        CleanupClientCallbacks();

        Action<string> callback =
            clientConnectionFailedCallback;

        ClearPendingCallbacks();

        callback?.Invoke(
            "Could not connect to the host."
        );
    }

    // =========================================================
    // SHUTDOWN
    // =========================================================

    public void Shutdown()
    {
        CleanupClientCallbacks();

        ClearPendingCallbacks();

        waitingForClientConnection =
            false;

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
    // CALLBACK CLEANUP
    // =========================================================

    private void CleanupClientCallbacks()
    {
        if (networkManager == null)
            return;

        networkManager.OnClientConnectedCallback -=
            HandleClientConnected;

        networkManager.OnClientDisconnectCallback -=
            HandleClientDisconnected;
    }

    private void ClearPendingCallbacks()
    {
        clientConnectedCallback =
            null;

        clientConnectionFailedCallback =
            null;
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