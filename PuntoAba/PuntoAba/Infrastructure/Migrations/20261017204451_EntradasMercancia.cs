using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PuntoAba.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EntradasMercancia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EntradasMercancia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OperacionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Proveedor = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Documento = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Notas = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: true),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntradasMercancia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntradasMercancia_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetallesEntradaMercancia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EntradaMercanciaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Importe = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesEntradaMercancia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetallesEntradaMercancia_EntradasMercancia_EntradaMercanciaId",
                        column: x => x.EntradaMercanciaId,
                        principalTable: "EntradasMercancia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetallesEntradaMercancia_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesEntradaMercancia_EntradaMercanciaId",
                table: "DetallesEntradaMercancia",
                column: "EntradaMercanciaId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesEntradaMercancia_ProductoId",
                table: "DetallesEntradaMercancia",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_EntradasMercancia_OperacionId",
                table: "EntradasMercancia",
                column: "OperacionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntradasMercancia_UsuarioId",
                table: "EntradasMercancia",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetallesEntradaMercancia");

            migrationBuilder.DropTable(
                name: "EntradasMercancia");
        }
    }
}
