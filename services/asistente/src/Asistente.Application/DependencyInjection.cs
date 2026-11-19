using Asistente.Application.CasosDeUso;
using Asistente.Application.Herramientas;
using Microsoft.Extensions.DependencyInjection;

namespace Asistente.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAplicacionAsistente(this IServiceCollection services)
    {
        services.AddScoped<IHerramienta, ConsultarMisNotas>();
        services.AddScoped<IHerramienta, ConsultarNotasCurso>();
        services.AddScoped<IHerramienta, ConsultarMisEntregas>();
        services.AddScoped<IHerramienta, ListarCursosProfesor>();
        services.AddScoped<IHerramienta, ListarEstudiantesCurso>();
        services.AddScoped<IHerramienta, ProponerCalificacion>();
        services.AddScoped<IHerramienta>(sp => new ConsultarMiHorario(sp.GetRequiredService<Puertos.IGatewayCliente>(), UsuarioAsistente.Estudiante));
        services.AddScoped<IHerramienta>(sp => new ConsultarMiHorario(sp.GetRequiredService<Puertos.IGatewayCliente>(), UsuarioAsistente.Profesor));
        services.AddScoped<IHerramienta>(sp => new ConsultarActividadesCurso(sp.GetRequiredService<Puertos.IGatewayCliente>(), UsuarioAsistente.Estudiante));
        services.AddScoped<IHerramienta>(sp => new ConsultarActividadesCurso(sp.GetRequiredService<Puertos.IGatewayCliente>(), UsuarioAsistente.Profesor));
        services.AddScoped<IHerramienta, ConsultarCalificacionesActividad>();
        services.AddScoped<CatalogoHerramientas>();
        services.AddScoped<ProcesarMensaje>();
        services.AddScoped<GestionarAcciones>();
        return services;
    }
}
