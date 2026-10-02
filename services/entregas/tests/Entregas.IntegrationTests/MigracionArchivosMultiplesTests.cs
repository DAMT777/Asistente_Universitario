using Entregas.Infrastructure.Persistencia;
using Entregas.IntegrationTests.Infraestructura;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Entregas.IntegrationTests;

/// <summary>La migración a varios archivos conserva las entregas hechas con el esquema anterior.</summary>
[Collection(ColeccionEntregas.Nombre)]
public sealed class MigracionArchivosMultiplesTests(EntregasFixture fixture)
{
    [Fact]
    public async Task Pasa_el_archivo_de_cada_entrega_a_ArchivoEntrega()
    {
        var cadena = fixture.CadenaPara($"entregas_migracion_{Guid.NewGuid():N}");
        await using var db = new EntregasDbContext(new DbContextOptionsBuilder<EntregasDbContext>().UseSqlServer(cadena).Options);
        var migrador = db.GetService<IMigrator>();
        var inicial = db.Database.GetMigrations().First();

        await migrador.MigrateAsync(inicial);
        var entregaId = Guid.NewGuid();
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO Entrega (Id, ActividadId, EstudianteId, FechaEnvio, Estado, NombreArchivo, Tamano, RutaBlob)
            VALUES ({entregaId}, {Guid.NewGuid()}, {Guid.NewGuid()}, SYSUTCDATETIME(), 'ENVIADA', 'Informe Final.DOCX', 1234, 'a/b/c')
            """);

        await migrador.MigrateAsync();

        var entrega = await db.Entregas.AsNoTracking().SingleAsync(e => e.Id == entregaId);
        var archivo = Assert.Single(entrega.Archivos);
        Assert.Equal(("Informe Final.DOCX", 1234L, "a/b/c", 0), (archivo.NombreArchivo, archivo.Tamano, archivo.RutaBlob, archivo.Orden));
        Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document", archivo.ContentType);

        await db.Database.EnsureDeletedAsync();
        SqlConnection.ClearAllPools();
    }
}
