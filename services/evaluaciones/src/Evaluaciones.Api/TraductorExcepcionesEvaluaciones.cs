using Evaluaciones.Domain;
using Unillanos.ServiceDefaults.Errores;

namespace Evaluaciones.Api;

public sealed class TraductorExcepcionesEvaluaciones : ITraductorExcepciones
{
    public ErrorDescrito? Traducir(Exception excepcion) => excepcion switch
    {
        ExcepcionDominio e => new(e.Codigo, e.Message),
        _ => null,
    };
}
