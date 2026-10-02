using System.Diagnostics;
using Evaluaciones.Domain;

namespace Evaluaciones.Api.Errores;

/// <summary>Formato de error común a todos los servicios (guía 8.2).</summary>
public sealed record RespuestaError(int Status, string Codigo, string Mensaje, string TraceId);

public static class Respuestas
{
    public static string TraceId(HttpContext ctx) => Activity.Current?.Id ?? ctx.TraceIdentifier;

    public static int EstadoHttp(string codigo) => codigo switch
    {
        CodigosError.NoAutenticado => StatusCodes.Status401Unauthorized,
        CodigosError.SinPermiso => StatusCodes.Status403Forbidden,
        CodigosError.NoEncontrado => StatusCodes.Status404NotFound,
        CodigosError.ValidacionFallida => StatusCodes.Status400BadRequest,
        CodigosError.ConflictoConcurrencia => StatusCodes.Status409Conflict,
        CodigosError.NotaFueraDeRango => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status400BadRequest,
    };

    public static Task EscribirAsync(HttpContext ctx, int status, string codigo, string mensaje, CancellationToken ct = default)
    {
        ctx.Response.StatusCode = status;
        return ctx.Response.WriteAsJsonAsync(new RespuestaError(status, codigo, mensaje, TraceId(ctx)), ct);
    }
}
