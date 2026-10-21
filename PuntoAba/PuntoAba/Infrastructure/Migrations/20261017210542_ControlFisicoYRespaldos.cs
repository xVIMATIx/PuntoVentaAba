using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PuntoAba.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ControlFisicoYRespaldos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OperacionId",
                table: "Ventas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProveedorHabitual",
                table: "Productos",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StockObjetivo",
                table: "Productos",
                type: "decimal(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "VersionInventario",
                table: "Productos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<decimal>(
                name: "CostoUnitario",
                table: "MovimientosInventario",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MovimientoOriginalId",
                table: "MovimientosInventario",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OperacionId",
                table: "MovimientosInventario",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CajasRecibidas",
                table: "DetallesEntradaMercancia",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PiezasPorCaja",
                table: "DetallesEntradaMercancia",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StockAnterior",
                table: "DetallesEntradaMercancia",
                type: "decimal(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "StockNuevo",
                table: "DetallesEntradaMercancia",
                type: "decimal(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ConteosFisicos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OperacionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaConfirmacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResponsableId = table.Column<int>(type: "INTEGER", nullable: false),
                    AutorizadoPorId = table.Column<int>(type: "INTEGER", nullable: true),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConteosFisicos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DetallesConteoFisico",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConteoFisicoId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductoId = table.Column<int>(type: "INTEGER", nullable: false),
                    CantidadSistema = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    CantidadContada = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    VersionProducto = table.Column<long>(type: "INTEGER", nullable: false),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesConteoFisico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetallesConteoFisico_ConteosFisicos_ConteoFisicoId",
                        column: x => x.ConteoFisicoId,
                        principalTable: "ConteosFisicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetallesConteoFisico_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_OperacionId",
                table: "Ventas",
                column: "OperacionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_MovimientoOriginalId",
                table: "MovimientosInventario",
                column: "MovimientoOriginalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_OperacionId",
                table: "MovimientosInventario",
                column: "OperacionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConteosFisicos_OperacionId",
                table: "ConteosFisicos",
                column: "OperacionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DetallesConteoFisico_ConteoFisicoId",
                table: "DetallesConteoFisico",
                column: "ConteoFisicoId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesConteoFisico_ProductoId",
                table: "DetallesConteoFisico",
                column: "ProductoId");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosInventario_MovimientosInventario_MovimientoOriginalId",
                table: "MovimientosInventario",
                column: "MovimientoOriginalId",
                principalTable: "MovimientosInventario",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosInventario_MovimientosInventario_MovimientoOriginalId",
                table: "MovimientosInventario");

            migrationBuilder.DropTable(
                name: "DetallesConteoFisico");

            migrationBuilder.DropTable(
                name: "ConteosFisicos");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_OperacionId",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosInventario_MovimientoOriginalId",
                table: "MovimientosInventario");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosInventario_OperacionId",
                table: "MovimientosInventario");

            migrationBuilder.DropColumn(
                name: "OperacionId",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "ProveedorHabitual",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "StockObjetivo",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "VersionInventario",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "CostoUnitario",
                table: "MovimientosInventario");

            migrationBuilder.DropColumn(
                name: "MovimientoOriginalId",
                table: "MovimientosInventario");

            migrationBuilder.DropColumn(
                name: "OperacionId",
                table: "MovimientosInventario");

            migrationBuilder.DropColumn(
                name: "CajasRecibidas",
                table: "DetallesEntradaMercancia");

            migrationBuilder.DropColumn(
                name: "PiezasPorCaja",
                table: "DetallesEntradaMercancia");

            migrationBuilder.DropColumn(
                name: "StockAnterior",
                table: "DetallesEntradaMercancia");

            migrationBuilder.DropColumn(
                name: "StockNuevo",
                table: "DetallesEntradaMercancia");
        }
    }
}
