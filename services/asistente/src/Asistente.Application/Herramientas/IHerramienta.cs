using System.Text.Json;
using Asistente.Application.Puertos;
using Asistente.Domain;

namespace Asistente.Application.Herramientas;

/// <summary>Una herramienta que se le ofrece al modelo. El rol que puede usarla lo fija el código, no el modelo.</summary>
public interface IHerramienta
{
    string Nombre { get; }
    string Descripcion { get; }
    string EsquemaJson { get; }
    string RolPermitido { get; }
    bool EsEscritura { get; }
    Task<object> EjecutarAsync(JsonElement argumentos, ContextoTurno contexto, CancellationToken ct);
}

/// <summary>Estado de un mensaje: quién es el usuario y qué acción pendiente se creó.</summary>
public sealed class ContextoTurno(UsuarioAsistente usuario)
{
    public UsuarioAsistente Usuario { get; } = usuario;
    public AccionPendiente? AccionCreada { get; set; }
}

/// <summary>Resultado que el modelo debe usar para preguntar en lugar de adivinar.</summary>
public sealed record Aclaracion(string Pregunta, IReadOnlyList<string> Opciones);

public sealed record ErrorHerramienta(string Error);

internal static class Argumentos
{
    public static string? Texto(JsonElement args, string nombre) =>
        args.ValueKind == JsonValueKind.Object && args.TryGetProperty(nombre, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    public static decimal? Decimal(JsonElement args, string nombre)
    {
        if (args.ValueKind != JsonValueKind.Object || !args.TryGetProperty(nombre, out var v)) return null;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var d)) return d;
        if (v.ValueKind == JsonValueKind.String && decimal.TryParse(v.GetString()?.Replace(',', '.'),
                System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var s)) return s;
        return null;
    }
}

/// <summary>Resuelve el curso del profesor: por código o nombre, o el único que tenga.</summary>
internal static class Cursos
{
    public static ResultadoResolucion Resolver(string? consulta, IReadOnlyList<CursoProfesor> cursos)
    {
        var candidatos = cursos.Select(c => new Candidato(c.Id, c.Nombre, c.Codigo)).ToList();
        if (string.IsNullOrWhiteSpace(consulta))
            return candidatos.Count == 1 ? new(TipoCoincidencia.Unica, candidatos) : new(candidatos.Count == 0 ? TipoCoincidencia.Ninguna : TipoCoincidencia.Varias, candidatos);
        return ResolutorNombres.Resolver(consulta, candidatos);
    }

    public static Aclaracion Preguntar(string que, ResultadoResolucion r) => new(
        r.Tipo == TipoCoincidencia.Varias
            ? $"Hay varias coincidencias para {que}. Pregunta al usuario cuál quiere."
            : $"No se encontró {que}. Pregunta al usuario el nombre exacto.",
        r.Candidatos.Select(c => c.Codigo is null ? c.Nombre : $"{c.Nombre} ({c.Codigo})").ToList());
}
