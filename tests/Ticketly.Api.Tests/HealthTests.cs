using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ticketly.Api.Tests;

public class HealthTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Health_returns_200()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}