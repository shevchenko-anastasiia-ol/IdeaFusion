using System.Text.Json;
using Collaboration.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Collaboration.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Клієнт відключився — не є помилкою, просто ігноруємо
            context.Response.StatusCode = 499;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Title = "An error occurred",
            Status = StatusCodes.Status500InternalServerError,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        switch (exception)
        {
            // Domain exceptions
            case NotFoundException notFound:
                problemDetails.Status = StatusCodes.Status404NotFound;
                problemDetails.Title = "Not Found";
                problemDetails.Detail = notFound.Message;
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                break;

            case ConflictException conflict:
                problemDetails.Status = StatusCodes.Status409Conflict;
                problemDetails.Title = "Conflict";
                problemDetails.Detail = conflict.Message;
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                break;

            case DomainException domain:
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Title = "Domain Error";
                problemDetails.Detail = domain.Message;
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                break;

            // Validation exceptions
            case CustomValidationException customValidation:
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Title = "Validation Failed";
                problemDetails.Extensions["errors"] = customValidation.Errors.Select(e => new
                {
                    property = e.PropertyName,
                    error = e.ErrorMessage
                });
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                break;

            case FluentValidation.ValidationException validation:
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Title = "Validation Failed";
                problemDetails.Extensions["errors"] = validation.Errors.Select(e => new
                {
                    property = e.PropertyName,
                    error = e.ErrorMessage
                });
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                break;

            // Concurrency exception
            case ConcurrencyException concurrency:
                problemDetails.Status = 409;
                problemDetails.Title = "Concurrency Conflict";
                problemDetails.Extensions["entityId"] = concurrency.EntityId;
                problemDetails.Extensions["expectedVersion"] = concurrency.ExpectedVersion;
                problemDetails.Extensions["actualVersion"] = concurrency.ActualVersion;
                context.Response.StatusCode = 412;
                break;

            // MongoDB exceptions
            case MongoDbConnectionException mongoConn:
                problemDetails.Status = StatusCodes.Status503ServiceUnavailable;
                problemDetails.Title = "Database Connection Error";
                problemDetails.Detail = mongoConn.Message;
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                break;

            case MongoDbWriteException mongoWrite:
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Title = "Database Write Error";
                problemDetails.Detail = mongoWrite.Message;
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                break;

            // Standard exceptions
            case ArgumentException argEx:
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Title = "Invalid Argument";
                problemDetails.Detail = argEx.Message;
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                break;

            case KeyNotFoundException keyNotFound:
                problemDetails.Status = StatusCodes.Status404NotFound;
                problemDetails.Title = "Not Found";
                problemDetails.Detail = keyNotFound.Message;
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                break;

            case InvalidOperationException invalidOp:
                problemDetails.Status = StatusCodes.Status409Conflict;
                problemDetails.Title = "Invalid Operation";
                problemDetails.Detail = invalidOp.Message;
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                break;

            case UnauthorizedAccessException:
                problemDetails.Status = StatusCodes.Status403Forbidden;
                problemDetails.Title = "Access Forbidden";
                problemDetails.Detail = "You do not have permission to access this resource.";
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                break;

            // Default
            default:
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Title = "Internal Server Error";
                problemDetails.Detail = exception.Message;
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                break;
        }

        var json = JsonSerializer.Serialize(problemDetails);
        return context.Response.WriteAsync(json);
    }
}