using Collaboration.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;

namespace Collaboration.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected readonly IMediator _mediator;

    protected BaseApiController(IMediator mediator)
    {
        _mediator = mediator;
    }

    protected IActionResult HandleException(Exception ex)
    {
        return ex switch
        {
            // Domain exceptions
            NotFoundException notFound => NotFound(new { message = notFound.Message }),
            
            ConflictException conflict => Conflict(new { message = conflict.Message }),
            
            DomainException domain => BadRequest(new { message = domain.Message }),
            
            // Validation exceptions
            CustomValidationException customValidation => BadRequest(new 
            { 
                message = "Validation failed",
                errors = customValidation.Errors.Select(e => new 
                { 
                    property = e.PropertyName, 
                    error = e.ErrorMessage 
                })
            }),
            
            ValidationException validation => BadRequest(new 
            { 
                message = "Validation failed",
                errors = validation.Errors.Select(e => new 
                { 
                    property = e.PropertyName, 
                    error = e.ErrorMessage 
                })
            }),
            
            // Concurrency exception
            ConcurrencyException concurrency => StatusCode(409, new 
            { 
                message = concurrency.Message,
                entityId = concurrency.EntityId,
                expectedVersion = concurrency.ExpectedVersion,
                actualVersion = concurrency.ActualVersion
            }),
            
            // MongoDB exceptions
            MongoDbConnectionException mongoConnection => StatusCode(503, new 
            { 
                message = "Database connection error",
                details = mongoConnection.Message 
            }),
            
            MongoDbWriteException mongoWrite => StatusCode(500, new 
            { 
                message = "Database write error",
                details = mongoWrite.Message 
            }),
            
            // Standard exceptions
            ArgumentException argument => BadRequest(new { message = argument.Message }),
            
            KeyNotFoundException keyNotFound => NotFound(new { message = keyNotFound.Message }),
            
            InvalidOperationException invalidOp => Conflict(new { message = invalidOp.Message }),
            
            UnauthorizedAccessException => StatusCode(403, new { message = "Access forbidden" }),
            
            // Default
            _ => StatusCode(500, new { message = "An unexpected error occurred.", details = ex.Message })
        };
    }

    protected string GenerateETag(DateTime timestamp)
    {
        return $"\"{timestamp.Ticks}\"";
    }

    protected void AddETagHeader(string etag)
    {
        Response.Headers.Add("ETag", etag);
    }

    protected string? GetIfMatchHeader()
    {
        return Request.Headers["If-Match"].FirstOrDefault();
    }

    protected bool ValidateETag(string? requestETag, string currentETag)
    {
        if (string.IsNullOrEmpty(requestETag))
            return true;

        return requestETag.Trim('"') == currentETag.Trim('"');
    }

    protected IActionResult HandleConcurrencyConflict(ConcurrencyException ex)
    {
        return StatusCode(412, new 
        { 
            message = "Concurrency conflict detected",
            entityId = ex.EntityId,
            expectedVersion = ex.ExpectedVersion,
            actualVersion = ex.ActualVersion,
            hint = "Please refresh the entity and try again"
        });
    }
}