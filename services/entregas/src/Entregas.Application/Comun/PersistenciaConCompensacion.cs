using Microsoft.Extensions.Logging;
using Entregas.Application.Puertos;

namespace Entregas.Application.Comun;

/// <summary>
/// Guarda los cambios de la entrega manteniendo coherentes BD y blobs:
/// si la BD falla se borran los blobs nuevos; si la BD confirma se borran los blobs reemplazados.
/// </summary>
public sealed class PersistenciaConCompensacion(
    IEntregaRepositorio repositorio,
    IAlmacenArchivos almacen,
    ILogger<PersistenciaConCompensacion> logger)
{
    public async Task GuardarAsync(IReadOnlyCollection<string> rutasNuevas, IReadOnlyCollection<string> rutasReemplazadas, CancellationToken ct)
    {
        try
        {
            await repositorio.GuardarCambiosAsync(ct);
        }
        catch
        {
            await EliminarSinFallarAsync(rutasNuevas);
            throw;
        }

        await EliminarSinFallarAsync(rutasReemplazadas.Except(rutasNuevas).ToList());
    }

    private async Task EliminarSinFallarAsync(IReadOnlyCollection<string> rutas)
    {
        foreach (var ruta in rutas)
        {
            try
            {
                await almacen.EliminarAsync(ruta, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "No se pudo eliminar el blob huérfano {RutaBlob}", ruta);
            }
        }
    }
}
