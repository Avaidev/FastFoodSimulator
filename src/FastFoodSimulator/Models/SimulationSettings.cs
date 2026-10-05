namespace FastFoodSimulator.Models;

public sealed record SimulationSettings(
    int CustomerArrivalIntervalMs,
    int OrderTakingIntervalMs,
    int OrderFulfillmentIntervalMs,
    int OrderTakerCount,
    int CookCount)
{
    public int PickupDurationMs { get; init; } = 600;
}
