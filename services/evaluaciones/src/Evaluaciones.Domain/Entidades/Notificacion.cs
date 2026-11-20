namespace Evaluaciones.Domain.Entidades;

/// <summary>
/// Aviso para un usuario (hoy, el estudiante) sobre algo que pasó en uno de sus cursos.
/// Se crea en la misma unidad de trabajo que el cambio que la origina: si el cambio no se guarda, tampoco el aviso.
/// </summary>
public class Notificacion
{
    public const int LargoMaximoTitulo = 150;
    public const int LargoMaximoMensaje = 500;

    public Guid Id { get; set; }

    /// <summary>Destinatario (sub del token).</summary>
    public Guid UsuarioId { get; set; }

    public Guid CursoId { get; set; }

    /// <summary>Actividad relacionada, si la hay (para abrirla desde el aviso).</summary>
    public Guid? ActividadId { get; set; }

    public TipoNotificacion Tipo { get; set; }
    public string Titulo { get; set; } = "";
    public string Mensaje { get; set; } = "";

    /// <summary>En UTC.</summary>
    public DateTime FechaCreacion { get; set; }

    public bool Leida { get; set; }

    public static Notificacion Nueva(
        Guid usuarioId, Guid cursoId, Guid? actividadId, TipoNotificacion tipo, string titulo, string mensaje, DateTime ahoraUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            CursoId = cursoId,
            ActividadId = actividadId,
            Tipo = tipo,
            Titulo = Recortar(titulo, LargoMaximoTitulo),
            Mensaje = Recortar(mensaje, LargoMaximoMensaje),
            FechaCreacion = ahoraUtc,
            Leida = false
        };

    /// <returns>Verdadero si estaba sin leer.</returns>
    public bool MarcarLeida()
    {
        if (Leida) return false;
        Leida = true;
        return true;
    }

    private static string Recortar(string texto, int largo) =>
        texto.Length <= largo ? texto : texto[..(largo - 1)] + "…";
}

public enum TipoNotificacion
{
    /// <summary>El profesor creó una actividad, tarea o trabajo en el curso.</summary>
    NuevaActividad,

    /// <summary>El profesor cambió algo del curso: una actividad existente o los pesos de los cortes.</summary>
    CursoActualizado,

    /// <summary>Se publicó una nota: de una actividad (CU-08) o de un corte (CU-10, CU-11).</summary>
    NotaPublicada
}

public static class TipoNotificacionExtensiones
{
    /// <summary>Texto del contrato y de la base de datos.</summary>
    public static string ComoTexto(this TipoNotificacion tipo) => tipo switch
    {
        TipoNotificacion.NuevaActividad => "NUEVA_ACTIVIDAD",
        TipoNotificacion.CursoActualizado => "CURSO_ACTUALIZADO",
        _ => "NOTA_PUBLICADA"
    };

    public static TipoNotificacion DesdeTexto(string texto) => texto switch
    {
        "NUEVA_ACTIVIDAD" => TipoNotificacion.NuevaActividad,
        "CURSO_ACTUALIZADO" => TipoNotificacion.CursoActualizado,
        _ => TipoNotificacion.NotaPublicada
    };
}
