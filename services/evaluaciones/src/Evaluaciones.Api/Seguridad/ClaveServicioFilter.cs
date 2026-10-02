using System.Security.Cryptography;
using System.Text;
using Evaluaciones.Api.Middleware;
using Evaluaciones.Domain.Errores;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Evaluaciones.Api.Seguridad;

/// <summary>
/// Exige el encabezado X-Service-Key con la llave compartida (variable ServiceKey) en /internal/**.
/// Si la llave no está configurada rechaza todo: el endpoint nunca queda abierto por accidente.
/// El gateway además bloquea /internal/** desde fuera (sección 12.4).
/// </summary>
public sealed class ClaveServicioFilter(IConfiguration configuracion, ILogger<ClaveServicioFilter> log) : IAsyncAuthorizationFilter
{
    public const string Encabezado = "X-Service-Key";

    public async Task OnAuthorizationAsync(AuthorizationFilterContext contexto)
    {
        var esperada = configuracion["ServiceKey"];
        if (string.IsNullOrEmpty(esperada))
            log.LogWarning("Falta la variable ServiceKey: se rechazan las llamadas a /internal.");

        var recibida = contexto.HttpContext.Request.Headers[Encabezado].ToString();
        if (!string.IsNullOrEmpty(esperada)
            && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(recibida), Encoding.UTF8.GetBytes(esperada)))
            return;

        await RespuestaError.EscribirAsync(contexto.HttpContext, StatusCodes.Status401Unauthorized,
            CodigosError.NoAutenticado, "Falta o es inválida la llave de servicio.");
        contexto.Result = new Microsoft.AspNetCore.Mvc.EmptyResult();
    }
}
