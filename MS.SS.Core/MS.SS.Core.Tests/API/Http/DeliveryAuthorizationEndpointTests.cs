using System.Net;
using MS.SS.Core.Modules.Reception.Application.Dtos;
using MS.SS.Core.Tests.Modules.Reception.Domain;
using MS.SS.Core.Tests.TestHelpers;

namespace MS.SS.Core.Tests.API.Http;

[Collection(PostgresCollection.Name)]
public sealed class DeliveryAuthorizationEndpointTests : IAsyncLifetime
{
    private readonly ReceptionApiFactory _factory;

    public DeliveryAuthorizationEndpointTests(PostgresFixture postgres) =>
        _factory = new ReceptionApiFactory(postgres);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    public static TheoryData<string, string, string?> ProtectedRoutes => new()
    {
        { "GET", "/api/reception/deliveries/current", null },
        { "GET", $"/api/reception/deliveries/{Guid.NewGuid()}", null },
        { "PUT", $"/api/reception/deliveries/{Guid.NewGuid()}/pallets/{Guid.NewGuid()}/validation", "{\"validated\":true}" },
        { "PUT", $"/api/reception/deliveries/{Guid.NewGuid()}/cartons/{Guid.NewGuid()}/validation", "{\"validated\":true}" },
        { "PUT", $"/api/reception/deliveries/{Guid.NewGuid()}/products/{Guid.NewGuid()}/validation", "{\"validated\":true}" },
        { "PUT", $"/api/reception/deliveries/{Guid.NewGuid()}/products/{Guid.NewGuid()}/received-quantity", "{\"receivedQuantity\":1}" }
    };

    [Theory]
    [MemberData(nameof(ProtectedRoutes))]
    public async Task Every_reception_endpoint_refuses_a_request_without_a_session(string method, string url, string? body)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (body is not null) request.Content = ApiJson.Body(body);

        var response = await _factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("auth.unauthenticated", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task A_refused_write_changes_nothing()
    {
        var s = new SampleDelivery();
        await _factory.SeedAsync(s.Order);

        var refused = await _factory.CreateClient().PutAsync(
            $"/api/reception/deliveries/{s.Order.Id}/pallets/{s.Pallet1.Id}/validation", ApiJson.Body("{\"validated\":true}"));

        Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode);
        var after = await new DeliveryClient(_factory.CreateAuthenticatedClient(), s.Order.Id).GetAsync();
        Assert.Equal(new ProgressResponse(0, 43), after.Progress);
    }

    [Fact]
    public async Task A_tampered_token_is_refused_on_reception_endpoints_too()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", "not.a.jwt");

        var response = await client.GetAsync("/api/reception/deliveries/current");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_write_from_the_browser_with_the_session_cookie_succeeds()
    {
        var s = new SampleDelivery();
        await _factory.SeedAsync(s.Order);
        var request = await CookieWriteAsync(s);
        request.Headers.Add("X-MS-CSRF", "1");

        var response = await _factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task The_same_write_without_the_csrf_header_is_refused_and_changes_nothing()
    {
        var s = new SampleDelivery();
        await _factory.SeedAsync(s.Order);
        var request = await CookieWriteAsync(s);

        var response = await _factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("csrf.header_missing", await response.Content.ReadAsStringAsync());
        var after = await new DeliveryClient(_factory.CreateAuthenticatedClient(), s.Order.Id).GetAsync();
        Assert.Equal(new ProgressResponse(0, 43), after.Progress);
    }

    private async Task<HttpRequestMessage> CookieWriteAsync(SampleDelivery s)
    {
        var signIn = await _factory.CreateClient().PostAsync(
            "/api/identity/auth/sign-in", ApiJson.Credentials("magasinier", "Reception2026"));
        var cookie = signIn.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("ms_access_token=")).Split(';')[0];

        var request = new HttpRequestMessage(HttpMethod.Put,
            $"/api/reception/deliveries/{s.Order.Id}/cartons/{s.CartonA.Id}/validation")
        { Content = ApiJson.Body("{\"validated\":true}") };
        request.Headers.Add("Cookie", cookie);
        request.Headers.Add("Origin", "http://localhost:3000");

        return request;
    }
}
