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

app.Run();


public partial class Program { }