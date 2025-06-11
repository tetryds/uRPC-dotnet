using uRPC.Core.Messages;

namespace uRPC.Client;

public class ResponseHandler<T>(Action<T>? onReceive) where T : IMessage<T>
{
    private readonly List<T> received = [];

    public IReadOnlyList<T> Received => received;

    private readonly TaskCompletionSource<T> complete = new();

    public Task<T> Last => complete.Task;

    public Task Wait => complete.Task;

    public bool IsFinished => complete.Task.IsCompleted;

    internal void AddResponse(RawMessage message)
    {
        if (message.Type != T.Type)
            complete.SetException(new ResponseException("Received incorrect message type"));

        if (Wait.IsCompleted) return;

        try
        {
            var msg = T.Deserialize(message.Payload);
            received.Add(msg);
            onReceive?.Invoke(msg);

            if (MessageStatus.IsError(message.Status))
                complete.SetException(new ResponseException(msg));
            else if (message.Status != MessageStatus.Continue)
                complete.SetResult(msg);
        }
        catch (Exception e)
        {
            complete.SetException(e);
        }
    }
}
