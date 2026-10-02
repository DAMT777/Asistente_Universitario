namespace MiniIdentityApi.Api.Middleware;

/// <summary>
/// Traduce las excepciones del dominio a respuestas HTTP correctas
/// (sin esto, un login fallido respondia 500 en lugar de 401).
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var status = ex switch
            {
                ArgumentException => StatusCodes.Status400BadRequest,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                KeyNotFoundException => StatusCodes.Status404NotFound,
                InvalidOperationException => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError
            };

            if (status == StatusCodes.Status500InternalServerError)
                _logger.LogError(ex, "Error no controlado");

            if (context.Response.HasStarted) throw;

            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new
            {
                error = status == StatusCodes.Status500InternalServerError
                    ? "Internal server error."
                    : ex.Message
            });
        }
    }
}
