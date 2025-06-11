//using System.Collections.Concurrent;
//using System.Net.Sockets;
//using uRPC.Core.Messages;

//namespace uRPC.Core;

//public class Gateway
//{
//    readonly ConcurrentDictionary<ClientId, ClientConnection> connMap = [];

//    CancellationTokenSource tokenSource;

//    public ClientId AddClient(TcpClient client)
//    {
//        ClientId id;

//        while (connMap.ContainsKey(id = ClientId.Generate())) { }

//        var conn = new ClientConnection(id, client, JoinedCTS(tokenSource.Token));
//        connMap.TryAdd(id, conn);

//        ListenToClient(conn);

//        return id;
//    }

//    public bool DropClient(ClientId id)
//    {
//        if (!connMap.TryRemove(id, out var conn)) return false;

//        conn.TokenSource.Cancel();

//        // stop listener
//        // remove from dict
//        // remove from map
//    }

//    public async Task<bool> SendAsync(ClientId id, RawMessage message, CancellationToken ct)
//    {
//        if (!connMap.TryGetValue(id, out var conn)) return false;

//        using var cts = JoinedCTS(ct);
//        await message.WriteBytesAsync(conn.stream, cts.Token);

//        return true;
//    }

//    private static async void ListenToClient(ClientConnection conn)
//    {
//        NetworkStream stream = conn.stream;
//        CancellationToken token = conn.Token;

//        while (!token.IsCancellationRequested)
//        {
//            RawMessage message = new();
//            await message.ReadBytesAsync(stream, token);
//        }
//    }

//    private static CancellationTokenSource JoinedCTS(CancellationToken token)
//    {
//        return CancellationTokenSource.CreateLinkedTokenSource(token);
//    }


//}
