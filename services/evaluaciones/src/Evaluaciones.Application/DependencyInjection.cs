using Microsoft.Extensions.DependencyInjection;
using Evaluaciones.Application.CasosDeUso;

namespace Evaluaciones.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAplicacionEvaluaciones(this IServiceCollection services)
    {
        services.AddScoped<VerificadorInscripcion>();
        services.AddScoped<ObtenerNotasActividades>();
        services.AddScoped<ObtenerMatrizNotas>();
        services.AddScoped<ObtenerActividadInterna>();
        return services;
    }
}
