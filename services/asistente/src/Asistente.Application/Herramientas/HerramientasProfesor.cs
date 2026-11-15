using System.Text.Json;
using Asistente.Application.Puertos;
using Asistente.Domain;
using Microsoft.Extensions.Options;

namespace Asistente.Application.Herramientas;

public sealed class ListarCursosProfesor(IGatewayCliente gateway) : IHerramienta
{
    public string Nombre => "listar_cursos_profesor";
    public string Descripcion => "Cursos que dicta el profesor (código y nombre).";
    public string EsquemaJson => """{"type":"object","properties":{},"additionalProperties":false}""";
    public string RolPermitido => UsuarioAsistente.Profesor;
    public bool EsEscritura => false;

    public async Task<object> EjecutarAsync(JsonElement argumentos, ContextoTurno contexto, CancellationToken ct) =>
        (await gateway.CursosAsync(ct)).Select(c => new { c.Codigo, c.Nombre }).ToList();
}

public sealed class ListarEstudiantesCurso(IGatewayCliente gateway) : IHerramienta
{
    public string Nombre => "listar_estudiantes_curso";
    public string Descripcion => "Estudiantes inscritos en un curso del profesor (nombre y código).";
    public string EsquemaJson => """{"type":"object","properties":{"curso":{"type":"string","description":"Código o nombre del curso; se puede omitir si el profesor tiene uno solo"}},"additionalProperties":false}""";
    public string RolPermitido => UsuarioAsistente.Profesor;
    public bool EsEscritura => false;

    public async Task<object> EjecutarAsync(JsonElement argumentos, ContextoTurno contexto, CancellationToken ct)
    {
        var curso = Cursos.Resolver(Argumentos.Texto(argumentos, "curso"), await gateway.CursosAsync(ct));
        if (curso.Tipo != TipoCoincidencia.Unica) return Cursos.Preguntar("el curso", curso);
        return (await gateway.EstudiantesDelCursoAsync(curso.Unico.Id, ct)).Select(e => new { e.Nombre, e.Codigo }).ToList();
    }
}

/// <summary>
/// La única herramienta de escritura, y NO escribe: crea una acción pendiente que el profesor debe confirmar
/// con POST /asistente/acciones/{id}/confirmar. Los nombres que da el modelo se resuelven contra datos reales.
/// </summary>
public sealed class ProponerCalificacion(
    IGatewayCliente gateway,
    IAccionesRepositorio acciones,
    IAuditoria auditoria,
    TimeProvider reloj,
    IOptions<AsistenteOpciones> opciones) : IHerramienta
{
    public string Nombre => "proponer_calificacion";
    public string Descripcion =>
        "Propone poner una nota en borrador a un estudiante en una actividad. No la guarda: el profesor debe confirmarla. " +
        "Usa los nombres tal como los dijo el profesor.";
    public string EsquemaJson => """
        {"type":"object","properties":{
          "curso":{"type":"string","description":"Código o nombre del curso; opcional si el profesor tiene uno solo"},
          "actividad":{"type":"string","description":"Título de la actividad"},
          "estudiante":{"type":"string","description":"Nombre o código del estudiante"},
          "valor":{"type":"number","description":"Nota de 0.0 a 5.0, un decimal"},
          "retroalimentacion":{"type":"string"}},
         "required":["actividad","estudiante","valor"],"additionalProperties":false}
        """;
    public string RolPermitido => UsuarioAsistente.Profesor;
    public bool EsEscritura => true;

    public async Task<object> EjecutarAsync(JsonElement argumentos, ContextoTurno contexto, CancellationToken ct)
    {
        if (!contexto.Usuario.EsProfesor)
            throw new ExcepcionAsistente(CodigosAsistente.SinPermiso, "Solo un profesor puede proponer calificaciones.");
        if (contexto.AccionCreada is not null)
            return new ErrorHerramienta("Ya hay una calificación propuesta en este mensaje. Pide al profesor que la confirme antes de proponer otra.");

        var valor = Argumentos.Decimal(argumentos, "valor");
        if (valor is null) return new ErrorHerramienta("Falta la nota. Pregunta al profesor qué nota quiere poner.");

        var curso = Cursos.Resolver(Argumentos.Texto(argumentos, "curso"), await gateway.CursosAsync(ct));
        if (curso.Tipo != TipoCoincidencia.Unica) return Cursos.Preguntar("el curso", curso);

        var actividades = await gateway.ActividadesDelCursoAsync(curso.Unico.Id, ct);
        var actividad = ResolutorNombres.Resolver(Argumentos.Texto(argumentos, "actividad"),
            actividades.Select(a => new Candidato(a.Id, a.Titulo)));
        if (actividad.Tipo != TipoCoincidencia.Unica)
            return Cursos.Preguntar("la actividad", actividad.Tipo == TipoCoincidencia.Ninguna
                ? actividad with { Candidatos = actividades.Select(a => new Candidato(a.Id, a.Titulo)).ToList() }
                : actividad);

        var inscritos = await gateway.EstudiantesDelCursoAsync(curso.Unico.Id, ct);
        var estudiante = ResolutorNombres.Resolver(Argumentos.Texto(argumentos, "estudiante"),
            inscritos.Select(e => new Candidato(e.Id, e.Nombre, e.Codigo)));
        if (estudiante.Tipo != TipoCoincidencia.Unica) return Cursos.Preguntar("el estudiante", estudiante);

        var anterior = (await gateway.CalificacionesDeActividadAsync(actividad.Unico.Id, ct))
            .FirstOrDefault(c => c.EstudianteId == estudiante.Unico.Id);

        AccionPendiente accion;
        try
        {
            accion = AccionPendiente.Proponer(
                contexto.Usuario.Id, curso.Unico.Id, actividad.Unico.Id, actividad.Unico.Nombre,
                estudiante.Unico.Id, estudiante.Unico.Nombre, valor.Value, Argumentos.Texto(argumentos, "retroalimentacion"),
                anterior?.Valor, anterior?.Version, reloj.GetUtcNow(), TimeSpan.FromMinutes(opciones.Value.ExpiracionAccionMinutos));
        }
        catch (ExcepcionAsistente e)
        {
            return new ErrorHerramienta(e.Message);
        }

        await acciones.AgregarAsync(accion, ct);
        await auditoria.RegistrarAsync(RegistroAuditoria.Crear(
            contexto.Usuario.Id, contexto.Usuario.Rol, AccionesAuditoria.Proponer, accion.Id,
            Datos.De(accion.ValorAnterior, null), Datos.De(accion.Valor, accion.ActividadId, accion.EstudianteId),
            "PENDIENTE", reloj.GetUtcNow()), ct);
        contexto.AccionCreada = accion;

        return new
        {
            Propuesta = accion.Resumen,
            Nota = "Todavía NO se guardó. El profesor debe confirmarla con el botón de confirmar.",
        };
    }
}

internal static class Datos
{
    /// <summary>JSON pequeño para la auditoría: solo valores e identificadores, nunca textos libres.</summary>
    public static string? De(decimal? valor, Guid? actividadId, Guid? estudianteId = null) =>
        valor is null && actividadId is null ? null
            : JsonSerializer.Serialize(new { valor, actividadId, estudianteId });
}
