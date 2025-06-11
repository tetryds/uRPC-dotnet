namespace uRPC.Server.Exceptions;

[Serializable]
internal class AppInitializationException : Exception
{
    public AppInitializationException()
    {
    }

    public AppInitializationException(string? message) : base(message)
    {
    }

    public AppInitializationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}