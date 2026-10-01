using Microsoft.Extensions.Logging;
using Entregas.Application.Comun;
using Entregas.Application.Puertos;
using Entregas.Domain;

namespace Entregas.Application.CasosDeUso;

/// <summary>CU-14 (anular): cambia el estado a ANULADA mientras no venza la fecha límite (RN-06).</summary>
public sealed class AnularEntrega(
    ConsultorActividades actividades,
    IEntregaRepositorio repositorio,
    TimeProvider reloj,
    ILogger<AnularEntrega> logger)
{
    public async Task<EntregaRespuesta> EjecutarAsync(Guid entregaId, Guid estudianteId, CancellationToken ct)
    {
        var entrega = await BuscarPropia.EjecutarAsync(repositorio, entregaId, estudianteId, ct);
        var actividad = await actividades.ObtenerParaEstudianteAsync(entrega.ActividadId, estudianteId, ct);
        new PlazoEntrega(actividad.FechaLimite).ExigirVigente(reloj.GetUtcNow());

        entrega.Anular();
        await repositorio.GuardarCambiosAsync(ct);

        logger.LogInformation("Entrega {EntregaId} anulada por {EstudianteId}", entrega.Id, entrega.EstudianteId);
        return EntregaRespuesta.Desde(entrega);
    }
}
