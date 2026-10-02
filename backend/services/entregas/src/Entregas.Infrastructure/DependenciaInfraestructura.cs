using Entregas.Application.Abstracciones;
using Entregas.Infrastructure.Archivos;
using Entregas.Infrastructure.Clientes;
using Entregas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Entregas.Infrastructure;

public static class DependenciaInfraestructura
{
    /// <summary>Solo desarrollo: base en memoria con datos semilla (Database:UseInMemory=true).</summary>
    public static bool UsaBaseEnMemoria(IConfiguration configuracion) =>
        bool.TryParse(configuracion["Database:UseInMemory"], out var enMemoria) && enMemoria;

    public static IServiceCollection AddInfraestructura(
        this IServiceCollection servicios, IConfiguration configuracion, IHostEnvironment entorno)
    {
        if (UsaBaseEnMemoria(configuracion))
        {
            var nombre = configuracion["Database:InMemoryName"] ?? "entregas_db";
            servicios.AddDbContext<EntregasDbContext>(o => o.UseInMemoryDatabase(nombre));
        }
        else
        {
            var conexion = configuracion.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Falta ConnectionStrings:Default (variable ConnectionStrings__Default).");
            servicios.AddDbContext<EntregasDbContext>(o => o.UseSqlServer(conexion));
        }

        servicios.AddScoped<IEntregaRepository, EntregaRepository>();

        // Disco local mientras no se conecta Blob Storage (Storage__ConnectionString, sección 10.5).
        var carpeta = Path.GetFullPath(configuracion["Storage:LocalPath"] ?? "almacen-entregas", entorno.ContentRootPath);
        servicios.AddSingleton(new AlmacenArchivosLocal(carpeta));
        servicios.AddSingleton<IAlmacenArchivos>(sp => sp.GetRequiredService<AlmacenArchivosLocal>());

        var urlEvaluaciones = configuracion["Services:EvaluacionesBaseUrl"]
            ?? throw new InvalidOperationException("Falta Services:EvaluacionesBaseUrl (variable Services__EvaluacionesBaseUrl).");
        var claveServicio = configuracion["ServiceKey"]
            ?? throw new InvalidOperationException("Falta ServiceKey (variable ServiceKey).");

        servicios.AddHttpClient<IEvaluacionesCliente, EvaluacionesClienteHttp>(http =>
        {
            http.BaseAddress = new Uri(urlEvaluaciones.TrimEnd('/') + "/");
            http.Timeout = TimeSpan.FromSeconds(5);
            http.DefaultRequestHeaders.Add(EvaluacionesClienteHttp.EncabezadoClave, claveServicio);
        });

        return servicios;
    }
}
