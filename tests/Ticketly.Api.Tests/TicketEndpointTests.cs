using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Ticketly.Api.Tests;

public class TicketEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_ticket_with_valid_data_returns_201()
    {
        var body = new { title = "Can't export invoices", description = "Export button does nothing", priority = "High" };

        var response = await _client.PostAsJsonAsync("/tickets", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_ticket_with_blank_title_returns_400()
    {
        var body = new { title = "", description = "Export button does nothing", priority = "High" };

        var response = await _client.PostAsJsonAsync("/tickets", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonNode>();
        var errors = json!["errors"]!.AsObject();
        Assert.Contains(errors, e => e.Key.Equals("title", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Create_ticket_with_title_over_200_chars_returns_400()
    {
        var body = new { title = new string('x', 201), description = "Export button does nothing", priority = "High" };

        var response = await _client.PostAsJsonAsync("/tickets", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonNode>();
        var errors = json!["errors"]!.AsObject();
        Assert.Contains(errors, e => e.Key.Equals("title", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Get_ticket_returns_the_created_ticket()
    {
        var body = new { title = "Can't export invoices", description = "Export button does nothing", priority = "High" };
        var created = await _client.PostAsJsonAsync("/tickets", body);
        var location = created.Headers.Location!;

        var response = await _client.GetAsync(location);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("Can't export invoices", json!["title"]!.GetValue<string>());
    }

    [Fact]
    public async Task Get_ticket_that_does_not_exist_returns_404()
    {
        var response = await _client.GetAsync($"/tickets/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

}