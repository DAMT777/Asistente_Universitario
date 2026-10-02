using System.Diagnostics;
using Entregas.Domain;

namespace Entregas.Api.Middleware;

/// <summary>Formato de error común a todos los servicios (sección 8.2).</summary>
public static class RespuestaError
{
    public static int StatusDe(string codigo) => codigo switch
    {
        CodigosError.ValidacionFallida => StatusCodes.Status400BadRequest,
        CodigosError.NoAutenticado => StatusCodes.Status401Unauthorized,
        CodigosError.SinPermiso => StatusCodes.Status403Forbidden,
        CodigosError.NoEncontrado => StatusCodes.Status404NotFound,
        CodigosError.ServicioNoDisponible => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status422UnprocessableEntity
    };

    public static Task EscribirAsync(HttpContext contexto, int status, string codigo, string mensaje)
    {
        contexto.Response.StatusCode = status;
        return contexto.Response.WriteAsJsonAsync(new
        {
            status,
            codigo,
            mensaje,
            traceId = Activity.Current?.Id ?? contexto.TraceIdentifier
        });
    }
}

/// <summary>Único lugar donde las excepciones de dominio se convierten en respuestas HTTP.</summary>
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
            if (ex.Codigo == CodigosError.ServicioNoDisponible)
                log.LogWarning("Evaluaciones no respondió en {Ruta}", contexto.Request.Path);
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
