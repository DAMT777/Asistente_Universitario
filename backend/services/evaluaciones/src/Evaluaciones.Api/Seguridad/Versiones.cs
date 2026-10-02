using Evaluaciones.Domain.Errores;

namespace Evaluaciones.Api.Seguridad;

/// <summary>Control de concurrencia optimista con If-Match y ETag (sección 9.6).</summary>
public static class Versiones
{
    /// <summary>Versión que el cliente envía en If-Match, o null si no la envió.</summary>
    public static byte[]? LeerVersionEsperada(this HttpRequest request)
    {
        var valor = request.Headers.IfMatch.ToString().Trim().Trim('"');
        if (valor.Length == 0) return null;

        try
        {
            return Convert.FromBase64String(valor);
        }
        catch (FormatException)
        {
            throw new DominioException(CodigosError.ValidacionFallida, "El encabezado If-Match no tiene un formato válido.");
        }
    }

    public static void EscribirETag(this HttpResponse response, string? version)
    {
        if (version is not null)
            response.Headers.ETag = $"\"{version}\"";
    }
}
