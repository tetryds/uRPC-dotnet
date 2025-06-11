
namespace uRPC.Client
{
    [Serializable]
    internal class ResponseException : Exception
    {
        public readonly object ErrorMsg;

        public ResponseException(object msg)
        {
            ErrorMsg = msg;
        }

        public ResponseException(string? message) : base(message)
        {
            ErrorMsg = message ?? new object();
        }

        public ResponseException(string? message, Exception? innerException) : base(message, innerException)
        {
            ErrorMsg = new object();
        }
    }
}