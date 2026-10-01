namespace Unillanos.Pruebas.Compartidas.Contratos;

public static class ContratoExtensions
{
    /// <summary>Valida la respuesta contra el contrato y falla con todos los errores encontrados.</summary>
    public static async Task CumpleContratoAsync(this HttpResponseMessage respuesta, ContratoOpenApi contrato, string metodo, string rutaPlantilla)
    {
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        var errores = contrato.ValidarRespuesta(rutaPlantilla, metodo, (int)respuesta.StatusCode, cuerpo,
            respuesta.Content.Headers.ContentType?.ToString());
        if (errores.Count > 0)
            throw new InvalidOperationException($"La respuesta no cumple el contrato {metodo} {rutaPlantilla}:\n - {string.Join("\n - ", errores)}\nCuerpo: {cuerpo}");
    }
}
