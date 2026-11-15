using System.Text.Json;
using Asistente.Application;
using Asistente.Application.Herramientas;
using Asistente.Application.Puertos;
using Asistente.Domain;

namespace Asistente.UnitTests;

internal sealed class HerramientaFalsa(string nombre, string rol, bool escritura) : IHerramienta
{
    public string Nombre => nombre;
    public string Descripcion => nombre;
    public string EsquemaJson => "{}";
    public string RolPermitido => rol;
    public bool EsEscritura => escritura;
    public Task<object> EjecutarAsync(JsonElement argumentos, ContextoTurno contexto, CancellationToken ct) => Task.FromResult<object>("ok");
}

internal sealed class RelojManual(DateTimeOffset ahora) : TimeProvider
{
    public DateTimeOffset Ahora { get; set; } = ahora;
    public override DateTimeOffset GetUtcNow() => Ahora;
}

internal sealed class AccionesEnMemoria : IAccionesRepositorio
{
    public Dictionary<Guid, AccionPendiente> Filas { get; } = [];
    public Task AgregarAsync(AccionPendiente accion, CancellationToken ct) { Filas[accion.Id] = accion; return Task.CompletedTask; }
    public Task<AccionPendiente?> BuscarDeUsuarioAsync(Guid id, Guid usuarioId, CancellationToken ct) =>
        Task.FromResult(Filas.TryGetValue(id, out var a) && a.CreadaPor == usuarioId ? a : null);
    public Task<bool> TomarAsync(Guid id, EstadoAccion nuevo, DateTimeOffset ahora, CancellationToken ct)
    {
        if (!Filas.TryGetValue(id, out var a) || a.Estado != EstadoAccion.Pendiente || a.Expirada(ahora)) return Task.FromResult(false);
        a.Resolver(nuevo, ahora);
        return Task.FromResult(true);
    }
    public Task ResolverAsync(Guid id, EstadoAccion estado, DateTimeOffset ahora, CancellationToken ct) { Filas[id].Resolver(estado, ahora); return Task.CompletedTask; }
}

internal sealed class AuditoriaEnMemoria : IAuditoria
{
    public List<RegistroAuditoria> Registros { get; } = [];
    public Task RegistrarAsync(RegistroAuditoria registro, CancellationToken ct) { Registros.Add(registro); return Task.CompletedTask; }
}

/// <summary>Gateway falso: solo cuenta escrituras.</summary>
internal sealed class GatewayEscrituras : IGatewayCliente
{
    public int Escrituras { get; private set; }
    public Task<JsonElement> CalificarAsync(Guid actividadId, Guid estudianteId, decimal valor, string? retroalimentacion, string? versionEsperada, CancellationToken ct)
    {
        Escrituras++;
        return Task.FromResult(JsonDocument.Parse("""{"estado":"BORRADOR"}""").RootElement);
    }
    public Task<MatrizNotas> MisNotasAsync(CancellationToken ct) => throw new NotSupportedException();
    public Task<NotasCurso> MisNotasDeCursoAsync(Guid cursoId, CancellationToken ct) => throw new NotSupportedException();
    public Task<IReadOnlyList<EntregaPropia>> MisEntregasAsync(CancellationToken ct) => throw new NotSupportedException();
    public Task<IReadOnlyList<CursoProfesor>> CursosAsync(CancellationToken ct) => throw new NotSupportedException();
    public Task<IReadOnlyList<EstudianteInscrito>> EstudiantesDelCursoAsync(Guid cursoId, CancellationToken ct) => throw new NotSupportedException();
    public Task<IReadOnlyList<ActividadCurso>> ActividadesDelCursoAsync(Guid cursoId, CancellationToken ct) => throw new NotSupportedException();
    public Task<IReadOnlyList<CalificacionActividad>> CalificacionesDeActividadAsync(Guid actividadId, CancellationToken ct) => throw new NotSupportedException();
}
