using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Entregas.Application.CasosDeUso;
using Entregas.Application.Comun;

namespace Entregas.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAplicacionEntregas(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<SubirEntregaValidador>(ServiceLifetime.Singleton);
        services.AddScoped<ConsultorActividades>();
        services.AddScoped<ReceptorArchivos>();
        services.AddScoped<PersistenciaConCompensacion>();
        services.AddScoped<SubirEntrega>();
        services.AddScoped<EditarEntrega>();
        services.AddScoped<AnularEntrega>();
        services.AddScoped<ListarMisEntregas>();
        services.AddScoped<DescargarArchivo>();
        services.AddScoped<ListarEntregasDeActividad>();
        return services;
    }
}
