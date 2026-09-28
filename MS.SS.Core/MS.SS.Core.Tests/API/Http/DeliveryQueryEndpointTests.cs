using System.Net;
using System.Text.Json;
using MS.SS.Core.Modules.Reception.Application.Dtos;
using MS.SS.Core.Modules.Reception.Domain.Enums;
using MS.SS.Core.Tests.Modules.Reception.Domain;
using MS.SS.Core.Tests.TestHelpers;

namespace MS.SS.Core.Tests.API.Http;

[Collection(PostgresCollection.Name)]
public sealed class DeliveryQueryEndpointTests : IAsyncLifetime
{
    private readonly ReceptionApiFactory _factory;
    private readonly HttpClient _http;

    public DeliveryQueryEndpointTests(PostgresFixture postgres)
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
    public async Task Current_delivery_is_the_seeded_CMD_2026_with_nothing_received()
    {
        var response = await _http.GetAsync("/api/reception/deliveries/current");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var delivery = (await ApiJson.ReadAsync<DeliveryResponse>(response))!;
        Assert.Equal("CMD-2026", delivery.OrderId);
        Assert.Equal(ValidationStatus.None, delivery.Status);
        Assert.Equal(new ProgressResponse(0, 504), delivery.Progress);
        Assert.Equal(new[] { "PAL-01", "PAL-02", "PAL-03" }, delivery.Pallets.Select(p => p.Code));
        Assert.Equal(7, delivery.Pallets.Sum(p => p.Cartons.Count));
        Assert.Equal(15, delivery.Pallets.Sum(p => p.Cartons.Sum(c => c.Products.Count)));
    }

    [Fact]
    public async Task A_delivery_can_be_fetched_by_id()
    {
        var s = new SampleDelivery();
        await _factory.SeedAsync(s.Order);

        var delivery = await new DeliveryClient(_http, s.Order.Id).GetAsync();

        Assert.Equal(s.Order.OrderNumber, delivery.OrderId);
        Assert.Equal(new ProgressResponse(0, 43), delivery.Progress);
        Assert.Equal(new[] { "CART-A", "CART-B" }, delivery.Pallets[0].Cartons.Select(c => c.Code));
    }

    [Fact]
    public async Task An_unknown_delivery_is_404_with_a_stable_code()
    {
        var response = await _http.GetAsync($"/api/reception/deliveries/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("reception.delivery_not_found", json.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_delivery_id_that_is_not_a_guid_does_not_match_any_route()
    {
        var response = await _http.GetAsync("/api/reception/deliveries/not-a-guid");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
