using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Ticketly.Api.Data;
using Ticketly.Api.Domain;
using Ticketly.Api.Tickets;
using System.Text.Json;
using Ticketly.Api.Auth;

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
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

//AUTH
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience = builder.Configuration["Auth:Audience"];
        options.RequireHttpsMetadata = false;   // local http only
        options.MapInboundClaims = false;       // keep claim names like "sub"
        options.TokenValidationParameters.RoleClaimType = "role";
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var identity = (ClaimsIdentity)context.Principal!.Identity!;
                var realmAccess = context.Principal.FindFirst("realm_access")?.Value;
                if (realmAccess is not null)
                {
                    using var doc = JsonDocument.Parse(realmAccess);
                    if (doc.RootElement.TryGetProperty("roles", out var roles))
                    {
                        foreach (var role in roles.EnumerateArray())
                            identity.AddClaim(new Claim("role", role.GetString()!));
                    }
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();
//END

//Map Requests
var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/me", (ClaimsPrincipal user) => new
{
    Sub = user.FindFirstValue("sub"),
    Username = user.FindFirstValue("preferred_username"),
    Roles = user.FindAll("role").Select(c => c.Value),
}).RequireAuthorization();

app.MapHealthChecks("/health");

app.MapGet("/", () => "Ticketly API");

app.MapPost("/tickets", async (CreateTicketRequest request, TicketlyDbContext db, ClaimsPrincipal user) =>
{
    var ticket = new Ticket
    {
        Title = request.Title,
        Description = request.Description,
        Priority = request.Priority ?? TicketPriority.Medium,
        CustomerId = user.GetUserId()
    };

    db.Tickets.Add(ticket);
    await db.SaveChangesAsync();

    return Results.Created($"/tickets/{ticket.Id}", ticket.ToResponse());
}).RequireAuthorization();

app.MapGet("/tickets/{id:guid}", async (Guid id, TicketlyDbContext db, ClaimsPrincipal user) =>
{
    // 1. fetch the ticket by id
    var ticket = await db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
    // 2. if it doesn't exist -> Results.NotFound()
    if (ticket is null || (!user.IsStaff() && ticket.CustomerId != user.GetUserId()))
    {
        return Results.NotFound();
    }
    // 3. otherwise -> Results.Ok(ticket.ToResponse())
    return Results.Ok(ticket.ToResponse());
}).RequireAuthorization();

app.MapGet("/tickets", async (TicketlyDbContext db, TicketStatus? status, ClaimsPrincipal user, int page = 1, int pageSize = 20) =>
{
    page = Math.Max(page, 1);
    pageSize = Math.Clamp(pageSize, 1, 50);

    var query = db.Tickets.AsNoTracking();
    if (!user.IsStaff())
    {
        var userId = user.GetUserId();
        query = query.Where(t => t.CustomerId == userId);
    }
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
}).RequireAuthorization();

app.Run();


public partial class Program { }