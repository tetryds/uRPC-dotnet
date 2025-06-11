
namespace uRPC.Server.Listening;

[Serializable]
public class ListenException : Exception
{
    public readonly int Port;

    public ListenException(int port, string? message, Exception? innerException) : base(message, innerException)
    {
        Port = port;
    }

    public ListenException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}