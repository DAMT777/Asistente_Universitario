using Microsoft.Extensions.Options;
using Entregas.Application.Puertos;
using Entregas.Domain;

namespace Entregas.Application.Comun;

/// <summary>Valida el archivo entrante (tamaño, extensión, firma) y lo guarda con una ruta basada en GUID.</summary>
public sealed class ReceptorArchivos(IAlmacenArchivos almacen, IOptions<EntregasOpciones> opciones)
{
    public async Task<TipoArchivo> ValidarAsync(ArchivoEntrante archivo, CancellationToken ct)
    {
        var cabecera = new byte[TipoArchivo.LongitudCabecera];
        var leidos = await archivo.Contenido.ReadAtLeastAsync(cabecera, cabecera.Length, throwOnEndOfStream: false, ct);
        archivo.Contenido.Position = 0;
        return ValidadorArchivo.Validar(archivo.NombreOriginal, archivo.Tamano, cabecera.AsSpan(0, leidos), opciones.Value.MaxBytes);
    }

    public async Task<ArchivoGuardado> GuardarAsync(Guid actividadId, Guid estudianteId, ArchivoEntrante archivo, TipoArchivo tipo, CancellationToken ct)
    {
        // Nunca se usa el nombre original en la ruta: solo se guarda como metadato.
        var ruta = $"{actividadId}/{estudianteId}/{Guid.NewGuid()}";
        archivo.Contenido.Position = 0;
        await almacen.GuardarAsync(ruta, archivo.Contenido, tipo.ContentType, ct);
        return new ArchivoGuardado(archivo.NombreOriginal, archivo.Tamano, ruta);
    }
}
