using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Freddy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoriesClientsAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "category",
                table: "packages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "client_id",
                table: "packages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "pending_client_id",
                table: "conversations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    details = table.Column<string>(type: "jsonb", nullable: true),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    aliases = table.Column<string[]>(type: "text[]", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clients", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_packages_category",
                table: "packages",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_packages_client_id",
                table: "packages",
                column: "client_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_packages_category_client",
                table: "packages",
                sql: "(category != 2 AND client_id IS NULL) OR (category = 2 AND client_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity_type",
                table: "audit_logs",
                column: "entity_type");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_timestamp",
                table: "audit_logs",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_clients_aliases",
                table: "clients",
                column: "aliases")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_clients_display_name",
                table: "clients",
                column: "display_name");

            migrationBuilder.CreateIndex(
                name: "ix_clients_is_active",
                table: "clients",
                column: "is_active");

            migrationBuilder.AddForeignKey(
                name: "FK_packages_clients_client_id",
                table: "packages",
                column: "client_id",
                principalTable: "clients",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_packages_clients_client_id",
                table: "packages");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "clients");

            migrationBuilder.DropIndex(
                name: "ix_packages_category",
                table: "packages");

            migrationBuilder.DropIndex(
                name: "ix_packages_client_id",
                table: "packages");

            migrationBuilder.DropCheckConstraint(
                name: "ck_packages_category_client",
                table: "packages");

            migrationBuilder.DropColumn(
                name: "category",
                table: "packages");

            migrationBuilder.DropColumn(
                name: "client_id",
                table: "packages");

            migrationBuilder.DropColumn(
                name: "pending_client_id",
                table: "conversations");
        }
    }
}
