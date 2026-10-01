using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Evaluaciones.Application.Puertos;
using Evaluaciones.Infrastructure.Persistencia;

namespace Evaluaciones.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfraestructuraEvaluaciones(this IServiceCollection services, IConfiguration configuracion)
    {
        services.AddDbContext<EvaluacionesDbContext>(o => o
            .UseSqlServer(configuracion.GetConnectionString("Default")
                          ?? throw new InvalidOperationException("Falta la variable ConnectionStrings__Default."))
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
        services.AddScoped<ICursoRepositorio, CursoRepositorio>();
        services.AddScoped<IActividadRepositorio, ActividadRepositorio>();
        services.AddScoped<ICalificacionRepositorio, CalificacionRepositorio>();
        services.AddScoped<IPublicacionCorteRepositorio, PublicacionCorteRepositorio>();
        services.AddScoped<InicializadorBaseDatos>();
        services.AddScoped<SemillaDesarrollo>();
        return services;
    }
}
