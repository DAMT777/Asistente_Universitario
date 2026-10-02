using Entregas.Domain;

namespace Entregas.Application.Dtos;

/// <summary>Forma de la sección 8.7 de la guía. Nunca expone la ruta interna del archivo (sección 12.5).</summary>
public sealed record EntregaDto(
    Guid Id,
    Guid ActividadId,
    Guid EstudianteId,
    DateTime FechaEnvio,
    string Estado,
    string NombreArchivo,
    long Tamano)
{
    public static EntregaDto De(Entrega e) =>
        new(e.Id, e.ActividadId, e.EstudianteId, e.FechaEnvio, e.Estado.ComoTexto(), e.NombreArchivo, e.Tamano);
}

/// <summary>Archivo listo para enviarse al cliente.</summary>
public sealed record ArchivoDescargable(Stream Contenido, string NombreArchivo, string TipoContenido);
