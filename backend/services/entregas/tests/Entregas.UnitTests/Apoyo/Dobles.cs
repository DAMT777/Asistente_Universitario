using System.Text;
using Entregas.Application.Abstracciones;
using Entregas.Domain;

namespace Entregas.UnitTests.Apoyo;

/// <summary>Repositorio y almacén en memoria, y un Evaluaciones simulado (LSP).</summary>
public sealed class Escenario : IEntregaRepository, IAlmacenArchivos, IEvaluacionesCliente
{
    public static readonly Guid Profesor = new("a0000000-0000-0000-0000-000000000001");
    public static readonly Guid OtroProfesor = new("a0000000-0000-0000-0000-000000000002");
    public static readonly Guid Ana = new("b0000000-0000-0000-0000-000000000001");
    public static readonly Guid Luis = new("b0000000-0000-0000-0000-000000000002");
    public static readonly Guid Curso = new("c0000000-0000-0000-0000-000000000001");
    public static readonly Guid Taller1 = new("d0000000-0000-0000-0000-000000000001");
    public static readonly Guid Proyecto1 = new("d0000000-0000-0000-0000-000000000003");
    public static readonly DateTime Ahora = new(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);

    public List<Entrega> Entregas { get; } = [];
    public Dictionary<string, byte[]> Archivos { get; } = [];
    public Dictionary<Guid, ActividadInfo> Actividades { get; } = [];
    public bool EvaluacionesCaido { get; set; }
    public int ConsultasAEvaluaciones { get; private set; }

    public Escenario()
    {
        Actividades[Taller1] = new(Taller1, Curso, Profesor, Ahora.AddDays(7), true, null);
        Actividades[Proyecto1] = new(Proyecto1, Curso, Profesor, Ahora.AddDays(-3), true, null);

        Entregar(Taller1, Ana, Ahora.AddDays(-1), "taller1-ana.pdf");
        Entregar(Taller1, Luis, Ahora.AddHours(-2), "taller1-luis.pdf");
        Entregar(Proyecto1, Ana, Ahora.AddDays(-4), "proyecto1-ana.zip");
    }

    public Entrega Entregar(Guid actividad, Guid estudiante, DateTime fecha, string nombre, EstadoEntrega estado = EstadoEntrega.Enviada)
    {
        var e = new Entrega
        {
            Id = Guid.NewGuid(), ActividadId = actividad, EstudianteId = estudiante, FechaEnvio = fecha,
            Estado = estado, NombreArchivo = nombre, Tamano = 100, RutaBlob = Entrega.RutaPara(actividad, estudiante, nombre)
        };
        Entregas.Add(e);
        Archivos[e.RutaBlob] = Encoding.UTF8.GetBytes("contenido de " + nombre);
        return e;
    }

    public Entrega De(Guid actividad, Guid estudiante) =>
        Entregas.Single(e => e.ActividadId == actividad && e.EstudianteId == estudiante);

    public Task<IReadOnlyList<Entrega>> ListarPorActividadAsync(Guid actividadId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Entrega>>(Entregas.Where(e => e.ActividadId == actividadId).ToList());

    public Task<IReadOnlyList<Entrega>> ListarPorEstudianteAsync(Guid estudianteId, Guid? actividadId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Entrega>>(
            Entregas.Where(e => e.EstudianteId == estudianteId && (actividadId is null || e.ActividadId == actividadId)).ToList());

    public Task<Entrega?> ObtenerAsync(Guid entregaId, CancellationToken ct) =>
        Task.FromResult(Entregas.FirstOrDefault(e => e.Id == entregaId));

    public Task<Stream?> AbrirAsync(string ruta, CancellationToken ct) =>
        Task.FromResult<Stream?>(Archivos.TryGetValue(ruta, out var b) ? new MemoryStream(b) : null);

    public Task<ActividadInfo?> ObtenerActividadAsync(Guid actividadId, Guid? estudianteId, CancellationToken ct)
    {
        ConsultasAEvaluaciones++;
        if (EvaluacionesCaido)
            throw new DominioException(CodigosError.ServicioNoDisponible, "El servicio de evaluaciones no respondió.");
        return Task.FromResult(Actividades.TryGetValue(actividadId, out var a) ? a : null);
    }
}
