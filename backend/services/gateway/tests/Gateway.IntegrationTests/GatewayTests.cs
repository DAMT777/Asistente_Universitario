using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Gateway.IntegrationTests;

/// <summary>
/// El gateway se prueba con la configuración real de rutas y con los servicios de destino apagados
/// (puertos 5001 a 5003 sin escuchar). Lo que se verifica es lo que decide el gateway por sí solo:
/// token, CORS, bloqueo de /internal, correlación y el error cuando un servicio no responde.
/// </summary>
public sealed class FabricaGateway : WebApplicationFactory<Program>
{
    private readonly RSA _llave = RSA.Create(2048);
    private readonly string _rutaPublica = Path.Combine(Path.GetTempPath(), $"jwt-public-{Guid.NewGuid():N}.pem");

    public FabricaGateway() => File.WriteAllText(_rutaPublica, _llave.ExportSubjectPublicKeyInfoPem());

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Jwt:PublicKeyPath", _rutaPublica);
        // Puertos reservados que nadie escucha: simulan servicios caídos.
        builder.UseSetting("ReverseProxy:Clusters:usuarios:Destinations:principal:Address", "http://127.0.0.1:1/");
        builder.UseSetting("ReverseProxy:Clusters:evaluaciones:Destinations:principal:Address", "http://127.0.0.1:1/");
        builder.UseSetting("ReverseProxy:Clusters:entregas:Destinations:principal:Address", "http://127.0.0.1:1/");
    }

    public HttpClient ClienteConToken(string rol = "PROFESOR", TimeSpan? vigencia = null)
    {
        var ahora = DateTime.UtcNow;
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "unillanos-usuarios",
            Audience = "unillanos-notas",
            Subject = new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString()), new Claim("rol", rol)]),
            NotBefore = ahora.AddMinutes(-10),
            IssuedAt = ahora.AddMinutes(-10),
            Expires = ahora.Add(vigencia ?? TimeSpan.FromMinutes(30)),
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(_llave), SecurityAlgorithms.RsaSha256)
        });
        var cliente = CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        _llave.Dispose();
        try { File.Delete(_rutaPublica); } catch (IOException) { /* temporal */ }
    }
}

public class GatewayTests
{
    private static async Task<JsonElement> Json(HttpResponseMessage r) => await r.Content.ReadFromJsonAsync<JsonElement>();

    [Theory]
    [InlineData("/cursos")]
    [InlineData("/cursos/c0000000-0000-0000-0000-000000000001/ponderado")]
    [InlineData("/mis-notas")]
    [InlineData("/mis-entregas")]
    [InlineData("/auth/me")]
    public async Task Las_rutas_protegidas_sin_token_responden_401_con_el_formato_comun(string ruta)
    {
        await using var f = new FabricaGateway();

        var r = await f.CreateClient().GetAsync(ruta);

        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        Assert.Equal("NO_AUTENTICADO", (await Json(r)).GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task Un_token_expirado_responde_401_TOKEN_EXPIRADO()
    {
        await using var f = new FabricaGateway();

        var r = await f.ClienteConToken(vigencia: TimeSpan.FromMinutes(-5)).GetAsync("/cursos");

        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        Assert.Equal("TOKEN_EXPIRADO", (await Json(r)).GetProperty("codigo").GetString());
    }

    [Theory]
    [InlineData("/internal/actividades/d0000000-0000-0000-0000-000000000001")]
    [InlineData("/internal/lo-que-sea")]
    public async Task E17_internal_esta_bloqueado_aunque_se_tenga_token(string ruta)
    {
        await using var f = new FabricaGateway();

        var conToken = await f.ClienteConToken().GetAsync(ruta);
        var sinToken = await f.CreateClient().GetAsync(ruta);

        Assert.Equal(HttpStatusCode.NotFound, conToken.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, sinToken.StatusCode);
    }

    [Fact]
    public async Task Login_no_exige_token_y_llega_a_la_etapa_de_proxy()
    {
        await using var f = new FabricaGateway();

        var r = await f.CreateClient().PostAsJsonAsync("/auth/login", new { usuario = "P0001", password = "x" });

        // No es 401: el gateway lo dejó pasar. Como usuarios está apagado, el error es el de servicio caído.
        Assert.Equal(HttpStatusCode.ServiceUnavailable, r.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/cursos")]
    [InlineData("GET", "/cursos/c0000000-0000-0000-0000-000000000001/estudiantes")]
    [InlineData("POST", "/cursos/c0000000-0000-0000-0000-000000000001/cortes/1/publicar")]
    [InlineData("PUT", "/cursos/c0000000-0000-0000-0000-000000000001/cortes/1/estudiantes/b0000000-0000-0000-0000-000000000001")]
    [InlineData("GET", "/actividades/d0000000-0000-0000-0000-000000000001/calificaciones")]
    [InlineData("GET", "/actividades/d0000000-0000-0000-0000-000000000001/entregas")]
    [InlineData("GET", "/mis-notas")]
    [InlineData("GET", "/mis-entregas")]
    [InlineData("DELETE", "/entregas/e0000000-0000-0000-0000-000000000001")]
    public async Task Con_token_cada_ruta_conocida_se_enruta_y_si_el_servicio_cae_responde_503_SERVICIO_NO_DISPONIBLE(string metodo, string ruta)
    {
        await using var f = new FabricaGateway();

        var r = await f.ClienteConToken().SendAsync(new HttpRequestMessage(new HttpMethod(metodo), ruta));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, r.StatusCode);
        Assert.Equal("SERVICIO_NO_DISPONIBLE", (await Json(r)).GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task Una_ruta_desconocida_responde_404_y_no_se_enruta()
    {
        await using var f = new FabricaGateway();

        var r = await f.ClienteConToken().GetAsync("/no-existe");

        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task Propaga_el_identificador_de_correlacion_que_llega_y_crea_uno_si_falta()
    {
        await using var f = new FabricaGateway();
        var cliente = f.CreateClient();

        var generado = await cliente.GetAsync("/health/live");
        var solicitud = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        solicitud.Headers.Add("X-Correlation-Id", "abc-123");
        var conservado = await cliente.SendAsync(solicitud);

        Assert.False(string.IsNullOrEmpty(generado.Headers.GetValues("X-Correlation-Id").Single()));
        Assert.Equal("abc-123", conservado.Headers.GetValues("X-Correlation-Id").Single());
    }

    [Fact]
    public async Task Cors_permite_al_frontend_configurado_y_responde_el_preflight_sin_token()
    {
        await using var f = new FabricaGateway();
        var preflight = new HttpRequestMessage(HttpMethod.Options, "/cursos");
        preflight.Headers.Add("Origin", "http://localhost:5173");
        preflight.Headers.Add("Access-Control-Request-Method", "GET");
        preflight.Headers.Add("Access-Control-Request-Headers", "authorization");

        var r = await f.CreateClient().SendAsync(preflight);

        Assert.True(r.IsSuccessStatusCode, $"El preflight respondió {(int)r.StatusCode}.");
        Assert.Equal("http://localhost:5173", r.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Cors_no_permite_otros_origenes()
    {
        await using var f = new FabricaGateway();
        var preflight = new HttpRequestMessage(HttpMethod.Options, "/cursos");
        preflight.Headers.Add("Origin", "http://sitio-malicioso.example");
        preflight.Headers.Add("Access-Control-Request-Method", "GET");

        var r = await f.CreateClient().SendAsync(preflight);

        Assert.False(r.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
