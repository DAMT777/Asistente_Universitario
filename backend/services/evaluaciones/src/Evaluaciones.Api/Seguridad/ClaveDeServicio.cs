using System.Security.Cryptography;
using System.Text;
using Evaluaciones.Domain.Errores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Evaluaciones.Api.Seguridad;

/// <summary>
/// Protege los endpoints /internal/** (sección 12.4). Exige el encabezado X-Service-Key igual a la
/// configuración ServiceKey, comparado en tiempo constante. Sin ServiceKey configurada, nadie entra.
/// </summary>
public sealed class ClaveDeServicioAttribute : Attribute, IAsyncAuthorizationFilter
{
    public const string Encabezado = "X-Service-Key";

    public Task OnAuthorizationAsync(AuthorizationFilterContext contexto)
    {
        var esperada = contexto.HttpContext.RequestServices.GetRequiredService<IConfiguration>()["ServiceKey"];
        var recibida = contexto.HttpContext.Request.Headers[Encabezado].ToString();

        if (string.IsNullOrEmpty(esperada) || !IgualesEnTiempoConstante(esperada, recibida))
        {
            contexto.Result = new ObjectResult(new
            {
                status = StatusCodes.Status401Unauthorized,
                codigo = CodigosError.NoAutenticado,
                mensaje = "Falta la clave de servicio o no es válida.",
                traceId = contexto.HttpContext.TraceIdentifier
            })
            { StatusCode = StatusCodes.Status401Unauthorized };
        }

        return Task.CompletedTask;
    }

    // Se comparan los hash para que el tiempo no dependa ni del contenido ni de la longitud.
    private static bool IgualesEnTiempoConstante(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(a)),
            SHA256.HashData(Encoding.UTF8.GetBytes(b)));
}
