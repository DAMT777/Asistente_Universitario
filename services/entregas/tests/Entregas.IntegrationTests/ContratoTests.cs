using System.Text.Json.Nodes;
using Entregas.IntegrationTests.Infraestructura;

namespace Entregas.IntegrationTests;

/// <summary>Las operaciones de contracts/entregas.yaml existen en el OpenAPI que genera el servicio.</summary>
public sealed class ContratoTests(EntregasFixture fixture) : PruebaEntregas(fixture)
{
    [Fact]
    public async Task Todas_las_operaciones_del_contrato_estan_implementadas()
    {
        var generado = JsonNode.Parse(await Api.CreateClient().GetStringAsync("/openapi/v1.json"))!;
        var faltantes = Contrato.Operaciones()
            .Where(o => !o.Ruta.StartsWith("/health/", StringComparison.Ordinal))
            .Where(o => generado["paths"]?[o.Ruta]?[o.Metodo] is null)
            .Select(o => $"{o.Metodo.ToUpperInvariant()} {o.Ruta}")
            .ToList();

        Assert.Empty(faltantes);
    }

    [Fact]
    public async Task El_servicio_no_expone_operaciones_fuera_del_contrato()
    {
        var generado = JsonNode.Parse(await Api.CreateClient().GetStringAsync("/openapi/v1.json"))!;
        var delContrato = Contrato.Operaciones().ToHashSet();
        var extra = generado["paths"]!.AsObject()
            .SelectMany(p => p.Value!.AsObject().Select(m => (Ruta: p.Key, Metodo: m.Key)))
            .Where(o => !delContrato.Contains(o))
            .ToList();

        Assert.Empty(extra);
    }
}
