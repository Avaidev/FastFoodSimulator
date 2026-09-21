using FastFoodSimulator.Core.Configuration;

namespace FastFoodSimulator.Core.Abstractions;

/// <summary>One wired-up run of the restaurant (queues + actors).</summary>
public interface ISimulationSession
{
    Task RunAsync(CancellationToken cancellationToken);
}

public interface ISimulationSessionFactory
{
    ISimulationSession Create(SimulationSettings settings);
}
