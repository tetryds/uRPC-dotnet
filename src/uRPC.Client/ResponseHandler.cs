using uRPC.Core.Messages;

namespace uRPC.Client;

public class ResponseHandler<TRequest, TResponse>(TRequest sent, Action<TResponse>? onReceive) where TResponse : IMessage<TResponse>
{
    private readonly List<TResponse> received = [];

    public TRequest Sent => sent;
    public IReadOnlyList<TResponse> Received => received;

    private readonly TaskCompletionSource<TResponse> complete = new();

    public Task<TResponse> Last => complete.Task;

    public Task Wait => complete.Task;

    public bool IsFinished => complete.Task.IsCompleted;

    internal void AddResponse(RawMessage message)
    {
        if (message.Type != TResponse.Type)
            complete.SetException(new ResponseException("Received incorrect message type"));

        if (Wait.IsCompleted) return;

        try
        {
            var msg = TResponse.Deserialize(message.Payload);
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
