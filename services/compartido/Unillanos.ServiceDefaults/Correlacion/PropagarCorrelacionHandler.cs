using Microsoft.AspNetCore.Http;

namespace Unillanos.ServiceDefaults.Correlacion;

/// <summary>Propaga el X-Correlation-Id de la solicitud actual a las llamadas HTTP salientes.</summary>
public sealed class PropagarCorrelacionHandler(IHttpContextAccessor accesor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage solicitud, CancellationToken cancellationToken)
    {
        var id = accesor.HttpContext?.TraceIdentifier;
        if (!string.IsNullOrEmpty(id) && !solicitud.Headers.Contains(CorrelacionMiddleware.Encabezado))
        {
            solicitud.Headers.Add(CorrelacionMiddleware.Encabezado, id);
        }
        return base.SendAsync(solicitud, cancellationToken);
    }
}
