using System.Text.RegularExpressions;

namespace Asistente.UnitTests;

/// <summary>
/// Conformidad con el diagrama: el asistente no depende de Usuarios, Evaluaciones ni Entregas
/// (ni de sus proyectos ni de sus rutas internas) y no conoce la llave de servicio.
/// </summary>
public sealed class ArquitecturaTests
{
    private static readonly string Raiz = BuscarRaiz();
    private static readonly string Src = Path.Combine(Raiz, "services", "asistente", "src");

    private static string BuscarRaiz()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "UnillanosNotas.slnx"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("No se encontró la raíz del repositorio.");
    }

    private static IEnumerable<string> Archivos(params string[] patrones) =>
        patrones.SelectMany(p => Directory.EnumerateFiles(Src, p, SearchOption.AllDirectories))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                        !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));

    [Fact]
    public void Los_proyectos_no_referencian_otros_servicios()
    {
        var csproj = Archivos("*.csproj").ToList();
        Assert.Equal(4, csproj.Count);
        foreach (var archivo in csproj)
        {
            var referencias = Regex.Matches(File.ReadAllText(archivo), "Include=\"([^\"]+)\"").Select(m => m.Groups[1].Value);
            Assert.DoesNotContain(referencias, r => Regex.IsMatch(r, "Usuarios|Evaluaciones|Entregas|Gateway", RegexOptions.IgnoreCase));
        }
    }

    [Fact]
    public void Configuracion_y_codigo_sin_llave_de_servicio_ni_rutas_internas()
    {
        var hallazgos = Archivos("*.json", "*.cs")
            .Where(f => !f.Contains("Migraciones"))
            .Where(f => Regex.IsMatch(File.ReadAllText(f), @"ServiceKey|X-Service-Key|/internal\b|""internal/", RegexOptions.IgnoreCase))
            .ToList();
        Assert.Empty(hallazgos);
    }

    [Fact]
    public void La_configuracion_no_apunta_a_otros_servicios_directamente()
    {
        var textos = Archivos("*.json").Select(File.ReadAllText);
        Assert.DoesNotContain(textos, t => Regex.IsMatch(t, @"localhost:500[123]|evaluaciones:8080|entregas:8080|usuarios:8080|EvaluacionesBaseUrl|EntregasBaseUrl"));
    }

    [Fact]
    public void Solo_infrastructure_conoce_al_proveedor_de_ia()
    {
        var fuera = Archivos("*.cs")
            .Where(f => !f.Contains($"Asistente.Infrastructure{Path.DirectorySeparatorChar}"))
            .Where(f => File.ReadAllText(f).Contains("Groq", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.Empty(fuera);
    }
}
