using System;

//
// IMPORTANT:
//
// These classes contain only the player's REQUEST.
//
// They do NOT contain the result of the action.
// They also do NOT directly change game state.
//
// Later these same objects can be serialized and sent
// across the network.
//

// =========================================================
// REQUEST SEAT
// =========================================================

[Serializable]
public class RequestSeatRequest
{
    public int SeatIndex;

    public RequestSeatRequest(
        int seatIndex)
    {
        SeatIndex = seatIndex;
    }
}

// =========================================================
// SET READY
// =========================================================

[Serializable]
public class SetReadyRequest
{
    public bool IsReady;

    public SetReadyRequest(
        bool isReady)
    {
        IsReady = isReady;
    }
}

// =========================================================
// PLAY CARD
// =========================================================

[Serializable]
public class PlayCardRequest
{
    public string CardCode;

    public int Row;
    public int Column;

    public PlayCardRequest(
        string cardCode,
        int row,
        int column)
    {
        CardCode = cardCode;
        Row = row;
        Column = column;
    }
}

// =========================================================
// REPLACE DEAD CARD
// =========================================================

[Serializable]
public class ReplaceDeadCardRequest
{
    public string CardCode;

    public ReplaceDeadCardRequest(
        string cardCode)
    {
        CardCode = cardCode;
    }
}

// =========================================================
// START MATCH
// =========================================================

[Serializable]
public class StartMatchRequest
{
    public StartMatchRequest()
    {
    }
}