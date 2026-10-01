using System.Net;
using System.Net.Http.Json;
using Unillanos.Entregas.IntegrationTests.Infraestructura;
using Unillanos.Pruebas.Compartidas;
using Unillanos.Pruebas.Compartidas.Contratos;

namespace Unillanos.Entregas.IntegrationTests;

public sealed class MisEntregasTests(EntregasFixture fixture) : PruebaEntregas(fixture)
{
    [Fact]
    public async Task Solo_devuelve_las_entregas_propias_y_respeta_el_filtro()
    {
        await SubirOkAsync(Ana, UsuariosSemilla.Taller1);
        await SubirOkAsync(Luis, UsuariosSemilla.Taller1);

        var todas = await Ana.GetAsync("/mis-entregas");
        var filtradas = await Ana.GetFromJsonAsync<List<EntregaDto>>($"/mis-entregas?actividadId={UsuariosSemilla.Taller1}");
        var otraActividad = await Ana.GetFromJsonAsync<List<EntregaDto>>($"/mis-entregas?actividadId={UsuariosSemilla.Proyecto1}");

        Assert.Equal(HttpStatusCode.OK, todas.StatusCode);
        await todas.CumpleContratoAsync(Contrato, "get", "/mis-entregas");
        var propias = (await todas.Content.ReadFromJsonAsync<List<EntregaDto>>())!;
        Assert.All(propias, e => Assert.Equal(UsuariosSemilla.Ana, e.EstudianteId));
        Assert.Single(propias);
        Assert.Single(filtradas!);
        Assert.Empty(otraActividad!);
    }

    [Fact]
    public async Task Sin_entregas_devuelve_lista_vacia()
    {
        var respuesta = await ClienteDe(UsuariosSemilla.Marta).GetAsync("/mis-entregas");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("[]", await respuesta.Content.ReadAsStringAsync());
    }
}
