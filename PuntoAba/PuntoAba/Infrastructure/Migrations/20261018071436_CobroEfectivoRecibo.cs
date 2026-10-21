using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PuntoAba.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CobroEfectivoRecibo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Cambio",
                table: "Ventas",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Recibido",
                table: "Ventas",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cambio",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "Recibido",
                table: "Ventas");
        }
    }
}
