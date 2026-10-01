using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Unillanos.ServiceDefaults.Correlacion;

/// <summary>
/// Toma el X-Correlation-Id entrante (o crea uno), lo usa como TraceIdentifier,
/// lo devuelve en la respuesta y lo agrega al scope de logging.
/// </summary>
public sealed partial class CorrelacionMiddleware(RequestDelegate siguiente, ILogger<CorrelacionMiddleware> logger)
{
    public const string Encabezado = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext contexto)
    {
        var entrante = contexto.Request.Headers[Encabezado].ToString();
        var id = FormatoValido().IsMatch(entrante) ? entrante : Guid.NewGuid().ToString("N");

        contexto.TraceIdentifier = id;
        contexto.Response.OnStarting(() =>
        {
            contexto.Response.Headers[Encabezado] = id;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = id }))
        {
            await siguiente(contexto);
        }
    }

    // Evita inyección en logs: solo caracteres seguros y longitud acotada.
    [GeneratedRegex("^[A-Za-z0-9._-]{1,64}$")]
    private static partial Regex FormatoValido();
}
