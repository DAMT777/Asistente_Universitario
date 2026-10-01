using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Entregas.Application.Puertos;

namespace Entregas.Infrastructure.Almacen;

/// <summary>Archivos de entregas en Azure Blob Storage (Azurite en local).</summary>
public sealed class BlobAlmacenArchivos(BlobContainerClient contenedor) : IAlmacenArchivos
{
    private readonly Lazy<Task> _contenedorListo = new(() => contenedor.CreateIfNotExistsAsync());

    public async Task GuardarAsync(string ruta, Stream contenido, string contentType, CancellationToken ct)
    {
        await _contenedorListo.Value;
        var opciones = new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } };
        await contenedor.GetBlobClient(ruta).UploadAsync(contenido, opciones, ct);
    }

    public Task EliminarAsync(string ruta, CancellationToken ct)
        => contenedor.GetBlobClient(ruta).DeleteIfExistsAsync(cancellationToken: ct);
}

public sealed class BlobSaludCheck(BlobContainerClient contenedor) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Basta con que el servicio responda; el contenedor se crea en la primera subida.
            await contenedor.ExistsAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Blob Storage no responde.", ex);
        }
    }
}
