using System.Diagnostics;
using Evaluaciones.Domain.Errores;

namespace Evaluaciones.Api.Middleware;

/// <summary>Formato de error común a todos los servicios (sección 8.2).</summary>
public static class RespuestaError
{
    public static int StatusDe(string codigo) => codigo switch
    {
        CodigosError.ValidacionFallida => StatusCodes.Status400BadRequest,
        CodigosError.NoAutenticado or CodigosError.TokenExpirado => StatusCodes.Status401Unauthorized,
        CodigosError.SinPermiso => StatusCodes.Status403Forbidden,
        CodigosError.NoEncontrado => StatusCodes.Status404NotFound,
        CodigosError.ConflictoConcurrencia => StatusCodes.Status409Conflict,
        "SERVICIO_NO_DISPONIBLE" => StatusCodes.Status503ServiceUnavailable,
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
