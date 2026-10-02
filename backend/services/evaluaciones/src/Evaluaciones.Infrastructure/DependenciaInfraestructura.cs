using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Evaluaciones.Infrastructure;

public static class DependenciaInfraestructura
{
    /// <summary>
    /// Solo desarrollo: usa una base en memoria con datos semilla, sin SQL Server ni Docker.
    /// Se activa con Database:UseInMemory=true (variable Database__UseInMemory).
    /// </summary>
    public static bool UsaBaseEnMemoria(IConfiguration configuracion) =>
        bool.TryParse(configuracion["Database:UseInMemory"], out var enMemoria) && enMemoria;

    public static IServiceCollection AddInfraestructura(this IServiceCollection servicios, IConfiguration configuracion)
    {
        if (UsaBaseEnMemoria(configuracion))
        {
            // El nombre se puede cambiar para aislar bases en memoria dentro del mismo proceso (pruebas).
            var nombre = configuracion["Database:InMemoryName"] ?? "evaluaciones_db";
            servicios.AddDbContext<EvaluacionesDbContext>(o => o.UseInMemoryDatabase(nombre));
        }
        else
        {
            var conexion = configuracion.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Falta ConnectionStrings:Default (variable ConnectionStrings__Default).");
            servicios.AddDbContext<EvaluacionesDbContext>(o => o.UseSqlServer(conexion));
        }

        servicios.AddScoped<ICursoRepository, CursoRepository>();
        servicios.AddScoped<IInscripcionRepository, InscripcionRepository>();
        servicios.AddScoped<IActividadRepository, ActividadRepository>();
        servicios.AddScoped<ICalificacionRepository, CalificacionRepository>();
        servicios.AddScoped<IPublicacionCorteRepository, PublicacionCorteRepository>();
        servicios.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();
        return servicios;
    }
}
