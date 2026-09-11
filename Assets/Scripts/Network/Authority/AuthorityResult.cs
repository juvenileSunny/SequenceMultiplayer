public enum AuthorityResultCode
{
    Success,

    InvalidRequest,
    InvalidPlayer,

    InvalidSeat,
    SeatTaken,

    PlayerNotSeated,

    LobbyNotAvailable,
    NotHost,
    MatchCannotStart,

    RequestRejected
}

public class AuthorityResult
{
    public bool Success { get; }
    public AuthorityResultCode Code { get; }
    public string Message { get; }

    private AuthorityResult(
        bool success,
        AuthorityResultCode code,
        string message)
    {
        Success = success;
        Code = code;
        Message = message;
    }

    public static AuthorityResult Accepted(
        string message = "")
    {
        return new AuthorityResult(
            true,
            AuthorityResultCode.Success,
            message
        );
    }

    public static AuthorityResult Rejected(
        AuthorityResultCode code,
        string message)
    {
        return new AuthorityResult(
            false,
            code,
            message
        );
    }
}