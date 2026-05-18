namespace PrivateAiChat.Web.Services;

public class ApiResult
{
    protected ApiResult(bool succeeded, string? error)
    {
        Succeeded = succeeded;
        Error = error;
    }

    public bool Succeeded { get; }

    public string? Error { get; }

    public static ApiResult Success() => new(succeeded: true, error: null);

    public static ApiResult Failure(string error) => new(succeeded: false, error);
}

public sealed class ApiResult<TValue> : ApiResult
{
    private ApiResult(bool succeeded, TValue? value, string? error) : base(succeeded, error)
    {
        Value = value;
    }

    public TValue? Value { get; }

    public static ApiResult<TValue> Success(TValue value) =>
        new(succeeded: true, value, error: null);

    public new static ApiResult<TValue> Failure(string error) =>
        new(succeeded: false, value: default, error);
}
