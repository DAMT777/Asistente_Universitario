using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Asistente.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccionPendiente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreadaPor = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CursoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActividadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActividadTitulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EstudianteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstudianteNombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: false),
                    Retroalimentacion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ValorAnterior = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: true),
                    VersionEsperada = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreadaEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiraEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ResueltaEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccionPendiente", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Auditoria",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccionPendienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DatosAntes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DatosDespues = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Resultado = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Origen = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auditoria", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccionPendiente_CreadaPor_Estado",
                table: "AccionPendiente",
                columns: new[] { "CreadaPor", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_UsuarioId_FechaUtc",
                table: "Auditoria",
                columns: new[] { "UsuarioId", "FechaUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccionPendiente");

            migrationBuilder.DropTable(
                name: "Auditoria");
        }
    }
}
