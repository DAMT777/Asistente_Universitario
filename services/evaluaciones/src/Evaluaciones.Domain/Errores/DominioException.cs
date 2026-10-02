namespace Evaluaciones.Domain.Errores;

/// <summary>
/// Error de regla de negocio. El middleware de la API lo convierte en la respuesta HTTP
/// que corresponde al <see cref="Codigo"/> (sección 8.2).
/// </summary>
public sealed class DominioException(string codigo, string mensaje) : Exception(mensaje)
{
    public string Codigo { get; } = codigo;
}
