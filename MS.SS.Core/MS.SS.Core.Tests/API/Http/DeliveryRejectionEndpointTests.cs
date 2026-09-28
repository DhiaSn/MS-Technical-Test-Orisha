using System.Net;
using System.Text;
using System.Text.Json;
using MS.SS.Core.Modules.Reception.Application.Dtos;
using MS.SS.Core.Tests.Modules.Reception.Domain;
using MS.SS.Core.Tests.TestHelpers;

namespace MS.SS.Core.Tests.API.Http;

[Collection(PostgresCollection.Name)]
public sealed class DeliveryRejectionEndpointTests : IAsyncLifetime
{
    private readonly ReceptionApiFactory _factory;
    private readonly HttpClient _http;

    public DeliveryRejectionEndpointTests(PostgresFixture postgres)
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

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"validated\":null}")]
    public async Task A_missing_validated_flag_is_a_400_and_changes_nothing(string body)
    {
        var s = new SampleDelivery();
        await _factory.SeedAsync(s.Order);
        var client = new DeliveryClient(_http, s.Order.Id);
        await client.ValidatePalletAsync(s.Pallet1.Id, true);

        var response = await _http.PutAsync(
            $"/api/reception/deliveries/{s.Order.Id}/pallets/{s.Pallet1.Id}/validation", ApiJson.Body(body));

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation.failed");
        Assert.Equal("field.required",
            problem.RootElement.GetProperty("errorCodes").GetProperty("validated")[0].GetProperty("code").GetString());
        Assert.Equal(new ProgressResponse(35, 43), (await client.GetAsync()).Progress);
    }

    [Theory]
    [InlineData("{}", "field.required")]
    [InlineData("{\"receivedQuantity\":null}", "field.required")]
    [InlineData("{\"receivedQuantity\":-1}", "reception.quantity_out_of_range")]
    [InlineData("{\"receivedQuantity\":11}", "reception.quantity_out_of_range")]
    public async Task An_unusable_quantity_is_a_field_error_and_changes_nothing(string body, string fieldCode)
    {
        var s = new SampleDelivery();
        await _factory.SeedAsync(s.Order);
        var client = new DeliveryClient(_http, s.Order.Id);
        await client.SetQuantityAsync(s.A1.Id, 3);

        var response = await client.SetQuantityRawAsync(s.A1.Id, body);

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation.failed");
        Assert.Equal(fieldCode,
            problem.RootElement.GetProperty("errorCodes").GetProperty("receivedQuantity")[0].GetProperty("code").GetString());
        Assert.Equal(3, (await client.GetAsync()).Pallets[0].Cartons[0].Products[0].ReceivedQuantity);
    }

    [Fact]
    public async Task The_out_of_range_error_reports_the_bounds()
    {
        var s = new SampleDelivery();
        await _factory.SeedAsync(s.Order);

        var response = await new DeliveryClient(_http, s.Order.Id).SetQuantityAsync(s.A1.Id, 11);

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation.failed");
        var parameters = problem.RootElement.GetProperty("errorCodes").GetProperty("receivedQuantity")[0].GetProperty("params");
        Assert.Equal("0", parameters.GetProperty("min").GetString());
        Assert.Equal("10", parameters.GetProperty("max").GetString());
    }

    [Theory]
    [InlineData("{\"receivedQuantity\":3.5}")]
    [InlineData("{\"receivedQuantity\":\"4\"}")]
    [InlineData("{\"receivedQuantity\":99999999999}")]
    [InlineData("{\"receivedQuantity\":")]
    [InlineData("")]
    public async Task An_unbindable_quantity_body_is_request_invalid_never_a_500(string body)
    {
        var s = new SampleDelivery();
        await _factory.SeedAsync(s.Order);

        var response = await new DeliveryClient(_http, s.Order.Id).SetQuantityRawAsync(s.A1.Id, body);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "request.invalid");
    }

    [Theory]
    [InlineData("{\"validated\":")]
    [InlineData("{\"validated\":\"yes\"}")]
    [InlineData("{\"validated\":1}")]
    [InlineData("[]")]
    [InlineData("")]
    public async Task An_unbindable_validation_body_is_request_invalid_and_changes_nothing(string body)
    {
        var s = new SampleDelivery();
        await _factory.SeedAsync(s.Order);
        var client = new DeliveryClient(_http, s.Order.Id);
        await client.ValidatePalletAsync(s.Pallet1.Id, true);

        var response = await _http.PutAsync(
            $"/api/reception/deliveries/{s.Order.Id}/pallets/{s.Pallet1.Id}/validation", ApiJson.Body(body));

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "request.invalid");
        Assert.Equal(new ProgressResponse(35, 43), (await client.GetAsync()).Progress);
    }

    [Fact]
    public async Task A_non_json_body_is_415()
    {
        var s = new SampleDelivery();
        await _factory.SeedAsync(s.Order);

        var response = await _http.PutAsync(
            $"/api/reception/deliveries/{s.Order.Id}/pallets/{s.Pallet1.Id}/validation",
            new StringContent("validated=true", Encoding.UTF8, "text/plain"));

        await AssertProblemAsync(response, HttpStatusCode.UnsupportedMediaType, "request.unsupported_media_type");
    }

    [Fact]
    public async Task Unknown_nodes_are_404_with_a_code_per_kind()
    {
        var s = new SampleDelivery();
        await _factory.SeedAsync(s.Order);
        var client = new DeliveryClient(_http, s.Order.Id);

        await AssertProblemAsync(await client.ValidatePalletAsync(Guid.NewGuid(), true), HttpStatusCode.NotFound, "reception.pallet_not_found");
        await AssertProblemAsync(await client.ValidateCartonAsync(Guid.NewGuid(), true), HttpStatusCode.NotFound, "reception.carton_not_found");
        await AssertProblemAsync(await client.ValidateProductAsync(Guid.NewGuid(), true), HttpStatusCode.NotFound, "reception.product_not_found");
        await AssertProblemAsync(await client.SetQuantityAsync(Guid.NewGuid(), 1), HttpStatusCode.NotFound, "reception.product_not_found");
    }

    [Fact]
    public async Task An_unknown_delivery_is_404_on_every_mutation()
    {
        var client = new DeliveryClient(_http, Guid.NewGuid());

        await AssertProblemAsync(await client.ValidatePalletAsync(Guid.NewGuid(), true), HttpStatusCode.NotFound, "reception.delivery_not_found");
        await AssertProblemAsync(await client.ValidateCartonAsync(Guid.NewGuid(), true), HttpStatusCode.NotFound, "reception.delivery_not_found");
        await AssertProblemAsync(await client.ValidateProductAsync(Guid.NewGuid(), true), HttpStatusCode.NotFound, "reception.delivery_not_found");
        await AssertProblemAsync(await client.SetQuantityAsync(Guid.NewGuid(), 1), HttpStatusCode.NotFound, "reception.delivery_not_found");
    }

    [Fact]
    public async Task A_node_belonging_to_another_delivery_is_404_and_leaves_that_delivery_untouched()
    {
        var mine = new SampleDelivery();
        var other = new SampleDelivery();
        await _factory.SeedAsync(mine.Order);
        await _factory.SeedAsync(other.Order);

        var response = await new DeliveryClient(_http, mine.Order.Id).ValidateCartonAsync(other.CartonA.Id, true);

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "reception.carton_not_found");
        Assert.Equal(new ProgressResponse(0, 43), (await new DeliveryClient(_http, other.Order.Id).GetAsync()).Progress);
    }

    private static async Task<JsonDocument> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(code, json.RootElement.GetProperty("code").GetString());

        return json;
    }
}
