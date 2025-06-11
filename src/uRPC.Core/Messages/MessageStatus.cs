namespace uRPC.Core.Messages;

public static class MessageStatus
{
    public const uint Close = 0;
    public const uint Continue = 1;

    public const uint Error = 100;

    public static bool IsError(uint status) => status > Error;

}
