using Entregas.Domain;

namespace Entregas.UnitTests;

public sealed class ValidadorArchivoTests
{
    private const long Max = 20 * 1024 * 1024;

    public static TheoryData<string, byte[], string> ArchivosValidos => new()
    {
        { "informe.pdf", "%PDF-1.7"u8.ToArray(), "PDF" },
        { "informe.doc", [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1], "DOC" },
        { "datos.xls", [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1], "XLS" },
        { "charla.ppt", [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1], "PPT" },
        { "informe.docx", [0x50, 0x4B, 0x03, 0x04, 0x14, 0x00], "DOCX" },
        { "datos.xlsx", [0x50, 0x4B, 0x03, 0x04, 0x14, 0x00], "XLSX" },
        { "charla.pptx", [0x50, 0x4B, 0x03, 0x04, 0x14, 0x00], "PPTX" },
        { "codigo.zip", [0x50, 0x4B, 0x03, 0x04, 0x14, 0x00], "ZIP" },
        { "vacio.zip", [0x50, 0x4B, 0x05, 0x06, 0x00, 0x00], "ZIP" },
        { "diagrama.png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], "PNG" },
        { "foto.jpg", [0xFF, 0xD8, 0xFF, 0xE0], "JPG" },
        { "foto.jpeg", [0xFF, 0xD8, 0xFF, 0xE1], "JPG" },
        { "INFORME.PDF", "%PDF-1.4"u8.ToArray(), "PDF" },
    };

    [Theory]
    [MemberData(nameof(ArchivosValidos))]
    public void Acepta_tipos_permitidos_con_firma_correcta(string nombre, byte[] cabecera, string tipoEsperado)
    {
        var tipo = ValidadorArchivo.Validar(nombre, 1024, cabecera, Max);

        Assert.Equal(tipoEsperado, tipo.Nombre);
    }

    [Fact]
    public void Rechaza_exe_renombrado_a_pdf_por_su_contenido()
    {
        byte[] cabeceraExe = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00];

        var ex = Assert.Throws<TipoArchivoNoPermitidoException>(() => ValidadorArchivo.Validar("tarea.pdf", 1024, cabeceraExe, Max));
        Assert.Equal("TIPO_ARCHIVO_NO_PERMITIDO", ex.Codigo);
    }

    [Theory]
    [InlineData("programa.exe")]
    [InlineData("script.sh")]
    [InlineData("sin_extension")]
    [InlineData("pagina.html")]
    public void Rechaza_extensiones_no_permitidas(string nombre)
        => Assert.Throws<TipoArchivoNoPermitidoException>(() => ValidadorArchivo.Validar(nombre, 1024, "%PDF-1.7"u8.ToArray(), Max));

    [Fact]
    public void Rechaza_docx_con_contenido_de_pdf()
        => Assert.Throws<TipoArchivoNoPermitidoException>(() => ValidadorArchivo.Validar("informe.docx", 1024, "%PDF-1.7"u8.ToArray(), Max));

    [Fact]
    public void Acepta_archivo_del_tamano_maximo_exacto()
        => ValidadorArchivo.Validar("informe.pdf", Max, "%PDF-1.7"u8.ToArray(), Max);

    [Fact]
    public void Rechaza_archivo_un_byte_mayor_al_maximo()
    {
        var ex = Assert.Throws<ArchivoDemasiadoGrandeException>(() => ValidadorArchivo.Validar("informe.pdf", Max + 1, "%PDF-1.7"u8.ToArray(), Max));
        Assert.Equal("ARCHIVO_DEMASIADO_GRANDE", ex.Codigo);
    }

    [Fact]
    public void Rechaza_archivo_vacio()
        => Assert.Throws<ArchivoInvalidoException>(() => ValidadorArchivo.Validar("informe.pdf", 0, [], Max));

    [Fact]
    public void Rechaza_nombre_de_mas_de_255_caracteres()
        => Assert.Throws<ArchivoInvalidoException>(() => ValidadorArchivo.Validar(new string('a', 252) + ".pdf", 10, "%PDF-1.7"u8.ToArray(), Max));

    [Fact]
    public void Rechaza_cabecera_mas_corta_que_la_firma()
        => Assert.Throws<TipoArchivoNoPermitidoException>(() => ValidadorArchivo.Validar("foto.png", 3, [0x89, 0x50, 0x4E], Max));
}
