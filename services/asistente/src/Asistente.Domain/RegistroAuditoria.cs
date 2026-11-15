namespace Asistente.Domain;

/// <summary>
/// Una fila por cada acción relevante del asistente. Nunca guarda tokens, llaves ni el texto de los prompts:
/// solo identificadores y los datos de la calificación antes y después.
/// </summary>
public sealed class RegistroAuditoria
{
    public const string OrigenAsistente = "asistente";

    private RegistroAuditoria() { }

    public long Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string Rol { get; private set; } = "";
    public string Accion { get; private set; } = "";
    public Guid? AccionPendienteId { get; private set; }
    public string? DatosAntes { get; private set; }
    public string? DatosDespues { get; private set; }
    public string Resultado { get; private set; } = "";
    public DateTimeOffset FechaUtc { get; private set; }
    public string Origen { get; private set; } = OrigenAsistente;

    public static RegistroAuditoria Crear(
        Guid usuarioId, string rol, string accion, Guid? accionPendienteId,
        string? datosAntes, string? datosDespues, string resultado, DateTimeOffset ahora) => new()
    {
        UsuarioId = usuarioId,
        Rol = rol,
        Accion = accion,
        AccionPendienteId = accionPendienteId,
        DatosAntes = datosAntes,
        DatosDespues = datosDespues,
        Resultado = resultado,
        FechaUtc = ahora.ToUniversalTime(),
        Origen = OrigenAsistente,
    };
}

public static class AccionesAuditoria
{
    public const string Proponer = "PROPONER_CALIFICACION";
    public const string Confirmar = "CONFIRMAR_CALIFICACION";
    public const string Cancelar = "CANCELAR_CALIFICACION";
    public const string HerramientaRechazada = "HERRAMIENTA_RECHAZADA";
}
