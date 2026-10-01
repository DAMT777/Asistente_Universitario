namespace Unillanos.ServiceDefaults.Errores;

/// <summary>Códigos de error estables del contrato y su estado HTTP.</summary>
public static class CodigosError
{
    public const string ValidacionFallida = "VALIDACION_FALLIDA";
    public const string NoAutenticado = "NO_AUTENTICADO";
    public const string TokenExpirado = "TOKEN_EXPIRADO";
    public const string SinPermiso = "SIN_PERMISO";
    public const string NoEncontrado = "NO_ENCONTRADO";
    public const string MetodoNoPermitido = "METODO_NO_PERMITIDO";
    public const string ArchivoDemasiadoGrande = "ARCHIVO_DEMASIADO_GRANDE";
    public const string TipoArchivoNoPermitido = "TIPO_ARCHIVO_NO_PERMITIDO";
    public const string TipoContenidoNoSoportado = "TIPO_CONTENIDO_NO_SOPORTADO";
    public const string FechaLimiteVencida = "FECHA_LIMITE_VENCIDA";
    public const string ActividadSinEntrega = "ACTIVIDAD_SIN_ENTREGA";
    public const string ErrorInterno = "ERROR_INTERNO";
    public const string ServicioNoDisponible = "SERVICIO_NO_DISPONIBLE";

    private static readonly Dictionary<string, int> Estados = new()
    {
        [ValidacionFallida] = 400,
        [NoAutenticado] = 401,
        [TokenExpirado] = 401,
        [SinPermiso] = 403,
        [NoEncontrado] = 404,
        [MetodoNoPermitido] = 405,
        [ArchivoDemasiadoGrande] = 413,
        [TipoArchivoNoPermitido] = 415,
        [TipoContenidoNoSoportado] = 415,
        [FechaLimiteVencida] = 422,
        [ActividadSinEntrega] = 422,
        [ErrorInterno] = 500,
        [ServicioNoDisponible] = 503,
    };

    public static int EstadoDe(string codigo) => Estados.GetValueOrDefault(codigo, 500);

    /// <summary>Código por defecto para respuestas de error generadas por el framework (sin cuerpo).</summary>
    public static string CodigoPorEstado(int estado) => estado switch
    {
        400 => ValidacionFallida,
        401 => NoAutenticado,
        403 => SinPermiso,
        404 => NoEncontrado,
        405 => MetodoNoPermitido,
        413 => ArchivoDemasiadoGrande,
        415 => TipoContenidoNoSoportado,
        503 => ServicioNoDisponible,
        _ => ErrorInterno,
    };
}
