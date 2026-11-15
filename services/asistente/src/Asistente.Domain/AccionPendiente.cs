namespace Asistente.Domain;

public enum EstadoAccion
{
    Pendiente,
    EnProceso,
    Confirmada,
    Cancelada,
    Fallida,
}

/// <summary>
/// Calificación que el modelo propuso y que el profesor todavía no confirma.
/// Es la única forma de que una escritura llegue a Evaluaciones: el modelo nunca escribe.
/// </summary>
public sealed class AccionPendiente
{
    public const string TipoCalificar = "CALIFICAR";

    private AccionPendiente() { }

    public Guid Id { get; private set; }
    public string Tipo { get; private set; } = TipoCalificar;
    /// <summary>Claim sub de quien la propuso. Solo esa persona puede confirmarla o cancelarla.</summary>
    public Guid CreadaPor { get; private set; }
    public Guid CursoId { get; private set; }
    public Guid ActividadId { get; private set; }
    public string ActividadTitulo { get; private set; } = "";
    public Guid EstudianteId { get; private set; }
    public string EstudianteNombre { get; private set; } = "";
    public decimal Valor { get; private set; }
    public string? Retroalimentacion { get; private set; }
    public decimal? ValorAnterior { get; private set; }
    /// <summary>Versión de la calificación leída al proponer (If-Match). Null si no existía.</summary>
    public string? VersionEsperada { get; private set; }
    public DateTimeOffset CreadaEn { get; private set; }
    public DateTimeOffset ExpiraEn { get; private set; }
    public EstadoAccion Estado { get; private set; }
    public DateTimeOffset? ResueltaEn { get; private set; }

    public static AccionPendiente Proponer(
        Guid creadaPor, Guid cursoId, Guid actividadId, string actividadTitulo, Guid estudianteId, string estudianteNombre,
        decimal valor, string? retroalimentacion, decimal? valorAnterior, string? versionEsperada,
        DateTimeOffset ahora, TimeSpan vigencia)
    {
        if (valor is < 0m or > 5m || decimal.Round(valor, 1) != valor)
            throw new ExcepcionAsistente(CodigosAsistente.ValidacionFallida, "La nota debe estar entre 0.0 y 5.0 con un decimal como máximo.");
        if (retroalimentacion is { Length: > 2000 })
            throw new ExcepcionAsistente(CodigosAsistente.ValidacionFallida, "La retroalimentación admite hasta 2000 caracteres.");

        return new AccionPendiente
        {
            Id = Guid.NewGuid(),
            CreadaPor = creadaPor,
            CursoId = cursoId,
            ActividadId = actividadId,
            ActividadTitulo = actividadTitulo,
            EstudianteId = estudianteId,
            EstudianteNombre = estudianteNombre,
            Valor = valor,
            Retroalimentacion = string.IsNullOrWhiteSpace(retroalimentacion) ? null : retroalimentacion,
            ValorAnterior = valorAnterior,
            VersionEsperada = versionEsperada,
            CreadaEn = ahora,
            ExpiraEn = ahora + vigencia,
            Estado = EstadoAccion.Pendiente,
        };
    }

    public string Resumen =>
        $"Poner {Valor.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)} a {EstudianteNombre} en {ActividadTitulo}"
        + (ValorAnterior is { } anterior ? $" (antes {anterior.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)})" : "")
        + ". Queda en borrador.";

    /// <summary>Verifica que la acción se pueda usar ahora. No cambia el estado: eso se hace de forma atómica en la base.</summary>
    public void VerificarUsable(DateTimeOffset ahora)
    {
        if (Estado != EstadoAccion.Pendiente)
            throw new ExcepcionAsistente(CodigosAsistente.AccionYaUsada, "La acción ya fue confirmada o cancelada.");
        if (ahora >= ExpiraEn)
            throw new ExcepcionAsistente(CodigosAsistente.AccionExpirada, "La acción expiró. Pide de nuevo la calificación.");
    }

    public bool Expirada(DateTimeOffset ahora) => ahora >= ExpiraEn;

    public void Resolver(EstadoAccion estado, DateTimeOffset ahora)
    {
        Estado = estado;
        ResueltaEn = ahora;
    }
}
