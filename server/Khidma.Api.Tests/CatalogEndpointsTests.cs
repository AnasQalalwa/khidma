using System.Net;
using System.Net.Http.Json;
using Khidma.Api.Contracts.Catalog;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Khidma.Api.Tests;

public sealed class CatalogEndpointsTests : IClassFixture<KhidmaApiFactory>
{
    private readonly KhidmaApiFactory _factory;

    public CatalogEndpointsTests(KhidmaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCategories_ReturnsSeededCategory()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/catalog/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>();
        Assert.NotNull(categories);
        Assert.Contains(categories, c => c.Name == "Home Services");
    }

    [Fact]
    public async Task GetServices_ReturnsSeededService()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/catalog/services");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var services = await response.Content.ReadFromJsonAsync<List<ServiceDto>>();
        Assert.NotNull(services);
        Assert.Contains(services, s => s.Name == "Plumbing");
        Assert.Contains(services, s => s.CategoryName == "Home Services");
    }
}
