// src/Ticketly.Api/Domain/Comment.cs
namespace Ticketly.Api.Domain;

public class Comment
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public required string AuthorId { get; set; }
    public required string Body { get; set; }
    public required bool IsInternal { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}