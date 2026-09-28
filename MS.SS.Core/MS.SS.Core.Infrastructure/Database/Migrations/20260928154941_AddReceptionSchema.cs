using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MS.SS.Core.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddReceptionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "reception");

            migrationBuilder.CreateTable(
                name: "deliveries",
                schema: "reception",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deliveries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pallets",
                schema: "reception",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    delivery_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pallets", x => x.id);
                    table.ForeignKey(
                        name: "fk_pallets_deliveries_delivery_id",
                        column: x => x.delivery_id,
                        principalSchema: "reception",
                        principalTable: "deliveries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cartons",
                schema: "reception",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    pallet_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cartons", x => x.id);
                    table.ForeignKey(
                        name: "fk_cartons_pallet_pallet_id",
                        column: x => x.pallet_id,
                        principalSchema: "reception",
                        principalTable: "pallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_lines",
                schema: "reception",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    size = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    expected_quantity = table.Column<int>(type: "integer", nullable: false),
                    received_quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    position = table.Column<int>(type: "integer", nullable: false),
                    carton_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_lines", x => x.id);
                    table.CheckConstraint("ck_product_lines_expected_positive", "expected_quantity > 0");
                    table.CheckConstraint("ck_product_lines_received_bounds", "received_quantity >= 0 AND received_quantity <= expected_quantity");
                    table.ForeignKey(
                        name: "fk_product_lines_cartons_carton_id",
                        column: x => x.carton_id,
                        principalSchema: "reception",
                        principalTable: "cartons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_cartons_pallet_code",
                schema: "reception",
                table: "cartons",
                columns: new[] { "pallet_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_deliveries_order_number",
                schema: "reception",
                table: "deliveries",
                column: "order_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_pallets_delivery_code",
                schema: "reception",
                table: "pallets",
                columns: new[] { "delivery_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_product_lines_carton_reference",
                schema: "reception",
                table: "product_lines",
                columns: new[] { "carton_id", "reference" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_lines",
                schema: "reception");

            migrationBuilder.DropTable(
                name: "cartons",
                schema: "reception");

            migrationBuilder.DropTable(
                name: "pallets",
                schema: "reception");

            migrationBuilder.DropTable(
                name: "deliveries",
                schema: "reception");
        }
    }
}
