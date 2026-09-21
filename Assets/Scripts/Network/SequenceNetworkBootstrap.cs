using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class SequenceNetworkBootstrap : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private NetworkManager networkManager;

    [Header("Client Connection")]
    [SerializeField]
    private float clientConnectionTimeoutSeconds = 8f;

    private bool waitingForClientConnection = false;
    private bool intentionalShutdown = false;

    private Action clientConnectedCallback;
    private Action<string> clientConnectionFailedCallback;

    private Coroutine clientConnectionTimeoutCoroutine;

    // Fired only for this local remote-client instance
    // after it had already connected successfully and
    // then lost the connection.
    public event Action OnLocalClientDisconnected;

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

    private void OnDestroy()
    {
        CancelClientConnectionTimeout();

        CleanupClientCallbacks();
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

        intentionalShutdown =
            false;

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

        intentionalShutdown =
            false;

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
        // Ensure we have exactly one copy of each callback.
        //
        // IMPORTANT:
        // After a successful connection we remove only the
        // connected callback. We keep the disconnect callback
        // alive so we can detect an accidental later drop.
        // -----------------------------------------------------

        CleanupClientCallbacks();

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

            CancelClientConnectionTimeout();

            CleanupClientCallbacks();

            Debug.LogError(
                "Failed to begin Sequence client connection."
            );

            Action<string> callback =
                clientConnectionFailedCallback;

            ClearPendingCallbacks();

            callback?.Invoke(
                "Could not begin connection."
            );

            return false;
        }

        Debug.Log(
            "Sequence network CLIENT connection attempt started."
        );

        // Start our own connection timeout.
        //
        // This prevents the UI from staying forever on:
        //
        // "Joining as ..."
        //
        // when the host/server no longer exists.
        StartClientConnectionTimeout();

        return true;
    }

    // =========================================================
    // CLIENT CONNECTION TIMEOUT
    // =========================================================

    private void StartClientConnectionTimeout()
    {
        CancelClientConnectionTimeout();

        clientConnectionTimeoutCoroutine =
            StartCoroutine(
                ClientConnectionTimeoutRoutine()
            );
    }

    private IEnumerator ClientConnectionTimeoutRoutine()
    {
        float timeoutSeconds =
            Mathf.Max(
                1f,
                clientConnectionTimeoutSeconds
            );

        yield return
            new WaitForSecondsRealtime(
                timeoutSeconds
            );

        clientConnectionTimeoutCoroutine =
            null;

        // The connection completed or already failed.
        if (!waitingForClientConnection)
            yield break;

        Debug.LogWarning(
            $"Sequence client connection timed out " +
            $"after {timeoutSeconds:0.#} seconds."
        );

        waitingForClientConnection =
            false;

        Action<string> callback =
            clientConnectionFailedCallback;

        // Remove NGO callbacks before shutting down so the
        // shutdown-triggered disconnect callback cannot report
        // the same failure a second time.
        CleanupClientCallbacks();

        ClearPendingCallbacks();

        intentionalShutdown =
            true;

        if (networkManager != null &&
            networkManager.IsListening)
        {
            networkManager.Shutdown();
        }

        callback?.Invoke(
            "Could not reach the host. " +
            "The room may no longer exist."
        );
    }

    private void CancelClientConnectionTimeout()
    {
        if (clientConnectionTimeoutCoroutine == null)
            return;

        StopCoroutine(
            clientConnectionTimeoutCoroutine
        );

        clientConnectionTimeoutCoroutine =
            null;
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

        if (clientId !=
            networkManager.LocalClientId)
        {
            return;
        }

        waitingForClientConnection =
            false;

        CancelClientConnectionTimeout();

        Debug.Log(
            $"Sequence network CLIENT connected. " +
            $"NGO ClientId = {clientId}"
        );

        // We no longer need to listen for another initial
        // connection event, but we KEEP the disconnect
        // callback so an accidental drop can be detected.
        networkManager.OnClientConnectedCallback -=
            HandleClientConnected;

        Action callback =
            clientConnectedCallback;

        ClearPendingCallbacks();

        callback?.Invoke();
    }

    // =========================================================
    // CLIENT CONNECTION FAILED / LATER DISCONNECTED
    // =========================================================

    private void HandleClientDisconnected(
        ulong clientId)
    {
        if (networkManager == null)
            return;

        if (clientId !=
            networkManager.LocalClientId)
        {
            return;
        }

        bool wasWaitingForInitialConnection =
            waitingForClientConnection;

        waitingForClientConnection =
            false;

        CancelClientConnectionTimeout();

        CleanupClientCallbacks();

        if (intentionalShutdown)
        {
            return;
        }

        if (wasWaitingForInitialConnection)
        {
            Debug.LogWarning(
                "Sequence client failed to connect " +
                "before joining."
            );

            Action<string> callback =
                clientConnectionFailedCallback;

            ClearPendingCallbacks();

            callback?.Invoke(
                "Could not connect to the host."
            );

            return;
        }

        // This was a client that HAD joined successfully
        // and later lost the connection.
        Debug.LogWarning(
            "Sequence client connection was lost. " +
            "Rejoin is available."
        );

        ClearPendingCallbacks();

        OnLocalClientDisconnected?.Invoke();
    }

    // =========================================================
    // SHUTDOWN
    // =========================================================

    public void Shutdown()
    {
        intentionalShutdown =
            true;

        CancelClientConnectionTimeout();

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
