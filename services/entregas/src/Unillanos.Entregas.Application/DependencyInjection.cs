using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Unillanos.Entregas.Application.CasosDeUso;
using Unillanos.Entregas.Application.Comun;

namespace Unillanos.Entregas.Application;

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
        return services;
    }
}
