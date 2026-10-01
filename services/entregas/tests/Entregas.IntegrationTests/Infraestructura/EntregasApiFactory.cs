using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Entregas.Application.Puertos;
using Unillanos.Pruebas.Compartidas;

namespace Entregas.IntegrationTests.Infraestructura;

/// <summary>El servicio real con SQL Server real; el almacén de archivos y el reloj son dobles.</summary>
public sealed class EntregasApiFactory(string cadenaSql, LlavesPrueba llaves, string? urlEvaluaciones) : WebApplicationFactory<Program>
{
    public const long MaxBytesPrueba = 64 * 1024;
    public const string ClaveServicio = "clave-de-servicio-solo-para-pruebas";

    public RelojFijo Reloj { get; } = new(RelojFijo.AhoraTruncado());
    public AlmacenEnMemoria Almacen { get; } = new();
    public EvaluacionesFalso Evaluaciones => _evaluaciones ??= new EvaluacionesFalso(Reloj);
    private EvaluacionesFalso? _evaluaciones;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Default", cadenaSql);
        builder.UseSetting("Jwt:PublicKeyPath", llaves.RutaLlavePublica);
        builder.UseSetting("ServiceKey", ClaveServicio);
        builder.UseSetting("Services:EvaluacionesBaseUrl", urlEvaluaciones ?? "http://evaluaciones.invalid");
        builder.UseSetting("Storage:ConnectionString", "UseDevelopmentStorage=true");
        builder.UseSetting("Entregas:MaxBytes", MaxBytesPrueba.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.UseSetting("Database:AplicarMigraciones", "true");

        builder.ConfigureTestServices(s =>
        {
            s.AddSingleton<TimeProvider>(Reloj);
            s.AddSingleton<IAlmacenArchivos>(Almacen);
            if (urlEvaluaciones is null) s.AddSingleton<IEvaluacionesClient>(Evaluaciones);
        });
    }
}
