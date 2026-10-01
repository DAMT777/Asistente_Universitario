namespace Unillanos.Entregas.Domain;

public enum EstadoEntrega
{
    ENVIADA,
    ANULADA,
}

/// <summary>
/// Una entrega por actividad y estudiante (RN-07). Editar reemplaza el archivo;
/// anular cambia el estado; volver a subir tras anular reutiliza la fila (DA-05).
/// </summary>
public sealed class Entrega
{
    public Guid Id { get; private set; }
    public Guid ActividadId { get; private set; }
    public Guid EstudianteId { get; private set; }
    public DateTime FechaEnvio { get; private set; }
    public EstadoEntrega Estado { get; private set; }
    public string NombreArchivo { get; private set; } = "";
    public long Tamano { get; private set; }
    public string RutaBlob { get; private set; } = "";

    private Entrega() { }

    public static Entrega Crear(Guid actividadId, Guid estudianteId, ArchivoGuardado archivo, DateTimeOffset ahora) => new()
    {
        Id = Guid.NewGuid(),
        ActividadId = actividadId,
        EstudianteId = estudianteId,
        Estado = EstadoEntrega.ENVIADA,
        FechaEnvio = ahora.UtcDateTime,
        NombreArchivo = archivo.NombreOriginal,
        Tamano = archivo.Tamano,
        RutaBlob = archivo.RutaBlob,
    };

    public bool PerteneceA(Guid estudianteId) => EstudianteId == estudianteId;

    /// <summary>Reemplaza el archivo y deja la entrega como ENVIADA. Devuelve la ruta del blob anterior.</summary>
    public string ReemplazarArchivo(ArchivoGuardado archivo, DateTimeOffset ahora)
    {
        var anterior = RutaBlob;
        NombreArchivo = archivo.NombreOriginal;
        Tamano = archivo.Tamano;
        RutaBlob = archivo.RutaBlob;
        FechaEnvio = ahora.UtcDateTime;
        Estado = EstadoEntrega.ENVIADA;
        return anterior;
    }

    public void Anular() => Estado = EstadoEntrega.ANULADA;
}

/// <summary>Metadatos de un archivo ya guardado en el almacén (el nombre original solo es metadato).</summary>
public sealed record ArchivoGuardado(string NombreOriginal, long Tamano, string RutaBlob);
