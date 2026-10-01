namespace Unillanos.Evaluaciones.Domain;

/// <summary>Regla de negocio incumplida. El código es estable y forma parte del contrato.</summary>
public abstract class ExcepcionDominio(string codigo, string mensaje) : Exception(mensaje)
{
    public string Codigo { get; } = codigo;
}

public sealed class NoEncontradoException(string recurso)
    : ExcepcionDominio("NO_ENCONTRADO", $"{recurso} no existe.");

public sealed class SinPermisoException(string detalle)
    : ExcepcionDominio("SIN_PERMISO", detalle);
