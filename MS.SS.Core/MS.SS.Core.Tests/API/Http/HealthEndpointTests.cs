using System.Net;
using System.Text.Json;
using MS.SS.Core.Tests.TestHelpers;

namespace MS.SS.Core.Tests.API.Http;

[Collection(PostgresCollection.Name)]
public sealed class HealthEndpointTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Ready_endpoint_reports_healthy_when_the_database_is_reachable()
    {
        await using var factory = new ReceptionApiFactory(postgres);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_route_answers_a_404_problem_with_the_not_found_code()
    {
        await using var factory = new ReceptionApiFactory(postgres);

        var response = await factory.CreateClient().GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("resource.not_found", body.RootElement.GetProperty("code").GetString());
    }
}
