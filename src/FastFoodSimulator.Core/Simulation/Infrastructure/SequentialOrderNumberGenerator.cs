using FastFoodSimulator.Core.Abstractions;

namespace FastFoodSimulator.Core.Simulation.Infrastructure;

public sealed class SequentialOrderNumberGenerator : IOrderNumberGenerator
{
    private int _last;

    public int Next() => Interlocked.Increment(ref _last);
}
