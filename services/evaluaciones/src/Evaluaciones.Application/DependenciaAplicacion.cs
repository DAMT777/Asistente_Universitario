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
        // Calificar, retroalimentar, modificar (CU-05 a CU-07) y publicar notas de actividad (CU-08).
        servicios.AddScoped<CalificarActividad>();
        servicios.AddScoped<PublicarCalificacionesDeActividad>();
        // Rol estudiante (CU-15, CU-16) y consulta interna para el servicio de entregas.
        servicios.AddScoped<ObtenerNotasActividades>();
        servicios.AddScoped<ObtenerMatrizNotas>();
        servicios.AddScoped<ObtenerActividadInterna>();
        return servicios;
    }
}
