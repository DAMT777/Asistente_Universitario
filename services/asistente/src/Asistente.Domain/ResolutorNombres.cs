using System.Globalization;
using System.Text;

namespace Asistente.Domain;

public enum TipoCoincidencia
{
    Ninguna,
    Unica,
    Varias,
}

public sealed record Candidato(Guid Id, string Nombre, string? Codigo = null);

public sealed record ResultadoResolucion(TipoCoincidencia Tipo, IReadOnlyList<Candidato> Candidatos)
{
    public Candidato Unico => Tipo == TipoCoincidencia.Unica ? Candidatos[0] : throw new InvalidOperationException("No hay una única coincidencia.");
}

/// <summary>
/// Resuelve lo que propone el modelo ("Luis", "ana demo", "E0002", "taller 1") contra una lista real
/// (inscritos del curso, actividades). Nunca adivina: con cero o varias coincidencias hay que preguntar.
/// </summary>
public static class ResolutorNombres
{
    public static ResultadoResolucion Resolver(string? consulta, IEnumerable<Candidato> candidatos)
    {
        var lista = candidatos.ToList();
        var buscado = Normalizar(consulta);
        if (buscado.Length == 0) return new(TipoCoincidencia.Ninguna, []);

        // 1) Código o nombre exacto.
        var exactos = lista.Where(c => CoincideExacto(c, buscado)).ToList();
        if (exactos.Count > 0) return Clasificar(exactos);

        // 2) Todas las palabras buscadas aparecen como palabras del nombre.
        var palabras = buscado.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var parciales = lista.Where(c =>
        {
            var delNombre = Normalizar(c.Nombre).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return palabras.All(p => delNombre.Contains(p));
        }).ToList();
        return Clasificar(parciales);
    }

    private static bool CoincideExacto(Candidato candidato, string buscado)
    {
        var nombre = Normalizar(candidato.Nombre);
        var codigo = Normalizar(candidato.Codigo);
        return buscado == nombre || buscado == codigo || (codigo.Length > 0 &&
            (buscado == $"{nombre} {codigo}" || buscado == $"{nombre} ({codigo})" || buscado == $"{codigo} {nombre}"));
    }

    private static ResultadoResolucion Clasificar(List<Candidato> encontrados) => encontrados.Count switch
    {
        0 => new(TipoCoincidencia.Ninguna, []),
        1 => new(TipoCoincidencia.Unica, encontrados),
        _ => new(TipoCoincidencia.Varias, encontrados),
    };

    /// <summary>Minúsculas, sin tildes y con espacios simples.</summary>
    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return "";
        var sinTildes = new StringBuilder();
        foreach (var c in texto.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sinTildes.Append(c);
        }
        return string.Join(' ', sinTildes.ToString().ToLowerInvariant()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
