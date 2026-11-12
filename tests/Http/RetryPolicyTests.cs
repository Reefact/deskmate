using System.Net;
using Demo.Http;
using Xunit;

public class RetryPolicyTests
{
    private readonly RetryPolicy _policy = new();

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public void Retries_transient_failures(HttpStatusCode status) =>
        Assert.True(_policy.ShouldRetry(0, new HttpResponseMessage(status)));

    [Fact]
    public void Does_not_retry_client_errors() =>
        Assert.False(_policy.ShouldRetry(0, new HttpResponseMessage(HttpStatusCode.NotFound)));

    [Fact]
    public void Doubles_the_delay_on_each_attempt() =>
        Assert.Equal(TimeSpan.FromMilliseconds(800), _policy.DelayFor(2));
}
