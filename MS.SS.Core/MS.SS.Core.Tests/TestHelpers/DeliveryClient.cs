using MS.SS.Core.Modules.Reception.Application.Dtos;

namespace MS.SS.Core.Tests.TestHelpers;

internal sealed class DeliveryClient(HttpClient http, Guid deliveryId)
{
    public Task<HttpResponseMessage> ValidatePalletAsync(Guid id, bool validated) =>
        ValidateAsync("pallets", id, validated);

    public Task<HttpResponseMessage> ValidateCartonAsync(Guid id, bool validated) =>
        ValidateAsync("cartons", id, validated);

    public Task<HttpResponseMessage> ValidateProductAsync(Guid id, bool validated) =>
        ValidateAsync("products", id, validated);

    public Task<HttpResponseMessage> SetQuantityRawAsync(Guid productId, string rawJson) =>
        http.PutAsync($"/api/reception/deliveries/{deliveryId}/products/{productId}/received-quantity", ApiJson.Body(rawJson));

    public Task<HttpResponseMessage> SetQuantityAsync(Guid productId, int quantity) =>
        SetQuantityRawAsync(productId, $"{{\"receivedQuantity\":{quantity}}}");

    public async Task<DeliveryResponse> GetAsync() =>
        (await ApiJson.ReadAsync<DeliveryResponse>(await http.GetAsync($"/api/reception/deliveries/{deliveryId}")))!;

    private Task<HttpResponseMessage> ValidateAsync(string kind, Guid id, bool validated) =>
        http.PutAsync(
            $"/api/reception/deliveries/{deliveryId}/{kind}/{id}/validation",
            ApiJson.Body($"{{\"validated\":{(validated ? "true" : "false")}}}"));
}
