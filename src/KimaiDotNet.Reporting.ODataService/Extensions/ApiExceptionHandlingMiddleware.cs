using Microsoft.AspNetCore.Mvc;
using Microsoft.Kiota.Abstractions;
using System.Text.Json;

namespace MarkZither.KimaiDotNet.Reporting.ODataService.Extensions
{
    public sealed class ApiExceptionHandlingMiddleware
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly RequestDelegate _next;
        private readonly ILogger<ApiExceptionHandlingMiddleware> _logger;

        public ApiExceptionHandlingMiddleware(RequestDelegate next, ILogger<ApiExceptionHandlingMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ApiException ex)
            {
                int statusCode = NormalizeStatusCode(ex.ResponseStatusCode);
                _logger.LogWarning(EventIds.Api.KimaiApiError, ex, "Kimai API error {StatusCode} for {Path}", statusCode, context.Request.Path);

                if (context.Response.HasStarted)
                {
                    throw;
                }

                context.Response.Clear();
                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json";

                var payload = new ProblemDetails
                {
                    Status = statusCode,
                    Title = "Kimai API error",
                    Detail = "Upstream Kimai API returned an error response."
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
            }
        }

        private static int NormalizeStatusCode(int statusCode)
        {
            if (statusCode >= StatusCodes.Status400BadRequest && statusCode <= 599)
            {
                return statusCode;
            }

            return StatusCodes.Status502BadGateway;
        }
    }
}
