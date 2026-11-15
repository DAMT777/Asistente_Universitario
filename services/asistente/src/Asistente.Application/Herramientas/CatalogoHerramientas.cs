namespace Asistente.Application.Herramientas;

/// <summary>
/// Decide qué herramientas recibe el modelo según el claim rol del token. Un estudiante nunca recibe
/// herramientas de escritura, aunque se registre una por error con su rol.
/// </summary>
public sealed class CatalogoHerramientas(IEnumerable<IHerramienta> todas)
{
    public IReadOnlyList<IHerramienta> Para(UsuarioAsistente usuario) =>
        usuario.Rol switch
        {
            UsuarioAsistente.Estudiante => todas.Where(h => h.RolPermitido == UsuarioAsistente.Estudiante && !h.EsEscritura).ToList(),
            UsuarioAsistente.Profesor => todas.Where(h => h.RolPermitido == UsuarioAsistente.Profesor).ToList(),
            _ => [],
        };
}
