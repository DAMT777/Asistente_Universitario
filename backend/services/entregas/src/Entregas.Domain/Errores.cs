namespace Entregas.Domain;

/// <summary>Error de regla de negocio. El middleware de la API lo convierte en la respuesta HTTP (sección 8.2).</summary>
public sealed class DominioException(string codigo, string mensaje) : Exception(mensaje)
{
    public string Codigo { get; } = codigo;
}

/// <summary>Códigos estables de error de la sección 8.2 de la guía técnica.</summary>
public static class CodigosError
{
    public const string ValidacionFallida = "VALIDACION_FALLIDA";
    public const string NoAutenticado = "NO_AUTENTICADO";
    public const string SinPermiso = "SIN_PERMISO";
    public const string NoEncontrado = "NO_ENCONTRADO";
    public const string ServicioNoDisponible = "SERVICIO_NO_DISPONIBLE";
}

public static class Roles
{
    public const string Profesor = "PROFESOR";
    public const string Estudiante = "ESTUDIANTE";
}
