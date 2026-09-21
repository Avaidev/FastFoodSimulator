using FastFoodSimulator.Core.Abstractions;

namespace FastFoodSimulator.Core.Simulation;

/// <summary>Runs all actors of one restaurant concurrently. If one of them fails, the others are stopped.</summary>
public sealed class SimulationSession(IReadOnlyList<ISimulationActor> actors) : ISimulationSession
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var running = actors.Select(actor => RunActorAsync(actor, linked)).ToArray();
        await Task.WhenAll(running).ConfigureAwait(false);
    }

    private static async Task RunActorAsync(ISimulationActor actor, CancellationTokenSource linked)
    {
        try
        {
            await actor.RunAsync(linked.Token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            linked.Cancel();
            throw;
        }
    }
}
