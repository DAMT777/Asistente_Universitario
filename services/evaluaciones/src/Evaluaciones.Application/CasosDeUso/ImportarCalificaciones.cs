using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Application.Internos;
using Evaluaciones.Domain.Entidades;
using Evaluaciones.Domain.Errores;

namespace Evaluaciones.Application.CasosDeUso;

public sealed record NotaImportada(Guid EstudianteId, decimal? Nota, bool Existia, string? Version);

public sealed partial class CalificarActividad
{
    public async Task<int> ImportarAsync(Guid profesorId, Guid actividadId, IReadOnlyList<NotaImportada> filas, CancellationToken ct)
    {
        if (filas.Count is < 1 or > 250 || filas.Any(f => f is null) || filas.Select(f => f.EstudianteId).Distinct().Count() != filas.Count)
            throw new DominioException(CodigosError.ValidacionFallida, "El archivo debe incluir entre 1 y 250 estudiantes distintos.");

        var actividad = await actividades.ObtenerActividadAsync(actividadId, ct)
            ?? throw new DominioException(CodigosError.NoEncontrado, "La actividad no existe.");
        await AccesoCurso.ObtenerDelProfesorAsync(cursos, actividad.CursoId, profesorId, ct);

        var modificadas = 0;
        foreach (var fila in filas)
        {
            var valor = Calificacion.ValidarValor(fila.Nota);
            if (!await inscripciones.EstaInscritoAsync(actividad.CursoId, fila.EstudianteId, ct))
                throw new DominioException(CodigosError.NoEncontrado, "Un estudiante ya no está inscrito en el curso.");

            var actual = await calificaciones.ObtenerCalificacionAsync(actividadId, fila.EstudianteId, ct);
            var version = actual?.Version;
            if (fila.Existia != (actual is not null) ||
                fila.Version != (version is null ? null : Convert.ToBase64String(version)))
                throw new DominioException(CodigosError.ConflictoConcurrencia, "Las calificaciones cambiaron. Genera una nueva vista previa.");

            if (actual is null)
            {
                calificaciones.AgregarCalificacion(Calificacion.Nueva(actividadId, fila.EstudianteId, valor, "", null));
                modificadas++;
                continue;
            }

            if (actual.Modificar(valor, actual.Retroalimentacion ?? "", actual.EntregaId))
                modificadas++;
        }

        await unidadDeTrabajo.GuardarCambiosAsync(ct);
        return modificadas;
    }
}
