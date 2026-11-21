namespace ContentDomain.Exception;

/// <summary>
/// The caller is authenticated but does not own the resource they asked to change.
/// Surfaced as 403, which is the one thing the existing 404/409/400 mapping cannot
/// express: telling a stranger that a post exists is a different answer from telling
/// them it does not.
/// </summary>
public class ForbiddenException : System.Exception
{
    public ForbiddenException() { }

    public ForbiddenException(string message)
        : base(message) { }

    public ForbiddenException(string message, System.Exception innerException)
        : base(message, innerException) { }
}
