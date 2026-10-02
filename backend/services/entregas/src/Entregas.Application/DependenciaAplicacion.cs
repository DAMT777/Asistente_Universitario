using Entregas.Application.CasosDeUso;
using Microsoft.Extensions.DependencyInjection;

namespace Entregas.Application;

public static class DependenciaAplicacion
{
    public static IServiceCollection AddAplicacion(this IServiceCollection servicios)
    {
        servicios.AddScoped<ListarEntregasDeActividad>();
        servicios.AddScoped<ListarMisEntregas>();
        servicios.AddScoped<DescargarArchivo>();
        return servicios;
    }
}
