using System.Net;
using System.Net.Http.Json;
using Unillanos.Evaluaciones.IntegrationTests.Infraestructura;
using Unillanos.Pruebas.Compartidas;
using Unillanos.Pruebas.Compartidas.Contratos;

namespace Unillanos.Evaluaciones.IntegrationTests;

/// <summary>CU-16: matriz de notas con cortes y definitiva parcial.</summary>
[Collection(ColeccionEvaluaciones.Nombre)]
public sealed class MatrizNotasTests(EvaluacionesFixture fixture)
{
    private static readonly ContratoOpenApi Contrato = ContratoOpenApi.Cargar("evaluaciones.yaml");

    [Fact]
    public async Task E13_Ana_corte_1_es_3_2_y_definitiva_parcial_1_0()
    {
        var respuesta = await fixture.ClienteDe(UsuariosSemilla.Ana).GetAsync("/mis-notas");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        await respuesta.CumpleContratoAsync(Contrato, "get", "/mis-notas");
        var curso = Assert.Single((await respuesta.Content.ReadFromJsonAsync<MatrizDto>())!.Cursos);
        Assert.Equal((UsuariosSemilla.Curso, "603803"), (curso.CursoId, curso.Codigo));
        Assert.False(string.IsNullOrEmpty(curso.Profesor));
        Assert.Equal(new CorteDto(1, 30, 3.2m, true), curso.Cortes[0]);
        Assert.Equal(new CorteDto(2, 30, null, false), curso.Cortes[1]);
        Assert.Equal(new CorteDto(3, 40, null, false), curso.Cortes[2]);
        Assert.Equal(1.0m, curso.DefinitivaParcial);
        Assert.True(curso.EsParcial);
    }

    [Fact]
    public async Task E14_Marta_tiene_cortes_null_sin_publicar_y_definitiva_0_0()
    {
        var respuesta = await fixture.ClienteDe(UsuariosSemilla.Marta).GetAsync("/mis-notas");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        await respuesta.CumpleContratoAsync(Contrato, "get", "/mis-notas");
        var curso = Assert.Single((await respuesta.Content.ReadFromJsonAsync<MatrizDto>())!.Cursos);
        Assert.All(curso.Cortes, c => Assert.Equal(((decimal?)null, false), (c.Nota, c.Publicado)));
        Assert.Equal(0.0m, curso.DefinitivaParcial);
        Assert.True(curso.EsParcial);
        Assert.Contains("\"definitivaParcial\":0.0", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Luis_con_solo_un_borrador_no_tiene_cortes_publicados()
    {
        var curso = Assert.Single((await fixture.ClienteDe(UsuariosSemilla.Luis).GetFromJsonAsync<MatrizDto>("/mis-notas"))!.Cursos);

        Assert.All(curso.Cortes, c => Assert.False(c.Publicado));
        Assert.Equal(0.0m, curso.DefinitivaParcial);
    }

    [Fact]
    public async Task Estudiante_sin_cursos_recibe_lista_vacia()
    {
        var r = await fixture.ClienteDe(UsuariosSemilla.Pedro).GetFromJsonAsync<MatrizDto>("/mis-notas");

        Assert.Empty(r!.Cursos);
    }
}
