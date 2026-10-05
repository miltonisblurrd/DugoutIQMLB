namespace DugoutIQ.Application.Exceptions;

public sealed class ExternalProviderException : Exception
{
    public ExternalProviderException(string provider, string operation, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Provider = provider;
        Operation = operation;
    }

    public string Provider { get; }

    public string Operation { get; }
}
