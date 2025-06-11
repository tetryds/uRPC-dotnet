namespace uRPC.Core.Messages;

public interface IMessage<T> where T : IMessage<T>
{
    static abstract uint Type { get; }

    static abstract T Deserialize(Memory<byte>[] bytes);

    Memory<byte>[] Serialize();
}
