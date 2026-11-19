using System.Text.Json;
using Asistente.Application.Puertos;
using Asistente.Domain;

namespace Asistente.Application.Herramientas;

// Herramientas del ESTUDIANTE: solo lectura. Los endpoints usan el sub del token, así que nunca
// reciben un estudianteId y el estudiante solo puede ver lo suyo y lo publicado.

public sealed class ConsultarMisNotas(IGatewayCliente gateway) : IHerramienta
{
    public string Nombre => "consultar_mis_notas";
    public string Descripcion => "Matriz de notas del estudiante: sus cursos, la nota publicada de cada corte y la definitiva parcial.";
    public string EsquemaJson => """{"type":"object","properties":{},"additionalProperties":false}""";
    public string RolPermitido => UsuarioAsistente.Estudiante;
    public bool EsEscritura => false;

    public async Task<object> EjecutarAsync(JsonElement argumentos, ContextoTurno contexto, CancellationToken ct)
    {
        var matriz = await gateway.MisNotasAsync(ct);
        return matriz.Cursos.Select(c => new
        {
            c.Codigo,
            c.Nombre,
            Cortes = c.Cortes.Select(k => new { k.Corte, k.PesoCorte, Nota = k.Publicado ? k.Nota : null, Estado = k.Publicado ? "PUBLICADO" : "SIN_PUBLICAR" }),
            c.DefinitivaParcial,
            c.EsParcial,
        }).ToList();
    }
}

public sealed class ConsultarNotasCurso(IGatewayCliente gateway) : IHerramienta
{
    public string Nombre => "consultar_notas_curso";
    public string Descripcion => "Notas publicadas y retroalimentación de cada actividad (talleres, parciales, proyectos) de un curso del estudiante. Las no publicadas salen SIN_CALIFICAR. Si se omite el curso, consulta las notas publicadas de las actividades en sus cursos.";
    public string EsquemaJson => """{"type":"object","properties":{"curso":{"type":"string","description":"Código o nombre del curso; si se omite, consulta todos"}},"additionalProperties":false}""";
    public string RolPermitido => UsuarioAsistente.Estudiante;
    public bool EsEscritura => false;

    public async Task<object> EjecutarAsync(JsonElement argumentos, ContextoTurno contexto, CancellationToken ct)
    {
        var matriz = await gateway.MisNotasAsync(ct);
        var nombreCurso = Argumentos.Texto(argumentos, "curso");
        if (string.IsNullOrWhiteSpace(nombreCurso) && matriz.Cursos.Count != 1)
        {
            const int maxCursos = 5;
            const int maxNotas = 20;
            var cursos = new List<object>();
            var parcial = matriz.Cursos.Count > maxCursos;
            foreach (var curso in matriz.Cursos.Take(maxCursos))
            {
                var notasCurso = await gateway.MisNotasDeCursoAsync(curso.CursoId, ct);
                var publicadas = notasCurso.Actividades.Where(a => a.Estado == "PUBLICADA").ToArray();
                parcial |= publicadas.Length > maxNotas;
                cursos.Add(new { curso.Nombre, curso.Codigo,
                    NotasPublicadas = publicadas.Take(maxNotas).Select(a => new { a.Titulo, a.Corte, a.Nota }).ToList() });
            }
            return new { Cursos = cursos, ResultadoParcial = parcial };
        }

        var r = Cursos.Resolver(nombreCurso,
            matriz.Cursos.Select(c => new CursoProfesor(c.CursoId, c.Codigo, c.Nombre)).ToList());
        if (r.Tipo != TipoCoincidencia.Unica) return Cursos.Preguntar("el curso", r with { Candidatos = r.Tipo == TipoCoincidencia.Ninguna ? matriz.Cursos.Select(c => new Candidato(c.CursoId, c.Nombre, c.Codigo)).ToList() : r.Candidatos });

        var notas = await gateway.MisNotasDeCursoAsync(r.Unico.Id, ct);
        return notas.Actividades.Select(a => new
        {
            a.Titulo,
            a.Corte,
            a.Peso,
            // Defensa adicional: solo una nota PUBLICADA se le muestra al modelo.
            Estado = a.Estado == "PUBLICADA" ? "PUBLICADA" : "SIN_CALIFICAR",
            Nota = a.Estado == "PUBLICADA" ? a.Nota : null,
            Retroalimentacion = a.Estado == "PUBLICADA" ? a.Retroalimentacion : null,
            a.FechaLimite,
        }).ToList();
    }
}

public sealed class ConsultarMisEntregas(IGatewayCliente gateway) : IHerramienta
{
    public string Nombre => "consultar_mis_entregas";
    public string Descripcion => "Entregas que el estudiante ha subido: actividad, fecha de envío, estado y archivos.";
    public string EsquemaJson => """{"type":"object","properties":{},"additionalProperties":false}""";
    public string RolPermitido => UsuarioAsistente.Estudiante;
    public bool EsEscritura => false;

    public async Task<object> EjecutarAsync(JsonElement argumentos, ContextoTurno contexto, CancellationToken ct)
    {
        var entregas = await gateway.MisEntregasAsync(ct);
        if (entregas.Count == 0) return Array.Empty<object>();

        // Títulos de las actividades para que el modelo no tenga que manejar identificadores.
        var titulos = new Dictionary<Guid, string>();
        foreach (var curso in (await gateway.MisNotasAsync(ct)).Cursos)
            foreach (var a in (await gateway.MisNotasDeCursoAsync(curso.CursoId, ct)).Actividades)
                titulos[a.ActividadId] = $"{a.Titulo} ({curso.Codigo})";

        return entregas.Select(e => new
        {
            Actividad = titulos.GetValueOrDefault(e.ActividadId, "Actividad"),
            e.FechaEnvio,
            e.Estado,
            Archivos = e.Archivos.Select(a => a.NombreArchivo),
        }).ToList();
    }
}
