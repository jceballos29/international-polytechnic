namespace Identity.Domain.Exceptions;

public class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException()
        : base("Invalid credentials provided.", "INVALID_CREDENTIALS", 401) { }

    public InvalidCredentialsException(string message)
        : base(message, "INVALID_CREDENTIALS", 401) { }

    public static InvalidCredentialsException AccountLocked(TimeSpan lockoutDuration)
    {
        var minutes = (int)Math.Ceiling(lockoutDuration.TotalMinutes);
        return new InvalidCredentialsException(
            $"Account is locked due to multiple failed login attempts. Try again in {minutes} minute(s)."
        );
    }
}
