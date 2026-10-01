using Unillanos.Entregas.Domain;

namespace Unillanos.Entregas.Application.Comun;

/// <summary>Archivo recibido en la solicitud. El flujo debe permitir volver al inicio (seekable).</summary>
public sealed record ArchivoEntrante(string NombreOriginal, long Tamano, Stream Contenido);

public sealed record EntregaRespuesta(
    Guid Id,
    Guid ActividadId,
    Guid EstudianteId,
    DateTime FechaEnvio,
    string Estado,
    string NombreArchivo,
    long Tamano)
{
    public static EntregaRespuesta Desde(Entrega e) =>
        new(e.Id, e.ActividadId, e.EstudianteId, DateTime.SpecifyKind(e.FechaEnvio, DateTimeKind.Utc),
            e.Estado.ToString(), e.NombreArchivo, e.Tamano);
}
