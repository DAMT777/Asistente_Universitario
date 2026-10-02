namespace Evaluaciones.Domain.Entidades;

public enum EstadoCalificacion
{
    Borrador,
    Publicada
}

public static class EstadoCalificacionExtensiones
{
    /// <summary>Texto del contrato y de la base de datos (BORRADOR o PUBLICADA).</summary>
    public static string ComoTexto(this EstadoCalificacion estado) =>
        estado == EstadoCalificacion.Borrador ? "BORRADOR" : "PUBLICADA";
}
