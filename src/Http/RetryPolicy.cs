namespace Demo.Http;

public sealed class RetryPolicy
{
    public int MaxAttempts { get; init; } = 4;
    public TimeSpan BaseDelay { get; init; } = TimeSpan.FromMilliseconds(200);
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(5);

    public bool ShouldRetry(int attempt, HttpResponseMessage response)
    {
        if (attempt >= MaxAttempts) return false;
        var status = (int)response.StatusCode;
        return status == 408 || status == 429 || status >= 500;
    }

    public TimeSpan DelayFor(int attempt)
    {
        var delay = BaseDelay * Math.Pow(2, attempt);
        return delay;
    }
}
