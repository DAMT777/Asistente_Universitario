using Entregas.Domain;

namespace Entregas.Application.Comun;

/// <summary>Archivo recibido en la solicitud. El flujo debe permitir volver al inicio (seekable).</summary>
public sealed record ArchivoEntrante(string NombreOriginal, long Tamano, Stream Contenido);

public sealed record ArchivoRespuesta(Guid Id, string NombreArchivo, long Tamano);

public sealed record EntregaRespuesta(
    Guid Id,
    Guid ActividadId,
    Guid EstudianteId,
    DateTime FechaEnvio,
    string Estado,
    IReadOnlyList<ArchivoRespuesta> Archivos,
    long TamanoTotal)
{
    public static EntregaRespuesta Desde(Entrega e) =>
        new(e.Id, e.ActividadId, e.EstudianteId, DateTime.SpecifyKind(e.FechaEnvio, DateTimeKind.Utc), e.Estado.ToString(),
            e.Archivos.Select(a => new ArchivoRespuesta(a.Id, a.NombreArchivo, a.Tamano)).ToList(), e.TamanoTotal);
}

/// <summary>Contenido de un archivo para descargar con su nombre original.</summary>
public sealed record ArchivoDescargable(Stream Contenido, string ContentType, string NombreArchivo, long Tamano);
