using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobProcessor.Migrations
{
    /// <inheritdoc />
    public partial class AddTestOrderUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "TestOrders",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(6)");

            migrationBuilder.Sql("UPDATE TestOrders SET UpdatedAt = CreatedAt WHERE UpdatedAt IS NULL OR UpdatedAt < CreatedAt;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "TestOrders");
        }
    }
}

