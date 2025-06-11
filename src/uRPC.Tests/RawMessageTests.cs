using uRPC.Core.Messages;

namespace uRPC.Tests
{
    public class RawMessageTests
    {

        [Fact]
        public async Task SerializesAndDeserializesRawMessageHeaderAsync()
        {
            var ct = TestContext.Current.CancellationToken;

            var originalMsg = new RawMessage
            {
                Id = (uint)Random.Shared.Next(),
                Payload = [],
                Status = (uint)Random.Shared.Next(),
                Type = (uint)Random.Shared.Next()
            };

            using var stream = new MemoryStream();

            await originalMsg.WriteBytesAsync(stream, ct);
            stream.Seek(0, SeekOrigin.Begin);

            var parsedMsg = await RawMessage.ReadNewMessageAsync(stream, ct);

            Assert.Equal(originalMsg, parsedMsg);
        }

        [Fact]
        public async Task SerializesAndDeserializesRawMessageHeaderEmptyBlockAsync()
        {
            var ct = TestContext.Current.CancellationToken;

            var originalMsg = new RawMessage
            {
                Id = (uint)Random.Shared.Next(),
                Payload = [Array.Empty<byte>()],
                Status = (uint)Random.Shared.Next(),
                Type = (uint)Random.Shared.Next()
            };

            using var stream = new MemoryStream();

            await originalMsg.WriteBytesAsync(stream, ct);
            stream.Seek(0, SeekOrigin.Begin);

            var parsedMsg = await RawMessage.ReadNewMessageAsync(stream, ct);

            Assert.Equal(originalMsg, parsedMsg);
        }

        [Fact]
        public async Task SerializesAndDeserializesRawMessageSinglePayloadItemAsync()
        {
            const int BlockSize = 55;

            var ct = TestContext.Current.CancellationToken;

            byte[] randomData = new byte[BlockSize];
            for (int i = 0; i < randomData.Length; i++)
            {
                randomData[i] = (byte)Random.Shared.Next();
            }

            var originalMsg = new RawMessage
            {
                Id = (uint)Random.Shared.Next(),
                Payload = [randomData],
                Status = (uint)Random.Shared.Next(),
                Type = (uint)Random.Shared.Next()
            };

            using var stream = new MemoryStream();

            await originalMsg.WriteBytesAsync(stream, ct);
            stream.Seek(0, SeekOrigin.Begin);

            var parsedMsg = await RawMessage.ReadNewMessageAsync(stream, ct);

            Assert.Equal(originalMsg, parsedMsg);
        }

        [Fact]
        public async Task SerializesAndDeserializesRawMessageMultiplePayloadItemsAsync()
        {
            const int BlockCount = 1 << 8;
            const int MaxPayloadSize = 1 << 12;

            var ct = TestContext.Current.CancellationToken;

            Memory<byte>[] payload = GeneratePayload(BlockCount, MaxPayloadSize);

            var originalMsg = new RawMessage
            {
                Id = (uint)Random.Shared.Next(),
                Payload = payload,
                Status = (uint)Random.Shared.Next(),
                Type = (uint)Random.Shared.Next()
            };

            using var stream = new MemoryStream();

            await originalMsg.WriteBytesAsync(stream, ct);
            stream.Seek(0, SeekOrigin.Begin);

            var parsedMsg = await RawMessage.ReadNewMessageAsync(stream, ct);

            Assert.Equal(originalMsg, parsedMsg);
        }

        [Fact]
        public async Task SerializesAndDeserializesRawMessageHighlyFragmentedAsync()
        {
            const int BlockCount = 1 << 16;
            const int MaxPayloadSize = 1 << 5;

            var ct = TestContext.Current.CancellationToken;

            Memory<byte>[] payload = GeneratePayload(BlockCount, MaxPayloadSize);

            var originalMsg = new RawMessage
            {
                Id = (uint)Random.Shared.Next(),
                Payload = payload,
                Status = (uint)Random.Shared.Next(),
                Type = (uint)Random.Shared.Next()
            };

            using var stream = new MemoryStream();

            await originalMsg.WriteBytesAsync(stream, ct);
            stream.Seek(0, SeekOrigin.Begin);

            var parsedMsg = await RawMessage.ReadNewMessageAsync(stream, ct);

            Assert.Equal(originalMsg, parsedMsg);
        }

        [Fact]
        public async Task SerializesAndDeserializesRawMessageIncludingEmptyBlocksAsync()
        {
            const int BlockCount = 20;

            var ct = TestContext.Current.CancellationToken;

            Memory<byte>[] payload = new Memory<byte>[BlockCount];

            for (int i = 0; i < payload.Length; i++)
            {
                int blockSize = i % 3;
                var block = new byte[blockSize];
                for (int j = 0; j < block.Length; j++)
                {
                    block[j] = (byte)Random.Shared.Next();
                }
                payload[i] = block;
            }

            var originalMsg = new RawMessage
            {
                Id = (uint)Random.Shared.Next(),
                Payload = payload,
                Status = (uint)Random.Shared.Next(),
                Type = (uint)Random.Shared.Next()
            };

            using var stream = new MemoryStream();

            await originalMsg.WriteBytesAsync(stream, ct);
            stream.Seek(0, SeekOrigin.Begin);

            var parsedMsg = await RawMessage.ReadNewMessageAsync(stream, ct);

            Assert.Equal(originalMsg, parsedMsg);
        }


        private static Memory<byte>[] GeneratePayload(int BlockCount, int MaxPayloadSize)
        {
            Memory<byte>[] payload = new Memory<byte>[BlockCount];

            for (int i = 0; i < payload.Length; i++)
            {
                var block = new byte[Random.Shared.Next(MaxPayloadSize)];
                for (int j = 0; j < block.Length; j++)
                {
                    block[j] = (byte)Random.Shared.Next();
                }
                payload[i] = block;
            }

            return payload;
        }
    }
}