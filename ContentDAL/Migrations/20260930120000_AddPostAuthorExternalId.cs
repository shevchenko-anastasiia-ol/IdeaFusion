using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentDAL.Migrations
{
    /// <inheritdoc />
    public partial class AddPostAuthorExternalId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "externalid",
                table: "postauthors",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // Filtered on purpose: the demo rows seeded before the realm existed hold
            // faker usernames rather than Keycloak subjects, so they all keep a null
            // externalid. A plain unique index would forbid more than one of them.
            migrationBuilder.CreateIndex(
                name: "ix_postauthors_externalid",
                table: "postauthors",
                column: "externalid",
                unique: true,
                filter: "externalid IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_postauthors_externalid",
                table: "postauthors");

            migrationBuilder.DropColumn(
                name: "externalid",
                table: "postauthors");
        }
    }
}
