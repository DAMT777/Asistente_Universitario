using FluentValidation;
using Unillanos.Entregas.Application.Puertos;
using Unillanos.Entregas.Domain;
using Unillanos.ServiceDefaults.Errores;

namespace Unillanos.Entregas.Api;

/// <summary>Excepciones de dominio y aplicación de Entregas a códigos del contrato.</summary>
public sealed class TraductorExcepcionesEntregas : ITraductorExcepciones
{
    public ErrorDescrito? Traducir(Exception excepcion) => excepcion switch
    {
        ExcepcionDominio e => new(e.Codigo, e.Message),
        ServicioNoDisponibleException e => new(ServicioNoDisponibleException.Codigo, e.Message),
        ValidationException e => new(CodigosError.ValidacionFallida,
            string.Join(" ", e.Errors.Select(f => f.ErrorMessage).Distinct())),
        _ => null,
    };
}
