using Evaluaciones.Domain;
using Microsoft.AspNetCore.Diagnostics;

namespace Evaluaciones.Api.Errores;

/// <summary>Único lugar donde los errores de negocio se convierten en HTTP. Los controladores no capturan excepciones.</summary>
public sealed class ManejadorExcepciones(ILogger<ManejadorExcepciones> log) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        if (ex is ErrorDeNegocio negocio)
        {
            await Respuestas.EscribirAsync(ctx, Respuestas.EstadoHttp(negocio.Codigo), negocio.Codigo, negocio.Message, ct);
            return true;
        }

        log.LogError(ex, "Error no controlado en {Metodo} {Ruta}", ctx.Request.Method, ctx.Request.Path);
        await Respuestas.EscribirAsync(ctx, StatusCodes.Status500InternalServerError, "ERROR_INTERNO", "Ocurrió un error inesperado.", ct);
        return true;
    }
}
