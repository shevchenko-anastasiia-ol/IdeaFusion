namespace Collaboration.Domain.Exceptions;

/// <summary>
/// The caller is authenticated but not allowed to do this. Kept distinct from
/// <see cref="DomainException"/>, which surfaces as 400, because "you are not
/// permitted" is a different answer from "your request was malformed" and clients
/// need to be able to tell them apart.
/// </summary>
public sealed class ForbiddenException : Exception
{
    public ForbiddenException() { }

    public ForbiddenException(string message)
        : base(message) { }

    public ForbiddenException(string message, Exception innerException)
        : base(message, innerException) { }
}
