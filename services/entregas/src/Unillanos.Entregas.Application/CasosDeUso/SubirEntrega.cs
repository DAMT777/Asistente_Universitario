using FluentValidation;
using Microsoft.Extensions.Logging;
using Unillanos.Entregas.Application.Comun;
using Unillanos.Entregas.Application.Puertos;
using Unillanos.Entregas.Domain;

namespace Unillanos.Entregas.Application.CasosDeUso;

public sealed record SubirEntregaComando(Guid ActividadId, Guid EstudianteId, ArchivoEntrante? Archivo);

public sealed class SubirEntregaValidador : AbstractValidator<SubirEntregaComando>
{
    public SubirEntregaValidador()
    {
        RuleFor(c => c.ActividadId).NotEmpty();
        RuleFor(c => c.EstudianteId).NotEmpty();
        RuleFor(c => c.Archivo).NotNull().WithMessage("El campo 'archivo' es obligatorio.");
    }
}

/// <summary>CU-13: el estudiante sube una entrega antes de la fecha límite.</summary>
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
        var archivo = comando.Archivo!;
        var tipo = await receptor.ValidarAsync(archivo, ct);

        var actividad = await actividades.ObtenerParaEstudianteAsync(comando.ActividadId, comando.EstudianteId, ct);
        PoliticaSubida.Verificar(actividad.RequiereEntrega, new PlazoEntrega(actividad.FechaLimite), reloj.GetUtcNow());

        var existente = await repositorio.ObtenerPorActividadYEstudianteAsync(comando.ActividadId, comando.EstudianteId, ct);
        var guardado = await receptor.GuardarAsync(comando.ActividadId, comando.EstudianteId, archivo, tipo, ct);

        // RN-07: una sola fila por actividad y estudiante; si ya existe (enviada o anulada) se reutiliza.
        string? rutaReemplazada = null;
        var entrega = existente;
        if (entrega is null)
        {
            entrega = Entrega.Crear(comando.ActividadId, comando.EstudianteId, guardado, reloj.GetUtcNow());
            repositorio.Agregar(entrega);
        }
        else
        {
            rutaReemplazada = entrega.ReemplazarArchivo(guardado, reloj.GetUtcNow());
        }

        await persistencia.GuardarAsync(guardado.RutaBlob, rutaReemplazada, ct);
        logger.LogInformation("Entrega {EntregaId} subida a la actividad {ActividadId} por {EstudianteId}",
            entrega.Id, entrega.ActividadId, entrega.EstudianteId);
        return EntregaRespuesta.Desde(entrega);
    }
}
