using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Unillanos.ServiceDefaults.Errores;

public static class ErroresExtensions
{
    public static IServiceCollection AddManejoErrores(this IServiceCollection services)
    {
        services.AddExceptionHandler<ManejadorExcepciones>();
        // PostConfigure: AddControllers registra su propia fábrica y no debe sobrescribir esta.
        services.PostConfigure<ApiBehaviorOptions>(o => o.InvalidModelStateResponseFactory = contexto =>
        {
            var detalle = contexto.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .Select(e => e.Key)
                .ToList();
            var mensaje = detalle.Count == 0
                ? "La solicitud no es válida."
                : $"Parámetros inválidos: {string.Join(", ", detalle)}.";
            var cuerpo = new ErrorApi(400, CodigosError.ValidacionFallida, mensaje, contexto.HttpContext.TraceIdentifier);
            return new ObjectResult(cuerpo) { StatusCode = 400, ContentTypes = { EscritorErrores.ContentType } };
        });
        return services;
    }

    public static IServiceCollection AddTraductorExcepciones<T>(this IServiceCollection services)
        where T : class, ITraductorExcepciones
        => services.AddSingleton<ITraductorExcepciones, T>();

    /// <summary>Excepciones y respuestas de error sin cuerpo (404 de ruta, 405, 415...) salen en formato estándar.</summary>
    public static IApplicationBuilder UseManejoErrores(this IApplicationBuilder app)
    {
        // ManejadorExcepciones siempre escribe la respuesta; este respaldo solo satisface la configuración.
        app.UseExceptionHandler(new ExceptionHandlerOptions { ExceptionHandler = _ => Task.CompletedTask });
        app.UseStatusCodePages(contexto =>
        {
            var respuesta = contexto.HttpContext.Response;
            var codigo = CodigosError.CodigoPorEstado(respuesta.StatusCode);
            return EscritorErrores.EscribirAsync(contexto.HttpContext, codigo, "La solicitud no pudo procesarse.", respuesta.StatusCode);
        });
        return app;
    }
}
