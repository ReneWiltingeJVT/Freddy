using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Freddy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationPendingPackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "pending_package_id",
                table: "conversations",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pending_package_id",
                table: "conversations");
        }
    }
}
