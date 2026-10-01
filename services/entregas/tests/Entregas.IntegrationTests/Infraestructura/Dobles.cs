using System.Collections.Concurrent;
using Entregas.Application.Puertos;
using Unillanos.Pruebas.Compartidas;

namespace Entregas.IntegrationTests.Infraestructura;

/// <summary>Almacén de archivos en memoria (doble de Blob Storage).</summary>
public sealed class AlmacenEnMemoria : IAlmacenArchivos
{
    public ConcurrentDictionary<string, (byte[] Datos, string ContentType)> Blobs { get; } = new();

    public async Task GuardarAsync(string ruta, Stream contenido, string contentType, CancellationToken ct)
    {
        using var copia = new MemoryStream();
        await contenido.CopyToAsync(copia, ct);
        Blobs[ruta] = (copia.ToArray(), contentType);
    }

    public Task EliminarAsync(string ruta, CancellationToken ct)
    {
        Blobs.TryRemove(ruta, out _);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Doble de Evaluaciones con las 3 actividades semilla, relativas al reloj de la prueba:
/// Taller 1 abierta (con entrega), Parcial 1 sin entrega y Proyecto 1 vencido.
/// Ana, Luis y Marta están inscritos; cualquier otro estudiante no.
/// </summary>
public sealed class EvaluacionesFalso(RelojFijo reloj) : IEvaluacionesClient
{
    private static readonly Guid Profesor = UsuariosSemilla.Profesor;
    private static readonly HashSet<Guid> Inscritos = [UsuariosSemilla.Ana, UsuariosSemilla.Luis, UsuariosSemilla.Marta];
    private readonly ConcurrentDictionary<Guid, (DateTimeOffset FechaLimite, bool RequiereEntrega)> _actividades = new();

    public bool Caido { get; set; }

    public void Reiniciar()
    {
        Caido = false;
        _actividades[UsuariosSemilla.Taller1] = (reloj.Ahora.AddDays(30), true);
        _actividades[UsuariosSemilla.Parcial1] = (reloj.Ahora.AddDays(-3), false);
        _actividades[UsuariosSemilla.Proyecto1] = (reloj.Ahora.AddDays(-7), true);
    }

    /// <summary>Simula que pasó el tiempo: la fecha límite de la actividad quedó 1 segundo atrás.</summary>
    public void Vencer(Guid actividadId) => _actividades[actividadId] = (reloj.Ahora.AddSeconds(-1), _actividades[actividadId].RequiereEntrega);

    public Task<ActividadInfo?> ObtenerActividadAsync(Guid actividadId, Guid estudianteId, CancellationToken ct)
    {
        if (Caido) throw new ServicioNoDisponibleException("Evaluaciones");
        return Task.FromResult(_actividades.TryGetValue(actividadId, out var a)
            ? new ActividadInfo(actividadId, UsuariosSemilla.Curso, Profesor, a.FechaLimite, a.RequiereEntrega, Inscritos.Contains(estudianteId))
            : null);
    }
}
