using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PunchKiosk.Migrations
{
    /// <inheritdoc />
    public partial class AddImagePathToPunchRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPunchIn",
                table: "PunchRecords");

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "PunchRecords",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "PunchRecords");

            migrationBuilder.AddColumn<bool>(
                name: "IsPunchIn",
                table: "PunchRecords",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
