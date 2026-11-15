using Asistente.Application.Puertos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Unillanos.Pruebas.Compartidas;

namespace Asistente.IntegrationTests.Infraestructura;

/// <summary>
/// El servicio real con SQL Server real. El gateway es WireMock. El modelo es el doble guionizado,
/// o el cliente Groq real apuntado a otro WireMock cuando <paramref name="urlLlm"/> no es null.
/// </summary>
public sealed class AsistenteApiFactory(string cadenaSql, LlavesPrueba llaves, string urlGateway, string? urlLlm) : WebApplicationFactory<Program>
{
    public RelojFijo Reloj { get; } = new(RelojFijo.AhoraTruncado());
    public LlmGuionizado Llm { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Default", cadenaSql);
        builder.UseSetting("Jwt:PublicKeyPath", llaves.RutaLlavePublica);
        builder.UseSetting("Services:GatewayBaseUrl", urlGateway);
        builder.UseSetting("Llm:BaseUrl", urlLlm ?? "http://llm.invalid/v1");
        builder.UseSetting("Llm:ApiKey", "llave-falsa-de-pruebas");
        builder.UseSetting("Llm:Model", "modelo-de-pruebas");
        builder.UseSetting("Llm:TimeoutSeconds", "1");
        builder.UseSetting("Asistente:MensajesPorMinuto", "1000");
        builder.UseSetting("Database:AplicarMigraciones", "true");

        builder.ConfigureTestServices(s =>
        {
            s.AddSingleton<TimeProvider>(Reloj);
            if (urlLlm is null) s.AddSingleton<ILlmClient>(Llm);
        });
    }
}
