using System.Text.Json;
using Asistente.Application.Puertos;

namespace Asistente.Application.Herramientas;

public sealed class ConsultarMiHorario(IGatewayCliente gateway, string rol) : IHerramienta
{
    private static readonly string[] Dias = ["Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];

    public string Nombre => "consultar_mi_horario";
    public string Descripcion => "Consulta el horario inscrito del propio usuario: día de la semana, horas de Colombia, materia y aula.";
    public string EsquemaJson => """{"type":"object","properties":{},"additionalProperties":false}""";
    public string RolPermitido => rol;
    public bool EsEscritura => false;

    public async Task<object> EjecutarAsync(JsonElement argumentos, ContextoTurno contexto, CancellationToken ct) =>
        (await gateway.HorarioSemanalAsync(ct)).Where(b => b.Dia >= 1 && b.Dia <= Dias.Length)
            .Select(b => new { Dia = Dias[b.Dia - 1], b.Inicio, b.Fin, b.Nombre, b.Codigo, b.Aula })
            .ToArray();
}
