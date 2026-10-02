using Entregas.Domain;

namespace Entregas.UnitTests;

public sealed class EntregaTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Actividad = Guid.NewGuid();
    private static readonly Guid Estudiante = Guid.NewGuid();

    private static ArchivoGuardado Archivo(string nombre, long tamano, string ruta) => new(nombre, tamano, "application/pdf", ruta);

    [Fact]
    public void Crear_deja_la_entrega_enviada_con_sus_archivos_y_fecha_en_utc()
    {
        var entrega = Entrega.Crear(Actividad, Estudiante, [Archivo("a.pdf", 10, "r1"), Archivo("b.pdf", 5, "r2")], Ahora.ToOffset(TimeSpan.FromHours(-5)));

        Assert.Equal(EstadoEntrega.ENVIADA, entrega.Estado);
        Assert.Equal(DateTimeKind.Utc, entrega.FechaEnvio.Kind);
        Assert.Equal(Ahora.UtcDateTime, entrega.FechaEnvio);
        Assert.Equal(["a.pdf", "b.pdf"], entrega.Archivos.Select(a => a.NombreArchivo));
        Assert.All(entrega.Archivos, a => Assert.Equal(entrega.Id, a.EntregaId));
        Assert.Equal(15, entrega.TamanoTotal);
        Assert.True(entrega.PerteneceA(Estudiante));
        Assert.False(entrega.PerteneceA(Guid.NewGuid()));
    }

    [Fact]
    public void Una_entrega_sin_archivos_no_es_valida()
        => Assert.Throws<ArchivoInvalidoException>(() => Entrega.Crear(Actividad, Estudiante, [], Ahora));

    [Fact]
    public void Reemplazar_cambia_todo_el_conjunto_y_devuelve_los_blobs_anteriores()
    {
        var entrega = Entrega.Crear(Actividad, Estudiante, [Archivo("a.pdf", 10, "r1"), Archivo("b.pdf", 5, "r2")], Ahora);

        var anteriores = entrega.ReemplazarArchivos([Archivo("c.docx", 20, "r3")], Ahora.AddHours(1));

        Assert.Equal(["r1", "r2"], anteriores);
        var unico = Assert.Single(entrega.Archivos);
        Assert.Equal(("c.docx", 20L, "r3"), (unico.NombreArchivo, unico.Tamano, unico.RutaBlob));
        Assert.Equal(Ahora.AddHours(1).UtcDateTime, entrega.FechaEnvio);
    }

    [Fact]
    public void Anular_y_volver_a_subir_reutiliza_la_misma_entrega()
    {
        var entrega = Entrega.Crear(Actividad, Estudiante, [Archivo("a.pdf", 10, "r1")], Ahora);
        var id = entrega.Id;

        entrega.Anular();
        Assert.Equal(EstadoEntrega.ANULADA, entrega.Estado);

        entrega.ReemplazarArchivos([Archivo("c.pdf", 30, "r3")], Ahora.AddHours(2));
        Assert.Equal(EstadoEntrega.ENVIADA, entrega.Estado);
        Assert.Equal(id, entrega.Id);
    }

    [Fact]
    public void Busca_un_archivo_por_id()
    {
        var entrega = Entrega.Crear(Actividad, Estudiante, [Archivo("a.pdf", 10, "r1")], Ahora);

        Assert.Same(entrega.Archivos[0], entrega.Archivo(entrega.Archivos[0].Id));
        Assert.Null(entrega.Archivo(Guid.NewGuid()));
    }
}
