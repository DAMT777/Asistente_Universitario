using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Unillanos.ServiceDefaults.Errores;

namespace Evaluaciones.Api.Seguridad;

/// <summary>Variable ServiceKey: secreto compartido para llamadas servicio a servicio.</summary>
public sealed class ClaveServicioOpciones
{
    public const string Encabezado = "X-Service-Key";

    [Required, MinLength(16)] public string ServiceKey { get; set; } = "";
}

/// <summary>Exige X-Service-Key en /internal/**. Comparación en tiempo constante.</summary>
public sealed class RequiereClaveServicioFilter(IOptions<ClaveServicioOpciones> opciones) : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext contexto)
    {
        var recibida = Encoding.UTF8.GetBytes(contexto.HttpContext.Request.Headers[ClaveServicioOpciones.Encabezado].ToString());
        var esperada = Encoding.UTF8.GetBytes(opciones.Value.ServiceKey);
        if (CryptographicOperations.FixedTimeEquals(recibida, esperada)) return;

        var cuerpo = new ErrorApi(401, CodigosError.NoAutenticado, "Falta o es inválida la llave de servicio.", contexto.HttpContext.TraceIdentifier);
        contexto.Result = new ObjectResult(cuerpo) { StatusCode = 401, ContentTypes = { EscritorErrores.ContentType } };
    }
}

public static class ClaveServicioExtensions
{
    public static IServiceCollection AddClaveServicio(this IServiceCollection services)
    {
        services.AddOptions<ClaveServicioOpciones>().BindConfiguration("").ValidateDataAnnotations().ValidateOnStart();
        services.AddScoped<RequiereClaveServicioFilter>();
        return services;
    }
}
