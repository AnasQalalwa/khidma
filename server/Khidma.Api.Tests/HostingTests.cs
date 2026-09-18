using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Khidma.Api.Tests;

public sealed class SpaFallbackTests : IClassFixture<KhidmaApiFactory>
{
    private readonly KhidmaApiFactory _factory;

    public SpaFallbackTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CustomerDeepLink_ReturnsIndexHtml()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var response = await client.GetAsync("/customer/requests/1");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("id=\"root\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("\"status\":404", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnknownApiRoute_ReturnsJson404()
    {
        var client = TestHarness.CreateClient(_factory);

        var response = await client.GetAsync("/api/nope");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Not Found", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("id=\"root\"", body, StringComparison.Ordinal);
    }
}

public sealed class ProductionExceptionTests : IClassFixture<ProductionHostFactory>
{
    private readonly ProductionHostFactory _factory;

    public ProductionExceptionTests(ProductionHostFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UnhandledException_OmitsStackTraceAndExceptionText()
    {
        var client = TestHarness.CreateClient(_factory);

        var response = await client.GetAsync("/api/catalog/categories");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("unexpected error", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(ProductionHostFactory.ExceptionMarker, body, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", body, StringComparison.Ordinal);
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("at Khidma.Api", body, StringComparison.Ordinal);
    }
}
