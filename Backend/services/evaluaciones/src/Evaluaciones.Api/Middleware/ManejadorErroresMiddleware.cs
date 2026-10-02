using Evaluaciones.Domain.Errores;

namespace Evaluaciones.Api.Middleware;

/// <summary>
/// Único lugar donde las excepciones de dominio se convierten en respuestas HTTP (sección 11.4).
/// Los controladores no capturan excepciones.
/// </summary>
public sealed class ManejadorErroresMiddleware(RequestDelegate siguiente, ILogger<ManejadorErroresMiddleware> log)
{
    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await siguiente(contexto);
        }
        catch (DominioException ex)
        {
            await RespuestaError.EscribirAsync(contexto, RespuestaError.StatusDe(ex.Codigo), ex.Codigo, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogError(ex, "Error no controlado en {Metodo} {Ruta}", contexto.Request.Method, contexto.Request.Path);
            await RespuestaError.EscribirAsync(
                contexto, StatusCodes.Status500InternalServerError, "ERROR_INTERNO", "Ocurrió un error inesperado.");
        }
    }
}
