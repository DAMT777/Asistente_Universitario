namespace Asistente.Domain;

/// <summary>Error con un código estable del contrato y, si no es de la tabla común, su estado HTTP.</summary>
public class ExcepcionAsistente(string codigo, string mensaje, int? status = null) : Exception(mensaje)
{
    public string Codigo { get; } = codigo;
    public int? Status { get; } = status ?? CodigosAsistente.EstadoDe(codigo);
}

public static class CodigosAsistente
{
    public const string ValidacionFallida = "VALIDACION_FALLIDA";
    public const string SinPermiso = "SIN_PERMISO";
    public const string AccionNoEncontrada = "ACCION_NO_ENCONTRADA";
    public const string AccionExpirada = "ACCION_EXPIRADA";
    public const string AccionYaUsada = "ACCION_YA_USADA";
    public const string LimiteMensajes = "LIMITE_MENSAJES";
    public const string AsistenteNoDisponible = "ASISTENTE_NO_DISPONIBLE";
    public const string ServicioNoDisponible = "SERVICIO_NO_DISPONIBLE";

    public static int? EstadoDe(string codigo) => codigo switch
    {
        AccionNoEncontrada => 404,
        AccionExpirada => 410,
        AccionYaUsada => 409,
        LimiteMensajes => 429,
        AsistenteNoDisponible => 503,
        _ => null,
    };
}
