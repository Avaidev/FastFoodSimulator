using FastFoodSimulator.Models;

namespace FastFoodSimulator.Services;

public interface ISimulationEngine : IDisposable
{
    event Action<SimulationSnapshot>? SnapshotPublished;

    event Action<LogEntry>? LogWritten;

    bool IsRunning { get; }

    void Start(SimulationSettings settings);

    Task StopAsync();
}
