using Evaluaciones.Application.Dtos;
using Evaluaciones.Domain.Entidades;
using Evaluaciones.Domain.Ponderado;

namespace Evaluaciones.Application.Internos;

/// <summary>Arma el ponderado de un estudiante a partir de datos ya cargados. No consulta nada.</summary>
internal static class ConstructorPonderado
{
    public static PonderadoEstudianteDto Construir(
        Curso curso,
        CursoEstudiante inscrito,
        IReadOnlyList<Actividad> actividades,
        IEnumerable<Calificacion> calificaciones,
        IEnumerable<PublicacionCorte> publicaciones,
        bool conDetalle)
    {
        var calificacionPorActividad = calificaciones.ToDictionary(c => c.ActividadId);
        var publicacionPorCorte = publicaciones.ToDictionary(p => p.Corte);

        var cortes = new List<PonderadoCorteDto>();
        var notaPorCorte = new Dictionary<int, decimal>();
        var incluyeBorradores = false;

        for (var corte = 1; corte <= MotorPonderado.CantidadCortes; corte++)
        {
            var delCorte = actividades.Where(a => a.Corte == corte).OrderBy(a => a.Titulo).ToList();

            var valores = new Dictionary<Guid, decimal>();
            foreach (var actividad in delCorte)
            {
                if (!calificacionPorActividad.TryGetValue(actividad.Id, out var calificacion)) continue;
                valores[actividad.Id] = calificacion.Valor;
                incluyeBorradores |= calificacion.Estado == EstadoCalificacion.Borrador;
            }

            decimal? nota = null;
            if (valores.Count > 0)
            {
                var calculada = MotorPonderado.NotaCorte(delCorte, valores);
                notaPorCorte[corte] = calculada;
                nota = calculada;
            }

            publicacionPorCorte.TryGetValue(corte, out var publicacion);

            cortes.Add(new PonderadoCorteDto(
                corte,
                curso.PesoDeCorte(corte),
                nota,
                publicacion is not null,
                publicacion?.Nota,
                conDetalle ? delCorte.Select(a => Detalle(a, calificacionPorActividad)).ToList() : null));
        }

        return new PonderadoEstudianteDto(
            inscrito.EstudianteId,
            inscrito.EstudianteNombre,
            inscrito.EstudianteCodigo,
            cortes,
            MotorPonderado.DefinitivaParcial(curso, notaPorCorte),
            EsParcial: notaPorCorte.Count < MotorPonderado.CantidadCortes,
            incluyeBorradores);
    }

    private static PonderadoActividadDto Detalle(
        Actividad actividad, IReadOnlyDictionary<Guid, Calificacion> calificacionPorActividad)
    {
        if (!calificacionPorActividad.TryGetValue(actividad.Id, out var calificacion))
            return new PonderadoActividadDto(actividad.Id, actividad.Titulo, actividad.Peso, null, null, null);

        return new PonderadoActividadDto(
            actividad.Id,
            actividad.Titulo,
            actividad.Peso,
            calificacion.Valor,
            calificacion.Estado.ComoTexto(),
            calificacion.Valor * actividad.Peso / 100m);
    }
}
