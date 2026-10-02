namespace Entregas.Application.Puertos;

/// <summary>Almacén de archivos de las entregas (Blob Storage). Nunca la base de datos.</summary>
public interface IAlmacenArchivos
{
    Task GuardarAsync(string ruta, Stream contenido, string contentType, CancellationToken ct);
    /// <summary>Abre el contenido para leerlo. Devuelve null si el blob no existe.</summary>
    Task<Stream?> AbrirAsync(string ruta, CancellationToken ct);
    Task EliminarAsync(string ruta, CancellationToken ct);
}
