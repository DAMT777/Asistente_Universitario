using FluentValidation;
using Microsoft.Extensions.Logging;
using Entregas.Application.Comun;
using Entregas.Application.Puertos;
using Entregas.Domain;

namespace Entregas.Application.CasosDeUso;

public sealed record SubirEntregaComando(Guid ActividadId, Guid EstudianteId, IReadOnlyList<ArchivoEntrante> Archivos);

public sealed class SubirEntregaValidador : AbstractValidator<SubirEntregaComando>
{
    public SubirEntregaValidador()
    {
        RuleFor(c => c.ActividadId).NotEmpty();
        RuleFor(c => c.EstudianteId).NotEmpty();
        RuleFor(c => c.Archivos).NotEmpty().WithMessage("Adjunte al menos un archivo en el campo 'archivo'.");
    }
}

/// <summary>CU-13: el estudiante entrega uno o varios archivos antes de la fecha límite.</summary>
public sealed class SubirEntrega(
    IValidator<SubirEntregaComando> validador,
    ReceptorArchivos receptor,
    ConsultorActividades actividades,
    IEntregaRepositorio repositorio,
    PersistenciaConCompensacion persistencia,
    TimeProvider reloj,
    ILogger<SubirEntrega> logger)
{
    public async Task<EntregaRespuesta> EjecutarAsync(SubirEntregaComando comando, CancellationToken ct)
    {
        await validador.ValidateAndThrowAsync(comando, ct);
        var tipos = await receptor.ValidarAsync(comando.Archivos, ct);

        var actividad = await actividades.ObtenerParaEstudianteAsync(comando.ActividadId, comando.EstudianteId, ct);
        PoliticaSubida.Verificar(actividad.RequiereEntrega, new PlazoEntrega(actividad.FechaLimite), reloj.GetUtcNow());

        var existente = await repositorio.ObtenerPorActividadYEstudianteAsync(comando.ActividadId, comando.EstudianteId, ct);
        var guardados = await receptor.GuardarAsync(comando.ActividadId, comando.EstudianteId, comando.Archivos, tipos, ct);

        // RN-07: una sola fila por actividad y estudiante; si ya existe (enviada o anulada) se reutiliza.
        IReadOnlyList<string> reemplazadas = [];
        var entrega = existente;
        if (entrega is null)
        {
            entrega = Entrega.Crear(comando.ActividadId, comando.EstudianteId, guardados, reloj.GetUtcNow());
            repositorio.Agregar(entrega);
        }
        else
        {
            reemplazadas = entrega.ReemplazarArchivos(guardados, reloj.GetUtcNow());
        }

        await persistencia.GuardarAsync(guardados.Select(g => g.RutaBlob).ToList(), reemplazadas, ct);
        logger.LogInformation("Entrega {EntregaId} con {Archivos} archivo(s) subida a la actividad {ActividadId} por {EstudianteId}",
            entrega.Id, guardados.Count, entrega.ActividadId, entrega.EstudianteId);
        return EntregaRespuesta.Desde(entrega);
    }
}
