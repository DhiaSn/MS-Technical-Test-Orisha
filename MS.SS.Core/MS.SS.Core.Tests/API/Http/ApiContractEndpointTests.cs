using System.Net;
using System.Text.Json;
using MS.SS.Core.Tests.TestHelpers;

namespace MS.SS.Core.Tests.API.Http;

[Collection(PostgresCollection.Name)]
public sealed class ApiContractEndpointTests : IAsyncLifetime
{
    private readonly ReceptionApiFactory _factory;
    private readonly HttpClient _http;

    public ApiContractEndpointTests(PostgresFixture postgres)
    {
        _factory = new ReceptionApiFactory(postgres);
        _http = _factory.CreateAuthenticatedClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        _http.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task The_wire_format_uses_camelCase_and_lowercase_status_strings()
    {
        var response = await _http.GetAsync("/api/reception/deliveries/current");

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var product = json.RootElement.GetProperty("pallets")[0].GetProperty("cartons")[0].GetProperty("products")[0];
        Assert.Equal(
            new[] { "color", "expectedQuantity", "id", "name", "receivedQuantity", "reference", "size", "status" },
            product.EnumerateObject().Select(p => p.Name).Order());
        Assert.Equal("none", product.GetProperty("status").GetString());
        Assert.Equal("none", json.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task The_openapi_document_describes_the_six_endpoints()
    {
        var response = await _http.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = json.RootElement.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToList();
        Assert.Contains("/api/reception/deliveries/current", paths);
        Assert.Contains("/api/reception/deliveries/{deliveryId}", paths);
        Assert.Contains("/api/reception/deliveries/{deliveryId}/pallets/{palletId}/validation", paths);
        Assert.Contains("/api/reception/deliveries/{deliveryId}/cartons/{cartonId}/validation", paths);
        Assert.Contains("/api/reception/deliveries/{deliveryId}/products/{productId}/validation", paths);
        Assert.Contains("/api/reception/deliveries/{deliveryId}/products/{productId}/received-quantity", paths);
    }

    [Fact]
    public async Task The_frontend_origin_may_call_the_api_cross_origin()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/reception/deliveries/current");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "PUT");

        var response = await _http.SendAsync(request);

        Assert.Equal("http://localhost:3000", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }
}
