using Entregas.Application.Abstracciones;
using Entregas.Application.Dtos;
using Entregas.Domain;

namespace Entregas.Application.CasosDeUso;

/// <summary>
/// Descarga del archivo de una entrega (CU-04 y CU-05). El contenedor es privado y las descargas pasan siempre
/// por aquí (sección 12.5): solo el estudiante autor o el profesor dueño del curso. Para los demás la entrega
/// no existe. Una entrega anulada ya no se descarga.
/// </summary>
public sealed class DescargarArchivo(
    IEntregaRepository entregas,
    IAlmacenArchivos almacen,
    IEvaluacionesCliente evaluaciones)
{
    public async Task<ArchivoDescargable> EjecutarAsync(Guid usuarioId, string rol, Guid entregaId, CancellationToken ct)
    {
        var entrega = await entregas.ObtenerAsync(entregaId, ct);
        if (entrega is null || entrega.Estado == EstadoEntrega.Anulada)
            throw NoEncontrada();

        if (rol == Roles.Estudiante)
        {
            if (entrega.EstudianteId != usuarioId) throw NoEncontrada(); // RN-16: no revela entregas ajenas
        }
        else if (rol == Roles.Profesor)
        {
            await PropiedadActividad.ExigirProfesorDuenoAsync(evaluaciones, entrega.ActividadId, usuarioId, ct);
        }
        else
        {
            throw new DominioException(CodigosError.SinPermiso, "Tu rol no permite descargar entregas.");
        }

        var contenido = await almacen.AbrirAsync(entrega.RutaBlob, ct)
            ?? throw new DominioException(CodigosError.NoEncontrado, "El archivo de la entrega no está disponible.");

        return new ArchivoDescargable(contenido, entrega.NombreArchivo, TipoDe(entrega.NombreArchivo));
    }

    private static DominioException NoEncontrada() => new(CodigosError.NoEncontrado, "La entrega no existe.");

    private static string TipoDe(string nombre) => Path.GetExtension(nombre).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        ".zip" => "application/zip",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".txt" => "text/plain; charset=utf-8",
        _ => "application/octet-stream"
    };
}
