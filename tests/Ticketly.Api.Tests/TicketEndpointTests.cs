using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Ticketly.Api.Tickets;

namespace Ticketly.Api.Tests;

public class TicketEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClientFor("alice", "customer");

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

    [Fact]
    public async Task List_tickets_caps_page_size_at_50()
    {
        var response = await _client.GetAsync("/tickets?pageSize=100000");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal(50, json!["pageSize"]!.GetValue<int>());
    }

    [Fact]
    public async Task List_tickets_returns_highest_priority_first()
    {
        var lowId = await CreateTicketAsync("Low");
        var urgentId = await CreateTicketAsync("Urgent");

        var response = await _client.GetAsync("/tickets?status=Open&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonNode>();
        var ids = json!["items"]!.AsArray()
            .Select(i => i!["id"]!.GetValue<string>())
            .ToList();

        var urgentIndex = ids.IndexOf(urgentId);
        var lowIndex = ids.IndexOf(lowId);

        Assert.True(urgentIndex >= 0 && lowIndex >= 0, "both tickets should be on the first page");
        Assert.True(urgentIndex < lowIndex);
    }

    [Fact]
    public async Task Create_ticket_without_login_returns_401()
    {
        var client = factory.CreateClient();   // plain client: no headers, so anonymous

        var response = await client.PostAsJsonAsync("/tickets",
            new { title = "No login", description = "should be rejected" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Customer_cannot_read_another_users_ticket()
    {
        var bob = factory.CreateClientFor("bob", "support");
        var alice = factory.CreateClientFor("alice", "customer");

        var created = await bob.PostAsJsonAsync("/tickets",
            new { title = "Bob's ticket", description = "Created by bob" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var json = await created.Content.ReadFromJsonAsync<JsonNode>();
        var ticketId = json!["id"]!.GetValue<Guid>();

        // Control: the ticket really exists, bob can read it
        var asBob = await bob.GetAsync($"/tickets/{ticketId}");
        Assert.Equal(HttpStatusCode.OK, asBob.StatusCode);

        // The actual rule: alice is not the owner
        var asAlice = await alice.GetAsync($"/tickets/{ticketId}");
        Assert.Equal(HttpStatusCode.NotFound, asAlice.StatusCode);
    }

    [Fact]
    public async Task Customer_list_only_contains_their_own_tickets()
    {
        var customerId = $"customer-{Guid.NewGuid()}";
        var customer = factory.CreateClientFor(customerId, "customer");
        var staff = factory.CreateClientFor("bob", "support");

        var mine = await customer.PostAsJsonAsync("/tickets",
            new { title = "Mine", description = "Created by the customer" });
        Assert.Equal(HttpStatusCode.Created, mine.StatusCode);

        var notMine = await staff.PostAsJsonAsync("/tickets",
            new { title = "Not mine", description = "Created by staff" });
        Assert.Equal(HttpStatusCode.Created, notMine.StatusCode);

        var list = await customer.GetFromJsonAsync<JsonNode>("/tickets");

        Assert.Equal(1, list!["totalCount"]!.GetValue<int>());
        foreach (var item in list["items"]!.AsArray())
            Assert.Equal(customerId, item!["customerId"]!.GetValue<string>());
    }

    private async Task<string> CreateTicketAsync(string priority)
    {
        var body = new { title = $"{priority} ticket", description = "Test", priority };
        var response = await _client.PostAsJsonAsync("/tickets", body);
        var json = await response.Content.ReadFromJsonAsync<JsonNode>();
        return json!["id"]!.GetValue<string>();
    }

}