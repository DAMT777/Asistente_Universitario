using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Asistente.Application.CasosDeUso;

internal static partial class SeguridadMensaje
{
    public static bool ContieneSql(string texto)
    {
        var normalizado = new StringBuilder(texto.Length);
        foreach (var caracter in texto.Normalize(NormalizationForm.FormKC))
        {
            if (char.GetUnicodeCategory(caracter) != UnicodeCategory.Format)
                normalizado.Append(caracter);
        }

        return SintaxisSql().IsMatch(normalizado.ToString());
    }

    [GeneratedRegex("""
        \bselect\b[\s\S]*?\bfrom\b |
        \binsert\s+into\b | \bupdate\s+\S+\s+set\b | \bdelete\s+from\b |
        \b(?:create|alter|drop|truncate)\s+(?:table|database|view|schema)\b |
        \bunion\s+(?:all\s+)?select\b | \bexec(?:ute)?\s*\( |
        [\x27\x22]\s*(?:or|and)\s+[\x27\x22\d] | [\x27\x22]\s*(?:--|/\*)
        """, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.NonBacktracking)]
    private static partial Regex SintaxisSql();
}
