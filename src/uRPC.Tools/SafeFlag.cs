using System.Runtime.CompilerServices;

namespace uRPC.Tools;

public struct SafeFlag(bool canReset = false)
{
    int state = FlagState.Clear;

    public readonly bool IsSet => state == FlagState.Set;

    public readonly bool CanReset => canReset;
    /// <summary>
    /// Checks if the object has been set as a disposed flag.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj">Parent object, usually <b>this</b></param>
    /// <exception cref="ObjectDisposedException">This flag has already been disposed</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void CheckDisposedOnSet(object obj)
    {
        if (IsSet)
            throw new ObjectDisposedException(obj?.GetType().Name ?? "unknown");
    }

    /// <summary>
    /// Sets the flag object. Thread safe.
    /// </summary>
    /// <returns>True if set on this call</returns>
    public bool Set()
    {
        return Interlocked.Exchange(ref state, FlagState.Set) == FlagState.Clear;
    }

    /// <summary>
    /// Resets the flag object. Thread safe.
    /// </summary>
    /// <returns>True if reset on this call</returns>
    public bool Reset()
    {
        if (!canReset)
            throw new InvalidOperationException("This flag is not resettable");

        return Interlocked.Exchange(ref state, FlagState.Clear) == FlagState.Set;
    }

    private static class FlagState
    {
        public const int Clear = 0;
        public const int Set = 1;
    }
}
