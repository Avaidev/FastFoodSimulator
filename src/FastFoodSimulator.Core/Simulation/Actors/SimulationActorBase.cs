using FastFoodSimulator.Core.Abstractions;

namespace FastFoodSimulator.Core.Simulation.Actors;

/// <summary>Common behaviour: cancellation is the normal way for an actor to finish.</summary>
public abstract class SimulationActorBase : ISimulationActor
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            await ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Stop requested – not an error.
        }
    }

    protected abstract Task ExecuteAsync(CancellationToken cancellationToken);
}
