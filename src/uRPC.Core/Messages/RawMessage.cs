

namespace uRPC.Core.Messages;

public struct RawMessage
{
    public required uint Id;
    public required uint Type;
    public required uint Status;
    public required Memory<byte>[] Payload;

    public const int HeaderSize = sizeof(uint) + sizeof(uint) + sizeof(uint);

    public readonly int PayloadSize => Payload.Aggregate(0, static (s, p) => s + p.Length);

    public readonly int FullSize => Payload.Aggregate(HeaderSize, static (s, p) => s + p.Length);

    public readonly async Task WriteBytesAsync(Stream stream, CancellationToken cancellationToken)
    {

        await stream.WriteAsync(BitConverter.GetBytes(Id), cancellationToken);
        await stream.WriteAsync(BitConverter.GetBytes(Type), cancellationToken);
        await stream.WriteAsync(BitConverter.GetBytes(Status), cancellationToken);

        // Write number of data blocks
        await stream.WriteAsync(BitConverter.GetBytes((uint)Payload.Length), cancellationToken);

        for (int i = 0; i < Payload.Length; i++)
        {
            var block = Payload[i];
            await stream.WriteAsync(BitConverter.GetBytes((uint)block.Length), cancellationToken);
            await stream.WriteAsync(block, cancellationToken);
        }
    }

    public static async Task<RawMessage> ReadNewMessageAsync(Stream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[sizeof(uint)];
        var uintMem = buffer.AsMemory();

        await stream.ReadExactlyAsync(uintMem, cancellationToken);
        var id = BitConverter.ToUInt32(uintMem.Span);

        await stream.ReadExactlyAsync(uintMem, cancellationToken);
        var type = BitConverter.ToUInt32(uintMem.Span);

        await stream.ReadExactlyAsync(uintMem, cancellationToken);
        var status = BitConverter.ToUInt32(uintMem.Span);

        await stream.ReadExactlyAsync(uintMem, cancellationToken);
        uint blockCount = BitConverter.ToUInt32(uintMem.Span);

        var payload = new Memory<byte>[blockCount];

        for (int i = 0; i < blockCount; i++)
        {
            await stream.ReadExactlyAsync(uintMem, cancellationToken);
            uint blockSize = BitConverter.ToUInt32(uintMem.Span);

            // TODO: get from pool
            byte[] block = new byte[blockSize];
            await stream.ReadExactlyAsync(block, cancellationToken);
            payload[i] = block;
        }

        return new RawMessage
        {
            Id = id,
            Type = type,
            Status = status,
            Payload = payload
        };
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is RawMessage message &&
               Id == message.Id &&
               Type == message.Type &&
               Status == message.Status &&
               ComparePayload(message);
    }

    private readonly bool ComparePayload(RawMessage message)
    {
        int blockCount = Payload.Length;
        if (blockCount != message.Payload.Length) return false;

        for (int i = 0; i < blockCount; i++)
        {
            var block = Payload[i].Span;
            var otherBlock = message.Payload[i].Span;

            if (block.Length != otherBlock.Length) return false;

            for (int j = 0; j < block.Length; j++)
            {
                if (block[j] != otherBlock[j]) return false;
            }
        }

        return true;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Id, Type, Status, Payload);
    }
    public static bool operator ==(RawMessage left, RawMessage right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(RawMessage left, RawMessage right)
    {
        return !(left == right);
    }
}
