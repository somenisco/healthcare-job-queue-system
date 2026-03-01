using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobProcessor.Migrations
{
    /// <inheritdoc />
    public partial class AddTestOrderListIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "TestOrders",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TestOrders_Status_UpdatedAt_TestOrderId",
                table: "TestOrders",
                columns: new[] { "Status", "UpdatedAt", "TestOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_TestOrders_UpdatedAt_TestOrderId",
                table: "TestOrders",
                columns: new[] { "UpdatedAt", "TestOrderId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TestOrders_Status_UpdatedAt_TestOrderId",
                table: "TestOrders");

            migrationBuilder.DropIndex(
                name: "IX_TestOrders_UpdatedAt_TestOrderId",
                table: "TestOrders");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "TestOrders",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
