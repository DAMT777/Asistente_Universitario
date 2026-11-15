namespace Asistente.Application;

/// <summary>Quién habla, tomado SOLO del token validado (sub y rol). El modelo no puede cambiarlo.</summary>
public sealed record UsuarioAsistente(Guid Id, string Rol)
{
    public const string Profesor = "PROFESOR";
    public const string Estudiante = "ESTUDIANTE";
    public bool EsProfesor => Rol == Profesor;
}

public sealed class AsistenteOpciones
{
    public const string Seccion = "Asistente";

    /// <summary>Vigencia de una acción pendiente antes de confirmarla.</summary>
    public int ExpiracionAccionMinutos { get; set; } = 5;
    /// <summary>Máximo de llamadas a herramientas en un mismo mensaje.</summary>
    public int MaxLlamadasHerramientas { get; set; } = 5;
    /// <summary>Mensajes por usuario por minuto (429 LIMITE_MENSAJES).</summary>
    public int MensajesPorMinuto { get; set; } = 10;
    /// <summary>Largo máximo del mensaje del usuario.</summary>
    public int MaxCaracteresMensaje { get; set; } = 1000;
}
