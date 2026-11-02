using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Ticketly.Api.Data;
using Ticketly.Api.Domain;
using Ticketly.Api.Tickets;

var builder = WebApplication.CreateBuilder(args);

//register services
//BEGIN
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddDbContext<TicketlyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddValidation();
//END

//Map Requests
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapHealthChecks("/health");

app.MapGet("/", () => "Ticketly API");

app.MapPost("/tickets", async (CreateTicketRequest request, TicketlyDbContext db) =>
{
    var ticket = new Ticket
    {
        Title = request.Title,
        Description = request.Description,
        Priority = request.Priority ?? TicketPriority.Medium,
        CustomerId = "temp-customer" // TEMPORARY: comes from the auth token in Week 3
    };

    db.Tickets.Add(ticket);
    await db.SaveChangesAsync();

    return Results.Created($"/tickets/{ticket.Id}", ticket.ToResponse());
});

app.MapGet("/tickets/{id:guid}", async (Guid id, TicketlyDbContext db) =>
{
    // 1. fetch the ticket by id
    var ticket = await db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
    // 2. if it doesn't exist -> Results.NotFound()
    if(ticket is null)
    {
        return Results.NotFound();
    }
    // 3. otherwise -> Results.Ok(ticket.ToResponse())
    return Results.Ok(ticket.ToResponse());
});

app.MapGet("/tickets", async (TicketlyDbContext db, TicketStatus? status, int page = 1, int pageSize = 20) =>
{
    page = Math.Max(page, 1);
    pageSize = Math.Clamp(pageSize, 1, 50);

    var query = db.Tickets.AsNoTracking();

    if (status is not null)
    {
        query = query.Where(t => t.Status == status.Value);
    }

    var totalCount = await query.CountAsync();

    var tickets = await query
        .OrderByDescending(t => t.Priority)
        .ThenBy(t => t.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    var items = tickets.Select(t => t.ToResponse()).ToList();

    return Results.Ok(new PagedResponse<TicketResponse>(items, page, pageSize, totalCount));
});

app.Run();


public partial class Program { }