using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticketly.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketAssigneeName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssigneeName",
                table: "Tickets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssigneeName",
                table: "Tickets");
        }
    }
}
