// src/Ticketly.Api/Domain/Ticket.cs
namespace Ticketly.Api.Domain;

public enum TicketStatus { Open, InProgress, Resolved }
public enum TicketPriority { Low = 1, Medium = 2, High = 3, Urgent = 4 }

public class Ticket
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Title { get; set; }
    public required string Description { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public required string CustomerId { get; set; }
    public string? AssigneeId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAt { get; set; }
    public List<Comment> Comments { get; set; } = [];
}