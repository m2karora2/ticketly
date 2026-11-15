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
    string CustomerId,
    string? AssigneeId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ResolvedAt
);

public record TicketDetailResponse(
    Guid Id, string Title, string Description,
    TicketStatus Status, TicketPriority Priority,
    string CustomerId, string? AssigneeId,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? ResolvedAt,
    IReadOnlyList<CommentResponse> Comments);

public record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public static class TicketMappings
{
    public static TicketResponse ToResponse(this Ticket t) =>
        new(t.Id, t.Title, t.Description, t.Status, t.Priority, t.CustomerId,
            t.AssigneeId, t.CreatedAt, t.UpdatedAt, t.ResolvedAt);

    public static CommentResponse ToResponse(this Comment c) =>
        new(c.Id, c.AuthorId, c.Body, c.IsInternal, c.CreatedAt);

    public static TicketDetailResponse ToDetailResponse(this Ticket t) => new(
        t.Id, t.Title, t.Description, t.Status, t.Priority, t.CustomerId, t.AssigneeId,
        t.CreatedAt, t.UpdatedAt, t.ResolvedAt,
        t.Comments.Select(c => c.ToResponse()).ToList());
}

public record UpdateTicketRequest(
    TicketStatus? Status,
    [property: StringLength(100)] string? AssigneeId);

public record CreateCommentRequest(
[property: Required, StringLength(5000, MinimumLength = 1)] string Body,
bool IsInternal = false);

public record CommentResponse(
    Guid Id, string AuthorId, string Body, bool IsInternal, DateTimeOffset CreatedAt);