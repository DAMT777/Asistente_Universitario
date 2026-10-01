using Azure.Storage.Blobs;
using Testcontainers.Azurite;
using Unillanos.Entregas.Infrastructure.Almacen;

namespace Unillanos.Entregas.IntegrationTests;

/// <summary>El adaptador de Blob Storage contra Azurite real (Testcontainers).</summary>
public sealed class BlobAlmacenArchivosTests : IAsyncLifetime
{
    // --skipApiVersionCheck: Azure.Storage.Blobs 12.30 usa una versión de API más nueva que la que reconoce Azurite.
    private readonly AzuriteContainer _azurite = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:latest")
        .WithCommand("--skipApiVersionCheck")
        .Build();
    private BlobContainerClient _contenedor = null!;

    public async ValueTask InitializeAsync()
    {
        await _azurite.StartAsync();
        _contenedor = new BlobContainerClient(_azurite.GetConnectionString(), "entregas-pruebas");
    }

    public ValueTask DisposeAsync() => _azurite.DisposeAsync();

    [Fact]
    public async Task Guarda_con_content_type_y_elimina()
    {
        var almacen = new BlobAlmacenArchivos(_contenedor);
        var ruta = $"{Guid.NewGuid()}/{Guid.NewGuid()}/{Guid.NewGuid()}";

        await almacen.GuardarAsync(ruta, new MemoryStream("%PDF-1.7 hola"u8.ToArray()), "application/pdf", CancellationToken.None);

        var blob = _contenedor.GetBlobClient(ruta);
        var propiedades = await blob.GetPropertiesAsync();
        Assert.Equal("application/pdf", propiedades.Value.ContentType);
        Assert.Equal(13, propiedades.Value.ContentLength);

        await almacen.EliminarAsync(ruta, CancellationToken.None);
        Assert.False((await blob.ExistsAsync()).Value);
    }

    [Fact]
    public async Task Health_check_reporta_sano_con_Azurite_arriba()
    {
        var resultado = await new BlobSaludCheck(_contenedor).CheckHealthAsync(new());

        Assert.Equal(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy, resultado.Status);
    }
}
