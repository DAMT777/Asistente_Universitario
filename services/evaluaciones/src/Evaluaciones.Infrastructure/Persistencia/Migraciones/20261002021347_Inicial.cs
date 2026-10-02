using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evaluaciones.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Curso",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ProfesorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfesorNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PesoCorte1 = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PesoCorte2 = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PesoCorte3 = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Curso", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Actividad",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CursoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Corte = table.Column<int>(type: "int", nullable: false),
                    Peso = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    FechaLimite = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequiereEntrega = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actividad", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Actividad_Curso_CursoId",
                        column: x => x.CursoId,
                        principalTable: "Curso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CursoEstudiante",
                columns: table => new
                {
                    CursoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstudianteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstudianteNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EstudianteCodigo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CursoEstudiante", x => new { x.CursoId, x.EstudianteId });
                    table.ForeignKey(
                        name: "FK_CursoEstudiante_Curso_CursoId",
                        column: x => x.CursoId,
                        principalTable: "Curso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PublicacionCorte",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CursoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstudianteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Corte = table.Column<int>(type: "int", nullable: false),
                    Nota = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    FechaPublicacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicacionCorte", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublicacionCorte_Curso_CursoId",
                        column: x => x.CursoId,
                        principalTable: "Curso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Calificacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActividadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstudianteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntregaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Valor = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    Retroalimentacion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Estado = table.Column<string>(type: "varchar(10)", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Calificacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Calificacion_Actividad_ActividadId",
                        column: x => x.ActividadId,
                        principalTable: "Actividad",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Actividad_CursoId_Corte",
                table: "Actividad",
                columns: new[] { "CursoId", "Corte" });

            migrationBuilder.CreateIndex(
                name: "IX_Calificacion_ActividadId_EstudianteId",
                table: "Calificacion",
                columns: new[] { "ActividadId", "EstudianteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Calificacion_EstudianteId",
                table: "Calificacion",
                column: "EstudianteId");

            migrationBuilder.CreateIndex(
                name: "IX_CursoEstudiante_EstudianteId",
                table: "CursoEstudiante",
                column: "EstudianteId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicacionCorte_CursoId_EstudianteId_Corte",
                table: "PublicacionCorte",
                columns: new[] { "CursoId", "EstudianteId", "Corte" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Calificacion");

            migrationBuilder.DropTable(
                name: "CursoEstudiante");

            migrationBuilder.DropTable(
                name: "PublicacionCorte");

            migrationBuilder.DropTable(
                name: "Actividad");

            migrationBuilder.DropTable(
                name: "Curso");
        }
    }
}
