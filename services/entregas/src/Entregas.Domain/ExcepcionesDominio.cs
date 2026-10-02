namespace Entregas.Domain;

/// <summary>Regla de negocio incumplida. El código es estable y forma parte del contrato.</summary>
public abstract class ExcepcionDominio(string codigo, string mensaje) : Exception(mensaje)
{
    public string Codigo { get; } = codigo;
}

public sealed class FechaLimiteVencidaException()
    : ExcepcionDominio("FECHA_LIMITE_VENCIDA", "La fecha límite de la actividad ya venció.");

public sealed class ActividadSinEntregaException()
    : ExcepcionDominio("ACTIVIDAD_SIN_ENTREGA", "La actividad no recibe entregas.");

public sealed class ArchivoDemasiadoGrandeException(string mensaje)
    : ExcepcionDominio("ARCHIVO_DEMASIADO_GRANDE", mensaje);

public sealed class TipoArchivoNoPermitidoException(string detalle)
    : ExcepcionDominio("TIPO_ARCHIVO_NO_PERMITIDO", detalle);

public sealed class ArchivoInvalidoException(string detalle)
    : ExcepcionDominio("VALIDACION_FALLIDA", detalle);

public sealed class NoEncontradoException(string recurso)
    : ExcepcionDominio("NO_ENCONTRADO", $"{recurso} no existe.");

public sealed class SinPermisoException(string detalle)
    : ExcepcionDominio("SIN_PERMISO", detalle);
