using System.Collections.Generic;
using UnityEngine;

public class GamePlayerStatusUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private LobbyManager lobbyManager;
    [SerializeField] private NetworkLobbyBridge networkLobbyBridge;
    [SerializeField] private NetworkGameState networkGameState;
    [SerializeField] private RoomSessionContext roomSessionContext;

    [Header("Player Containers")]
    [SerializeField] private Transform topPlayers;
    [SerializeField] private Transform rightPlayers;
    [SerializeField] private Transform bottomPlayers;
    [SerializeField] private Transform leftPlayers;

    [Header("Prefab")]
    [SerializeField] private GamePlayerStatusSlotUI playerSlotPrefab;

    private readonly List<GamePlayerStatusSlotUI> spawnedSlots =
        new List<GamePlayerStatusSlotUI>();

    private void OnEnable()
    {
        if (networkLobbyBridge != null)
            networkLobbyBridge.OnPublicPlayerStateChanged += Refresh;

        if (networkGameState != null)
            networkGameState.OnPublicGameStateChanged += Refresh;

        if (roomSessionContext != null)
            roomSessionContext.OnSessionChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (networkLobbyBridge != null)
            networkLobbyBridge.OnPublicPlayerStateChanged -= Refresh;

        if (networkGameState != null)
            networkGameState.OnPublicGameStateChanged -= Refresh;

        if (roomSessionContext != null)
            roomSessionContext.OnSessionChanged -= Refresh;
    }

    public void Refresh()
    {
        if (lobbyManager == null ||
            playerSlotPrefab == null ||
            topPlayers == null ||
            rightPlayers == null ||
            bottomPlayers == null ||
            leftPlayers == null)
            return;

        List<LobbyPlayerData> players = GetPlayersSortedBySeat();

        EnsureSlotCount(players.Count);

        GetSeatLayout(
            players.Count,
            out List<int> topSeats,
            out List<int> rightSeats,
            out List<int> bottomSeats,
            out List<int> leftSeats
        );

        Dictionary<int, LobbyPlayerData> playerBySeat =
            new Dictionary<int, LobbyPlayerData>();

        foreach (LobbyPlayerData player in players)
        {
            if (player != null && player.SeatIndex > 0)
                playerBySeat[player.SeatIndex] = player;
        }

        int slotIndex = 0;

        slotIndex = PlaceSide(topSeats, topPlayers, playerBySeat, slotIndex);
        slotIndex = PlaceSide(rightSeats, rightPlayers, playerBySeat, slotIndex);
        slotIndex = PlaceSide(bottomSeats, bottomPlayers, playerBySeat, slotIndex);
        slotIndex = PlaceSide(leftSeats, leftPlayers, playerBySeat, slotIndex);

        for (int i = slotIndex; i < spawnedSlots.Count; i++)
            spawnedSlots[i].gameObject.SetActive(false);
    }

    private List<LobbyPlayerData> GetPlayersSortedBySeat()
    {
        List<LobbyPlayerData> players =
            new List<LobbyPlayerData>();

        if (lobbyManager == null)
            return players;

        for (int playerId = 1;
             playerId <= lobbyManager.PlayerCount;
             playerId++)
        {
            LobbyPlayerData player =
                lobbyManager.GetPlayer(playerId);

            if (player != null && player.HasSeat)
                players.Add(player);
        }

        players.Sort(
            (a, b) => a.SeatIndex.CompareTo(b.SeatIndex)
        );

        return players;
    }

    private int PlaceSide(
        List<int> seatNumbers,
        Transform parent,
        Dictionary<int, LobbyPlayerData> playerBySeat,
        int slotIndex)
    {
        foreach (int seatNumber in seatNumbers)
        {
            if (!playerBySeat.TryGetValue(
                    seatNumber,
                    out LobbyPlayerData player))
                continue;

            GamePlayerStatusSlotUI slot =
                spawnedSlots[slotIndex];

            slotIndex++;

            slot.transform.SetParent(parent, false);
            slot.transform.SetAsLastSibling();
            slot.gameObject.SetActive(true);

            string displayName =
                networkLobbyBridge != null
                    ? networkLobbyBridge.GetPlayerDisplayName(
                        player.PlayerId)
                    : player.DisplayName;

            bool isConnected =
                networkLobbyBridge != null
                    ? networkLobbyBridge.IsPlayerConnected(
                        player.PlayerId)
                    : player.IsConnected;

            int currentPlayerId =
                networkGameState != null
                    ? networkGameState.CurrentPlayerId
                    : -1;

            int localPlayerId =
                roomSessionContext != null &&
                roomSessionContext.HasLocalPlayer
                    ? roomSessionContext.LocalPlayerId
                    : -1;

            slot.Configure(
                player.PlayerId,
                displayName,
                player.SeatIndex,
                player.TeamId,
                isConnected,
                player.PlayerId == currentPlayerId,
                player.PlayerId == localPlayerId
            );
        }

        return slotIndex;
    }

    private void GetSeatLayout(
        int playerCount,
        out List<int> top,
        out List<int> right,
        out List<int> bottom,
        out List<int> left)
    {
        top = new List<int>();
        right = new List<int>();
        bottom = new List<int>();
        left = new List<int>();

        switch (playerCount)
        {
            case 2:
                top.Add(1);
                bottom.Add(2);
                break;

            case 3:
                top.Add(1);
                bottom.AddRange(new[] { 3, 2 });
                break;

            case 4:
                top.Add(1);
                right.Add(2);
                bottom.Add(3);
                left.Add(4);
                break;

            case 6:
                top.AddRange(new[] { 1, 2 });
                right.Add(3);
                bottom.AddRange(new[] { 5, 4 });
                left.Add(6);
                break;

            case 8:
                top.AddRange(new[] { 1, 2 });
                right.AddRange(new[] { 3, 4 });
                bottom.AddRange(new[] { 6, 5 });
                left.AddRange(new[] { 8, 7 });
                break;

            case 9:
                top.AddRange(new[] { 1, 2 });
                right.AddRange(new[] { 3, 4 });
                bottom.AddRange(new[] { 7, 6, 5 });
                left.AddRange(new[] { 9, 8 });
                break;

            case 10:
                top.AddRange(new[] { 1, 2, 3 });
                right.AddRange(new[] { 4, 5 });
                bottom.AddRange(new[] { 8, 7, 6 });
                left.AddRange(new[] { 10, 9 });
                break;

            case 12:
                top.AddRange(new[] { 1, 2, 3 });
                right.AddRange(new[] { 4, 5, 6 });
                bottom.AddRange(new[] { 9, 8, 7 });
                left.AddRange(new[] { 12, 11, 10 });
                break;

            default:
                BuildFallbackCircularLayout(
                    playerCount,
                    top,
                    right,
                    bottom,
                    left
                );
                break;
        }
    }

    private void BuildFallbackCircularLayout(
        int playerCount,
        List<int> top,
        List<int> right,
        List<int> bottom,
        List<int> left)
    {
        if (playerCount <= 0)
            return;

        int topCount =
            Mathf.CeilToInt(playerCount / 4f);

        int rightCount =
            Mathf.CeilToInt(
                (playerCount - topCount) / 3f);

        int bottomCount =
            Mathf.CeilToInt(
                (playerCount - topCount - rightCount) / 2f);

        int seat = 1;

        for (int i = 0;
             i < topCount && seat <= playerCount;
             i++)
            top.Add(seat++);

        for (int i = 0;
             i < rightCount && seat <= playerCount;
             i++)
            right.Add(seat++);

        List<int> clockwiseBottom =
            new List<int>();

        for (int i = 0;
             i < bottomCount && seat <= playerCount;
             i++)
            clockwiseBottom.Add(seat++);

        clockwiseBottom.Reverse();
        bottom.AddRange(clockwiseBottom);

        List<int> clockwiseLeft =
            new List<int>();

        while (seat <= playerCount)
            clockwiseLeft.Add(seat++);

        clockwiseLeft.Reverse();
        left.AddRange(clockwiseLeft);
    }

    private void EnsureSlotCount(int requiredCount)
    {
        while (spawnedSlots.Count < requiredCount)
        {
            GamePlayerStatusSlotUI slot =
                Instantiate(playerSlotPrefab);

            slot.gameObject.SetActive(false);
            spawnedSlots.Add(slot);
        }
    }
}
