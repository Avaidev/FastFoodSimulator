using FastFoodSimulator.Models;

namespace FastFoodSimulator.Services;

public interface ISimulationEngine : IDisposable
{
    event Action<SimulationSnapshot>? SnapshotPublished;

    void Start(SimulationSettings settings);

    Task StopAsync();
}
