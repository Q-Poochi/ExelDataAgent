using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataAgent.API.Middlewares;

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
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";
        var statusCode = StatusCodes.Status500InternalServerError;
        var problemDetails = new ProblemDetails
        {
            Type = "https://datatracker.ietf.org/doc/html/rfc7807",
            Instance = context.Request.Path
        };

        switch (exception)
        {
            case FluentValidation.ValidationException validationEx:
                statusCode = StatusCodes.Status400BadRequest;
                problemDetails.Status = statusCode;
                problemDetails.Title = "Validation Error";
                problemDetails.Detail = "One or more validation errors occurred.";
                
                var errors = new System.Collections.Generic.Dictionary<string, string[]>();
                foreach (var err in validationEx.Errors)
                {
                    if (errors.ContainsKey(err.PropertyName))
                    {
                        var list = new System.Collections.Generic.List<string>(errors[err.PropertyName]) { err.ErrorMessage };
                        errors[err.PropertyName] = list.ToArray();
                    }
                    else
                    {
                        errors.Add(err.PropertyName, new[] { err.ErrorMessage });
                    }
                }
                problemDetails.Extensions.Add("errors", errors);
                break;

            case DataAgent.Domain.Exceptions.NotFoundException notFoundEx:
                statusCode = StatusCodes.Status404NotFound;
                problemDetails.Status = statusCode;
                problemDetails.Title = "Not Found";
                problemDetails.Detail = notFoundEx.Message;
                break;

            case DataAgent.Domain.Exceptions.UnauthorizedException authEx:
                statusCode = StatusCodes.Status401Unauthorized;
                problemDetails.Status = statusCode;
                problemDetails.Title = "Unauthorized";
                problemDetails.Detail = authEx.Message;
                break;

            default:
                statusCode = StatusCodes.Status500InternalServerError;
                problemDetails.Status = statusCode;
                problemDetails.Title = "Server Error";
                // Hide exact error message in non-development environment
                var env = context.RequestServices.GetService(typeof(Microsoft.AspNetCore.Hosting.IWebHostEnvironment)) as Microsoft.AspNetCore.Hosting.IWebHostEnvironment;
                if (env != null && (env.EnvironmentName == "Development" || env.EnvironmentName == "Testing"))
                {
                    problemDetails.Detail = exception.Message;
                    problemDetails.Extensions.Add("StackTrace", exception.StackTrace);
                }
                else
                {
                    problemDetails.Detail = "An unexpected error occurred.";
                }
                break;
        }

        context.Response.StatusCode = statusCode;
        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return context.Response.WriteAsync(json);
    }
}
