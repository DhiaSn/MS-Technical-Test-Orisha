using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MS.SS.Core.Tests.TestHelpers;

public static class ApiJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static StringContent Body(string json) => new(json, Encoding.UTF8, "application/json");

    public static StringContent Credentials(string username, string password) =>
        Body(JsonSerializer.Serialize(new { username, password }));

    public static Task<T?> ReadAsync<T>(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<T>(Options);

    // Records holding lists compare those by reference, so whole-delivery equality goes through JSON.
    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);
}
