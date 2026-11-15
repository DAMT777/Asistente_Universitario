using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace Asistente.Infrastructure.Gateway;

/// <summary>
/// Reenvía el JWT del usuario de la solicitud actual. El asistente no tiene token ni llave propios:
/// Evaluaciones y Entregas aplican sus permisos como si el usuario llamara directamente.
/// </summary>
public sealed class ReenviarJwtHandler(IHttpContextAccessor accesor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage solicitud, CancellationToken ct)
    {
        var autorizacion = accesor.HttpContext?.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(autorizacion) || !AuthenticationHeaderValue.TryParse(autorizacion, out var valor) || valor.Scheme != "Bearer")
            throw new InvalidOperationException("No hay un JWT de usuario para reenviar.");
        solicitud.Headers.Authorization = valor;
        return base.SendAsync(solicitud, ct);
    }
}
