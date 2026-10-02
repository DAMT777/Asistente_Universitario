using Entregas.Application.Comun;
using Entregas.Application.Puertos;
using Entregas.Domain;

namespace Entregas.Application.CasosDeUso;

/// <summary>
/// Descarga un archivo de una entrega con su nombre original. Puede hacerlo el estudiante dueño
/// (una entrega ajena se trata como inexistente, RN-16) o el profesor dueño del curso (RN-15).
/// No depende de la fecha límite.
/// </summary>
public sealed class DescargarArchivo(IEntregaRepositorio repositorio, IAlmacenArchivos almacen, ConsultorActividades actividades)
{
    public async Task<ArchivoDescargable> EjecutarAsync(Guid entregaId, Guid archivoId, Guid usuarioId, bool esProfesor, CancellationToken ct)
    {
        Entrega entrega;
        if (esProfesor)
        {
            entrega = await repositorio.ObtenerAsync(entregaId, ct) ?? throw new NoEncontradoException("La entrega");
            await actividades.ObtenerParaProfesorAsync(entrega.ActividadId, usuarioId, ct);
        }
        else
        {
            entrega = await BuscarPropia.EjecutarAsync(repositorio, entregaId, usuarioId, ct);
        }

        var archivo = entrega.Archivo(archivoId) ?? throw new NoEncontradoException("El archivo");
        var contenido = await almacen.AbrirAsync(archivo.RutaBlob, ct) ?? throw new NoEncontradoException("El archivo");
        return new ArchivoDescargable(contenido, archivo.ContentType, archivo.NombreArchivo, archivo.Tamano);
    }
}
