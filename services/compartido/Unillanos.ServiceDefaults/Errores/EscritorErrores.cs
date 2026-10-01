using Microsoft.AspNetCore.Http;

namespace Unillanos.ServiceDefaults.Errores;

public static class EscritorErrores
{
    public const string ContentType = "application/problem+json";

    public static Task EscribirAsync(HttpContext contexto, string codigo, string mensaje, int? estado = null)
    {
        var status = estado ?? CodigosError.EstadoDe(codigo);
        contexto.Response.StatusCode = status;
        var cuerpo = new ErrorApi(status, codigo, mensaje, contexto.TraceIdentifier);
        return contexto.Response.WriteAsJsonAsync(cuerpo, options: null, contentType: ContentType, contexto.RequestAborted);
    }
}
