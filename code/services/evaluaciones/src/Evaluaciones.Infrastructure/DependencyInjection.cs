using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Infrastructure.Persistencia;
using Evaluaciones.Infrastructure.Repositorios;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Evaluaciones.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEvaluacionesInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<EvaluacionesDbContext>(o => o.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));
        services.AddScoped<IActividadRepository, ActividadRepository>();
        services.AddScoped<ICalificacionRepository, CalificacionRepository>();
        services.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();
        return services;
    }
}
