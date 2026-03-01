using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobProcessor.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateOfBirth = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.PatientId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Samples",
                columns: table => new
                {
                    SampleId = table.Column<long>(type: "bigint", nullable: false),
                    SampleType = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CollectedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PatientId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Samples", x => x.SampleId);
                    table.ForeignKey(
                        name: "FK_Samples_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TestOrders",
                columns: table => new
                {
                    TestOrderId = table.Column<long>(type: "bigint", nullable: false),
                    SampleId = table.Column<long>(type: "bigint", nullable: false),
                    TestOrderType = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Payload = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    MaxRetries = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestOrders", x => x.TestOrderId);
                    table.ForeignKey(
                        name: "FK_TestOrders_Samples_SampleId",
                        column: x => x.SampleId,
                        principalTable: "Samples",
                        principalColumn: "SampleId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TestResults",
                columns: table => new
                {
                    TestResultId = table.Column<long>(type: "bigint", nullable: false),
                    TestOrderId = table.Column<long>(type: "bigint", nullable: false),
                    ResultValue = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Unit = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestResults", x => x.TestResultId);
                    table.ForeignKey(
                        name: "FK_TestResults_TestOrders_TestOrderId",
                        column: x => x.TestOrderId,
                        principalTable: "TestOrders",
                        principalColumn: "TestOrderId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Samples_PatientId",
                table: "Samples",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_TestOrders_SampleId",
                table: "TestOrders",
                column: "SampleId");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_TestOrderId",
                table: "TestResults",
                column: "TestOrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestResults");

            migrationBuilder.DropTable(
                name: "TestOrders");

            migrationBuilder.DropTable(
                name: "Samples");

            migrationBuilder.DropTable(
                name: "Patients");
        }
    }
}
