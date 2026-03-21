namespace ContentDomain.Exception;

public class BusinessConflictException : System.Exception
{
    public BusinessConflictException()
    {
    }

    public BusinessConflictException(string message)
        : base(message)
    {
    }

    public BusinessConflictException(string message, System.Exception innerException)
        : base(message, innerException)
    {
    }
}