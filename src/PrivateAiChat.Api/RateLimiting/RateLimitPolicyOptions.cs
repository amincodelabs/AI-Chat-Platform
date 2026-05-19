namespace PrivateAiChat.Api.RateLimiting;

public sealed class RateLimitPolicyOptions
{
    public int PermitLimit { get; set; } = 60;

    public int WindowSeconds { get; set; } = 60;
}
