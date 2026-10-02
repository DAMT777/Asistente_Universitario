using Entregas.Application.Abstracciones;

namespace Entregas.Infrastructure.Archivos;

/// <summary>
/// Almacenamiento en disco local para desarrollo (la guía permite empezar así y pasar a Blob Storage después).
/// Las rutas siempre quedan dentro de la carpeta raíz: una ruta que intente salirse se trata como inexistente.
/// </summary>
public sealed class AlmacenArchivosLocal(string carpetaRaiz) : IAlmacenArchivos
{
    private readonly string _raiz = Path.GetFullPath(carpetaRaiz);

    public Task<Stream?> AbrirAsync(string ruta, CancellationToken ct)
    {
        var completa = RutaSegura(ruta);
        Stream? contenido = completa is not null && File.Exists(completa) ? File.OpenRead(completa) : null;
        return Task.FromResult(contenido);
    }

    /// <summary>Solo para la semilla de desarrollo y, más adelante, para CU-13.</summary>
    public async Task GuardarAsync(string ruta, byte[] contenido, CancellationToken ct)
    {
        var completa = RutaSegura(ruta) ?? throw new ArgumentException("Ruta de archivo inválida.", nameof(ruta));
        Directory.CreateDirectory(Path.GetDirectoryName(completa)!);
        await File.WriteAllBytesAsync(completa, contenido, ct);
    }

    private string? RutaSegura(string ruta)
    {
        var completa = Path.GetFullPath(Path.Combine(_raiz, ruta));
        return completa.StartsWith(_raiz + Path.DirectorySeparatorChar, StringComparison.Ordinal) ? completa : null;
    }
}
