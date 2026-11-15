using System.Text.Json;
using Asistente.Application.Puertos;
using Asistente.Domain;

namespace Asistente.Application.CasosDeUso;

/// <summary>
/// POST /asistente/acciones/{id}/confirmar: la ÚNICA ruta por la que el asistente escribe.
/// Exige que la acción exista, sea del mismo usuario (sub), no haya expirado y no se haya usado.
/// La escritura va al gateway con el JWT del profesor y nunca se reintenta.
/// </summary>
public sealed class GestionarAcciones(
    IAccionesRepositorio acciones,
    IGatewayCliente gateway,
    IAuditoria auditoria,
    TimeProvider reloj)
{
    public async Task<JsonElement> ConfirmarAsync(UsuarioAsistente usuario, Guid accionId, CancellationToken ct)
    {
        var accion = await BuscarAsync(usuario, accionId, ct);
        if (!usuario.EsProfesor)
            throw new ExcepcionAsistente(CodigosAsistente.SinPermiso, "Solo un profesor puede confirmar calificaciones.");

        var ahora = reloj.GetUtcNow();
        accion.VerificarUsable(ahora);
        // Toma atómica: dos confirmaciones simultáneas no pueden escribir dos veces.
        if (!await acciones.TomarAsync(accion.Id, EstadoAccion.EnProceso, ahora, ct))
            throw new ExcepcionAsistente(CodigosAsistente.AccionYaUsada, "La acción ya fue confirmada o cancelada.");

        var antes = JsonSerializer.Serialize(new { valor = accion.ValorAnterior, accion.ActividadId, accion.EstudianteId });
        try
        {
            var resultado = await gateway.CalificarAsync(
                accion.ActividadId, accion.EstudianteId, accion.Valor, accion.Retroalimentacion, accion.VersionEsperada, ct);

            await acciones.ResolverAsync(accion.Id, EstadoAccion.Confirmada, reloj.GetUtcNow(), ct);
            await auditoria.RegistrarAsync(RegistroAuditoria.Crear(usuario.Id, usuario.Rol, AccionesAuditoria.Confirmar, accion.Id,
                antes, JsonSerializer.Serialize(new { valor = accion.Valor, accion.ActividadId, accion.EstudianteId, estado = "BORRADOR" }),
                "EXITO", reloj.GetUtcNow()), CancellationToken.None);
            return resultado;
        }
        catch (Exception e) when (e is ErrorRemotoException or ExcepcionAsistente or HttpRequestException or TaskCanceledException)
        {
            // Nunca se informa éxito si la escritura falló; la acción queda inutilizable (hay que proponer de nuevo).
            var codigo = e switch
            {
                ErrorRemotoException r => r.Codigo,
                ExcepcionAsistente a => a.Codigo,
                _ => CodigosAsistente.ServicioNoDisponible,
            };
            await acciones.ResolverAsync(accion.Id, EstadoAccion.Fallida, reloj.GetUtcNow(), CancellationToken.None);
            await auditoria.RegistrarAsync(RegistroAuditoria.Crear(usuario.Id, usuario.Rol, AccionesAuditoria.Confirmar, accion.Id,
                antes, null, $"ERROR:{codigo}", reloj.GetUtcNow()), CancellationToken.None);
            if (e is HttpRequestException or TaskCanceledException)
                throw new ExcepcionAsistente(CodigosAsistente.ServicioNoDisponible, "Evaluaciones no respondió. La calificación NO se guardó.", 503);
            throw;
        }
    }

    public async Task CancelarAsync(UsuarioAsistente usuario, Guid accionId, CancellationToken ct)
    {
        var accion = await BuscarAsync(usuario, accionId, ct);
        var ahora = reloj.GetUtcNow();
        accion.VerificarUsable(ahora);
        if (!await acciones.TomarAsync(accion.Id, EstadoAccion.Cancelada, ahora, ct))
            throw new ExcepcionAsistente(CodigosAsistente.AccionYaUsada, "La acción ya fue confirmada o cancelada.");
        await auditoria.RegistrarAsync(RegistroAuditoria.Crear(usuario.Id, usuario.Rol, AccionesAuditoria.Cancelar, accion.Id,
            null, null, "CANCELADA", ahora), ct);
    }

    private async Task<AccionPendiente> BuscarAsync(UsuarioAsistente usuario, Guid id, CancellationToken ct) =>
        await acciones.BuscarDeUsuarioAsync(id, usuario.Id, ct)
        ?? throw new ExcepcionAsistente(CodigosAsistente.AccionNoEncontrada, "La acción no existe.");
}
