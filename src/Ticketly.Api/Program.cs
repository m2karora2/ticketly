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

//cors
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
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Staff", policy => policy.RequireRole("support", "admin"));
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
}).Produces<TicketResponse>(StatusCodes.Status201Created).RequireAuthorization();

app.MapGet("/tickets/{id:guid}", async (Guid id, ClaimsPrincipal user, TicketlyDbContext db) =>
{
    var isStaff = user.IsStaff();
    var ticket = await db.Tickets.AsNoTracking()
        .Include(t => t.Comments
            .Where(c => isStaff || !c.IsInternal)
            .OrderBy(c => c.CreatedAt))
        .FirstOrDefaultAsync(t => t.Id == id);

    if (ticket is null || (!isStaff && ticket.CustomerId != user.GetUserId()))
        return Results.NotFound();

    return Results.Ok(ticket.ToDetailResponse());
})
.RequireAuthorization()
.Produces<TicketDetailResponse>()
.Produces(StatusCodes.Status404NotFound);

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
}).Produces<PagedResponse<TicketResponse>>().RequireAuthorization();

app.MapPatch("/tickets/{id:guid}", async (Guid id, UpdateTicketRequest request, TicketlyDbContext db, ClaimsPrincipal user) =>
{
    var ticket = await db.Tickets.FindAsync(id);
    if (ticket is null) return Results.NotFound();

    if (request.Status is { } status)
    {
        ticket.Status = status;
        ticket.ResolvedAt = status == TicketStatus.Resolved ? DateTimeOffset.UtcNow : null;
    }
    if (request.AssignToMe)
    {
        ticket.AssigneeId = user.GetUserId();
        ticket.AssigneeName = user.GetDisplayName();
    }

    ticket.UpdatedAt = DateTimeOffset.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(ticket.ToResponse());
}).RequireAuthorization("Staff")
.Produces<TicketResponse>()
.Produces(StatusCodes.Status404NotFound);

//comments endpoints

app.MapPost("/tickets/{id:guid}/comments", async (
    Guid id, CreateCommentRequest request, ClaimsPrincipal user, TicketlyDbContext db) =>
{
    var ticket = await db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
    if (ticket is null || (!user.IsStaff() && ticket.CustomerId != user.GetUserId()))
        return Results.NotFound();

    if (request.IsInternal && !user.IsStaff())
        return Results.Forbid();

    var comment = new Comment
    {
        TicketId = id,
        AuthorId = user.GetUserId(),
        AuthorName = user.GetDisplayName(),
        Body = request.Body,
        IsInternal = request.IsInternal,
    };
    db.Comments.Add(comment);
    await db.SaveChangesAsync();

    return Results.Created($"/tickets/{id}", comment.ToResponse());
})
.RequireAuthorization()
.Produces<CommentResponse>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status403Forbidden);

app.Run();


public partial class Program { }