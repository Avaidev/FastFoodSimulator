namespace FastFoodSimulator.Models;

public sealed record SimulationSnapshot(
    long Version,
    bool IsRunning,
    IReadOnlyList<int> OrderLine,
    IReadOnlyList<int> OrderTakers,
    IReadOnlyList<int> Carousel,
    IReadOnlyList<int> Cooks,
    IReadOnlyList<int> ServingLine,
    IReadOnlyList<int> PickupCounter);
