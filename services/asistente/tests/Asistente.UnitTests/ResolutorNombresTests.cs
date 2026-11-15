using Asistente.Domain;

namespace Asistente.UnitTests;

public sealed class ResolutorNombresTests
{
    private static readonly Candidato[] Inscritos =
    [
        new(Guid.NewGuid(), "Ana Demo", "E0001"),
        new(Guid.NewGuid(), "Luis Demo", "E0002"),
        new(Guid.NewGuid(), "Marta Demo", "E0003"),
        new(Guid.NewGuid(), "Luis Ángel Pérez", "E0004"),
    ];

    [Theory]
    [InlineData("Ana")]
    [InlineData("ana demo")]
    [InlineData("E0003")]
    [InlineData("luis angel")]
    public void Una_coincidencia(string consulta) =>
        Assert.Equal(TipoCoincidencia.Unica, ResolutorNombres.Resolver(consulta, Inscritos).Tipo);

    [Fact]
    public void Una_coincidencia_devuelve_el_estudiante_correcto() =>
        Assert.Equal("Marta Demo", ResolutorNombres.Resolver("marta", Inscritos).Unico.Nombre);

    [Theory]
    [InlineData("Pedro")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("An")]
    public void Ninguna_coincidencia(string? consulta) =>
        Assert.Equal(TipoCoincidencia.Ninguna, ResolutorNombres.Resolver(consulta, Inscritos).Tipo);

    [Fact]
    public void Varias_coincidencias_no_adivina()
    {
        var r = ResolutorNombres.Resolver("Luis", Inscritos);
        Assert.Equal(TipoCoincidencia.Varias, r.Tipo);
        Assert.Equal(2, r.Candidatos.Count);
        Assert.Throws<InvalidOperationException>(() => r.Unico);
    }

    [Fact]
    public void El_nombre_exacto_gana_sobre_parciales() =>
        Assert.Equal("Luis Demo", ResolutorNombres.Resolver("Luis Demo", Inscritos).Unico.Nombre);
}
