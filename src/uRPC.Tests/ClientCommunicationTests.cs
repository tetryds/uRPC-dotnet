using System.Collections;
using System.Net.Sockets;
using uRPC.Client;
using uRPC.Core.Messages;
using uRPC.Server;
using Xunit.Internal;

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

            var sent = EchoMessage.GetRandom(1, 6);
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

            for (int i = 0; i < MessageCount; i++)
            {
                var sent = EchoMessage.GetRandom(1, 32);

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

            var sent = EchoMessage.GetRandom(ReplyCount, 6);
            var handler = await client.SendAsync<EchoMessage, EchoMessage>(sent, cancellationToken: ct);

            await handler.Wait;

            Assert.Equal(ReplyCount, handler.Received.Count);

            for (int i = 0; i < ReplyCount; i++)
            {
                Assert.Equal(sent, handler.Received[i]);
            }
        }

        [Fact]
        public async Task AssertCanCommunicateWithServerMultipleClientsAsync()
        {
            const int ClientCount = 10;
            const int RequestCount = 200;
            const int ReplyPerRequestCount = 10;
            const int DataSize = 8327;

            int port = Interlocked.Increment(ref Port);
            var ct = TestContext.Current.CancellationToken;

            var appRunner = new App().ListenTo(port).Start(ct);

            AppClient[] clients = [.. Enumerable.Range(0, ClientCount).Select(_ => new AppClient("localhost", port).Start(ct))];

            static EchoMessage MakeMsg() => EchoMessage.GetRandom(ReplyPerRequestCount, DataSize);

            var requests = clients.Select(c => Enumerable.Range(0, RequestCount).Select(_ => c.SendAsync<EchoMessage, EchoMessage>(MakeMsg(), cancellationToken: ct)).ToArray());

            foreach (var handlerTasks in requests)
            {
                var handlers = await Task.WhenAll(handlerTasks);

                foreach (var handler in handlers)
                {
                    await handler.Wait;
                    foreach (var response in handler.Received)
                    {
                        Assert.Equal(handler.Sent, response);
                    }
                }
            }
        }

        [Fact]
        public async Task AssertCanCommunicateWithServerLotsOfRequestsAsync()
        {
            const int ClientCount = 10;
            const int RequestCount = 2000;
            const int ReplyPerRequestCount = 10;
            const int DataSize = 8327;

            int port = Interlocked.Increment(ref Port);
            var ct = TestContext.Current.CancellationToken;

            var appRunner = new App().ListenTo(port).Start(ct);

            AppClient[] clients = [.. Enumerable.Range(0, ClientCount).Select(_ => new AppClient("localhost", port))];

            foreach (var client in clients)
            {
                client.Start(ct);
            }

            static EchoMessage MakeMsg() => EchoMessage.GetRandom(ReplyPerRequestCount, DataSize);

            var requests = clients.Select(c => Enumerable.Range(0, RequestCount).Select(_ => c.SendAsync<EchoMessage, EchoMessage>(MakeMsg(), cancellationToken: ct)).ToArray());

            foreach (var handlerTasks in requests)
            {
                var handlers = await Task.WhenAll(handlerTasks);

                foreach (var handler in handlers)
                {
                    await handler.Wait;
                    foreach (var response in handler.Received)
                    {
                        Assert.Equal(handler.Sent, response);
                    }
                }
            }
        }
    }
}