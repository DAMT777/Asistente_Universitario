using System.ComponentModel.DataAnnotations;

namespace Unillanos.Entregas.Infrastructure.Almacen;

/// <summary>Variables Storage__*.</summary>
public sealed class AlmacenOpciones
{
    public const string Seccion = "Storage";

    [Required] public string ConnectionString { get; set; } = "";
    [Required] public string Container { get; set; } = "";
}
