using FluentValidation;
using Microsoft.Extensions.Logging;
using Entregas.Application.Comun;
using Entregas.Application.Puertos;
using Entregas.Domain;

namespace Entregas.Application.CasosDeUso;

public sealed record EditarEntregaComando(Guid EntregaId, Guid EstudianteId, ArchivoEntrante? Archivo);

public sealed class EditarEntregaValidador : AbstractValidator<EditarEntregaComando>
{
    public EditarEntregaValidador()
    {
        RuleFor(c => c.EntregaId).NotEmpty();
        RuleFor(c => c.EstudianteId).NotEmpty();
        RuleFor(c => c.Archivo).NotNull().WithMessage("El campo 'archivo' es obligatorio.");
    }
}

/// <summary>CU-14 (editar): reemplaza el archivo mientras no venza la fecha límite (RN-06, RN-07).</summary>
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
        var archivo = comando.Archivo!;
        var tipo = await receptor.ValidarAsync(archivo, ct);

        var entrega = await BuscarPropia.EjecutarAsync(repositorio, comando.EntregaId, comando.EstudianteId, ct);
        var actividad = await actividades.ObtenerParaEstudianteAsync(entrega.ActividadId, comando.EstudianteId, ct);
        new PlazoEntrega(actividad.FechaLimite).ExigirVigente(reloj.GetUtcNow());

        var guardado = await receptor.GuardarAsync(entrega.ActividadId, entrega.EstudianteId, archivo, tipo, ct);
        var rutaReemplazada = entrega.ReemplazarArchivo(guardado, reloj.GetUtcNow());
        await persistencia.GuardarAsync(guardado.RutaBlob, rutaReemplazada, ct);

        logger.LogInformation("Entrega {EntregaId} editada por {EstudianteId}", entrega.Id, entrega.EstudianteId);
        return EntregaRespuesta.Desde(entrega);
    }
}
