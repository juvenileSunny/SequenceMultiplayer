using System.Collections.Generic;
using UnityEngine;

public class LobbySeatMapUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LobbyManager lobbyManager;
    [SerializeField] private RectTransform seatMapContainer;
    [SerializeField] private LobbySeatTileUI seatTilePrefab;

    [Header("Circular Layout")]
    [SerializeField] private float circleRadius = 150f;
    [SerializeField] private float startAngle = 90f;

    [Header("Seat Sizes")]
    [SerializeField] private float largeSeatSize = 110f;
    [SerializeField] private float mediumSeatSize = 90f;
    [SerializeField] private float smallSeatSize = 72f;

    private readonly List<LobbySeatTileUI> seatTiles =
        new List<LobbySeatTileUI>();

    private int generatedSeatCount = -1;

    // =========================================================
    // UNITY
    // =========================================================

    private void OnEnable()
    {
        if (lobbyManager != null)
        {
            lobbyManager.OnLobbyChanged += RefreshSeatMap;
        }
    }

    private void Start()
    {
        RefreshSeatMap();
    }

    private void OnDisable()
    {
        if (lobbyManager != null)
        {
            lobbyManager.OnLobbyChanged -= RefreshSeatMap;
        }
    }

    // =========================================================
    // REFRESH
    // =========================================================

    public void RefreshSeatMap()
    {
        if (lobbyManager == null ||
            seatMapContainer == null ||
            seatTilePrefab == null)
        {
            return;
        }

        int seatCount = lobbyManager.PlayerCount;

        if (seatCount <= 0)
            return;

        // Rebuild only when player count changes.
        if (generatedSeatCount != seatCount ||
            seatTiles.Count != seatCount)
        {
            BuildSeatMap(seatCount);
        }

        // Update who occupies each seat.
        for (int seatIndex = 1;
             seatIndex <= seatCount;
             seatIndex++)
        {
            LobbyPlayerData player =
                lobbyManager.GetPlayerInSeat(seatIndex);

            LobbySeatTileUI tile =
                seatTiles[seatIndex - 1];

            tile.Refresh(player);
        }
    }

    // =========================================================
    // BUILD
    // =========================================================

    private void BuildSeatMap(int seatCount)
    {
        ClearSeatMap();

        generatedSeatCount = seatCount;

        float seatSize = GetSeatSize(seatCount);
        float radius = GetRadius(seatCount, seatSize);

        for (int i = 0; i < seatCount; i++)
        {
            int seatIndex = i + 1;

            LobbySeatTileUI tile =
                Instantiate(
                    seatTilePrefab,
                    seatMapContainer
                );

            RectTransform tileRect =
                tile.GetComponent<RectTransform>();

            if (tileRect != null)
            {
                // Make every generated tile use a centered anchor.
                tileRect.anchorMin =
                    new Vector2(0.5f, 0.5f);

                tileRect.anchorMax =
                    new Vector2(0.5f, 0.5f);

                tileRect.pivot =
                    new Vector2(0.5f, 0.5f);

                tileRect.sizeDelta =
                    new Vector2(
                        seatSize,
                        seatSize
                    );

                // Seat 1 begins at the top.
                // Seats then continue clockwise.
                float angleStep =
                    360f / seatCount;

                float angle =
                    startAngle -
                    (angleStep * i);

                float radians =
                    angle * Mathf.Deg2Rad;

                float x =
                    Mathf.Cos(radians) *
                    radius;

                float y =
                    Mathf.Sin(radians) *
                    radius;

                tileRect.anchoredPosition =
                    new Vector2(x, y);
            }

            tile.Initialize(seatIndex);

            seatTiles.Add(tile);
        }
    }

    // =========================================================
    // RESPONSIVE SIZE
    // =========================================================

    private float GetSeatSize(int seatCount)
    {
        if (seatCount <= 6)
            return largeSeatSize;

        if (seatCount <= 10)
            return mediumSeatSize;

        return smallSeatSize;
    }

    private float GetRadius(
        int seatCount,
        float seatSize)
    {
        if (seatMapContainer == null)
            return circleRadius;

        float width =
            seatMapContainer.rect.width;

        float height =
            seatMapContainer.rect.height;

        float smallestDimension =
            Mathf.Min(width, height);

        // Keep the tiles inside the container.
        float maximumRadius =
            (smallestDimension * 0.5f) -
            (seatSize * 0.55f) -
            10f;

        // Prevent a tiny/invalid radius during early layout passes.
        if (maximumRadius <= 40f)
        {
            return circleRadius;
        }

        return Mathf.Min(
            circleRadius,
            maximumRadius
        );
    }

    // =========================================================
    // CLEAR
    // =========================================================

    private void ClearSeatMap()
    {
        foreach (LobbySeatTileUI tile in seatTiles)
        {
            if (tile != null)
            {
                Destroy(tile.gameObject);
            }
        }

        seatTiles.Clear();

        // Also remove any generated leftovers if needed.
        for (int i = seatMapContainer.childCount - 1;
             i >= 0;
             i--)
        {
            Transform child =
                seatMapContainer.GetChild(i);

            // Preserve the center/table graphic.
            if (child.name == "TableCenterImage")
                continue;

            Destroy(child.gameObject);
        }
    }
}