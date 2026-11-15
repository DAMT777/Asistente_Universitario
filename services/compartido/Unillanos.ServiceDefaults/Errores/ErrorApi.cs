namespace Unillanos.ServiceDefaults.Errores;

/// <summary>Cuerpo estándar de error (estilo ProblemDetails con código estable).</summary>
public sealed record ErrorApi(int Status, string Codigo, string Mensaje, string TraceId);

/// <summary>Traducción de una excepción a un código del contrato.</summary>
/// <param name="Status">Estado HTTP explícito para códigos que no están en <see cref="CodigosError"/>; null usa la tabla común.</param>
public sealed record ErrorDescrito(string Codigo, string Mensaje, int? Status = null);
