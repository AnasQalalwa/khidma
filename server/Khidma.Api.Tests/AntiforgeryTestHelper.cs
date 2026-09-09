using System.Net.Http.Headers;

namespace Khidma.Api.Tests;

internal static class AntiforgeryTestHelper
{
    public static async Task AttachTokenAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/antiforgery/token");
        response.EnsureSuccessStatusCode();

        var token = ReadCookie(response, "XSRF-TOKEN");
        Assert.False(string.IsNullOrWhiteSpace(token));

        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", token);
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private static string? ReadCookie(HttpResponseMessage response, string name)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values) &&
            !response.Headers.TryGetValues("Set-Cookie", out values))
        {
            values = response.Headers
                .Where(h => h.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
                .SelectMany(h => h.Value)
                .ToList();
        }

        if (values is null)
        {
            return null;
        }

        foreach (var header in values)
        {
            var firstPart = header.Split(';')[0];
            var separator = firstPart.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var cookieName = firstPart[..separator];
            if (cookieName.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(firstPart[(separator + 1)..]);
            }
        }

        return null;
    }
}
