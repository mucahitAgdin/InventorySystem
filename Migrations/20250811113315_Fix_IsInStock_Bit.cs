using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventorySystem.Migrations
{
    /// <inheritdoc />
    public partial class Fix_IsInStock_Bit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsInStock",
                table: "Products",
                type: "bit",
                nullable: false,
                computedColumnSql: "CASE WHEN [Quantity] > 0 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END",
                stored: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldComputedColumnSql: "CASE WHEN [Quantity] > 0 THEN 1 ELSE 0 END",
                oldStored: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsInStock",
                table: "Products",
                type: "bit",
                nullable: false,
                computedColumnSql: "CASE WHEN [Quantity] > 0 THEN 1 ELSE 0 END",
                stored: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldComputedColumnSql: "CASE WHEN [Quantity] > 0 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END",
                oldStored: true);
        }
    }
}
