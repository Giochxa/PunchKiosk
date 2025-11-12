using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PunchKiosk.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalIdToPunchRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PersonalId",
                table: "PunchRecords",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PersonalId",
                table: "PunchRecords");
        }
    }
}
