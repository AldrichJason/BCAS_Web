namespace BCAS.Application.Common;

/// <summary>Base class for the errors the API translates into a problem response.</summary>
public abstract class AppException : Exception
{
    protected AppException(string message) : base(message)
    {
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string resource, object key)
        : base($"{resource} '{key}' was not found.")
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }
}

public class ConflictException : AppException
{
    public ConflictException(string message) : base(message)
    {
    }
}

public class ValidationFailedException : AppException
{
    public ValidationFailedException(string message) : base(message)
    {
    }
}

public class AuthenticationFailedException : AppException
{
    public AuthenticationFailedException(string message) : base(message)
    {
    }
}
