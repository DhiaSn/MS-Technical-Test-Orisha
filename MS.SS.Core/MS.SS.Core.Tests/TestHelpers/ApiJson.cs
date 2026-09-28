using System.Text;
using System.Text.Json;

namespace MS.SS.Core.Tests.TestHelpers;

public static class ApiJson
{
    public static StringContent Body(string json) => new(json, Encoding.UTF8, "application/json");

    public static StringContent Credentials(string username, string password) =>
        Body(JsonSerializer.Serialize(new { username, password }));
}
