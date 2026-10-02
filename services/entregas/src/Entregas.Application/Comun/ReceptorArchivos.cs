using Microsoft.Extensions.Options;
using Entregas.Application.Puertos;
using Entregas.Domain;

namespace Entregas.Application.Comun;

/// <summary>
/// Valida los archivos entrantes (cantidad, tamaño total, y por archivo tamaño, extensión y firma)
/// y los guarda con rutas basadas en GUID.
/// </summary>
public sealed class ReceptorArchivos(IAlmacenArchivos almacen, IOptions<EntregasOpciones> opciones)
{
    public async Task<IReadOnlyList<TipoArchivo>> ValidarAsync(IReadOnlyList<ArchivoEntrante> archivos, CancellationToken ct)
    {
        var limites = opciones.Value.Limites;
        limites.ValidarConjunto(archivos.Select(a => a.Tamano).ToList());

        var tipos = new List<TipoArchivo>(archivos.Count);
        foreach (var archivo in archivos)
        {
            var cabecera = new byte[TipoArchivo.LongitudCabecera];
            var leidos = await archivo.Contenido.ReadAtLeastAsync(cabecera, cabecera.Length, throwOnEndOfStream: false, ct);
            archivo.Contenido.Position = 0;
            tipos.Add(ValidadorArchivo.Validar(archivo.NombreOriginal, archivo.Tamano, cabecera.AsSpan(0, leidos), limites.MaxBytesPorArchivo));
        }
        return tipos;
    }

    /// <summary>Guarda todos o ninguno: si uno falla, borra los que alcanzaron a subirse.</summary>
    public async Task<IReadOnlyList<ArchivoGuardado>> GuardarAsync(
        Guid actividadId, Guid estudianteId, IReadOnlyList<ArchivoEntrante> archivos, IReadOnlyList<TipoArchivo> tipos, CancellationToken ct)
    {
        var guardados = new List<ArchivoGuardado>(archivos.Count);
        try
        {
            for (var i = 0; i < archivos.Count; i++)
            {
                // Nunca se usa el nombre original en la ruta: solo se guarda como metadato.
                var ruta = $"{actividadId}/{estudianteId}/{Guid.NewGuid()}";
                archivos[i].Contenido.Position = 0;
                await almacen.GuardarAsync(ruta, archivos[i].Contenido, tipos[i].ContentType, ct);
                guardados.Add(new ArchivoGuardado(archivos[i].NombreOriginal, archivos[i].Tamano, tipos[i].ContentType, ruta));
            }
            return guardados;
        }
        catch
        {
            foreach (var g in guardados) await almacen.EliminarAsync(g.RutaBlob, CancellationToken.None);
            throw;
        }
    }
}
