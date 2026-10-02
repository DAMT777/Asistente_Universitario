namespace Evaluaciones.Domain;

/// <summary>Códigos estables de error (guía técnica, sección 8.2).</summary>
public static class CodigosError
{
    public const string NoAutenticado = "NO_AUTENTICADO";
    public const string SinPermiso = "SIN_PERMISO";
    public const string NoEncontrado = "NO_ENCONTRADO";
    public const string ValidacionFallida = "VALIDACION_FALLIDA";
    public const string ConflictoConcurrencia = "CONFLICTO_CONCURRENCIA";
    public const string NotaFueraDeRango = "NOTA_FUERA_DE_RANGO";
}

/// <summary>Error de negocio con código estable. Un único middleware lo convierte a HTTP.</summary>
public abstract class ErrorDeNegocio(string codigo, string mensaje) : Exception(mensaje)
{
    public string Codigo { get; } = codigo;
}

public sealed class NoAutenticadoException(string mensaje = "Falta el token o no es válido.")
    : ErrorDeNegocio(CodigosError.NoAutenticado, mensaje);

public sealed class SinPermisoException(string mensaje = "No tienes permiso para esta acción.")
    : ErrorDeNegocio(CodigosError.SinPermiso, mensaje);

public sealed class NoEncontradoException(string mensaje) : ErrorDeNegocio(CodigosError.NoEncontrado, mensaje);

public sealed class ValidacionFallidaException(string mensaje) : ErrorDeNegocio(CodigosError.ValidacionFallida, mensaje);

public sealed class ConflictoConcurrenciaException(string mensaje = "Otro cambio modificó esta calificación. Vuelve a consultarla e inténtalo de nuevo.")
    : ErrorDeNegocio(CodigosError.ConflictoConcurrencia, mensaje);

public sealed class NotaFueraDeRangoException(decimal valor)
    : ErrorDeNegocio(CodigosError.NotaFueraDeRango, $"La nota {valor} está fuera de la escala de 0.0 a 5.0.");
