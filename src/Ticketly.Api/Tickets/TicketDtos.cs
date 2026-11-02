using System.ComponentModel.DataAnnotations;
using Ticketly.Api.Domain;

namespace Ticketly.Api.Tickets;

public record CreateTicketRequest(
    [property: Required, StringLength(200, MinimumLength = 1)] string Title,
    [property: Required, StringLength(5000, MinimumLength = 1)] string Description,
    TicketPriority? Priority);

public record TicketResponse(
    Guid Id,
    string Title,
    string Description,
    TicketStatus Status,
    TicketPriority Priority,
    string? AssigneeId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ResolvedAt
);

public static class TicketMappings
{
    public static TicketResponse ToResponse(this Ticket t) =>
        new(t.Id, t.Title, t.Description, t.Status, t.Priority,
            t.AssigneeId, t.CreatedAt, t.UpdatedAt, t.ResolvedAt);
}