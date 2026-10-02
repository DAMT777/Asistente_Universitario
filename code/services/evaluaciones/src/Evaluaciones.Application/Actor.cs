using Evaluaciones.Domain;

namespace Evaluaciones.Application;

public static class Roles
{
    public const string Profesor = "PROFESOR";
    public const string Estudiante = "ESTUDIANTE";
}

/// <summary>Quien hace la petición, leído del token (sub y rol).</summary>
public sealed record Actor(Guid Id, string Rol)
{
    public bool EsProfesor => Rol == Roles.Profesor;

    public void ExigirProfesor()
    {
        if (!EsProfesor) throw new SinPermisoException("Solo el profesor puede calificar y publicar notas.");
    }
}
