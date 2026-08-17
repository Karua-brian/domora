namespace Domora.Application.Common.Exceptions;

public sealed class ConcurrencyException : Exception

{
    public ConcurrencyException(
        string message,
        Exception? inner
    ) : 
    base(message, inner)
        {   
        }
}