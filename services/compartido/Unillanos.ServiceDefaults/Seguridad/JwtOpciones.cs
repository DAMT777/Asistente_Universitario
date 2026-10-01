using System.ComponentModel.DataAnnotations;

namespace Unillanos.ServiceDefaults.Seguridad;

/// <summary>Variables Jwt__*. Los servicios solo verifican: nunca firman tokens.</summary>
public sealed class JwtOpciones
{
    public const string Seccion = "Jwt";

    [Required] public string PublicKeyPath { get; set; } = "";
    [Required] public string Issuer { get; set; } = "";
    [Required] public string Audience { get; set; } = "";
    [Range(0, 300)] public int ClockSkewSegundos { get; set; }
}
