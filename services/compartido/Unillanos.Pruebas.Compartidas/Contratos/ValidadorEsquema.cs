using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Json.Nodes;

namespace Unillanos.Pruebas.Compartidas.Contratos;

/// <summary>
/// Validador del subconjunto de JSON Schema (OpenAPI 3.1) que usan los contratos:
/// $ref, type (incluido [tipo, "null"]), enum, required, properties, additionalProperties: false,
/// items, format uuid/date-time, minimum/maximum y maxLength.
/// </summary>
public sealed partial class ValidadorEsquema(JsonNode documento)
{
    public IReadOnlyList<string> Validar(JsonNode? esquema, JsonNode? valor)
    {
        var errores = new List<string>();
        Validar(esquema, valor, "$", errores);
        return errores;
    }

    public JsonNode Resolver(JsonNode nodo)
    {
        while (nodo is JsonObject o && o["$ref"] is JsonValue referencia)
        {
            var ruta = referencia.GetValue<string>();
            if (!ruta.StartsWith("#/", StringComparison.Ordinal)) throw new NotSupportedException($"Solo se soportan $ref locales: {ruta}");
            JsonNode? actual = documento;
            foreach (var parte in ruta[2..].Split('/'))
                actual = actual?[parte.Replace("~1", "/").Replace("~0", "~")];
            nodo = actual ?? throw new InvalidOperationException($"$ref no encontrado: {ruta}");
        }
        return nodo;
    }

    private void Validar(JsonNode? esquema, JsonNode? valor, string ruta, List<string> errores)
    {
        if (esquema is null) return;
        var s = Resolver(esquema).AsObject();

        if (s["type"] is { } tipo)
        {
            var permitidos = tipo is JsonArray arr ? arr.Select(t => t!.GetValue<string>()).ToList() : [tipo.GetValue<string>()];
            if (!permitidos.Any(t => EsDelTipo(valor, t)))
            {
                errores.Add($"{ruta}: se esperaba {string.Join("|", permitidos)} y llegó {Describir(valor)}.");
                return;
            }
        }
        if (valor is null) return;

        if (s["enum"] is JsonArray valores && !valores.Any(v => JsonNode.DeepEquals(v, valor)))
            errores.Add($"{ruta}: '{valor.ToJsonString()}' no está en enum {valores.ToJsonString()}.");

        switch (valor.GetValueKind())
        {
            case JsonValueKind.Object:
                ValidarObjeto(s, valor.AsObject(), ruta, errores);
                break;
            case JsonValueKind.Array when s["items"] is { } items:
                var i = 0;
                foreach (var elemento in valor.AsArray()) Validar(items, elemento, $"{ruta}[{i++}]", errores);
                break;
            case JsonValueKind.String:
                ValidarTexto(s, valor.GetValue<string>(), ruta, errores);
                break;
            case JsonValueKind.Number:
                var numero = Numero(valor);
                if (s["minimum"] is { } min && numero < Numero(min)) errores.Add($"{ruta}: {numero} < minimum {min}.");
                if (s["maximum"] is { } max && numero > Numero(max)) errores.Add($"{ruta}: {numero} > maximum {max}.");
                break;
        }
    }

    private void ValidarObjeto(JsonObject s, JsonObject objeto, string ruta, List<string> errores)
    {
        var propiedades = s["properties"]?.AsObject();
        if (s["required"] is JsonArray requeridas)
        {
            foreach (var r in requeridas.Select(r => r!.GetValue<string>()))
                if (!objeto.ContainsKey(r)) errores.Add($"{ruta}: falta la propiedad requerida '{r}'.");
        }
        foreach (var (nombre, valor) in objeto)
        {
            if (propiedades?[nombre] is { } esquemaPropiedad)
                Validar(esquemaPropiedad, valor, $"{ruta}.{nombre}", errores);
            else if (s["additionalProperties"] is JsonValue ap && ap.GetValueKind() == JsonValueKind.False)
                errores.Add($"{ruta}: propiedad no declarada en el contrato '{nombre}'.");
        }
    }

    private static void ValidarTexto(JsonObject s, string texto, string ruta, List<string> errores)
    {
        if (s["maxLength"] is { } max && texto.Length > Numero(max)) errores.Add($"{ruta}: supera maxLength {max}.");
        switch (s["format"]?.GetValue<string>())
        {
            case "uuid" when !Guid.TryParseExact(texto, "D", out _):
                errores.Add($"{ruta}: '{texto}' no es un uuid.");
                break;
            case "date-time" when !FechaIso().IsMatch(texto) || !DateTimeOffset.TryParse(texto, CultureInfo.InvariantCulture, out _):
                errores.Add($"{ruta}: '{texto}' no es date-time ISO 8601 con zona.");
                break;
        }
    }

    private static bool EsDelTipo(JsonNode? valor, string tipo) => (tipo, valor?.GetValueKind()) switch
    {
        ("null", null) => true,
        ("object", JsonValueKind.Object) => true,
        ("array", JsonValueKind.Array) => true,
        ("string", JsonValueKind.String) => true,
        ("boolean", JsonValueKind.True or JsonValueKind.False) => true,
        ("number", JsonValueKind.Number) => true,
        ("integer", JsonValueKind.Number) => decimal.IsInteger(Numero(valor!)),
        _ => false,
    };

    // Los números del contrato (YAML) y de la respuesta (JSON) pueden venir como long o decimal.
    private static decimal Numero(JsonNode nodo) => decimal.Parse(nodo.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture);

    private static string Describir(JsonNode? valor) => valor is null ? "null" : valor.GetValueKind().ToString();

    // ISO 8601 / RFC 3339 con zona explícita (Z u offset).
    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?(Z|[+-]\d{2}:\d{2})$")]
    private static partial Regex FechaIso();
}
