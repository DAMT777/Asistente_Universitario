namespace Evaluaciones.Domain.Errores;

/// <summary>Códigos estables de error de la sección 8.2 de la guía técnica.</summary>
public static class CodigosError
{
    public const string ValidacionFallida = "VALIDACION_FALLIDA";
    public const string NoAutenticado = "NO_AUTENTICADO";
    public const string TokenExpirado = "TOKEN_EXPIRADO";
    public const string SinPermiso = "SIN_PERMISO";
    public const string NoEncontrado = "NO_ENCONTRADO";
    public const string ConflictoConcurrencia = "CONFLICTO_CONCURRENCIA";
    public const string CalificacionesEnBorrador = "CALIFICACIONES_EN_BORRADOR";
    public const string SinCalificaciones = "SIN_CALIFICACIONES";
}
