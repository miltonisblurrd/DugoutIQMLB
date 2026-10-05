namespace DugoutIQ.Application.Exceptions;

public sealed class RequestValidationException : Exception
{
    public RequestValidationException(string field, string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>
        {
            [field] = [message]
        };
    }

    public IDictionary<string, string[]> Errors { get; }
}
