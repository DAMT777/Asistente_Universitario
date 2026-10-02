using Entregas.Application.Comun;
using Entregas.Application.Puertos;
using Entregas.Domain;

namespace Entregas.Application.CasosDeUso;

/// <summary>
/// Descarga un archivo de una entrega propia, con su nombre original. Una entrega o archivo
/// ajeno se trata como inexistente (RN-16). No depende de la fecha límite.
/// </summary>
public sealed class DescargarArchivo(IEntregaRepositorio repositorio, IAlmacenArchivos almacen)
{
    public async Task<ArchivoDescargable> EjecutarAsync(Guid entregaId, Guid archivoId, Guid estudianteId, CancellationToken ct)
    {
        var entrega = await BuscarPropia.EjecutarAsync(repositorio, entregaId, estudianteId, ct);
        var archivo = entrega.Archivo(archivoId) ?? throw new NoEncontradoException("El archivo");
        var contenido = await almacen.AbrirAsync(archivo.RutaBlob, ct) ?? throw new NoEncontradoException("El archivo");
        return new ArchivoDescargable(contenido, archivo.ContentType, archivo.NombreArchivo, archivo.Tamano);
    }
}
