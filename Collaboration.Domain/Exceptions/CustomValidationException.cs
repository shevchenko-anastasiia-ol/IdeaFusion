using FluentValidation;
using FluentValidation.Results;

namespace Collaboration.Domain.Exceptions;

public class CustomValidationException : ValidationException
{
    public CustomValidationException(IEnumerable<ValidationFailure> failures)
        : base(failures) { }
}