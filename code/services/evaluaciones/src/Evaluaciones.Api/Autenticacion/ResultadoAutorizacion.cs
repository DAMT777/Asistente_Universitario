using Evaluaciones.Api.Errores;
using Evaluaciones.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Evaluaciones.Api.Autenticacion;

public static class Politicas
{
    public const string Profesor = "Profesor";
}

/// <summary>Cuando el token es válido pero el rol no alcanza, responde 403 SIN_PERMISO con el formato común.</summary>
public sealed class ResultadoAutorizacion : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler porDefecto = new();

    public Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult resultado)
    {
        if (resultado.Forbidden)
            return Respuestas.EscribirAsync(context, StatusCodes.Status403Forbidden, CodigosError.SinPermiso, "Tu rol no permite esta acción.", context.RequestAborted);

        return porDefecto.HandleAsync(next, context, policy, resultado);
    }
}
