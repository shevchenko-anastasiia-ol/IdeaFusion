using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentDAL.Migrations
{
    /// <inheritdoc />
    public partial class AddCollaborationSnapshotExternalId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "externalid",
                table: "collaborationsnapshots",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "externalid",
                table: "collaborationsnapshots");
        }
    }
}
