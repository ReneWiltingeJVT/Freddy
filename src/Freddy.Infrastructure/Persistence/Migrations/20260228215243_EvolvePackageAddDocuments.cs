using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Freddy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EvolvePackageAddDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_packages_is_active",
                table: "packages");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "packages");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "packages",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "keywords",
                table: "packages",
                newName: "tags");

            migrationBuilder.RenameIndex(
                name: "ix_packages_keywords",
                table: "packages",
                newName: "ix_packages_tags");

            migrationBuilder.AddColumn<bool>(
                name: "is_published",
                table: "packages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Migrate existing active packages to published
            migrationBuilder.Sql("UPDATE packages SET is_published = true WHERE is_published = false;");

            migrationBuilder.AddColumn<bool>(
                name: "requires_confirmation",
                table: "packages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string[]>(
                name: "synonyms",
                table: "packages",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    steps_content = table.Column<string>(type: "jsonb", nullable: true),
                    file_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.id);
                    table.ForeignKey(
                        name: "FK_documents_packages_package_id",
                        column: x => x.package_id,
                        principalTable: "packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_packages_is_published",
                table: "packages",
                column: "is_published");

            migrationBuilder.CreateIndex(
                name: "ix_packages_synonyms",
                table: "packages",
                column: "synonyms")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_documents_package_id",
                table: "documents",
                column: "package_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "documents");

            migrationBuilder.DropIndex(
                name: "ix_packages_is_published",
                table: "packages");

            migrationBuilder.DropIndex(
                name: "ix_packages_synonyms",
                table: "packages");

            migrationBuilder.DropColumn(
                name: "is_published",
                table: "packages");

            migrationBuilder.DropColumn(
                name: "requires_confirmation",
                table: "packages");

            migrationBuilder.DropColumn(
                name: "synonyms",
                table: "packages");

            migrationBuilder.RenameColumn(
                name: "title",
                table: "packages",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "tags",
                table: "packages",
                newName: "keywords");

            migrationBuilder.RenameIndex(
                name: "ix_packages_tags",
                table: "packages",
                newName: "ix_packages_keywords");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "packages",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "ix_packages_is_active",
                table: "packages",
                column: "is_active");
        }
    }
}
