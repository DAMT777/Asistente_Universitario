namespace Unillanos.ServiceDefaults.Errores;

/// <summary>Cuerpo estándar de error (estilo ProblemDetails con código estable).</summary>
public sealed record ErrorApi(int Status, string Codigo, string Mensaje, string TraceId);

/// <summary>Traducción de una excepción a un código del contrato.</summary>
public sealed record ErrorDescrito(string Codigo, string Mensaje);
