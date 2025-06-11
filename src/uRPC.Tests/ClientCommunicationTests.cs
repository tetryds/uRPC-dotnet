using System.Net.Sockets;
using uRPC.Client;
using uRPC.Core.Messages;
using uRPC.Server;

namespace uRPC.Tests
{
    public class ClientCommunicationTests
    {
        private static int Port = 29182;

        [Fact]
        public async Task AssertCanCommunicateWithServerAsync()
        {
            int port = Interlocked.Increment(ref Port);
            var ct = TestContext.Current.CancellationToken;

            var appRunner = new App().ListenTo(port).Start(ct);

            var client = new AppClient("localhost", port);

            client.Start(ct);

            byte[] data = new byte[6];
            RandomizeBytes(data);
            var sent = new EchoMessage(1, data);
            var handler = await client.SendAsync<EchoMessage, EchoMessage>(sent, cancellationToken: ct);

            var received = await handler.Last;

            Assert.Equal(sent, received);
        }

        [Fact]
        public async Task AssertCanCommunicateWithServerMultipleRequestsAsync()
        {
            const int MessageCount = 100;

            int port = Interlocked.Increment(ref Port);

            var ct = TestContext.Current.CancellationToken;

            var appRunner = new App().ListenTo(port).Start(ct);

            var client = new AppClient("localhost", port);

            client.Start(ct);

            byte[] data = new byte[32];
            var sent = new EchoMessage(1, data);
            for (int i = 0; i < MessageCount; i++)
            {
                RandomizeBytes(data);
                var handler = await client.SendAsync<EchoMessage, EchoMessage>(sent, cancellationToken: ct);
                var received = await handler.Last;
                Assert.Equal(sent, received);
            }
        }

        [Fact]
        public async Task AssertCanCommunicateWithServerMultipleResponsesAsync()
        {
            const int ReplyCount = 10;

            int port = Interlocked.Increment(ref Port);
            var ct = TestContext.Current.CancellationToken;

            var appRunner = new App().ListenTo(port).Start(ct);

            var client = new AppClient("localhost", port);

            client.Start(ct);

            byte[] data = new byte[6];
            RandomizeBytes(data);
            var sent = new EchoMessage(ReplyCount, data);
            var handler = await client.SendAsync<EchoMessage, EchoMessage>(sent, cancellationToken: ct);

            await handler.Wait;

            Assert.Equal(ReplyCount, handler.Received.Count);

            for (int i = 0; i < ReplyCount; i++)
            {
                Assert.Equal(i, handler.Received[i].ReplyCount);
            }

        }

        private static void RandomizeBytes(byte[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)Random.Shared.Next();
            }
        }
    }
}