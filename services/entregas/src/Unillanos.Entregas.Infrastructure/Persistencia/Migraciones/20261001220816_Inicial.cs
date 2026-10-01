using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unillanos.Entregas.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Entrega",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActividadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstudianteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaEnvio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    NombreArchivo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Tamano = table.Column<long>(type: "bigint", nullable: false),
                    RutaBlob = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entrega", x => x.Id);
                    table.CheckConstraint("CK_Entrega_Estado", "[Estado] IN ('ENVIADA', 'ANULADA')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Entrega_Estudiante",
                table: "Entrega",
                column: "EstudianteId");

            migrationBuilder.CreateIndex(
                name: "UX_Entrega_Actividad_Estudiante",
                table: "Entrega",
                columns: new[] { "ActividadId", "EstudianteId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Entrega");
        }
    }
}
