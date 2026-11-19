using System.Text.Json;
using Asistente.Application.Puertos;
using Asistente.Domain;

namespace Asistente.Application.Herramientas;

public sealed class ConsultarActividadesCurso(IGatewayCliente gateway, string rol) : IHerramienta
{
    public string Nombre => "consultar_actividades_curso";
    public string Descripcion => "Lista actividades de un curso propio: títulos, cortes, pesos, fechas límite y si requieren entrega.";
    public string EsquemaJson => """{"type":"object","properties":{"curso":{"type":"string","description":"Nombre o código; opcional para un solo curso"}},"additionalProperties":false}""";
    public string RolPermitido => rol;
    public bool EsEscritura => false;

    public async Task<object> EjecutarAsync(JsonElement argumentos, ContextoTurno contexto, CancellationToken ct)
    {
        var curso = Cursos.Resolver(Argumentos.Texto(argumentos, "curso"), await gateway.CursosAsync(ct));
        if (curso.Tipo != TipoCoincidencia.Unica) return Cursos.Preguntar("el curso", curso);

        var actividades = await gateway.ActividadesDelCursoAsync(curso.Unico.Id, ct);
        return actividades.Select(a => new
        {
            a.Titulo, a.Corte, a.Peso, a.RequiereEntrega,
            FechaLimiteColombia = a.FechaLimite?.AddHours(-5).ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture),
        }).ToList();
    }
}

public sealed class ConsultarCalificacionesActividad(IGatewayCliente gateway) : IHerramienta
{
    private const int MaxActividades = 8;
    private const int MaxNotas = 20;

    public string Nombre => "consultar_calificaciones_actividad";
    public string Descripcion => """
        Consulta notas de cursos propios del profesor, incluidas las publicadas, borradores y pendientes.
        Todos los campos son opcionales. Sin curso ni actividad consulta las actividades de los cursos propios.
        Usa estado BORRADOR o PUBLICADA cuando el profesor pida esas notas en particular.
        """;
    public string EsquemaJson => """
        {"type":"object","properties":{"curso":{"type":"string"},"actividad":{"type":"string"},
        "estudiante":{"type":"string"},"estado":{"type":"string","enum":["BORRADOR","PUBLICADA","SIN_CALIFICAR"]}},
        "additionalProperties":false}
        """;
    public string RolPermitido => UsuarioAsistente.Profesor;
    public bool EsEscritura => false;

    public async Task<object> EjecutarAsync(JsonElement argumentos, ContextoTurno contexto, CancellationToken ct)
    {
        var cursos = await gateway.CursosAsync(ct);
        var nombreCurso = Argumentos.Texto(argumentos, "curso");
        var nombreActividad = Argumentos.Texto(argumentos, "actividad");
        var nombreEstudiante = Argumentos.Texto(argumentos, "estudiante");
        var estado = Argumentos.Texto(argumentos, "estado");
        if (!string.IsNullOrWhiteSpace(nombreCurso))
        {
            var coincidencia = Cursos.Resolver(nombreCurso, cursos);
            if (coincidencia.Tipo != TipoCoincidencia.Unica) return Cursos.Preguntar("el curso", coincidencia);
            cursos = cursos.Where(c => c.Id == coincidencia.Unico.Id).ToArray();
        }

        var actividades = new List<(CursoProfesor Curso, ActividadCurso Actividad)>();
        foreach (var curso in cursos)
            actividades.AddRange((await gateway.ActividadesDelCursoAsync(curso.Id, ct)).Select(a => (curso, a)));

        if (!string.IsNullOrWhiteSpace(nombreActividad))
        {
            var coincidencia = ResolutorNombres.Resolver(nombreActividad,
                actividades.Select(x => new Candidato(x.Actividad.Id, x.Actividad.Titulo, x.Curso.Codigo)));
            if (coincidencia.Tipo != TipoCoincidencia.Unica)
                return Cursos.Preguntar("la actividad", coincidencia.Tipo == TipoCoincidencia.Ninguna
                    ? coincidencia with { Candidatos = actividades.Take(MaxActividades).Select(x => new Candidato(x.Actividad.Id, x.Actividad.Titulo, x.Curso.Codigo)).ToArray() }
                    : coincidencia);
            actividades = actividades.Where(x => x.Actividad.Id == coincidencia.Unico.Id).ToList();
        }

        var resultados = new List<object>();
        var inscripciones = new Dictionary<Guid, IReadOnlyList<EstudianteInscrito>>();
        var disponibles = MaxNotas;
        var incompleto = actividades.Count > MaxActividades;
        foreach (var (curso, actividad) in actividades.Take(MaxActividades))
        {
            if (!inscripciones.TryGetValue(curso.Id, out var inscritos))
            {
                inscritos = await gateway.EstudiantesDelCursoAsync(curso.Id, ct);
                inscripciones.Add(curso.Id, inscritos);
            }

            var seleccion = inscritos;
            if (!string.IsNullOrWhiteSpace(nombreEstudiante))
            {
                var coincidencia = ResolutorNombres.Resolver(nombreEstudiante,
                    inscritos.Select(e => new Candidato(e.Id, e.Nombre, e.Codigo)));
                if (coincidencia.Tipo == TipoCoincidencia.Varias) return Cursos.Preguntar("el estudiante", coincidencia);
                if (coincidencia.Tipo == TipoCoincidencia.Ninguna) continue;
                seleccion = inscritos.Where(e => e.Id == coincidencia.Unico.Id).ToArray();
            }

            var notas = (await gateway.CalificacionesDeActividadAsync(actividad.Id, ct)).ToDictionary(n => n.EstudianteId);
            var filas = seleccion.Select(e =>
            {
                var nota = notas.GetValueOrDefault(e.Id);
                return new { Estudiante = e.Nombre, Nota = nota?.Valor, Estado = nota?.Estado ?? "SIN_CALIFICAR" };
            }).ToArray();
            var filtradas = filas.Where(f => estado is null || f.Estado == estado).ToArray();
            var detalle = filtradas.Take(disponibles).ToArray();
            disponibles -= detalle.Length;
            incompleto |= filtradas.Length > detalle.Length;

            resultados.Add(new
            {
                Curso = curso.Nombre, curso.Codigo, Actividad = actividad.Titulo,
                Borradores = filas.Count(f => f.Estado == "BORRADOR"),
                Publicadas = filas.Count(f => f.Estado == "PUBLICADA"),
                SinCalificar = filas.Count(f => f.Estado == "SIN_CALIFICAR"),
                Notas = detalle,
            });
        }

        return new { Resultados = resultados, ResultadoParcial = incompleto,
            Aviso = incompleto ? "Hay más resultados. Se puede consultar por curso, actividad o estudiante." : null };
    }
}
