using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PromotionMonitor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase2CouponsShippingComparison : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CashbackProgram",
                table: "Calculations",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CouponCode",
                table: "Calculations",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CouponConfirmed",
                table: "Calculations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ShippingProvider",
                table: "Calculations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShippingSimulated",
                table: "Calculations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CashbackRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxCashback = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashbackRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashbackRules_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CouponRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MinSubtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxDiscount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ValidFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ValidUntil = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CouponRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CouponRules_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShippingQuotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MonitoringId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Cep = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ListedPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    QuoteAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Simulated = table.Column<bool>(type: "bit", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawPayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuotedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingQuotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoreLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MonitoringId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProductUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    SourceKind = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreLinks_Monitorings_MonitoringId",
                        column: x => x.MonitoringId,
                        principalTable: "Monitorings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StoreOffers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MonitoringId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComparisonRoundId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreLinkId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StoreLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProductUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    SourceKind = table.Column<int>(type: "int", nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CollectedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ListedPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Shipping = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ShippingProvider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ShippingSimulated = table.Column<bool>(type: "bit", nullable: false),
                    CouponCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CouponDiscount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CouponConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    CashbackEstimate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PayAtCheckout = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    NetAfterCashback = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    VoltageDetected = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VoltageMatches = table.Column<bool>(type: "bit", nullable: true),
                    RawPayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsBestPayAtCheckout = table.Column<bool>(type: "bit", nullable: false),
                    IsBestNetAfterCashback = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreOffers_Monitorings_MonitoringId",
                        column: x => x.MonitoringId,
                        principalTable: "Monitorings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashbackRules_UserId",
                table: "CashbackRules",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CouponRules_UserId_Code",
                table: "CouponRules",
                columns: new[] { "UserId", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_ShippingQuotes_Cep_QuotedAt",
                table: "ShippingQuotes",
                columns: new[] { "Cep", "QuotedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StoreLinks_MonitoringId",
                table: "StoreLinks",
                column: "MonitoringId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreOffers_MonitoringId_ComparisonRoundId",
                table: "StoreOffers",
                columns: new[] { "MonitoringId", "ComparisonRoundId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashbackRules");

            migrationBuilder.DropTable(
                name: "CouponRules");

            migrationBuilder.DropTable(
                name: "ShippingQuotes");

            migrationBuilder.DropTable(
                name: "StoreLinks");

            migrationBuilder.DropTable(
                name: "StoreOffers");

            migrationBuilder.DropColumn(
                name: "CashbackProgram",
                table: "Calculations");

            migrationBuilder.DropColumn(
                name: "CouponCode",
                table: "Calculations");

            migrationBuilder.DropColumn(
                name: "CouponConfirmed",
                table: "Calculations");

            migrationBuilder.DropColumn(
                name: "ShippingProvider",
                table: "Calculations");

            migrationBuilder.DropColumn(
                name: "ShippingSimulated",
                table: "Calculations");
        }
    }
}
