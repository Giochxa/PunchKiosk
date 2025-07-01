using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PunchKiosk.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncedAtToPunchRecord1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SyncedAt",
                table: "PunchRecords",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SyncedAt",
                table: "PunchRecords");
        }
    }
}
