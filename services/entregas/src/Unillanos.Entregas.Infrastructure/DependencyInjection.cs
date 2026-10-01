using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Unillanos.Entregas.Application.Puertos;
using Unillanos.Entregas.Infrastructure.Almacen;
using Unillanos.Entregas.Infrastructure.Evaluaciones;
using Unillanos.Entregas.Infrastructure.Persistencia;

namespace Unillanos.Entregas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfraestructuraEntregas(this IServiceCollection services, IConfiguration configuracion)
    {
        services.AddDbContext<EntregasDbContext>(o => o.UseSqlServer(
            configuracion.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Falta la variable ConnectionStrings__Default.")));
        services.AddScoped<IEntregaRepositorio, EntregaRepositorio>();
        services.AddScoped<InicializadorBaseDatos>();

        services.AddOptions<AlmacenOpciones>().BindConfiguration(AlmacenOpciones.Seccion).ValidateDataAnnotations().ValidateOnStart();
        services.AddSingleton(sp =>
        {
            var o = sp.GetRequiredService<IOptions<AlmacenOpciones>>().Value;
            return new BlobContainerClient(o.ConnectionString, o.Container);
        });
        services.AddSingleton<IAlmacenArchivos, BlobAlmacenArchivos>();
        return services;
    }

    /// <summary>Cliente de Evaluaciones con X-Service-Key y timeout configurable (3 s por defecto), sin reintentos.</summary>
    public static IHttpClientBuilder AddClienteEvaluaciones(this IServiceCollection services)
    {
        services.AddOptions<ServiciosOpciones>().BindConfiguration(ServiciosOpciones.Seccion).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<ClaveServicioOpciones>().BindConfiguration("").ValidateDataAnnotations().ValidateOnStart();

        return services.AddHttpClient<IEvaluacionesClient, EvaluacionesHttpClient>((sp, http) =>
        {
            var servicios = sp.GetRequiredService<IOptions<ServiciosOpciones>>().Value;
            var clave = sp.GetRequiredService<IOptions<ClaveServicioOpciones>>().Value;
            http.BaseAddress = new Uri(servicios.EvaluacionesBaseUrl.TrimEnd('/') + "/");
            http.Timeout = TimeSpan.FromSeconds(servicios.EvaluacionesTimeoutSegundos);
            http.DefaultRequestHeaders.Add(ClaveServicioOpciones.Encabezado, clave.ServiceKey);
        });
    }
}
