namespace Demo.Http;

public sealed class ResilientHttpClient(HttpClient inner, RetryPolicy policy)
{
    public async Task<HttpResponseMessage> SendAsync(
        Func<HttpRequestMessage> createRequest,
        CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        while (true)
        {
            var response = await inner.SendAsync(createRequest(), cancellationToken);
            if (!policy.ShouldRetry(attempt, response)) return response;

            response.Dispose();
            await Task.Delay(policy.DelayFor(attempt), cancellationToken);
            attempt++;
        }
    }
}
