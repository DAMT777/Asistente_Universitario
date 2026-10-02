using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entregas.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class ArchivosMultiples : Migration
    {
        /// <inheritdoc />
        // Pasa de un archivo por entrega a varios: crea ArchivoEntrega, copia el archivo de cada
        // entrega existente y solo después elimina las columnas viejas, para no perder datos.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArchivoEntrega",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntregaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    NombreArchivo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Tamano = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    RutaBlob = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchivoEntrega", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchivoEntrega_Entrega_EntregaId",
                        column: x => x.EntregaId,
                        principalTable: "Entrega",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UX_ArchivoEntrega_Entrega_Orden",
                table: "ArchivoEntrega",
                columns: new[] { "EntregaId", "Orden" },
                unique: true);

            // El tipo de contenido no se guardaba: se deduce de la extensión (ya validada al subir).
            migrationBuilder.Sql("""
                INSERT INTO ArchivoEntrega (Id, EntregaId, Orden, NombreArchivo, Tamano, ContentType, RutaBlob)
                SELECT NEWID(), Id, 0, NombreArchivo, Tamano,
                    CASE LOWER(RIGHT(NombreArchivo, CHARINDEX('.', REVERSE(NombreArchivo) + '.')))
                        WHEN '.pdf' THEN 'application/pdf'
                        WHEN '.doc' THEN 'application/msword'
                        WHEN '.xls' THEN 'application/vnd.ms-excel'
                        WHEN '.ppt' THEN 'application/vnd.ms-powerpoint'
                        WHEN '.docx' THEN 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'
                        WHEN '.xlsx' THEN 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
                        WHEN '.pptx' THEN 'application/vnd.openxmlformats-officedocument.presentationml.presentation'
                        WHEN '.zip' THEN 'application/zip'
                        WHEN '.png' THEN 'image/png'
                        WHEN '.jpg' THEN 'image/jpeg'
                        WHEN '.jpeg' THEN 'image/jpeg'
                        ELSE 'application/octet-stream'
                    END,
                    RutaBlob
                FROM Entrega;
                """);

            migrationBuilder.DropColumn(name: "NombreArchivo", table: "Entrega");
            migrationBuilder.DropColumn(name: "RutaBlob", table: "Entrega");
            migrationBuilder.DropColumn(name: "Tamano", table: "Entrega");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NombreArchivo", table: "Entrega", type: "nvarchar(255)", maxLength: 255, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(
                name: "RutaBlob", table: "Entrega", type: "nvarchar(500)", maxLength: 500, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<long>(
                name: "Tamano", table: "Entrega", type: "bigint", nullable: false, defaultValue: 0L);

            // Un archivo por entrega: se conserva el primero de cada una.
            migrationBuilder.Sql("""
                UPDATE e SET e.NombreArchivo = a.NombreArchivo, e.RutaBlob = a.RutaBlob, e.Tamano = a.Tamano
                FROM Entrega e
                CROSS APPLY (SELECT TOP 1 * FROM ArchivoEntrega x WHERE x.EntregaId = e.Id ORDER BY x.Orden) a;
                """);

            migrationBuilder.DropTable(name: "ArchivoEntrega");
        }
    }
}
