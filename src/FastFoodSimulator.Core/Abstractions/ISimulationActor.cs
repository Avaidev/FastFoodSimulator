namespace FastFoodSimulator.Core.Abstractions;

/// <summary>A long-running simulated process (customer source, order taker, cook, server ...).</summary>
public interface ISimulationActor
{
    /// <summary>Runs until <paramref name="cancellationToken"/> is cancelled. Cancellation is not an error.</summary>
    Task RunAsync(CancellationToken cancellationToken);
}
