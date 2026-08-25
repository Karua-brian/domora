namespace Domora.Domain.Common.Exceptions;

public sealed class ResourceConflictException : Exception
{
    public ResourceConflictException(string message)
        : base(message)
    {
    }

    public ResourceConflictException(
        string message,
        Exception innerException
    ) : base(message, innerException)
    {
    }
}