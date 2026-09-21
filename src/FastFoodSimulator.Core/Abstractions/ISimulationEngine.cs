using FastFoodSimulator.Core.Configuration;

namespace FastFoodSimulator.Core.Abstractions;

public interface ISimulationEngine : IDisposable
{
    bool IsRunning { get; }

    /// <summary>Raised when an actor fails with an unexpected exception (the run is cancelled).</summary>
    event EventHandler<Exception>? Faulted;

    void Start(SimulationSettings settings);

    Task StopAsync();
}
