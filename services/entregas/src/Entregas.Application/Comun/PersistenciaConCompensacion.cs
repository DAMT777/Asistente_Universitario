using Microsoft.Extensions.Logging;
using Entregas.Application.Puertos;

namespace Entregas.Application.Comun;

/// <summary>
/// Guarda los cambios de la entrega manteniendo coherentes BD y blob:
/// si la BD falla se borra el blob nuevo; si la BD confirma se borra el blob reemplazado.
/// </summary>
public sealed class PersistenciaConCompensacion(
    IEntregaRepositorio repositorio,
    IAlmacenArchivos almacen,
    ILogger<PersistenciaConCompensacion> logger)
{
    public async Task GuardarAsync(string rutaNueva, string? rutaReemplazada, CancellationToken ct)
    {
        try
        {
            await repositorio.GuardarCambiosAsync(ct);
        }
        catch
        {
            await EliminarSinFallarAsync(rutaNueva);
            throw;
        }

        if (rutaReemplazada is not null && rutaReemplazada != rutaNueva)
        {
            await EliminarSinFallarAsync(rutaReemplazada);
        }
    }

    private async Task EliminarSinFallarAsync(string ruta)
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
