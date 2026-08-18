namespace HygiaTrade.API.Middlewares;
using System.Net;
using HygiaTrade.Core.Exceptions;
public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;
    private readonly string _responseContentType = "application/json";

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
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
        catch (AppException e)
        {
            _logger.LogWarning(e, "Application exception handled with status code {StatusCode}.", e.StatusCode);
            context.Response.StatusCode = e.StatusCode;
            context.Response.ContentType = _responseContentType;

            ErrorDetails errorResponse = new ErrorDetails()
                .SetStatusCode(context.Response.StatusCode)
                .SetMessage(e.Message);

            if (e.Args != null && e.Args.Count > 0)
            {
                errorResponse = new ErrorDetails()
                    .SetStatusCode(context.Response.StatusCode)
                    .SetMessage(e.Message)
                    .AddArgs(e.Args);
            }

            await context.Response.WriteAsync(errorResponse.ToJson());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unhandled exception reached exception middleware.");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = _responseContentType;

            await context.Response.WriteAsync(new ErrorDetails()
                .SetStatusCode(context.Response.StatusCode)
                .SetMessage(e.Message + "\n" + e.StackTrace + "\n" + e.InnerException)
                .ToJson());
        }
    }
}
