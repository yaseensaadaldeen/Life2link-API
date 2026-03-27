using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace LifeLink_V2.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _env = env;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // TEMP: log full details and break into debugger when attached
            _logger.LogError("Unhandled exception caught by middleware. Type={ExceptionType}, Message={Message}\nStackTrace:\n{Stack}\nInner:\n{Inner}",
                exception.GetType().FullName,
                exception.Message,
                exception.StackTrace ?? "<no stack>",
                exception.InnerException?.ToString() ?? "<no inner>");

            if (Debugger.IsAttached)
                Debugger.Break();

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var returnDetailed = _env.IsDevelopment() || _configuration.GetValue<bool>("ReturnDetailedErrors", false);

            var response = returnDetailed
                ? new ApiErrorResponse(context.Response.StatusCode, exception.Message, exception.ToString())
                : new ApiErrorResponse(context.Response.StatusCode, "حدث خطأ داخلي في الخادم");

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonSerializer.Serialize(response, options);

            await context.Response.WriteAsync(json);
        }
    }

    public class ApiErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public string? Details { get; set; }

        public ApiErrorResponse(int statusCode, string message, string? details = null)
        {
            StatusCode = statusCode;
            Message = message;
            Details = details;
        }
    }
}