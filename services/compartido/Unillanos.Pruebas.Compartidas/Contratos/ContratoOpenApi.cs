using System.Text.Json.Nodes;

namespace Unillanos.Pruebas.Compartidas.Contratos;

/// <summary>Un contrato de contracts/*.yaml cargado para validar respuestas reales del servicio.</summary>
public sealed class ContratoOpenApi
{
    private static readonly string[] Metodos = ["get", "put", "post", "delete", "patch"];
    private readonly JsonNode _documento;
    private readonly ValidadorEsquema _validador;

    private ContratoOpenApi(JsonNode documento)
    {
        _documento = documento;
        _validador = new ValidadorEsquema(documento);
    }

    /// <summary>Carga contracts/{archivo} copiado junto a los binarios de prueba.</summary>
    public static ContratoOpenApi Cargar(string archivo)
    {
        var ruta = Path.Combine(AppContext.BaseDirectory, "contracts", archivo);
        var documento = LectorYamlMinimo.Leer(File.ReadAllText(ruta))
                        ?? throw new InvalidOperationException($"Contrato vacío: {ruta}");
        return new ContratoOpenApi(documento);
    }

    public IEnumerable<(string Ruta, string Metodo)> Operaciones()
        => _documento["paths"]!.AsObject()
            .SelectMany(p => p.Value!.AsObject()
                .Where(m => Metodos.Contains(m.Key))
                .Select(m => (p.Key, m.Key)));

    /// <summary>Valida estado y cuerpo de una respuesta contra la operación (ruta plantilla, p. ej. /entregas/{entregaId}).</summary>
    public IReadOnlyList<string> ValidarRespuesta(string ruta, string metodo, int estado, string? cuerpo, string? contentType)
    {
        var operacion = _documento["paths"]?[ruta]?[metodo.ToLowerInvariant()]
                        ?? throw new InvalidOperationException($"El contrato no define {metodo} {ruta}.");
        var respuesta = operacion["responses"]?[estado.ToString(System.Globalization.CultureInfo.InvariantCulture)];
        if (respuesta is null) return [$"{metodo} {ruta}: el estado {estado} no está declarado en el contrato."];

        var contenido = _validador.Resolver(respuesta)["content"]?.AsObject();
        if (contenido is null) return [];

        var tipo = (contentType ?? "").Split(';')[0].Trim();
        if (contenido[tipo] is not { } medio)
            return [$"{metodo} {ruta} {estado}: content-type '{tipo}' no declarado ({string.Join(", ", contenido.Select(c => c.Key))})."];

        var json = string.IsNullOrEmpty(cuerpo) ? null : JsonNode.Parse(cuerpo);
        return _validador.Validar(medio["schema"], json);
    }
}
