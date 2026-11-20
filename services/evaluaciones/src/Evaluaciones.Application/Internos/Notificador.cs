using System.Globalization;
using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Domain.Entidades;

namespace Evaluaciones.Application.Internos;

/// <summary>
/// Redacta y agrega las notificaciones de los estudiantes. No guarda: el caso de uso que origina el aviso
/// llama a la unidad de trabajo una sola vez, así el cambio y sus avisos se guardan juntos o no se guarda nada.
/// </summary>
public sealed class Notificador(INotificacionRepository notificaciones, IInscripcionRepository inscripciones, TimeProvider reloj)
{
    /// <summary>Colombia no tiene horario de verano: UTC-5 todo el año.</summary>
    private static readonly TimeSpan HoraColombia = TimeSpan.FromHours(-5);

    private static readonly CultureInfo Formato = CultureInfo.InvariantCulture;

    /// <summary>CU-03. El profesor creó una actividad: avisa a todos los inscritos.</summary>
    public Task NuevaActividadAsync(Curso curso, Actividad actividad, CancellationToken ct)
    {
        var detalle = $"Corte {actividad.Corte}, {actividad.Peso.ToString("0.##", Formato)}% del corte";
        var plazo = actividad.FechaLimite is null
            ? ""
            : actividad.RequiereEntrega
                ? $". Entrega hasta el {Fecha(actividad.FechaLimite.Value)}"
                : $". Fecha: {Fecha(actividad.FechaLimite.Value)}";

        return ATodosAsync(
            curso, actividad.Id, TipoNotificacion.NuevaActividad,
            $"Nueva actividad: {actividad.Titulo}",
            $"{curso.Nombre}: {Profesor(curso)} agregó «{actividad.Titulo}». {detalle}{plazo}.",
            ct);
    }

    /// <summary>CU-03. El profesor editó una actividad. Solo avisa si cambió algo que el estudiante ve.</summary>
    public Task ActividadActualizadaAsync(Curso curso, DatosActividad antes, Actividad despues, CancellationToken ct)
    {
        var cambios = new List<string>();
        if (antes.Titulo != despues.Titulo) cambios.Add($"ahora se llama «{despues.Titulo}»");
        if (antes.FechaLimite != despues.FechaLimite)
            cambios.Add(despues.FechaLimite is null ? "ya no tiene fecha límite" : $"nueva fecha límite: {Fecha(despues.FechaLimite.Value)}");
        if (antes.Corte != despues.Corte) cambios.Add($"pasó al corte {despues.Corte}");
        if (antes.Peso != despues.Peso) cambios.Add($"ahora pesa {despues.Peso.ToString("0.##", Formato)}% del corte");
        if (antes.RequiereEntrega != despues.RequiereEntrega)
            cambios.Add(despues.RequiereEntrega ? "ahora requiere entrega" : "ya no requiere entrega");

        if (cambios.Count == 0) return Task.CompletedTask;

        return ATodosAsync(
            curso, despues.Id, TipoNotificacion.CursoActualizado,
            $"Cambios en {despues.Titulo}",
            $"{curso.Nombre}: «{antes.Titulo}» {string.Join(", ", cambios)}.",
            ct);
    }

    /// <summary>CU-02. Cambiaron los pesos de los cortes.</summary>
    public Task PesosActualizadosAsync(Curso curso, CancellationToken ct)
    {
        string P(decimal p) => p.ToString("0.##", Formato) + "%";
        return ATodosAsync(
            curso, null, TipoNotificacion.CursoActualizado,
            $"Nuevos pesos en {curso.Nombre}",
            $"{curso.Nombre}: los cortes ahora pesan {P(curso.PesoCorte1)}, {P(curso.PesoCorte2)} y {P(curso.PesoCorte3)}.",
            ct);
    }

    /// <summary>CU-08. La calificación de la actividad quedó visible para el estudiante.</summary>
    public void NotaDeActividadPublicada(Curso curso, Actividad actividad, Calificacion calificacion) =>
        Agregar(
            calificacion.EstudianteId, curso.Id, actividad.Id, TipoNotificacion.NotaPublicada,
            $"Nota publicada: {actividad.Titulo}",
            $"{curso.Nombre}: tu nota en «{actividad.Titulo}» es {Nota(calificacion.Valor)}." +
            (string.IsNullOrWhiteSpace(calificacion.Retroalimentacion) ? "" : " Tiene retroalimentación del profesor."));

    /// <summary>CU-10 y CU-11. Se publicó o corrigió la nota de un corte del estudiante.</summary>
    public void NotaDeCortePublicada(Curso curso, int corte, Guid estudianteId, decimal nota, bool corregida) =>
        Agregar(
            estudianteId, curso.Id, null, TipoNotificacion.NotaPublicada,
            corregida ? $"Nota del corte {corte} corregida" : $"Nota del corte {corte} publicada",
            $"{curso.Nombre}: tu nota del corte {corte} es {Nota(nota)}.");

    private async Task ATodosAsync(
        Curso curso, Guid? actividadId, TipoNotificacion tipo, string titulo, string mensaje, CancellationToken ct)
    {
        foreach (var inscrito in await inscripciones.ListarInscritosAsync(curso.Id, ct))
            Agregar(inscrito.EstudianteId, curso.Id, actividadId, tipo, titulo, mensaje);
    }

    private void Agregar(Guid usuarioId, Guid cursoId, Guid? actividadId, TipoNotificacion tipo, string titulo, string mensaje) =>
        notificaciones.AgregarNotificacion(
            Notificacion.Nueva(usuarioId, cursoId, actividadId, tipo, titulo, mensaje, reloj.GetUtcNow().UtcDateTime));

    private static string Profesor(Curso curso) =>
        string.IsNullOrWhiteSpace(curso.ProfesorNombre) ? "El profesor" : curso.ProfesorNombre;

    private static string Nota(decimal valor) => valor.ToString("0.0", Formato);

    private static string Fecha(DateTime utc) =>
        new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToOffset(HoraColombia).ToString("dd/MM/yyyy HH:mm", Formato);
}
