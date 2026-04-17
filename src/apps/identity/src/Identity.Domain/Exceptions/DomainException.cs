namespace Identity.Domain.Exceptions;

public abstract class DomainException : Exception
{
    public string ErrorCode { get; }
    public int HttpStatusCode { get; }

    protected DomainException(string message, string errorCode, int httpStatusCode = 400)
        : base(message)
    {
        ErrorCode = errorCode;
        HttpStatusCode = httpStatusCode;
    }
}
