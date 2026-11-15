using Asistente.Application.Puertos;
using Asistente.Infrastructure.Gateway;
using Asistente.Infrastructure.Llm;
using Asistente.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Asistente.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfraestructuraAsistente(this IServiceCollection services, IConfiguration configuracion)
    {
        services.AddDbContext<AsistenteDbContext>(o => o.UseSqlServer(
            configuracion.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Falta la variable ConnectionStrings__Default.")));
        services.AddScoped<IAccionesRepositorio, AccionesRepositorio>();
        services.AddScoped<IAuditoria, AuditoriaRepositorio>();

        services.AddOptions<LlmOpciones>().BindConfiguration(LlmOpciones.Seccion).ValidateDataAnnotations().ValidateOnStart();
        services.AddHttpClient<ILlmClient, GroqLlmClient>((sp, http) =>
            GroqLlmClient.Configurar(http, sp.GetRequiredService<IOptions<LlmOpciones>>().Value));

        services.AddOptions<ServiciosOpciones>().BindConfiguration(ServiciosOpciones.Seccion).ValidateDataAnnotations().ValidateOnStart();
        services.AddTransient<ReenviarJwtHandler>();
        services.AddHttpClient<IGatewayCliente, GatewayHttpCliente>((sp, http) =>
            {
                var o = sp.GetRequiredService<IOptions<ServiciosOpciones>>().Value;
                http.BaseAddress = new Uri(o.GatewayBaseUrl.TrimEnd('/') + "/");
                http.Timeout = TimeSpan.FromSeconds(o.GatewayTimeoutSeconds);
            })
            .AddHttpMessageHandler<ReenviarJwtHandler>()
            .AddHttpMessageHandler<Unillanos.ServiceDefaults.Correlacion.PropagarCorrelacionHandler>();
        return services;
    }
}
