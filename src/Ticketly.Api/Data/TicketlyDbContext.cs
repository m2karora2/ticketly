// src/Ticketly.Api/Data/TicketlyDbContext.cs
using Microsoft.EntityFrameworkCore;
using Ticketly.Api.Domain;

namespace Ticketly.Api.Data;

public class TicketlyDbContext(DbContextOptions<TicketlyDbContext> options) : DbContext(options)
{
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ticket>(e =>
        {
            e.HasKey(t => t.Id);

            e.Property(t => t.Title).IsRequired().HasMaxLength(200);
            e.Property(t => t.Description).IsRequired().HasMaxLength(5000);

            // Status is a label: store as readable text
            e.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);

            // Priority has an order: store as a small number so ORDER BY works
            e.Property(t => t.Priority).HasConversion<short>();

            e.Property(t => t.CustomerId).IsRequired().HasMaxLength(100);
            e.Property(t => t.AssigneeId).HasMaxLength(100);

            // Support queue: open tickets, highest priority first, oldest first
            e.HasIndex(t => new { t.Status, t.Priority, t.CreatedAt })
                .IsDescending(false, true, false);

            // Customer view: my tickets, newest first
            e.HasIndex(t => new { t.CustomerId, t.CreatedAt })
                .IsDescending(false, true);
        });

        modelBuilder.Entity<Comment>(e =>
        {
            e.HasKey(c => c.Id);

            e.Property(c => c.Body).IsRequired().HasMaxLength(5000);
            e.Property(c => c.AuthorId).IsRequired().HasMaxLength(100);

            e.HasOne(c => c.Ticket)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            // Comments on a ticket, in order (also covers the FK lookup)
            e.HasIndex(c => new { c.TicketId, c.CreatedAt });
        });
    }
}