using System.Net;
using MS.SS.Core.Tests.TestHelpers;

namespace MS.SS.Core.Tests.API.Http;

[Collection(PostgresCollection.Name)]
public sealed class ThrottlingEndpointTests(PostgresFixture postgres)
{
    private const string SignIn = "/api/identity/auth/sign-in";

    [Fact]
    public async Task Sign_in_is_throttled_after_the_permitted_attempts()
    {
        await using var factory = new ReceptionApiFactory(postgres) { AuthPermitsPerMinute = 3 };
        var client = factory.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 4; i++)
        {
            statuses.Add((await client.PostAsync(SignIn, ApiJson.Credentials("nobody", "Reception2026"))).StatusCode);
        }

        Assert.Equal(
            new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.TooManyRequests },
            statuses);
    }

    [Fact]
    public async Task The_budget_is_per_forwarded_client_not_shared_by_everyone_behind_the_proxy()
    {
        await using var factory = new ReceptionApiFactory(postgres) { AuthPermitsPerMinute = 2 };
        var client = factory.CreateClient();

        async Task<HttpStatusCode> From(string ip)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, SignIn)
            {
                Content = ApiJson.Credentials("nobody", "Reception2026")
            };
            request.Headers.Add("X-Forwarded-For", ip);

            return (await client.SendAsync(request)).StatusCode;
        }

        await From("203.0.113.1");
        await From("203.0.113.1");
        Assert.Equal(HttpStatusCode.TooManyRequests, await From("203.0.113.1"));
        Assert.Equal(HttpStatusCode.Unauthorized, await From("203.0.113.2"));
    }

    [Fact]
    public async Task A_throttled_answer_is_a_problem_with_a_retry_delay()
    {
        await using var factory = new ReceptionApiFactory(postgres) { AuthPermitsPerMinute = 1 };
        var client = factory.CreateClient();
        await client.PostAsync(SignIn, ApiJson.Credentials("nobody", "Reception2026"));

        var response = await client.PostAsync(SignIn, ApiJson.Credentials("nobody", "Reception2026"));

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("rate_limit.exceeded", await response.Content.ReadAsStringAsync());
        Assert.True(response.Headers.RetryAfter?.Delta >= TimeSpan.FromSeconds(1));
    }
}
