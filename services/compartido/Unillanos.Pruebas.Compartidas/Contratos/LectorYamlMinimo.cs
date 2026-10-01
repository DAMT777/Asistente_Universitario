using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

namespace Unillanos.Pruebas.Compartidas.Contratos;

/// <summary>
/// Lector del subconjunto de YAML que usan los contratos: mapas y listas en bloque por indentación,
/// listas en línea de escalares ([a, "b"], []), escalares simples o entre comillas y comentarios con #.
/// No soporta textos multilínea, anclas ni mapas en línea (salvo {}). Falla con mensaje claro si los encuentra.
/// </summary>
public static class LectorYamlMinimo
{
    private sealed record Linea(int Numero, int Sangria, string Texto);

    public static JsonNode? Leer(string yaml)
    {
        var lineas = Tokenizar(yaml);
        var i = 0;
        var raiz = lineas.Count == 0 ? null : LeerBloque(lineas, ref i, lineas[0].Sangria);
        if (i < lineas.Count) throw Error(lineas[i], "sangría inesperada");
        return raiz;
    }

    private static List<Linea> Tokenizar(string yaml)
    {
        var resultado = new List<Linea>();
        var numero = 0;
        foreach (var cruda in yaml.Replace("\r\n", "\n").Split('\n'))
        {
            numero++;
            if (cruda.Contains('\t')) throw new FormatException($"Línea {numero}: no se permiten tabuladores.");
            var sinComentario = QuitarComentario(cruda).TrimEnd();
            if (sinComentario.Trim().Length == 0) continue;
            var sangria = sinComentario.Length - sinComentario.TrimStart().Length;
            resultado.Add(new Linea(numero, sangria, sinComentario.Trim()));
        }
        return resultado;
    }

    private static string QuitarComentario(string linea)
    {
        char? comilla = null;
        for (var k = 0; k < linea.Length; k++)
        {
            var c = linea[k];
            if (comilla is null && (c == '"' || c == '\'')) comilla = c;
            else if (comilla == c && (c != '"' || linea[k - 1] != '\\')) comilla = null;
            else if (comilla is null && c == '#' && (k == 0 || linea[k - 1] == ' ')) return linea[..k];
        }
        return linea;
    }

    private static JsonNode? LeerBloque(List<Linea> lineas, ref int i, int sangria)
        => EsItemLista(lineas[i].Texto) ? LeerLista(lineas, ref i, sangria) : LeerMapa(lineas, ref i, sangria);

    private static bool EsItemLista(string texto) => texto == "-" || texto.StartsWith("- ", StringComparison.Ordinal);

    private static JsonObject LeerMapa(List<Linea> lineas, ref int i, int sangria)
    {
        var mapa = new JsonObject();
        while (i < lineas.Count && lineas[i].Sangria == sangria && !EsItemLista(lineas[i].Texto))
        {
            var linea = lineas[i];
            var (clave, valor) = SepararClave(linea);
            if (mapa.ContainsKey(clave)) throw Error(linea, $"clave duplicada '{clave}'");
            i++;
            mapa[clave] = valor.Length > 0 ? Escalar(linea, valor) : LeerHijo(lineas, ref i, sangria);
        }
        return mapa;
    }

    private static JsonNode? LeerHijo(List<Linea> lineas, ref int i, int sangriaPadre)
    {
        if (i >= lineas.Count) return null;
        var siguiente = lineas[i];
        if (siguiente.Sangria > sangriaPadre) return LeerBloque(lineas, ref i, siguiente.Sangria);
        // YAML permite la lista al mismo nivel que su clave: "clave:\n- a".
        if (siguiente.Sangria == sangriaPadre && EsItemLista(siguiente.Texto)) return LeerLista(lineas, ref i, sangriaPadre);
        return null;
    }

    private static JsonArray LeerLista(List<Linea> lineas, ref int i, int sangria)
    {
        var lista = new JsonArray();
        while (i < lineas.Count && lineas[i].Sangria == sangria && EsItemLista(lineas[i].Texto))
        {
            var linea = lineas[i];
            var resto = linea.Texto.Length > 1 ? linea.Texto[2..].TrimStart() : "";
            if (resto.Length == 0)
            {
                i++;
                lista.Add(LeerHijo(lineas, ref i, sangria));
            }
            else if (TieneClave(resto))
            {
                // "- clave: valor" abre un mapa cuyas claves siguientes están alineadas con "clave".
                var sangriaMapa = sangria + (linea.Texto.Length - resto.Length);
                lineas[i] = linea with { Sangria = sangriaMapa, Texto = resto };
                lista.Add(LeerMapa(lineas, ref i, sangriaMapa));
            }
            else
            {
                i++;
                lista.Add(Escalar(linea, resto));
            }
        }
        return lista;
    }

    private static bool TieneClave(string texto) => IndiceSeparador(texto) >= 0;

    private static int IndiceSeparador(string texto)
    {
        char? comilla = null;
        for (var k = 0; k < texto.Length; k++)
        {
            var c = texto[k];
            if (comilla is null && k == 0 && (c == '"' || c == '\'')) comilla = c;
            else if (comilla == c) comilla = null;
            else if (comilla is null && c == '[') return -1;
            else if (comilla is null && c == ':' && (k == texto.Length - 1 || texto[k + 1] == ' ')) return k;
        }
        return -1;
    }

    private static (string Clave, string Valor) SepararClave(Linea linea)
    {
        var k = IndiceSeparador(linea.Texto);
        if (k < 0) throw Error(linea, "se esperaba 'clave: valor'");
        var clave = linea.Texto[..k].Trim();
        if (clave.Length >= 2 && (clave[0] == '"' || clave[0] == '\'')) clave = DesComillar(linea, clave);
        return (clave, linea.Texto[(k + 1)..].Trim());
    }

    private static JsonNode? Escalar(Linea linea, string texto)
    {
        if (texto.StartsWith('[')) return ListaEnLinea(linea, texto);
        if (texto == "{}") return new JsonObject();
        if (texto.StartsWith('{') || texto is "|" or ">" || texto.StartsWith('&') || texto.StartsWith('*'))
            throw Error(linea, "construcción YAML no soportada por el lector mínimo");
        if (texto[0] is '"' or '\'') return JsonValue.Create(DesComillar(linea, texto));
        return texto switch
        {
            "null" or "~" => null,
            "true" => JsonValue.Create(true),
            "false" => JsonValue.Create(false),
            _ when long.TryParse(texto, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var entero) => JsonValue.Create(entero),
            _ when decimal.TryParse(texto, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var numero) => JsonValue.Create(numero),
            _ => JsonValue.Create(texto),
        };
    }

    private static JsonArray ListaEnLinea(Linea linea, string texto)
    {
        if (!texto.EndsWith(']')) throw Error(linea, "lista en línea sin cerrar");
        var interior = texto[1..^1].Trim();
        var lista = new JsonArray();
        if (interior.Length == 0) return lista;

        var actual = new StringBuilder();
        char? comilla = null;
        foreach (var c in interior)
        {
            if (comilla is null && (c == '"' || c == '\'')) comilla = c;
            else if (comilla == c) comilla = null;
            if (comilla is null && c == ',')
            {
                lista.Add(Escalar(linea, actual.ToString().Trim()));
                actual.Clear();
            }
            else actual.Append(c);
        }
        lista.Add(Escalar(linea, actual.ToString().Trim()));
        return lista;
    }

    private static string DesComillar(Linea linea, string texto)
    {
        var comilla = texto[0];
        if (texto.Length < 2 || texto[^1] != comilla) throw Error(linea, "texto entre comillas sin cerrar");
        var interior = texto[1..^1];
        return comilla == '\''
            ? interior.Replace("''", "'")
            : interior.Replace("\\\"", "\"").Replace("\\n", "\n").Replace("\\\\", "\\");
    }

    private static FormatException Error(Linea linea, string detalle) => new($"Línea {linea.Numero}: {detalle}: '{linea.Texto}'.");
}
