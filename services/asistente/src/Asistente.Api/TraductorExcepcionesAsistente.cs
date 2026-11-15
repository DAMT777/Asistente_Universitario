using Asistente.Application.Puertos;
using Asistente.Domain;
using Unillanos.ServiceDefaults.Errores;

namespace Asistente.Api;

/// <summary>
/// Errores propios y errores de Evaluaciones durante la confirmación (409, 403, 422...): se devuelven
/// con su estado y código originales. El asistente nunca convierte un fallo de escritura en éxito.
/// </summary>
public sealed class TraductorExcepcionesAsistente : ITraductorExcepciones
{
    public ErrorDescrito? Traducir(Exception excepcion) => excepcion switch
    {
        ExcepcionAsistente e => new(e.Codigo, e.Message, e.Status),
        ErrorRemotoException e => new(e.Codigo, e.Message, e.Status),
        _ => null,
    };
}

public static class LimiteMensajes
{
    public const string Politica = "mensajes-por-usuario";
}
