namespace Formarca.Domain.Common;

public interface IClock
{
    DateTime UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

/// <summary>
/// Ambient clock so domain entities can stamp timestamps without taking a dependency on Application.
/// Tests swap it with <see cref="Use"/>.
/// </summary>
public static class Clock
{
    private static readonly AsyncLocal<IClock?> Override = new();
    private static readonly IClock Default = new SystemClock();

    public static IClock Current => Override.Value ?? Default;

    public static IDisposable Use(IClock clock)
    {
        var previous = Override.Value;
        Override.Value = clock;
        return new Reset(previous);
    }

    private sealed class Reset(IClock? previous) : IDisposable
    {
        public void Dispose() => Override.Value = previous;
    }
}
