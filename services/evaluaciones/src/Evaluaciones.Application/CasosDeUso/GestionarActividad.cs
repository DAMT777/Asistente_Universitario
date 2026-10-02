using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Application.Dtos;
using Evaluaciones.Application.Internos;
using Evaluaciones.Domain.Entidades;
using Evaluaciones.Domain.Errores;

namespace Evaluaciones.Application.CasosDeUso;

/// <summary>
/// CU-03. El profesor dueño del curso (RN-15) crea o edita una actividad con corte, peso, fecha límite y si
/// requiere entrega. Reglas: RN-02 (peso mayor que 0 y el corte no pasa de 100), RN-04 y RN-05 (con entrega
/// necesita fecha límite). Una fecha límite nueva debe ser posterior a la hora actual.
/// </summary>
public sealed class GestionarActividad(
    ICursoRepository cursos,
    IActividadRepository actividades,
    IUnidadDeTrabajo unidadDeTrabajo,
    TimeProvider reloj)
{
    public async Task<ActividadDto> CrearAsync(
        Guid profesorId, Guid cursoId, SolicitudActividad solicitud, CancellationToken ct)
    {
        var datos = Validar(solicitud);
        var ahora = reloj.GetUtcNow().UtcDateTime;
        ExigirFechaFutura(datos.FechaLimite, ahora);

        await AccesoCurso.ObtenerDelProfesorAsync(cursos, cursoId, profesorId, ct);

        var delCorte = (await actividades.ListarActividadesAsync(cursoId, ct)).Where(a => a.Corte == datos.Corte);
        Actividad.ValidarPesoDisponible(datos.Corte, datos.Peso, delCorte);

        var actividad = Actividad.Nueva(cursoId, datos);
        actividades.AgregarActividad(actividad);
        await unidadDeTrabajo.GuardarCambiosAsync(ct);

        return actividad.ADto(ahora);
    }

    public async Task<ActividadDto> EditarAsync(
        Guid profesorId, Guid actividadId, SolicitudActividad solicitud, CancellationToken ct)
    {
        var datos = Validar(solicitud);
        var ahora = reloj.GetUtcNow().UtcDateTime;

        var actividad = await actividades.ObtenerActividadParaEditarAsync(actividadId, ct)
            ?? throw new DominioException(CodigosError.NoEncontrado, "La actividad no existe.");

        await AccesoCurso.ObtenerDelProfesorAsync(cursos, actividad.CursoId, profesorId, ct);

        // Conservar la fecha que ya tenía es válido aunque haya pasado; cambiarla exige una fecha futura.
        // Se compara al minuto porque el formulario no maneja segundos.
        if (!MismoMinuto(datos.FechaLimite, actividad.FechaLimite))
            ExigirFechaFutura(datos.FechaLimite, ahora);

        var otrasDelCorte = (await actividades.ListarActividadesAsync(actividad.CursoId, ct))
            .Where(a => a.Corte == datos.Corte && a.Id != actividad.Id);
        Actividad.ValidarPesoDisponible(datos.Corte, datos.Peso, otrasDelCorte);

        actividad.Editar(datos);
        await unidadDeTrabajo.GuardarCambiosAsync(ct);

        return actividad.ADto(ahora);
    }

    private static DatosActividad Validar(SolicitudActividad s) =>
        Actividad.ValidarDatos(s.Titulo, s.Corte, s.Peso, s.FechaLimite, s.RequiereEntrega);

    private static bool MismoMinuto(DateTime? a, DateTime? b) =>
        a is null || b is null ? a == b : Math.Abs((a.Value - b.Value).TotalMinutes) < 1;

    private static void ExigirFechaFutura(DateTime? fechaLimite, DateTime ahoraUtc)
    {
        if (fechaLimite is not null && fechaLimite <= ahoraUtc)
            throw new DominioException(
                CodigosError.ValidacionFallida, "La fecha límite debe ser posterior a la hora actual.");
    }
}
