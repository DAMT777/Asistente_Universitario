using FluentValidation;
using Microsoft.Extensions.Logging;
using Entregas.Application.Comun;
using Entregas.Application.Puertos;
using Entregas.Domain;

namespace Entregas.Application.CasosDeUso;

public sealed record EditarEntregaComando(Guid EntregaId, Guid EstudianteId, IReadOnlyList<ArchivoEntrante> Archivos);

public sealed class EditarEntregaValidador : AbstractValidator<EditarEntregaComando>
{
    public EditarEntregaValidador()
    {
        RuleFor(c => c.EntregaId).NotEmpty();
        RuleFor(c => c.EstudianteId).NotEmpty();
        RuleFor(c => c.Archivos).NotEmpty().WithMessage("Adjunte al menos un archivo en el campo 'archivo'.");
    }
}

/// <summary>CU-14 (editar): reemplaza los archivos mientras no venza la fecha límite (RN-06, RN-07).</summary>
public sealed class EditarEntrega(
    IValidator<EditarEntregaComando> validador,
    ReceptorArchivos receptor,
    ConsultorActividades actividades,
    IEntregaRepositorio repositorio,
    PersistenciaConCompensacion persistencia,
    TimeProvider reloj,
    ILogger<EditarEntrega> logger)
{
    public async Task<EntregaRespuesta> EjecutarAsync(EditarEntregaComando comando, CancellationToken ct)
    {
        await validador.ValidateAndThrowAsync(comando, ct);
        var tipos = await receptor.ValidarAsync(comando.Archivos, ct);

        var entrega = await BuscarPropia.EjecutarAsync(repositorio, comando.EntregaId, comando.EstudianteId, ct);
        var actividad = await actividades.ObtenerParaEstudianteAsync(entrega.ActividadId, comando.EstudianteId, ct);
        new PlazoEntrega(actividad.FechaLimite).ExigirVigente(reloj.GetUtcNow());

        var guardados = await receptor.GuardarAsync(entrega.ActividadId, entrega.EstudianteId, comando.Archivos, tipos, ct);
        var reemplazadas = entrega.ReemplazarArchivos(guardados, reloj.GetUtcNow());
        await persistencia.GuardarAsync(guardados.Select(g => g.RutaBlob).ToList(), reemplazadas, ct);

        logger.LogInformation("Entrega {EntregaId} editada por {EstudianteId} con {Archivos} archivo(s)", entrega.Id, entrega.EstudianteId, guardados.Count);
        return EntregaRespuesta.Desde(entrega);
    }
}
