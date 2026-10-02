using Evaluaciones.Domain.Entidades;

namespace Evaluaciones.UnitTests.Apoyo;

/// <summary>
/// Datos semilla de la sección 9.8 de la guía: un profesor, tres estudiantes, un curso con cortes 30/30/40,
/// tres actividades y las calificaciones de Ana, Luis y Marta.
/// </summary>
public sealed class Escenario
{
    public static readonly Guid ProfesorId = new("a0000000-0000-0000-0000-000000000001");
    public static readonly Guid OtroProfesorId = new("a0000000-0000-0000-0000-000000000002");
    public static readonly Guid Ana = new("b0000000-0000-0000-0000-000000000001");
    public static readonly Guid Luis = new("b0000000-0000-0000-0000-000000000002");
    public static readonly Guid Marta = new("b0000000-0000-0000-0000-000000000003");
    public static readonly Guid CursoId = new("c0000000-0000-0000-0000-000000000001");
    public static readonly Guid Taller1 = new("d0000000-0000-0000-0000-000000000001");
    public static readonly Guid Parcial1 = new("d0000000-0000-0000-0000-000000000002");
    public static readonly Guid Proyecto1 = new("d0000000-0000-0000-0000-000000000003");

    public static readonly DateTimeOffset Ahora = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

    public BaseEnMemoria Base { get; } = new();
    public RelojFijo Reloj { get; } = new(Ahora);

    /// <param name="anaConCorte1Publicado">En la semilla de la guía, Ana ya tiene el corte 1 publicado con 3.2.</param>
    public Escenario(bool anaConCorte1Publicado = true)
    {
        Base.Cursos.Add(new Curso
        {
            Id = CursoId, Codigo = "603803", Nombre = "Simulación computacional",
            ProfesorId = ProfesorId, ProfesorNombre = "Profesor Demo",
            PesoCorte1 = 30, PesoCorte2 = 30, PesoCorte3 = 40
        });

        Base.Inscripciones.Add(Inscrito(Ana, "Ana Demo", "E0001"));
        Base.Inscripciones.Add(Inscrito(Luis, "Luis Demo", "E0002"));
        Base.Inscripciones.Add(Inscrito(Marta, "Marta Demo", "E0003"));

        Base.Actividades.Add(new Actividad
        {
            Id = Taller1, CursoId = CursoId, Titulo = "Taller 1", Corte = 1, Peso = 20,
            FechaLimite = Ahora.UtcDateTime.AddDays(7), RequiereEntrega = true
        });
        Base.Actividades.Add(new Actividad
        {
            Id = Parcial1, CursoId = CursoId, Titulo = "Parcial 1", Corte = 1, Peso = 80,
            FechaLimite = null, RequiereEntrega = false
        });
        Base.Actividades.Add(new Actividad
        {
            Id = Proyecto1, CursoId = CursoId, Titulo = "Proyecto 1", Corte = 2, Peso = 100,
            FechaLimite = Ahora.UtcDateTime.AddDays(-3), RequiereEntrega = true
        });

        Calificar(Ana, Taller1, 4.0m, EstadoCalificacion.Publicada);
        Calificar(Ana, Parcial1, 3.0m, EstadoCalificacion.Publicada);
        Calificar(Luis, Taller1, 2.5m, EstadoCalificacion.Borrador);

        if (anaConCorte1Publicado)
            Publicar(Ana, corte: 1, nota: 3.2m);
    }

    public Calificacion Calificar(Guid estudianteId, Guid actividadId, decimal valor, EstadoCalificacion estado)
    {
        var calificacion = new Calificacion
        {
            Id = Guid.NewGuid(), ActividadId = actividadId, EstudianteId = estudianteId,
            Valor = valor, Estado = estado
        };
        Base.Calificaciones.Add(calificacion);
        return calificacion;
    }

    public PublicacionCorte Publicar(Guid estudianteId, int corte, decimal nota)
    {
        var publicacion = new PublicacionCorte
        {
            Id = Guid.NewGuid(), CursoId = CursoId, EstudianteId = estudianteId, Corte = corte,
            Nota = nota, FechaPublicacion = Ahora.UtcDateTime.AddDays(-1)
        };
        Base.Publicaciones.Add(publicacion);
        return publicacion;
    }

    public Calificacion CalificacionDe(Guid estudianteId, Guid actividadId) =>
        Base.Calificaciones.Single(c => c.EstudianteId == estudianteId && c.ActividadId == actividadId);

    private static CursoEstudiante Inscrito(Guid estudianteId, string nombre, string codigo) =>
        new() { CursoId = CursoId, EstudianteId = estudianteId, EstudianteNombre = nombre, EstudianteCodigo = codigo };
}
