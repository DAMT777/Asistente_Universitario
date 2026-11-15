using System.ComponentModel.DataAnnotations;

namespace Asistente.Infrastructure.Gateway;

/// <summary>Services__GatewayBaseUrl: el asistente solo conoce el gateway, nunca los servicios directamente.</summary>
public sealed class ServiciosOpciones
{
    public const string Seccion = "Services";

    [Required, Url] public string GatewayBaseUrl { get; set; } = "";
    [Range(1, 60)] public int GatewayTimeoutSeconds { get; set; } = 10;
}
