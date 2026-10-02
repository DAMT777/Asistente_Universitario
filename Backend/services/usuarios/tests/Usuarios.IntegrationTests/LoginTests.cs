using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Usuarios.IntegrationTests;

/// <summary>Cada prueba genera su propio par de llaves, como lo haría cada integrante con infra/scripts.</summary>
public sealed class FabricaUsuarios : WebApplicationFactory<Program>
{
    private readonly RSA _llave = RSA.Create(2048);
    private readonly string _rutaPrivada = Path.Combine(Path.GetTempPath(), $"jwt-private-{Guid.NewGuid():N}.pem");

    public FabricaUsuarios() => File.WriteAllText(_rutaPrivada, _llave.ExportPkcs8PrivateKeyPem());

    /// <summary>Solo la parte pública, que es lo que reciben los demás servicios.</summary>
    public SecurityKey LlavePublica
    {
        get
        {
            var publica = RSA.Create();
            publica.ImportSubjectPublicKeyInfo(_llave.ExportSubjectPublicKeyInfo(), out _);
            return new RsaSecurityKey(publica);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Jwt:PrivateKeyPath", _rutaPrivada);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        _llave.Dispose();
        try { File.Delete(_rutaPrivada); } catch (IOException) { /* temporal */ }
    }
}

public class LoginTests
{
    private static Task<HttpResponseMessage> Login(HttpClient c, string? usuario, string? password) =>
        c.PostAsJsonAsync("/auth/login", new { usuario, password });

    private static async Task<JsonElement> Json(HttpResponseMessage r) => await r.Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task E01_el_profesor_inicia_sesion_y_recibe_un_token_con_su_rol()
    {
        await using var f = new FabricaUsuarios();

        var r = await Login(f.CreateClient(), "P0001", "Demo1234!");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var cuerpo = await Json(r);
        Assert.Equal("Bearer", cuerpo.GetProperty("tokenType").GetString());
        Assert.Equal(3600, cuerpo.GetProperty("expiresIn").GetInt32());
        var usuario = cuerpo.GetProperty("usuario");
        Assert.Equal("a0000000-0000-0000-0000-000000000001", usuario.GetProperty("id").GetString());
        Assert.Equal("Profesor Demo", usuario.GetProperty("nombre").GetString());
        Assert.Equal("PROFESOR", usuario.GetProperty("rol").GetString());
        Assert.False(usuario.TryGetProperty("passwordHash", out _));
    }

    [Fact]
    public async Task E01_el_estudiante_inicia_sesion_con_su_codigo_documento_o_correo()
    {
        await using var f = new FabricaUsuarios();
        var c = f.CreateClient();

        foreach (var identificador in new[] { "E0001", "e0001", "1000000011", "ana@demo.unillanos.edu.co" })
        {
            var r = await Login(c, identificador, "Demo1234!");
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Equal("ESTUDIANTE", (await Json(r)).GetProperty("usuario").GetProperty("rol").GetString());
        }
    }

    [Fact]
    public async Task El_token_trae_los_claims_acordados_y_se_verifica_solo_con_la_llave_publica()
    {
        await using var f = new FabricaUsuarios();
        var token = (await Json(await Login(f.CreateClient(), "E0002", "Demo1234!"))).GetProperty("accessToken").GetString()!;

        var validacion = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = "unillanos-usuarios",
            ValidAudience = "unillanos-notas",
            IssuerSigningKey = f.LlavePublica,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
        });

        Assert.True(validacion.IsValid, validacion.Exception?.Message);
        var jwt = (JsonWebToken)validacion.SecurityToken;
        Assert.Equal("b0000000-0000-0000-0000-000000000002", jwt.GetClaim("sub").Value);
        Assert.Equal("ESTUDIANTE", jwt.GetClaim("rol").Value);
        Assert.Equal("Luis Demo", jwt.GetClaim("nombre").Value);
        Assert.Equal(TimeSpan.FromMinutes(60), jwt.ValidTo - jwt.ValidFrom);
    }

    [Fact]
    public async Task E02_contrasena_erronea_responde_401_NO_AUTENTICADO()
    {
        await using var f = new FabricaUsuarios();

        var r = await Login(f.CreateClient(), "P0001", "otra-clave");

        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        var error = await Json(r);
        Assert.Equal("NO_AUTENTICADO", error.GetProperty("codigo").GetString());
        Assert.Equal(401, error.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Un_usuario_inexistente_recibe_el_mismo_mensaje_que_una_clave_erronea()
    {
        await using var f = new FabricaUsuarios();
        var c = f.CreateClient();

        var inexistente = await Json(await Login(c, "nadie", "Demo1234!"));
        var claveErronea = await Json(await Login(c, "P0001", "mala"));

        Assert.Equal(claveErronea.GetProperty("mensaje").GetString(), inexistente.GetProperty("mensaje").GetString());
    }

    [Theory]
    [InlineData(null, "Demo1234!")]
    [InlineData("P0001", null)]
    [InlineData("   ", "Demo1234!")]
    [InlineData("P0001", "")]
    public async Task Faltan_credenciales_responde_400(string? usuario, string? password)
    {
        await using var f = new FabricaUsuarios();

        var r = await Login(f.CreateClient(), usuario, password);

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("VALIDACION_FALLIDA", (await Json(r)).GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task Auth_me_devuelve_los_datos_del_usuario_del_token()
    {
        await using var f = new FabricaUsuarios();
        var c = f.CreateClient();
        var token = (await Json(await Login(c, "P0001", "Demo1234!"))).GetProperty("accessToken").GetString()!;
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var r = await c.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var yo = await Json(r);
        Assert.Equal("Profesor Demo", yo.GetProperty("nombre").GetString());
        Assert.Equal("PROFESOR", yo.GetProperty("rol").GetString());
        Assert.Equal("P0001", yo.GetProperty("codigoInstitucional").GetString());
    }

    [Fact]
    public async Task Auth_me_sin_token_responde_401()
    {
        await using var f = new FabricaUsuarios();

        var r = await f.CreateClient().GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        Assert.Equal("NO_AUTENTICADO", (await Json(r)).GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task Fuera_de_desarrollo_no_hay_usuarios_semilla()
    {
        await using var base_ = new FabricaUsuarios();
        await using var f = base_.WithWebHostBuilder(b => b.UseEnvironment("Production"));

        // En producción tampoco hay llave en el archivo de desarrollo, pero la de la fábrica sigue aplicando.
        var r = await Login(f.CreateClient(), "P0001", "Demo1234!");

        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }
}
