#:property PublishAot=false
// Emite un JWT RS256 de PRUEBA para los usuarios semilla, firmado con infra/keys/jwt-privada.pem.
// Sustituye al servicio de Usuarios mientras no esté disponible. Solo para desarrollo.
//
//   dotnet run infra/scripts/generar-token.cs -- ana
//   dotnet run infra/scripts/generar-token.cs -- profesor --minutos 5
//   dotnet run infra/scripts/generar-token.cs -- luis --expirado
//
// Usuarios: ana, luis, marta (ESTUDIANTE), profesor (PROFESOR), pedro (ESTUDIANTE no inscrito).
// Opciones: --llave <ruta PEM privada> (por defecto infra/keys/jwt-privada.pem), --minutos <n> (60), --expirado.
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

var usuarios = new Dictionary<string, (string Id, string Rol, string Nombre)>(StringComparer.OrdinalIgnoreCase)
{
    ["profesor"] = ("a0000000-0000-0000-0000-000000000001", "PROFESOR", "Laura Rincón"),
    ["ana"] = ("b0000000-0000-0000-0000-000000000001", "ESTUDIANTE", "Ana"),
    ["luis"] = ("b0000000-0000-0000-0000-000000000002", "ESTUDIANTE", "Luis"),
    ["marta"] = ("b0000000-0000-0000-0000-000000000003", "ESTUDIANTE", "Marta"),
    ["pedro"] = ("b0000000-0000-0000-0000-000000000099", "ESTUDIANTE", "Pedro (no inscrito)"),
};

if (args.Length == 0 || !usuarios.TryGetValue(args[0], out var usuario))
{
    Console.Error.WriteLine($"Uso: dotnet run infra/scripts/generar-token.cs -- <{string.Join("|", usuarios.Keys)}> [--minutos n] [--expirado] [--llave ruta]");
    return 1;
}

string? Opcion(string nombre) => Array.IndexOf(args, nombre) is var i and >= 0 && i + 1 < args.Length ? args[i + 1] : null;
var rutaLlave = Opcion("--llave") ?? Path.Combine(AppContext.GetData("EntryPointFileDirectoryPath") as string ?? ".", "..", "keys", "jwt-privada.pem");
var minutos = int.Parse(Opcion("--minutos") ?? "60", System.Globalization.CultureInfo.InvariantCulture);
var expirado = args.Contains("--expirado");

if (!File.Exists(rutaLlave))
{
    Console.Error.WriteLine($"No existe {rutaLlave}. Ejecute primero infra/scripts/generar-llaves.sh");
    return 1;
}

using var rsa = RSA.Create();
rsa.ImportFromPem(File.ReadAllText(rutaLlave));

var ahora = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - (expirado ? 2 * 3600 : 0);
var encabezado = new Dictionary<string, object> { ["alg"] = "RS256", ["typ"] = "JWT" };
var claims = new Dictionary<string, object>
{
    ["sub"] = usuario.Id,
    ["rol"] = usuario.Rol,
    ["nombre"] = usuario.Nombre,
    ["iss"] = "unillanos-usuarios",
    ["aud"] = "unillanos-notas",
    ["iat"] = ahora,
    ["nbf"] = ahora,
    ["exp"] = ahora + minutos * 60,
};

static string B64(byte[] datos) => Convert.ToBase64String(datos).TrimEnd('=').Replace('+', '-').Replace('/', '_');

var sinFirma = $"{B64(JsonSerializer.SerializeToUtf8Bytes(encabezado))}.{B64(JsonSerializer.SerializeToUtf8Bytes(claims))}";
var firma = rsa.SignData(Encoding.ASCII.GetBytes(sinFirma), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
Console.WriteLine($"{sinFirma}.{B64(firma)}");
return 0;
