using System.Diagnostics.CodeAnalysis;

namespace uRPC.Core.Messages;

public readonly struct EchoMessage(int replyCount, Memory<byte> data) : IMessage<EchoMessage>
{
    public static uint Type => 0;

    public readonly Memory<byte> Data = data;

    public readonly int ReplyCount = replyCount;

    public static EchoMessage Deserialize(Memory<byte>[] bytes)
    {
        return new EchoMessage(BitConverter.ToInt32(bytes[0].Span), bytes[1]);
    }

    public Memory<byte>[] Serialize()
    {
        return [BitConverter.GetBytes(ReplyCount), Data];
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is EchoMessage other &&
               ReplyCount == other.ReplyCount &&
               CompareData(other);
    }

    private readonly bool CompareData(EchoMessage other)
    {
        if (Data.Length != other.Data.Length) return false;

        var dataSpan = Data.Span;
        var otherSpan = other.Data.Span;

        for (int j = 0; j < Data.Length; j++)
        {
            if (dataSpan[j] != otherSpan[j]) return false;
        }

        return true;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Data);
    }

    public static EchoMessage GetRandom(int replyCount, int dataLength)
    {
        byte[] data = new byte[dataLength];

        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (byte)Random.Shared.Next();
        }

        return new EchoMessage(replyCount, data);
    }

    public static bool operator ==(EchoMessage left, EchoMessage right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(EchoMessage left, EchoMessage right)
    {
        return !(left == right);
    }
}
