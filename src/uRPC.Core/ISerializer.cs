namespace uRPC.Core;

public interface ISerializer
{
    int Serialize<T>(T obj, byte[] buffer);

    int Serialize(object obj, byte[] buffer);

    T Deserialize<T>(byte buffer, int length);

    object Deserialize(byte buffer, int length, Type type);
}
