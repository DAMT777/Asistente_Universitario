using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace Unillanos.ServiceDefaults.Errores;

/// <summary>
/// Único punto donde las excepciones se convierten en respuestas con el formato estándar.
/// Cada servicio aporta su traductor de excepciones de dominio/aplicación.
/// </summary>
public sealed class ManejadorExcepciones(
    IEnumerable<ITraductorExcepciones> traductores,
    ILogger<ManejadorExcepciones> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excepcion, CancellationToken cancellationToken)
    {
        var error = traductores.Select(t => t.Traducir(excepcion)).FirstOrDefault(e => e is not null)
                    ?? TraducirComunes(excepcion);

        if (error is null)
        {
            logger.LogError(excepcion, "Error no controlado en {Metodo} {Ruta}", contexto.Request.Method, contexto.Request.Path);
            error = new ErrorDescrito(CodigosError.ErrorInterno, "Ocurrió un error inesperado.");
        }
        else
        {
            logger.LogInformation("Solicitud rechazada con {Codigo}: {Mensaje}", error.Codigo, error.Mensaje);
        }

        await EscritorErrores.EscribirAsync(contexto, error.Codigo, error.Mensaje);
        return true;
    }

    private static ErrorDescrito? TraducirComunes(Exception excepcion)
    {
        // MVC envuelve los errores al leer el formulario (p. ej. el 413 de Kestrel) en otra excepción.
        for (var e = excepcion; e is not null; e = e.InnerException)
        {
            if (e is BadHttpRequestException { StatusCode: StatusCodes.Status413PayloadTooLarge })
                return new(CodigosError.ArchivoDemasiadoGrande, "El archivo supera el tamaño máximo permitido.");
        }

        return excepcion switch
        {
            BadHttpRequestException or InvalidDataException or ValueProviderException =>
                new(CodigosError.ValidacionFallida, "La solicitud no tiene un formato válido."),
            UnauthorizedAccessException =>
                new(CodigosError.NoAutenticado, "El token no identifica a un usuario válido."),
            _ => null,
        };
    }
}

public interface ITraductorExcepciones
{
    ErrorDescrito? Traducir(Exception excepcion);
}
