using Entregas.Domain;

namespace Entregas.UnitTests;

public sealed class EntregaTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Actividad = Guid.NewGuid();
    private static readonly Guid Estudiante = Guid.NewGuid();

    [Fact]
    public void Crear_deja_la_entrega_enviada_con_fecha_en_utc()
    {
        var entrega = Entrega.Crear(Actividad, Estudiante, new ArchivoGuardado("a.pdf", 10, "r1"), Ahora.ToOffset(TimeSpan.FromHours(-5)));

        Assert.Equal(EstadoEntrega.ENVIADA, entrega.Estado);
        Assert.Equal(DateTimeKind.Utc, entrega.FechaEnvio.Kind);
        Assert.Equal(Ahora.UtcDateTime, entrega.FechaEnvio);
        Assert.True(entrega.PerteneceA(Estudiante));
        Assert.False(entrega.PerteneceA(Guid.NewGuid()));
    }

    [Fact]
    public void Reemplazar_actualiza_metadatos_y_devuelve_el_blob_anterior()
    {
        var entrega = Entrega.Crear(Actividad, Estudiante, new ArchivoGuardado("a.pdf", 10, "r1"), Ahora);

        var anterior = entrega.ReemplazarArchivo(new ArchivoGuardado("b.docx", 20, "r2"), Ahora.AddHours(1));

        Assert.Equal("r1", anterior);
        Assert.Equal(("b.docx", 20L, "r2"), (entrega.NombreArchivo, entrega.Tamano, entrega.RutaBlob));
        Assert.Equal(Ahora.AddHours(1).UtcDateTime, entrega.FechaEnvio);
    }

    [Fact]
    public void Anular_y_volver_a_subir_reutiliza_la_misma_entrega()
    {
        var entrega = Entrega.Crear(Actividad, Estudiante, new ArchivoGuardado("a.pdf", 10, "r1"), Ahora);
        var id = entrega.Id;

        entrega.Anular();
        Assert.Equal(EstadoEntrega.ANULADA, entrega.Estado);

        entrega.ReemplazarArchivo(new ArchivoGuardado("c.pdf", 30, "r3"), Ahora.AddHours(2));
        Assert.Equal(EstadoEntrega.ENVIADA, entrega.Estado);
        Assert.Equal(id, entrega.Id);
    }
}
