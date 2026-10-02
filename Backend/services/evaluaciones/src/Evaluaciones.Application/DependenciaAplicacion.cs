using Evaluaciones.Application.CasosDeUso;
using Microsoft.Extensions.DependencyInjection;

namespace Evaluaciones.Application;

public static class DependenciaAplicacion
{
    public static IServiceCollection AddAplicacion(this IServiceCollection servicios)
    {
        servicios.AddScoped<ConsultarPonderado>();
        servicios.AddScoped<PublicarCorte>();
        servicios.AddScoped<CorregirCorte>();
        servicios.AddScoped<ListarCursos>();
        servicios.AddScoped<ListarActividadesDelCurso>();
        servicios.AddScoped<ListarEstudiantesDelCurso>();
        servicios.AddScoped<ListarCalificacionesDeActividad>();
        return servicios;
    }
}
